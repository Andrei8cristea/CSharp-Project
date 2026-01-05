using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsAPP.Data;
using SportsAPP.Models;

namespace SportsAPP.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            db = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: Users/Index
        // Enhanced search with filters, sorting, and pagination
        public async Task<IActionResult> Index(
            string searchString,
            string filterType = "all",
            string sortBy = "name",
            int page = 1)
        {
            const int pageSize = 12;

            // Store current search parameters for the view
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFilterType"] = filterType;
            ViewData["CurrentSortBy"] = sortBy;
            ViewData["CurrentPage"] = page;

            // Start with all users and include Posts for counting
            var users = db.ApplicationUsers
                .Include(u => u.Posts)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .AsNoTracking()
                .AsQueryable();

            // Apply search filter (case-insensitive)
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                users = users.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(searchLower)));
            }

            // Apply profile type filter
            if (filterType == "public")
            {
                users = users.Where(u => u.IsPublic == true);
            }
            else if (filterType == "private")
            {
                users = users.Where(u => u.IsPublic == false);
            }

            // Apply sorting
            users = sortBy switch
            {
                "newest" => users.OrderByDescending(u => u.Id),
                "posts" => users.OrderByDescending(u => u.Posts.Count),
                _ => users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            };

            // Get total count before pagination
            var totalUsers = await users.CountAsync();
            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewData["PageSize"] = pageSize;

            // Apply pagination
            var paginatedUsers = await users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedUsers);
        }

        // GET: Users/Show/id
        // Afisare profil utilizator - public sau privat
        public async Task<IActionResult> Show(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await db.ApplicationUsers
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Likes)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            
            // Check privacy settings
            ViewBag.IsOwnProfile = currentUserId == id;
            ViewBag.IsFollowing = false;
            ViewBag.CanViewPosts = true; // Default: can view posts
            ViewBag.HasPendingRequest = false;

            if (!user.IsPublic && currentUserId != id && !User.IsInRole("Admin"))
            {
                // Private account - check if current user is following
                if (currentUserId != null)
                {
                    var isFollowing = await db.Follows
                        .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == id);
                    
                    ViewBag.IsFollowing = isFollowing;
                    ViewBag.CanViewPosts = isFollowing;

                    // Check for pending follow request
                    if (!isFollowing)
                    {
                        ViewBag.HasPendingRequest = await db.FollowRequests
                            .AnyAsync(fr => fr.SenderId == currentUserId && 
                                          fr.ReceiverId == id && 
                                          fr.Status == FollowRequestStatus.Pending);
                    }
                }
                else
                {
                    // Not authenticated - cannot view private account posts
                    ViewBag.CanViewPosts = false;
                }
            }
            else if (currentUserId != null && currentUserId != id)
            {
                // Check if following for public accounts (for UI purposes)
                ViewBag.IsFollowing = await db.Follows
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == id);
            }

            // Sort posts by date (only if posts should be visible)
            if (user.Posts != null && (bool)ViewBag.CanViewPosts)
            {
                user.Posts = user.Posts.OrderByDescending(p => p.Date).ToList();
            }
            else if (user.Posts != null)
            {
                // Clear posts if user shouldn't see them
                user.Posts = new List<Post>();
            }

            return View(user);
        }

        // GET: Users/Edit/id
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Verificam daca userul curent incearca sa editeze propriul profil sau este admin
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest profil!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            // Daca este admin, afisam dropdown cu roluri
            if (User.IsInRole("Admin"))
            {
                var roles = await db.Roles.ToListAsync();
                user.AllRoles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = r.Name
                });

                var currentUserRoles = await _userManager.GetRolesAsync(user);
                ViewBag.UserRole = currentUserRoles.FirstOrDefault();
            }

            return View(user);
        }

        // POST: Users/Edit/id
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser requestUser, IFormFile? profileImage, string? newRole)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Verificam daca userul curent incearca sa editeze propriul profil sau este admin
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest profil!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                // Verificare: Dacă utilizatorul nu are imagine de profil și nu a încărcat una nouă
                if (string.IsNullOrEmpty(user.ProfileImagePath) && (profileImage == null || profileImage.Length == 0))
                {
                    ModelState.AddModelError("profileImage", "Poza de profil este obligatorie.");
                    
                    // Reincarcam dropdown-ul cu roluri pentru admin
                    if (User.IsInRole("Admin"))
                    {
                        var roles = await db.Roles.ToListAsync();
                        user.AllRoles = roles.Select(r => new SelectListItem
                        {
                            Value = r.Id,
                            Text = r.Name
                        });
                    }
                    
                    return View(user);
                }

                // Actualizam informatiile profilului
                user.FirstName = requestUser.FirstName;
                user.LastName = requestUser.LastName;
                user.ProfileDescription = requestUser.ProfileDescription;
                user.IsPublic = requestUser.IsPublic;

                // Upload imagine profil daca exista
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Stergem vechea imagine daca exista
                    if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, user.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    user.ProfileImagePath = $"/uploads/profiles/{fileName}";
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Daca este admin si a selectat un rol nou
                    if (User.IsInRole("Admin") && !string.IsNullOrEmpty(newRole))
                    {
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);

                        var roleName = await db.Roles
                            .Where(r => r.Id == newRole)
                            .Select(r => r.Name)
                            .FirstOrDefaultAsync();

                        if (roleName != null)
                        {
                            await _userManager.AddToRoleAsync(user, roleName);
                        }
                    }

                    TempData["message"] = "Profilul a fost actualizat cu succes!";
                    return RedirectToAction("Show", new { id = user.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Daca salvarea esueaza, reincarcam dropdown-ul cu roluri pentru admin
            if (User.IsInRole("Admin"))
            {
                var roles = await db.Roles.ToListAsync();
                user.AllRoles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = r.Name
                });
            }

            return View(user);
        }
    }
}
