using System.Diagnostics;
using System.Runtime.InteropServices;
using Memorizer.Settings;
using Microsoft.Playwright;

namespace Memorizer.Services;

/// <summary>
/// Ensures Playwright Chromium browser is installed at application startup for headless mode.
/// </summary>
public sealed class WebSearchBrowserBootstrapService : BackgroundService
{
    private readonly WebSearchSettings _settings;
    private readonly ILogger<WebSearchBrowserBootstrapService> _logger;

    public WebSearchBrowserBootstrapService(
        WebSearchSettings settings,
        ILogger<WebSearchBrowserBootstrapService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Headless.AutoInstallBrowserOnStartup)
        {
            return;
        }

        _logger.LogInformation("WebSearch headless bootstrap started on OS: {OS}, Arch: {Arch}",
            RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture);

        if (!ApplyBrowserPathEnvironment(_settings.Headless.BrowserInstallPath, _logger))
        {
            _logger.LogWarning("WebSearch headless browser install path is invalid: {Path}", _settings.Headless.BrowserInstallPath);
            return;
        }

        try
        {
            var alreadyInstalled = await IsChromiumInstalledAsync(stoppingToken);
            if (alreadyInstalled)
            {
                _logger.LogInformation("Playwright Chromium already installed.");
                await ValidateLaunchPrerequisitesAsync(stoppingToken);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                _settings.Headless.AutoInstallLinuxDependenciesOnStartup)
            {
                await TryInstallLinuxDependenciesAsync(stoppingToken);
            }

            _logger.LogInformation("Playwright Chromium not found. Starting auto-install at startup.");
            var installed = await InstallChromiumWithBundledNodeAsync(stoppingToken);

            if (installed)
            {
                _logger.LogInformation("Playwright Chromium auto-install completed.");
                await ValidateLaunchPrerequisitesAsync(stoppingToken);
            }
            else
            {
                _logger.LogWarning("Playwright Chromium auto-install was skipped or failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-install Playwright Chromium at startup.");
        }
    }

    internal static bool ApplyBrowserPathEnvironment(string? browserInstallPath, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(browserInstallPath))
        {
            return true;
        }

        try
        {
            var normalized = Path.GetFullPath(browserInstallPath);
            Directory.CreateDirectory(normalized);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", normalized);
            logger?.LogInformation("Set PLAYWRIGHT_BROWSERS_PATH to {Path}", normalized);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Unable to set PLAYWRIGHT_BROWSERS_PATH.");
            return false;
        }
    }

    private async Task<bool> IsChromiumInstalledAsync(CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        var executablePath = playwright.Chromium.ExecutablePath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return File.Exists(executablePath);
    }

    private async Task<bool> InstallChromiumWithBundledNodeAsync(CancellationToken cancellationToken)
    {
        var baseDir = AppContext.BaseDirectory;
        var nodePath = ResolveBundledNodePath(baseDir);
        var cliPath = Path.Combine(baseDir, ".playwright", "package", "cli.js");

        if (string.IsNullOrWhiteSpace(nodePath) || !File.Exists(nodePath) || !File.Exists(cliPath))
        {
            _logger.LogWarning("Bundled Playwright node/cli not found. node: {Node}, cli: {Cli}", nodePath, cliPath);
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = nodePath,
            Arguments = $"\"{cliPath}\" install chromium",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var configuredPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            psi.Environment["PLAYWRIGHT_BROWSERS_PATH"] = configuredPath;
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            _logger.LogInformation("Playwright install output: {Output}", stdout);
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            _logger.LogWarning("Playwright install warnings: {Error}", stderr);
        }

        return process.ExitCode == 0;
    }

    private async Task TryInstallLinuxDependenciesAsync(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        var baseDir = AppContext.BaseDirectory;
        var nodePath = ResolveBundledNodePath(baseDir);
        var cliPath = Path.Combine(baseDir, ".playwright", "package", "cli.js");
        if (string.IsNullOrWhiteSpace(nodePath) || !File.Exists(nodePath) || !File.Exists(cliPath))
        {
            _logger.LogWarning("Cannot install Linux dependencies automatically. Playwright node/cli not found.");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = nodePath,
            Arguments = $"\"{cliPath}\" install-deps chromium",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            _logger.LogInformation("Linux browser dependencies installation completed.");
            return;
        }

        _logger.LogWarning("Linux dependencies auto-install failed (exit: {ExitCode}). Output: {Stdout} {Stderr}",
            process.ExitCode, stdout, stderr);
        _logger.LogWarning("Ubuntu manual command example: sudo apt-get update && sudo apt-get install -y libnss3 libxkbcommon0 libgtk-3-0 libgbm1");
    }

    private async Task ValidateLaunchPrerequisitesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                ChromiumSandbox = _settings.Headless.ChromiumSandbox
            });
            await browser.CloseAsync();
        }
        catch (PlaywrightException ex)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                ex.Message.Contains("missing dependencies", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Chromium is installed but Linux dependencies are missing.");
                _logger.LogWarning("Ubuntu manual command example: sudo apt-get update && sudo apt-get install -y libnss3 libxkbcommon0 libgtk-3-0 libgbm1");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("Chromium launch validation failed on Windows. Check antivirus policy/sandbox restrictions. Error: {Error}", ex.Message);
                return;
            }

            _logger.LogWarning("Chromium launch validation failed. Error: {Error}", ex.Message);
        }
    }

    private static string? ResolveBundledNodePath(string baseDir)
    {
        var osFolder = GetNodeOsFolder();
        if (string.IsNullOrWhiteSpace(osFolder))
        {
            return null;
        }

        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "node.exe" : "node";
        return Path.Combine(baseDir, ".playwright", "node", osFolder, fileName);
    }

    private static string? GetNodeOsFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win32_x64";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _ => null
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "darwin-x64",
                Architecture.Arm64 => "darwin-arm64",
                _ => null
            };
        }

        return null;
    }
}
