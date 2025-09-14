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
}