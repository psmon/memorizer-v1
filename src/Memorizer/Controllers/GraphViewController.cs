using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

public class GraphViewController : Controller
{
    private readonly ILogger<GraphViewController> _logger;
    
    public GraphViewController(ILogger<GraphViewController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("/ui/graph")]
    public IActionResult Index()
    {
        return View("~/Views/Graph/Index.cshtml");
    }
}