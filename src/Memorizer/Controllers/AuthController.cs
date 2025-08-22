using Memorizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

public class AuthController : Controller
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("/auth/login")]
    public IActionResult Login()
    {
        // If already authenticated, redirect to main page
        if (HttpContext.Session.GetString("IsAuthenticated") == "true")
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost("/login")]
    public IActionResult Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Username and password are required";
            return View();
        }

        if (_authService.ValidateCredentials(username, password))
        {
            _logger.LogInformation("User {Username} logged in successfully", username);
            
            HttpContext.Session.SetString("IsAuthenticated", "true");
            HttpContext.Session.SetString("Username", username);
            
            // Redirect to the main UI page after successful login
            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("Failed login attempt for user {Username}", username);
        ViewBag.Error = "Invalid username or password";
        return View();
    }

    [HttpGet("/logout")]
    [HttpPost("/logout")]
    public IActionResult Logout()
    {
        var username = HttpContext.Session.GetString("Username");
        _logger.LogInformation("User {Username} logged out", username);
        
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}