using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    [Authorize]
    public class FollowsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // POST: Follows/Follow/userId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == userId)
            {
                TempData["message"] = "Nu te poți urmări pe tine însuți!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", "Users", new { id = userId });
            }

            var userToFollow = await db.ApplicationUsers.FindAsync(userId);
            if (userToFollow == null)
            {
                return NotFound();
            }

            var existingFollow = await db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

            if (existingFollow != null)
            {
                TempData["message"] = "Urmărești deja acest utilizator!";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Show", "Users", new { id = userId });
            }

            var existingRequest = await db.FollowRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == currentUserId && 
                                          fr.ReceiverId == userId && 
                                          fr.Status == FollowRequestStatus.Pending);

            if (existingRequest != null)
            {
                TempData["message"] = "Ai trimis deja o cerere de urmărire!";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Show", "Users", new { id = userId });
            }

            if (userToFollow.IsPublic)
            {
                var follow = new Follow
                {
                    FollowerId = currentUserId!,
                    FollowingId = userId,
                    FollowedAt = DateTime.Now
                };

                db.Follows.Add(follow);
                await db.SaveChangesAsync();

                TempData["message"] = $"Acum urmărești pe {userToFollow.FullName}!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                var followRequest = new FollowRequest
                {
                    SenderId = currentUserId!,
                    ReceiverId = userId,
                    RequestedAt = DateTime.Now,
                    Status = FollowRequestStatus.Pending
                };

                db.FollowRequests.Add(followRequest);
                await db.SaveChangesAsync();

                TempData["message"] = $"Cerere de urmărire trimisă către {userToFollow.FullName}!";
                TempData["messageType"] = "alert-success";
            }

            return RedirectToAction("Show", "Users", new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var follow = await db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

            if (follow == null)
            {
                TempData["message"] = "Nu urmărești acest utilizator!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", "Users", new { id = userId });
            }

            db.Follows.Remove(follow);
            await db.SaveChangesAsync();

            var user = await db.ApplicationUsers.FindAsync(userId);
            TempData["message"] = $"Nu mai urmărești pe {user?.FullName}!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", "Users", new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var request = await db.FollowRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == currentUserId && 
                                          fr.ReceiverId == userId && 
                                          fr.Status == FollowRequestStatus.Pending);

            if (request == null)
            {
                TempData["message"] = "Nu există o cerere de urmărire activă!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", "Users", new { id = userId });
            }

            db.FollowRequests.Remove(request);
            await db.SaveChangesAsync();

            TempData["message"] = "Cerere de urmărire anulată!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", "Users", new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var request = await db.FollowRequests
                .Include(fr => fr.Sender)
                .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.ReceiverId == currentUserId);

            if (request == null)
            {
                TempData["message"] = "Cererea nu a fost găsită!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Requests");
            }

            if (request.Status != FollowRequestStatus.Pending)
            {
                TempData["message"] = "Această cerere a fost deja procesată!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Requests");
            }

            var follow = new Follow
            {
                FollowerId = request.SenderId,
                FollowingId = currentUserId!,
                FollowedAt = DateTime.Now
            };

            db.Follows.Add(follow);
            request.Status = FollowRequestStatus.Accepted;
            await db.SaveChangesAsync();

            TempData["message"] = $"Ai acceptat cererea de la {request.Sender?.FullName}!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Requests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var request = await db.FollowRequests
                .Include(fr => fr.Sender)
                .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.ReceiverId == currentUserId);

            if (request == null)
            {
                TempData["message"] = "Cererea nu a fost găsită!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Requests");
            }

            if (request.Status != FollowRequestStatus.Pending)
            {
                TempData["message"] = "Această cerere a fost deja procesată!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Requests");
            }

            request.Status = FollowRequestStatus.Rejected;
            await db.SaveChangesAsync();

            TempData["message"] = $"Ai respins cererea de la {request.Sender?.FullName}!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Requests");
        }

        public async Task<IActionResult> Requests()
        {
            var currentUserId = _userManager.GetUserId(User);

            var requests = await db.FollowRequests
                .Include(fr => fr.Sender)
                .Where(fr => fr.ReceiverId == currentUserId && fr.Status == FollowRequestStatus.Pending)
                .OrderByDescending(fr => fr.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> Followers(string id)
        {
            var user = await db.ApplicationUsers
                .Include(u => u.Followers)
                    .ThenInclude(f => f.Follower)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var isOwnProfile = currentUserId == id;
            var canView = user.IsPublic || isOwnProfile || User.IsInRole("Admin");

            if (!canView)
            {
                TempData["message"] = "Acest profil este privat!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", "Users", new { id });
            }

            ViewData["UserId"] = id;
            ViewData["UserName"] = user.FullName;

            return View(user.Followers.OrderByDescending(f => f.FollowedAt).ToList());
        }

        public async Task<IActionResult> Following(string id)
        {
            var user = await db.ApplicationUsers
                .Include(u => u.Following)
                    .ThenInclude(f => f.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var isOwnProfile = currentUserId == id;
            var canView = user.IsPublic || isOwnProfile || User.IsInRole("Admin");

            if (!canView)
            {
                TempData["message"] = "Acest profil este privat!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", "Users", new { id });
            }

            ViewData["UserId"] = id;
            ViewData["UserName"] = user.FullName;

            return View(user.Following.OrderByDescending(f => f.FollowedAt).ToList());
        }
    }
}
