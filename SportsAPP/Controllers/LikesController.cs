using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    [Authorize]
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LikesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // POST: Likes/Like/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int postId, string? returnUrl)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Check if user already liked this post
            var existingLike = await db.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);

            if (existingLike != null)
            {
                TempData["message"] = "Ai dat deja like la această postare!";
                TempData["messageType"] = "alert-info";
                return Redirect(returnUrl ?? Url.Action("Index", "Posts")!);
            }

            var like = new Like
            {
                PostId = postId,
                UserId = currentUserId!,
                LikedAt = DateTime.Now
            };

            db.Likes.Add(like);
            await db.SaveChangesAsync();

            TempData["message"] = "Like ad\u0103ugat!";
            TempData["messageType"] = "alert-success";

            return Redirect(returnUrl ?? Url.Action("Index", "Posts")!);
        }

        // POST: Likes/Unlike/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(int postId, string? returnUrl)
        {
            var currentUserId = _userManager.GetUserId(User);

            var like = await db.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);

            if (like == null)
            {
                TempData["message"] = "Nu ai dat like la această postare!";
                TempData["messageType"] = "alert-warning";
                return Redirect(returnUrl ?? Url.Action("Index", "Posts")!);
            }

            db.Likes.Remove(like);
            await db.SaveChangesAsync();

            TempData["message"] = "Like eliminat!";
            TempData["messageType"] = "alert-success";

            return Redirect(returnUrl ?? Url.Action("Index", "Posts")!);
        }
    }
}
