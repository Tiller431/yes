using System.IO;
using Microsoft.AspNetCore.Mvc;
using Sora.Helpers;

namespace Jibril.Controllers
{
    [ApiController]
    [Route("/ss/{screenshot}")]
    public class ScreenshotController : Controller
    {
        [HttpGet]
        public IActionResult Index(string screenshot)
        {
            Logger.Info($"data/screenshots/{screenshot}");
            if (!Directory.Exists("data/screenshots"))
                Directory.CreateDirectory("data/screenshots");

            // No config reading for you :3
            screenshot = screenshot.Replace("..", string.Empty);

            if (!System.IO.File.Exists($"data/screenshots/{screenshot}"))
                return NotFound($"Could not find ScreenShot with the Id of {screenshot}");

            return File(System.IO.File.OpenRead($"data/screenshots/{screenshot}"), "image/jpg");
        }
    }
}
