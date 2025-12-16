using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SportsAPP.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<Data.ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            context.Database.Migrate();

            // Seed Roles
            SeedRoles(roleManager);

            // Seed Users
            SeedUsers(userManager);

            // Seed Posts and Comments (doar daca nu exista deja)
            if (!context.Posts.Any())
            {
                SeedPosts(context, userManager);
            }
        }

        private static void SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!roleManager.RoleExistsAsync("Admin").Result)
            {
                roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
            }

            if (!roleManager.RoleExistsAsync("User").Result)
            {
                roleManager.CreateAsync(new IdentityRole("User")).Wait();
            }
        }

        private static void SeedUsers(UserManager<ApplicationUser> userManager)
        {
            // Admin User
            if (userManager.FindByEmailAsync("admin@sports.com").Result == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@sports.com",
                    Email = "admin@sports.com",
                    FirstName = "Admin",
                    LastName = "Sports",
                    ProfileDescription = "Administrator al platformei Sports Social",
                    IsPublic = true,
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(admin, "Admin123!").Result;
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(admin, "Admin").Wait();
                }
            }

            // Regular User 1
            if (userManager.FindByEmailAsync("ion.popescu@sports.com").Result == null)
            {
                var user1 = new ApplicationUser
                {
                    UserName = "ion.popescu@sports.com",
                    Email = "ion.popescu@sports.com",
                    FirstName = "Ion",
                    LastName = "Popescu",
                    ProfileDescription = "Pasionat de fotbal și tenis. Susținător al echipei naționale!",
                    IsPublic = true,
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(user1, "User123!").Result;
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user1, "User").Wait();
                }
            }

            // Regular User 2
            if (userManager.FindByEmailAsync("maria.ionescu@sports.com").Result == null)
            {
                var user2 = new ApplicationUser
                {
                    UserName = "maria.ionescu@sports.com",
                    Email = "maria.ionescu@sports.com",
                    FirstName = "Maria",
                    LastName = "Ionescu",
                    ProfileDescription = "Iubitoare de baschet și volei. Fan NBA!",
                    IsPublic = false, // profil privat
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(user2, "User123!").Result;
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user2, "User").Wait();
                }
            }

            // Regular User 3
            if (userManager.FindByEmailAsync("andrei.stan@sports.com").Result == null)
            {
                var user3 = new ApplicationUser
                {
                    UserName = "andrei.stan@sports.com",
                    Email = "andrei.stan@sports.com",
                    FirstName = "Andrei",
                    LastName = "Stan",
                    ProfileDescription = "Antrenor de fitness și nutrition. Pasionat de MMA și box.",
                    IsPublic = true,
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(user3, "User123!").Result;
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user3, "User").Wait();
                }
            }
        }

        private static void SeedPosts(Data.ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var user1 = userManager.FindByEmailAsync("ion.popescu@sports.com").Result;
            var user2 = userManager.FindByEmailAsync("maria.ionescu@sports.com").Result;
            var user3 = userManager.FindByEmailAsync("andrei.stan@sports.com").Result;

            if (user1 == null || user2 == null || user3 == null) return;

            var posts = new List<Post>
            {
                new Post
                {
                    Title = "Victorie incredibilă în Champions League!",
                    Content = "Meciul de aseară a fost unul pentru cărțile de istorie! O revenire spectaculoasă în minutul 90!",
                    MediaType = MediaType.Text,
                    Date = DateTime.Now.AddDays(-5),
                    UserId = user1.Id
                },
                new Post
                {
                    Title = "Top 10 momente din NBA All-Star 2024",
                    Content = "Am compilat cele mai tari momente din meciul All-Star de anul acesta. Ce dunks!",
                    MediaType = MediaType.Video,
                    MediaPath = "/uploads/videos/sample-video.mp4",
                    Date = DateTime.Now.AddDays(-4),
                    UserId = user2.Id
                },
                new Post
                {
                    Title = "5 exerciții esențiale pentru forță",
                    Content = "Dacă vrei să îți îmbunătățești forța, aceste 5 exerciții sunt fundamentale. Le-am încercat toate!",
                    MediaType = MediaType.Image,
                    MediaPath = "/uploads/images/sample-workout.jpg",
                    Date = DateTime.Now.AddDays(-3),
                    UserId = user3.Id
                },
                new Post
                {
                    Title = "Analiza tactică: Cum a câștigat România",
                    Content = "O analiză detaliată a strategiei folosite în meciul de ieri. Formația 4-3-3 a fost cheia succesului.",
                    MediaType = MediaType.Text,
                    Date = DateTime.Now.AddDays(-2),
                    UserId = user1.Id
                },
                new Post
                {
                    Title = "Antrenamentul meu de dimineață",
                    Content = "Nimic nu se compară cu un antrenament bun la prima oră. 100 de burpees și 5km alergare!",
                    MediaType = MediaType.Image,
                    MediaPath = "/uploads/images/morning-run.jpg",
                    Date = DateTime.Now.AddDays(-1),
                    UserId = user3.Id
                }
            };

            context.Posts.AddRange(posts);
            context.SaveChanges();

            // Seed Comments
            var comments = new List<Comment>
            {
                new Comment
                {
                    Content = "Absolut incredibil! Nu am crezut că pot reveni!",
                    Date = DateTime.Now.AddDays(-4),
                    PostId = posts[0].Id,
                    UserId = user2.Id
                },
                new Comment
                {
                    Content = "Cel mai bun meci pe care l-am văzut vreodată!",
                    Date = DateTime.Now.AddDays(-4),
                    PostId = posts[0].Id,
                    UserId = user3.Id
                },
                new Comment
                {
                    Content = "Dunk-ul lui LeBron a fost fenomenal!",
                    Date = DateTime.Now.AddDays(-3),
                    PostId = posts[1].Id,
                    UserId = user1.Id
                },
                new Comment
                {
                    Content = "Mulțumesc pentru share! O să le încerc săptămâna viitoare.",
                    Date = DateTime.Now.AddDays(-2),
                    PostId = posts[2].Id,
                    UserId = user1.Id
                }
            };

            context.Comments.AddRange(comments);
            context.SaveChanges();
        }
    }
}
