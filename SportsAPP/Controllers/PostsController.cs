using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public PostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            db = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: Posts
        // Afisare toate postările, ordonate descrescător după dată
        public async Task<IActionResult> Index()
        {
            var posts = await db.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            return View(posts);
        }

        // GET: Posts/Show/5
        // Afisare o postare cu toate comentariile
        public async Task<IActionResult> Show(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await db.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Posts/New
        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,MediaType")] Post post, IFormFile? mediaFile)
        {
            if (ModelState.IsValid)
            {
                post.Date = DateTime.Now;
                post.UserId = _userManager.GetUserId(User);

                // Upload media file daca exista
                if (mediaFile != null && mediaFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    
                    // Determinam folderul in functie de tipul de media
                    var mediaFolder = post.MediaType == MediaType.Image ? "images" : "videos";
                    var targetFolder = Path.Combine(uploadsFolder, mediaFolder);
                    
                    // Cream folderul daca nu exista
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    // Generam un nume unic pentru fisier
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
                    var filePath = Path.Combine(targetFolder, fileName);

                    // Salvam fisierul
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mediaFile.CopyToAsync(stream);
                    }

                    // Salvam calea relativa in baza de date
                    post.MediaPath = $"/uploads/{mediaFolder}/{fileName}";
                }

                db.Add(post);
                await db.SaveChangesAsync();
                TempData["message"] = "Postarea a fost adăugată cu succes!";
                return RedirectToAction(nameof(Index));
            }

            return View("New", post);
        }

        // GET: Posts/Edit/5
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await db.Posts.FindAsync(id);
            
            if (post == null)
            {
                return NotFound();
            }

            // Verificam daca userul curent este owner-ul sau admin
            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați această postare!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            return View(post);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,MediaType,MediaPath,Date,UserId")] Post post, IFormFile? mediaFile)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            // Verificam daca userul curent este owner-ul sau admin
            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați această postare!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload nou fisier media daca exista
                    if (mediaFile != null && mediaFile.Length > 0)
                    {
                        // Stergem vechiul fisier daca exista
                        if (!string.IsNullOrEmpty(post.MediaPath))
                        {
                            var oldFilePath = Path.Combine(_env.WebRootPath, post.MediaPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                        var mediaFolder = post.MediaType == MediaType.Image ? "images" : "videos";
                        var targetFolder = Path.Combine(uploadsFolder, mediaFolder);
                        
                        if (!Directory.Exists(targetFolder))
                        {
                            Directory.CreateDirectory(targetFolder);
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
                        var filePath = Path.Combine(targetFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await mediaFile.CopyToAsync(stream);
                        }

                        post.MediaPath = $"/uploads/{mediaFolder}/{fileName}";
                    }

                    db.Update(post);
                    await db.SaveChangesAsync();
                    TempData["message"] = "Postarea a fost modificată cu succes!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await db.Posts.FindAsync(id);
            
            if (post == null)
            {
                return NotFound();
            }

            // Verificam daca userul curent este owner-ul sau admin
            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți această postare!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            // Stergem fisierul media daca exista
            if (!string.IsNullOrEmpty(post.MediaPath))
            {
                var filePath = Path.Combine(_env.WebRootPath, post.MediaPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            db.Posts.Remove(post);
            await db.SaveChangesAsync();
            TempData["message"] = "Postarea a fost ștearsă cu succes!";
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return db.Posts.Any(e => e.Id == id);
        }
    }
}
