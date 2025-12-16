using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // POST: Comments/Create
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Content,PostId")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                comment.Date = DateTime.Now;
                comment.UserId = _userManager.GetUserId(User);

                db.Add(comment);
                await db.SaveChangesAsync();
                TempData["message"] = "Comentariul a fost adăugat cu succes!";
                
                return RedirectToAction("Show", "Posts", new { id = comment.PostId });
            }

            // Daca validarea esueaza, redirectionam inapoi la post
            return RedirectToAction("Show", "Posts", new { id = comment.PostId });
        }

        // GET: Comments/Edit/5
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await db.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            // Verificam daca userul curent este owner-ul sau admin
            if (comment.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest comentariu!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Posts", new { id = comment.PostId });
            }

            return View(comment);
        }

        // POST: Comments/Edit/5
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,Date,PostId,UserId")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            // Verificam daca userul curent este owner-ul sau admin
            if (comment.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest comentariu!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Posts", new { id = comment.PostId });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Update(comment);
                    await db.SaveChangesAsync();
                    TempData["message"] = "Comentariul a fost modificat cu succes!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Show", "Posts", new { id = comment.PostId });
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await db.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            // Verificam daca userul curent este owner-ul sau admin
            if (comment.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți acest comentariu!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Posts", new { id = comment.PostId });
            }

            var postId = comment.PostId;
            db.Comments.Remove(comment);
            await db.SaveChangesAsync();
            TempData["message"] = "Comentariul a fost șters cu succes!";
            
            return RedirectToAction("Show", "Posts", new { id = postId });
        }

        private bool CommentExists(int id)
        {
            return db.Comments.Any(e => e.Id == id);
        }
    }
}
