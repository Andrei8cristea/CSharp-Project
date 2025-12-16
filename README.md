# SportsAPP - Sports Social Media Platform

## 📋 Cuprins

1. [Descriere Proiect](#descriere-proiect)
2. [Tehnologii Utilizate](#tehnologii-utilizate)
3. [Structura Proiectului](#structura-proiectului)
4. [Structura Bazei de Date](#structura-bazei-de-date)
5. [Pași de Implementare](#pași-de-implementare)
6. [Probleme Întâlnite și Soluții](#probleme-întâlnite-și-soluții)
7. [Funcționalități Implementate](#funcționalități-implementate)
8. [Cum să Rulezi Aplicația](#cum-să-rulezi-aplicația)
9. [Conturi de Test](#conturi-de-test)

---

## 📝 Descriere Proiect

**SportsAPP** este o platformă de social media dedicată pasionaților de sport, construită cu ASP.NET Core MVC. Aplicația permite utilizatorilor să creeze și să partajeze postări despre sport (text, imagini, videoclipuri), să comenteze, să caute alți utilizatori și să gestioneze profiluri publice sau private.

Platforma a fost dezvoltată cu un design modern inspirat de Instagram/Twitter, cu accent pe experiența utilizatorului și funcționalități sociale.

---

## 🛠️ Tehnologii Utilizate

### Backend
- **ASP.NET Core 9.0 MVC** - Framework pentru aplicație
- **Entity Framework Core 9.0** - ORM pentru baza de date
- **ASP.NET Core Identity** - Sistem de autentificare și autorizare
- **SQL Server** - Baza de date relațională

### Frontend
- **Razor Pages** - Template engine
- **Bootstrap 5** - Framework CSS pentru design responsive
- **Bootstrap Icons** - Set de iconițe
- **CSS Custom** - Stiluri personalizate pentru grid layout și efecte hover

### Tools
- **dotnet-ef 9.0.0** - Tool pentru migrări și managementul bazei de date
- **Visual Studio Code** / **Visual Studio 2022** - IDE

---

## 📂 Structura Proiectului

```
SportsAPP/
├── Controllers/
│   ├── PostsController.cs       # CRUD pentru postări + upload media
│   ├── CommentsController.cs    # CRUD pentru comentarii
│   ├── UsersController.cs       # Căutare, profile, editare
│   └── HomeController.cs        # Controller implicit
│
├── Models/
│   ├── ApplicationUser.cs       # Model utilizator extins
│   ├── Post.cs                  # Model postare cu enum MediaType
│   ├── Comment.cs               # Model comentariu
│   └── SeedData.cs             # Date inițiale pentru testare
│
├── Data/
│   └── ApplicationDbContext.cs  # Context EF Core + configurări
│
├── Views/
│   ├── Posts/
│   │   ├── Index.cshtml        # Lista toate postările
│   │   ├── Show.cshtml         # Detalii postare + comentarii
│   │   ├── New.cshtml          # Formular creare postare
│   │   └── Edit.cshtml         # Formular editare postare
│   │
│   ├── Comments/
│   │   └── Edit.cshtml         # Formular editare comentariu
│   │
│   ├── Users/
│   │   ├── Index.cshtml        # Căutare utilizatori
│   │   ├── Show.cshtml         # Profil utilizator (Instagram-style)
│   │   └── Edit.cshtml         # Editare profil
│   │
│   └── Shared/
│       ├── _Layout.cshtml      # Layout principal
│       └── _LoginPartial.cshtml # Widget login/logout
│
├── wwwroot/
│   ├── uploads/
│   │   ├── images/             # Imagini uploadate pentru postări
│   │   ├── videos/             # Videoclipuri uploadate
│   │   └── profiles/           # Poze de profil
│   └── css/
│       └── site.css
│
├── Migrations/                  # Migrări Entity Framework
│   ├── InitialSportsModels.cs
│   ├── RemoveUserIdRequired.cs
│   └── RemoveCommentUserIdRequired.cs
│
└── Program.cs                   # Configurare aplicație + Identity
```

---

## 🗄️ Structura Bazei de Date

### Diagrama ER

```
┌─────────────────────┐         ┌──────────────────┐
│   AspNetUsers       │         │      Posts       │
│─────────────────────│         │──────────────────│
│ Id (PK)             │◄────────│ Id (PK)          │
│ Email               │    1    │ Title            │
│ FirstName           │         │ Content          │
│ LastName            │    *    │ MediaType        │
│ ProfileDescription  │         │ MediaPath        │
│ ProfileImagePath    │         │ Date             │
│ IsPublic            │         │ UserId (FK)      │
└─────────────────────┘         └──────────────────┘
         │                               │
         │                               │ 1
         │ 1                             │
         │                               │
         │                               │ *
         │                      ┌────────┴─────────┐
         │                      │    Comments      │
         │                      │──────────────────│
         └──────────────────────┤ Id (PK)          │
                           *    │ Content          │
                                │ Date             │
                                │ PostId (FK)      │
                                │ UserId (FK)      │
                                └──────────────────┘
```

### Tabele Principale

#### 1. **AspNetUsers** (Extended by ApplicationUser)
```csharp
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileDescription { get; set; }
    public string? ProfileImagePath { get; set; }
    public bool IsPublic { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<Post> Posts { get; set; }
    public virtual ICollection<Comment> Comments { get; set; }
}
```

**Câmpuri cheie:**
- `IsPublic` - Determină dacă profilul este public sau privat
- `ProfileImagePath` - Calea către poza de profil uploadată

#### 2. **Posts**
```csharp
public class Post
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Titlul este obligatoriu")]
    [StringLength(200)]
    [MinLength(5)]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "Conținutul este obligatoriu")]
    public string Content { get; set; }
    
    [Required]
    public MediaType MediaType { get; set; } = MediaType.Text;
    
    public string? MediaPath { get; set; }
    public DateTime Date { get; set; }
    
    // FK - fără [Required] (setat automat de controller)
    public string? UserId { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<Comment> Comments { get; set; }
}

public enum MediaType
{
    Text,    // 0
    Image,   // 1
    Video    // 2
}
```

**Observații importante:**
- `UserId` NU are `[Required]` - se setează automat în controller
- `MediaType` este enum pentru a restricționa valorile
- `MediaPath` este nullable pentru postări text-only

#### 3. **Comments**
```csharp
public class Comment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(1000)]
    public string Content { get; set; }
    
    public DateTime Date { get; set; }
    
    [Required]
    public int PostId { get; set; }
    
    // FK - fără [Required] (setat automat de controller)
    public string? UserId { get; set; }
    
    // Navigation properties
    public virtual Post? Post { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
```

### Configurări Speciale în DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Previne cascade delete conflicts
    modelBuilder.Entity<Comment>()
        .HasOne(c => c.User)
        .WithMany(u => u.Comments)
        .OnDelete(DeleteBehavior.NoAction);
}
```

**De ce?** SQL Server nu permite multiple cascade paths. Această configurare previne ștergerea automată a comentariilor când un user este șters.

---

## 🚀 Pași de Implementare

### Pas 1: Setup Proiect

```bash
# Creare proiect nou ASP.NET Core MVC
dotnet new mvc -n SportsAPP

# Navigare în folder
cd SportsAPP

# Instalare pachet EF Core Design
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0

# Instalare local tool pentru entity framework
dotnet new tool-manifest
dotnet tool install --local dotnet-ef --version 9.0.0
```

### Pas 2: Creare Modele

**ApplicationUser.cs** - Extinde IdentityUser cu câmpuri custom:

```csharp
using Microsoft.AspNetCore.Identity;

namespace SportsAPP.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfileDescription { get; set; }
        public string? ProfileImagePath { get; set; }
        public bool IsPublic { get; set; } = true;
        
        public string FullName => $"{FirstName} {LastName}";
        
        public virtual ICollection<Post> Posts { get; set; } = [];
        public virtual ICollection<Comment> Comments { get; set; } = [];
    }
}
```

**Post.cs** - Model pentru postări:

```csharp
public class Post
{
    [Key]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Titlul este obligatoriu")]
    [StringLength(200, ErrorMessage = "Titlul nu poate avea mai mult de 200 de caractere")]
    [MinLength(5, ErrorMessage = "Titlul trebuie sa aiba cel putin 5 caractere")]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "Conținutul este obligatoriu")]
    public string Content { get; set; }
    
    [Required(ErrorMessage = "Tipul de media este obligatoriu")]
    public MediaType MediaType { get; set; } = MediaType.Text;
    
    public string? MediaPath { get; set; }
    public DateTime Date { get; set; }
    public string? UserId { get; set; }  // NU are [Required]!
    
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<Comment> Comments { get; set; } = [];
}
```

### Pas 3: Configurare Identity și DbContext

**ApplicationDbContext.cs:**

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
    
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Fix cascade delete conflict
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
```

**Program.cs:**

```csharp
// Configurare Identity cu ApplicationUser și Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Seed data la startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}
```

### Pas 4: Creare și Aplicare Migrări

```bash
# Creare migrare inițială
dotnet ef migrations add InitialSportsModels

# Aplicare migrare în baza de date
dotnet ef database update
```

**Dacă ai probleme cu cascade delete:**

```bash
# Șterge ultima migrare
dotnet ef migrations remove

# Adaugă configurarea OnModelCreating în DbContext
# Apoi recreează migrarea
dotnet ef migrations add InitialSportsModels
dotnet ef database update
```

### Pas 5: Implementare Controllers

**PostsController.cs** - Exemplu metoda Create:

```csharp
[HttpPost]
[Authorize(Roles = "User,Admin")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    [Bind("Title,Content,MediaType")] Post post, 
    IFormFile? mediaFile)
{
    if (ModelState.IsValid)
    {
        post.Date = DateTime.Now;
        post.UserId = _userManager.GetUserId(User);  // Setare automată!
        
        // Upload media file
        if (mediaFile != null && mediaFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            var mediaFolder = post.MediaType == MediaType.Image ? "images" : "videos";
            var targetFolder = Path.Combine(uploadsFolder, mediaFolder);
            
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);
            
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
            var filePath = Path.Combine(targetFolder, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await mediaFile.CopyToAsync(stream);
            }
            
            post.MediaPath = $"/uploads/{mediaFolder}/{fileName}";
        }
        
        db.Add(post);
        await db.SaveChangesAsync();
        TempData["message"] = "Postarea a fost adăugată cu succes!";
        return RedirectToAction(nameof(Index));
    }
    
    return View("New", post);
}
```

### Pas 6: Creare Views

**Posts/New.cshtml** - Formular cu upload media:

```html
<form method="post" asp-controller="Posts" asp-action="Create" enctype="multipart/form-data">
    @Html.AntiForgeryToken()
    
    <div asp-validation-summary="All" class="text-danger"></div>
    
    <div class="mb-3">
        <label for="Title" class="form-label">Titlu <span class="text-danger">*</span></label>
        <input type="text" class="form-control" id="Title" name="Title" required />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>
    
    <div class="mb-3">
        <label for="Content" class="form-label">Conținut <span class="text-danger">*</span></label>
        <textarea class="form-control" id="Content" name="Content" rows="5" required></textarea>
        <span asp-validation-for="Content" class="text-danger"></span>
    </div>
    
    <div class="mb-3">
        <label for="MediaType" class="form-label">Tipul Postării</label>
        <select class="form-select" id="MediaType" name="MediaType" required>
            <option value="0">Text</option>
            <option value="1">Imagine</option>
            <option value="2">Videoclip</option>
        </select>
    </div>
    
    <div class="mb-3" id="mediaFileDiv" style="display: none;">
        <label for="mediaFile" class="form-label">Încarcă Fișier Media</label>
        <input type="file" class="form-control" id="mediaFile" name="mediaFile" accept="image/*,video/*" />
    </div>
    
    <button type="submit" class="btn btn-primary">
        <i class="bi bi-check-circle"></i> Creează Postarea
    </button>
</form>

@section Scripts {
    <script>
        // Show/hide media upload based on MediaType
        document.getElementById('MediaType').addEventListener('change', function() {
            var mediaType = this.value;
            var mediaFileDiv = document.getElementById('mediaFileDiv');
            var mediaFileInput = document.getElementById('mediaFile');
            
            if (mediaType == '1' || mediaType == '2') {
                mediaFileDiv.style.display = 'block';
                mediaFileInput.accept = mediaType == '1' ? 'image/*' : 'video/*';
            } else {
                mediaFileDiv.style.display = 'none';
                mediaFileInput.value = '';
            }
        });
    </script>
}
```

---

## 🐛 Probleme Întâlnite și Soluții

### Problema 1: Nu se pot crea postări - "Utilizatorul este obligatoriu"

**Simptom:**
```
Eroare validare: "Utilizatorul este obligatoriu"
Chiar dacă utilizatorul este autentificat
```

**Cauză:**
Modelele `Post` și `Comment` aveau `[Required]` pe `UserId`:

```csharp
// GREȘIT ❌
[Required(ErrorMessage = "Utilizatorul este obligatoriu")]
public string? UserId { get; set; }
```

UserId este setat de controller **DUPĂ** validare, deci validarea eșua mereu.

**Soluție:**
```csharp
// CORECT ✅
public string? UserId { get; set; }  // Fără [Required]
```

Controller-ul setează UserId automat:
```csharp
post.UserId = _userManager.GetUserId(User);
```

**Pași de rezolvare:**
```bash
# 1. Șters [Required] din Post.cs și Comment.cs
# 2. Creat migrări
dotnet ef migrations add RemoveUserIdRequired
dotnet ef migrations add RemoveCommentUserIdRequired

# 3. Aplicat migrări
dotnet ef database update
```

### Problema 2: Eroare Cascade Delete în Migrare

**Simptom:**
```
SqlException: Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths
```

**Cauză:**
SQL Server nu permite multiple cascade paths. Când ștergi un User:
- Posts → Comments (cascade delete)
- User → Comments (cascade delete)

Conflict!

**Soluție:**
Configurare în `ApplicationDbContext.OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Previne cascade delete pentru Comment -> User
    modelBuilder.Entity<Comment>()
        .HasOne(c => c.User)
        .WithMany(u => u.Comments)
        .OnDelete(DeleteBehavior.NoAction);
}
```

### Problema 3: UserManager nu este găsit în Views

**Simptom:**
```
CS0246: The type or namespace name 'UserManager<>' could not be found
```

**Soluție:**
Adăugat în `_ViewImports.cshtml`:

```csharp
@using Microsoft.AspNetCore.Identity
@using SportsAPP.Models
```

Apoi inject în fiecare view care folosește UserManager:

```csharp
@inject UserManager<ApplicationUser> UserManager
```

### Problema 4: _LoginPartial folosea IdentityUser în loc de ApplicationUser

**Simptom:**
```
InvalidOperationException: No service for type 
'UserManager<IdentityUser>' has been registered
```

**Soluție:**
Modificat `_LoginPartial.cshtml`:

```csharp
// ÎNAINTE ❌
@inject SignInManager<IdentityUser> SignInManager
@inject UserManager<IdentityUser> UserManager

// DUPĂ ✅
@using SportsAPP.Models
@inject SignInManager<ApplicationUser> SignInManager
@inject UserManager<ApplicationUser> UserManager
```

### Problema 5: Folder-ele pentru Upload nu există

**Simptom:**
Aplicația crașa la upload de imagini/videoclipuri.

**Soluție:**
Controller-ul creează automat folder-ele:

```csharp
var targetFolder = Path.Combine(_env.WebRootPath, "uploads", mediaFolder);

if (!Directory.Exists(targetFolder))
{
    Directory.CreateDirectory(targetFolder);
}
```

---

## ✨ Funcționalități Implementate

### 1. **Autentificare și Autorizare**
- ✅ Register/Login cu ASP.NET Identity
- ✅ Role-based authorization (Admin, User)
- ✅ Validare email și password

### 2. **User Management**
- ✅ Profile publice/private (IsPublic toggle)
- ✅ Upload poză de profil
- ✅ Editare FirstName, LastName, ProfileDescription
- ✅ Admin poate edita orice profil și poate schimba role-uri

### 3. **Posts (CRUD)**
- ✅ Creare postări cu text, imagini sau videoclipuri
- ✅ Edit propriile postări
- ✅ Delete propriile postări (Admin poate șterge orice)
- ✅ Vizualizare lista toate postările
- ✅ Vizualizare detalii postare cu toate comentariile

### 4. **Comments (CRUD)**
- ✅ Adăugare comentarii la postări
- ✅ Edit propriile comentarii
- ✅ Delete propriile comentarii (Admin poate șterge orice)

### 5. **Search & Discovery**
- ✅ Căutare utilizatori după nume (partial match)
- ✅ Vizualizare profil utilizator
- ✅ Respect privacy settings (profiluri private)

### 6. **UI/UX Modern**
- ✅ Design Instagram-style pentru profiluri
- ✅ Grid layout 3x3 pentru postări pe profil
- ✅ Hover effects pe grid items
- ✅ Responsive design cu Bootstrap 5
- ✅ Icons cu Bootstrap Icons
- ✅ Validation messages vizibile

### 7. **Media Upload**
- ✅ Upload imagini (JPG, PNG, GIF)
- ✅ Upload videoclipuri (MP4, AVI, MOV)
- ✅ Preview media în views
- ✅ Ștergere automată fișiere când se șterge postarea

---

## 🏃‍♂️ Cum să Rulezi Aplicația

### Cerințe Prerequisite
- .NET 9.0 SDK
- SQL Server (LocalDB sau SQL Server Express)
- Visual Studio 2022 sau VS Code

### Pași de Rulare

1. **Clone repository / Deschide proiectul:**
```bash
cd c:\Users\Razvan\Documents\ProiectASP\SportsAPP
```

2. **Restore packages:**
```bash
dotnet restore
```

3. **Update connection string (dacă e necesar):**
În `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SportsApp;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

4. **Aplică migrările:**
```bash
dotnet ef database update
```

5. **Rulează aplicația:**
```bash
dotnet run
```

6. **Deschide browser la:**
```
http://localhost:5036
```

### Pentru Development (cu hot reload):
```bash
dotnet watch run
```

---

## 👤 Conturi de Test

Aplicația vine cu 4 conturi pre-create prin `SeedData`:

### 1. **Administrator**
- **Email:** admin@sports.com
- **Password:** Admin123!
- **Role:** Admin
- **Profil:** Public
- **Privilegii:** Poate edita/șterge orice postare, comentariu, profil

### 2. **Ion Popescu** (User Normal)
- **Email:** ion.popescu@sports.com
- **Password:** User123!
- **Role:** User
- **Profil:** Public
- **Are:** 2 postări (1 text, 1 imagine despre fotbal)

### 3. **Maria Ionescu** (Profil Privat)
- **Email:** maria.ionescu@sports.com
- **Password:** User123!
- **Role:** User
- **Profil:** PRIVAT
- **Are:** 1 postare (text despre tenis)

### 4. **Andrei Popescu** (User Normal)
- **Email:** andrei.popescu@sports.com
- **Password:** User123!
- **Role:** User
- **Profil:** Public
- **Are:** 2 postări (1 text despre baschet, 1 videoclip)

---

## 📊 Code Snippets Utile

### Verificare dacă user poate vedea profil complet

```csharp
var currentUserId = UserManager.GetUserId(User);
var isOwnProfile = currentUserId == Model.Id;
var canViewFull = Model.IsPublic || isOwnProfile || User.IsInRole("Admin");

@if (canViewFull)
{
    <!-- Afișează postări -->
}
else
{
    <!-- Mesaj "Profil Privat" -->
}
```

### Upload fișier și salvare în wwwroot

```csharp
if (mediaFile != null && mediaFile.Length > 0)
{
    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
    var mediaFolder = post.MediaType == MediaType.Image ? "images" : "videos";
    var targetFolder = Path.Combine(uploadsFolder, mediaFolder);
    
    if (!Directory.Exists(targetFolder))
        Directory.CreateDirectory(targetFolder);
    
    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
    var filePath = Path.Combine(targetFolder, fileName);
    
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await mediaFile.CopyToAsync(stream);
    }
    
    post.MediaPath = $"/uploads/{mediaFolder}/{fileName}";
}
```

### Verificare ownership pentru Edit/Delete

```csharp
if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
{
    TempData["message"] = "Nu aveți dreptul să editați această postare!";
    TempData["messageType"] = "alert-danger";
    return RedirectToAction(nameof(Index));
}
```

### Grid Layout Instagram-style (CSS)

```css
.post-grid-item {
    overflow: hidden;
    transition: transform 0.2s ease;
}

.post-grid-item:hover {
    transform: scale(1.02);
    z-index: 1;
}

.post-overlay {
    background: rgba(0, 0, 0, 0.5);
    opacity: 0;
    transition: opacity 0.3s ease;
}

.post-grid-item:hover .post-overlay {
    opacity: 1;
}
```

---

## 🎯 Lecții Învățate

1. **Validare cu [Required] trebuie folosită doar pentru câmpuri din formular**, nu pentru câmpuri setate programatic de controller

2. **Cascade Delete** poate crea conflicte în SQL Server când ai multiple relații - folosește `DeleteBehavior.NoAction`

3. **ApplicationUser** trebuie folosit consistent în toată aplicația, nu amesteca cu `IdentityUser`

4. **Media files** ar trebui stocate cu nume unice (GUID) pentru a evita conflictele

5. **ModelState.IsValid** verifică doar validările definite cu atribute, nu logica de business

6. **Dependency Injection** în views se face cu `@inject`, nu prin constructor

7. **Migrările EF Core** trebuie aplicate la fiecare schimbare de model, altfel aplicația crașează

---

## 📌 Status Proiect

✅ **Faza 1:** Modele și baza de date - **COMPLET**  
✅ **Faza 2:** Controllers și logică business - **COMPLET**  
✅ **Faza 3:** Views și UI - **COMPLET**  
✅ **Faza 4:** Bug fixes și validare - **COMPLET**  
✅ **Faza 5:** Instagram-style profile - **COMPLET**

### Posibile Îmbunătățiri Viitoare
- [ ] Sistem de Like/React pentru postări
- [ ] Follow/Unfollow utilizatori
- [ ] News Feed personalizat cu postări de la utilizatorii urmăriți
- [ ] Grupuri și moderatori
- [ ] Notificări în timp real
- [ ] Chat privat între utilizatori
- [ ] AI content filtering pentru limbaj neadecvat

---

**Developed by:** Razvan & Andrei
**Data:** Decembrie 2025  
**Framework:** ASP.NET Core 9.0 MVC  
**Database:** SQL Server
