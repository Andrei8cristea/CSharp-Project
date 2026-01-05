using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public GroupsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            db = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: Groups
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var allGroups = await db.Groups
                .Include(g => g.Moderator)
                .Include(g => g.Members.Where(m => m.Status == MemberStatus.Approved))
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            // Separate groups into two lists: user's groups and available groups
            var userGroups = allGroups
                .Where(g => g.Members.Any(m => m.UserId == currentUserId && m.Status == MemberStatus.Approved))
                .ToList();

            var availableGroups = allGroups
                .Where(g => !g.Members.Any(m => m.UserId == currentUserId && m.Status == MemberStatus.Approved))
                .ToList();

            ViewBag.UserGroups = userGroups;
            ViewBag.AvailableGroups = availableGroups;

            return View();
        }

        // GET: Groups/Show/5
        public async Task<IActionResult> Show(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await db.Groups
                .Include(g => g.Moderator)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.Messages)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId);

            ViewBag.IsMember = membership != null && membership.Status == MemberStatus.Approved;
            ViewBag.IsModerator = group.ModeratorId == currentUserId;
            ViewBag.HasPendingRequest = membership != null && membership.Status == MemberStatus.Pending;
            ViewBag.CurrentUserId = currentUserId;

            // Sort messages by date
            if (group.Messages != null)
            {
                group.Messages = group.Messages.OrderBy(m => m.SentAt).ToList();
            }

            return View(group);
        }

        // GET: Groups/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Group group, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = _userManager.GetUserId(User)!;
                group.ModeratorId = currentUserId;
                group.CreatedAt = DateTime.Now;

                // Upload group image if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "groups");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    group.ImagePath = $"/uploads/groups/{fileName}";
                }

                // Save group first to get the ID
                db.Groups.Add(group);
                await db.SaveChangesAsync();

                // Now add moderator as approved member with the correct GroupId
                var moderatorMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = currentUserId,
                    Status = MemberStatus.Approved,
                    Role = MemberRole.Moderator,
                    JoinedAt = DateTime.Now
                };

                db.GroupMembers.Add(moderatorMember);
                await db.SaveChangesAsync();

                TempData["message"] = "Grupul a fost creat cu succes!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction(nameof(Show), new { id = group.Id });
            }

            return View(group);
        }

        // POST: Groups/Join/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var currentUserId = _userManager.GetUserId(User)!;

            // Check if already a member or has pending request
            var existingMember = await db.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == currentUserId);

            if (existingMember != null)
            {
                TempData["message"] = "Ai deja o cerere în așteptare sau ești deja membru!";
                TempData["messageType"] = "alert-info";
                return RedirectToAction(nameof(Show), new { id });
            }

            var member = new GroupMember
            {
                GroupId = id,
                UserId = currentUserId,
                Status = MemberStatus.Pending,
                Role = MemberRole.Member,
                JoinedAt = DateTime.Now
            };

            db.GroupMembers.Add(member);
            await db.SaveChangesAsync();

            TempData["message"] = "Cererea ta de alăturare a fost trimisă! Așteaptă aprobarea moderatorului.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction(nameof(Show), new { id });
        }

        // POST: Groups/ApproveJoin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveJoin(int memberId)
        {
            var member = await db.GroupMembers
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (member.Group!.ModeratorId != currentUserId)
            {
                TempData["message"] = "Nu ai permisiunea să aprobi membri!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Show), new { id = member.GroupId });
            }

            member.Status = MemberStatus.Approved;
            await db.SaveChangesAsync();

            TempData["message"] = "Membru aprobat cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction(nameof(Show), new { id = member.GroupId });
        }

        // POST: Groups/RejectJoin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectJoin(int memberId)
        {
            var member = await db.GroupMembers
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (member.Group!.ModeratorId != currentUserId)
            {
                TempData["message"] = "Nu ai permisiunea să respingi membri!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Show), new { id = member.GroupId });
            }

            db.GroupMembers.Remove(member);
            await db.SaveChangesAsync();

            TempData["message"] = "Cerere respinsă!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction(nameof(Show), new { id = member.GroupId });
        }

        // POST: Groups/Leave/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var currentUserId = _userManager.GetUserId(User)!;

            var member = await db.GroupMembers
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == currentUserId);

            if (member == null)
            {
                TempData["message"] = "Nu ești membru al acestui grup!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction(nameof(Index));
            }

            if (member.Group!.ModeratorId == currentUserId)
            {
                TempData["message"] = "Moderatorul nu poate părăsi grupul! Șterge grupul în schimb.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Show), new { id });
            }

            db.GroupMembers.Remove(member);
            await db.SaveChangesAsync();

            TempData["message"] = "Ai părăsit grupul cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction(nameof(Index));
        }

        // POST: Groups/Kick
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kick(int groupId, string userId)
        {
            var group = await db.Groups.FindAsync(groupId);
            if (group == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (group.ModeratorId != currentUserId)
            {
                TempData["message"] = "Nu ai permisiunea să elimini membri!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Show), new { id = groupId });
            }

            var member = await db.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (member == null)
            {
                return NotFound();
            }

            db.GroupMembers.Remove(member);
            await db.SaveChangesAsync();

            TempData["message"] = "Membru eliminat cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction(nameof(Show), new { id = groupId });
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await db.Groups.FindAsync(id);

            if (group == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (group.ModeratorId != currentUserId)
            {
                TempData["message"] = "Nu ai permisiunea să ștergi acest grup!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            // Delete group image if exists
            if (!string.IsNullOrEmpty(group.ImagePath))
            {
                var filePath = Path.Combine(_env.WebRootPath, group.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            db.Groups.Remove(group);
            await db.SaveChangesAsync();

            TempData["message"] = "Grupul a fost șters cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction(nameof(Index));
        }
    }
}
