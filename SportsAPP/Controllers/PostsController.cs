using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;
using SportsAPP.Services;

namespace SportsAPP.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IContentModerationService _moderationService;
        private readonly IRateLimitService _rateLimitService;

        public PostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IContentModerationService moderationService,
            IRateLimitService rateLimitService)
        {
            db = context;
            _userManager = userManager;
            _env = env;
            _moderationService = moderationService;
            _rateLimitService = rateLimitService;
        }

        // GET: Posts
        // Feed personalizat - postările de la persoanele urmarite + propriile postări
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                // If user is not authenticated, show only posts from public accounts
                var allPosts = await db.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                    .Include(p => p.Likes)
                    .Where(p => p.User!.IsPublic) // Only public accounts
                    .OrderByDescending(p => p.Date)
                    .ToListAsync();

                return View(allPosts);
            }

            // Get IDs of users that current user is following
            var followingIds = await db.Follows
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            // Get posts with privacy filtering:
            // 1. Own posts (always visible)
            // 2. Posts from followed users (regardless of privacy setting)
            // 3. Posts from public accounts that the user doesn't follow
            var posts = await db.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => 
                    p.UserId == currentUserId || // Own posts
                    followingIds.Contains(p.UserId!) || // Posts from followed users
                    p.User!.IsPublic) // Posts from public accounts
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            // Pass info to view for empty state message
            ViewBag.IsFollowingAnyone = followingIds.Any();
            ViewBag.HasOwnPosts = posts.Any(p => p.UserId == currentUserId);

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
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            // Privacy check: if the post author has a private account
            if (!post.User!.IsPublic)
            {
                // Allow if:
                // 1. Not authenticated -> redirect to login
                // 2. Owner can always see their own posts
                // 3. Followers can see posts
                // 4. Admins can see all posts

                if (currentUserId == null)
                {
                    TempData["message"] = "Trebuie să fii autentificat pentru a vedea această postare.";
                    TempData["messageType"] = "alert-warning";
                    return RedirectToAction("Index");
                }

                if (post.UserId != currentUserId && !User.IsInRole("Admin"))
                {
                    // Check if current user follows the post author
                    var isFollowing = await db.Follows
                        .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == post.UserId);

                    if (!isFollowing)
                    {
                        TempData["message"] = "Acest utilizator are contul privat. Trebuie să îl urmărești pentru a vedea postările.";
                        TempData["messageType"] = "alert-warning";
                        return RedirectToAction("Index");
                    }
                }
            }

            // Check if current user liked this post
            if (currentUserId != null)
            {
                ViewBag.HasLiked = post.Likes.Any(l => l.UserId == currentUserId);
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
                var userId = _userManager.GetUserId(User)!;

                // Check rate limit
                if (!await _rateLimitService.IsAllowedAsync(userId, RateLimitType.Post))
                {
                    var remaining = _rateLimitService.GetRemainingCount(userId, RateLimitType.Post);
                    TempData["message"] = "Ai atins limita de postări! Poți posta din nou peste câteva minute.";
                    TempData["messageType"] = "alert-warning";
                    return View("New", post);
                }

                // Check content moderation
                var contentToModerate = $"{post.Title} {post.Content}";
                var moderationResult = await _moderationService.ModerateAsync(contentToModerate);
                
                if (!moderationResult.IsApproved)
                {
                    TempData["message"] = $"Postarea a fost blocată: {moderationResult.Reason}";
                    TempData["messageType"] = "alert-danger";
                    return View("New", post);
                }

                post.Date = DateTime.Now;
                post.UserId = userId;

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
                TempData["messageType"] = "alert-success";
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
                // Check content moderation for edited content
                var contentToModerate = $"{post.Title} {post.Content}";
                var moderationResult = await _moderationService.ModerateAsync(contentToModerate);
                
                if (!moderationResult.IsApproved)
                {
                    TempData["message"] = $"Modificarea a fost blocată: {moderationResult.Reason}";
                    TempData["messageType"] = "alert-danger";
                    return View(post);
                }

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
                    TempData["messageType"] = "alert-success";
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
