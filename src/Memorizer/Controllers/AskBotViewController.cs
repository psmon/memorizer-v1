using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

[AllowAnonymous]
public class AskBotViewController : Controller
{
    private readonly ILogger<AskBotViewController> _logger;

    public AskBotViewController(ILogger<AskBotViewController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/ui/askbot")]
    public IActionResult Index()
    {
        _logger.LogInformation("AskBot UI page accessed");
        return View("~/Views/AskBot/Index.cshtml");
    }

    [HttpGet("/ui/askbot/share/{shortCode}")]
    public IActionResult Share(string shortCode)
    {
        _logger.LogInformation("Shared AskBot conversation accessed with code: {ShortCode}", shortCode);
        ViewData["ShortCode"] = shortCode;
        return View("~/Views/AskBot/Share.cshtml");
    }
}