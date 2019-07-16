using System;
using Microsoft.AspNetCore.Mvc;
using Sora.Allocation;
using Sora.Database;
using Sora.Database.Models;

namespace Sora.Controllers
{
    [ApiController]
    [Route("/images/medals-client/{achievement}")]
    public class AchievementController : Controller
    {
        private readonly Cache _cache;
        private readonly SoraDbContextFactory _factory;

        public AchievementController(
            SoraDbContextFactory factory,
            Cache cache
        )
        {
            _factory = factory;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Index(string achievement)
        {
            byte[] res;
            if ((res = _cache.Get<byte[]>($"jibril:achievements:{achievement}")) != null)
                return File(res, "image/png");

            res = Achievements
                  .GetAchievement(_factory, achievement.Replace(".png", string.Empty))
                  ?.GetIconImage();

            if (res == null)
                return NotFound();

            _cache.Set("sora:achievements:" + achievement, res, TimeSpan.FromHours(1));
            return File(res, "image/png");
        }
    }
}
