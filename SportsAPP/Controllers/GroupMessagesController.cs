using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    [Authorize]
    public class GroupMessagesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupMessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // POST: GroupMessages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int groupId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["message"] = "Mesajul nu poate fi gol!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", "Groups", new { id = groupId });
            }

            var currentUserId = _userManager.GetUserId(User)!;

            // Check if user is an approved member
            var membership = await db.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && 
                                         m.UserId == currentUserId && 
                                         m.Status == MemberStatus.Approved);

            if (membership == null)
            {
                TempData["message"] = "Trebuie să fii membru aprobat pentru a trimite mesaje!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Groups", new { id = groupId });
            }

            var message = new GroupMessage
            {
                GroupId = groupId,
                UserId = currentUserId,
                Content = content,
                SentAt = DateTime.Now
            };

            db.GroupMessages.Add(message);
            await db.SaveChangesAsync();

            return RedirectToAction("Show", "Groups", new { id = groupId });
        }

        // GET: GroupMessages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await db.GroupMessages
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (message.UserId != currentUserId)
            {
                TempData["message"] = "Nu poți edita mesajele altora!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Groups", new { id = message.GroupId });
            }

            return View(message);
        }

        // POST: GroupMessages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string content)
        {
            var message = await db.GroupMessages.FindAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (message.UserId != currentUserId)
            {
                TempData["message"] = "Nu poți edita mesajele altora!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Groups", new { id = message.GroupId });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["message"] = "Mesajul nu poate fi gol!";
                TempData["messageType"] = "alert-warning";
                return View(message);
            }

            message.Content = content;
            await db.SaveChangesAsync();

            TempData["message"] = "Mesaj editat cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", "Groups", new { id = message.GroupId });
        }

        // POST: GroupMessages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await db.GroupMessages
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            
            // Allow deletion if user owns message OR is moderator
            var isModerator = message.Group!.ModeratorId == currentUserId;
            var isOwner = message.UserId == currentUserId;

            if (!isOwner && !isModerator)
            {
                TempData["message"] = "Nu ai permisiunea să ștergi acest mesaj!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Groups", new { id = message.GroupId });
            }

            db.GroupMessages.Remove(message);
            await db.SaveChangesAsync();

            TempData["message"] = "Mesaj șters cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", "Groups", new { id = message.GroupId });
        }
    }
}
