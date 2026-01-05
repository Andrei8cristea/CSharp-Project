# Sports Social Platform

O platformă de tip micro-social network dezvoltată în ASP.NET Core MVC, inspirată din design-ul Instagram și Twitter, dedicată comunității pasionate de sport.

## Tehnologii Utilizate

- **Framework:** ASP.NET Core 9.0 MVC
- **Limbaj:** C# 12
- **Bază de date:** SQL Server cu Entity Framework Core 9.0
- **Autentificare:** ASP.NET Identity
- **Frontend:** Bootstrap 5, Bootstrap Icons, CSS Variables pentru Dark Mode
- **AI Integration:** Content Moderation Service (filtrare conținut neadecvat)

## Funcționalități Principale

### 1. Sistem de Autentificare și Roluri
- Trei tipuri de utilizatori: Vizitator neînregistrat, Utilizator înregistrat, Administrator
- Autentificare și înregistrare cu ASP.NET Identity
- Managementul rolurilor (User, Admin)

### 2. Profiluri și Căutare
- Profiluri personalizate cu nume complet, descriere și poză (toate obligatorii)
- Căutare utilizatori după nume (funcționează și cu părți din nume)
- Vizibilitate profil: Public sau Privat
- Profilurile private afișează doar informații de bază pentru non-followers

### 3. Sistem de Follow (Unidirecțional)
- Cereri de follow pentru profiluri private (status: Pending/Accepted/Rejected)
- Follow automat pentru profiluri publice
- Numărare followers și following
- Vizualizare liste followers/following

### 4. Postări și Media
- Tipuri de conținut suportate: Text, Imagini, Videoclipuri
- Upload fișiere în `wwwroot/uploads/`
- Comentarii la postări
- Edit/Delete pentru propriile postări și comentarii
- Ordine cronologică descrescătoare

### 5. Sistem de Reacții
- Like/Unlike la postări
- Prevenire dublă reacție (un utilizator = un like)
- Numărare likes în timp real

### 6. Feed Personalizat
- Afișare postări de la utilizatorii urmăriți
- Privacy filtering automat (doar postări publice sau de la followings)
- Sortare cronologică

### 7. Grupuri și Chat
- Creare grupuri cu nume și descriere obligatorii
- Sistem de aprobare membri (Pending/Approved) de către moderator
- Mesaje în grup pentru membri aprobați
- Moderator poate elimina membri și șterge grupul

### 8. Filtrare Automată Conținut (AI)
- Verificare conținut înainte de publicare
- Detectare limbaj neadecvat (insulte, hate speech, discriminare)
- Blocare automată cu mesaj informativ pentru utilizator
- Integrare prin `IContentModerationService`

### 9. Panou Administrator
- Ștergere orice conținut (postări, comentarii, utilizatori, grupuri)
- Vizualizare toate profilurile, inclusiv private
- Gestionare roluri utilizatori

### 10. Dark Mode
- Toggle instant între Light/Dark theme
- Persistență preferință în localStorage
- Auto-detecție preferință sistem operare
- Design inspirat din Instagram Dark Mode
- CSS Variables pentru tranziții smooth

## Instalare și Rulare

### Cerințe
- .NET 9.0 SDK
- SQL Server (LocalDB sau instanță completă)
- Visual Studio 2022 sau JetBrains Rider (opțional)

### Pași de instalare

1. Clonează repository-ul:
```bash
git clone <repository-url>
cd SportsAPP
```

2. Restaurează pachetele NuGet:
```bash
dotnet restore
```

3. Actualizează connection string-ul în `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SportsAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

4. Aplică migrările la baza de date:
```bash
dotnet ef database update
```

5. Rulează aplicația:
```bash
dotnet run
```

6. Accesează aplicația la `https://localhost:5001` sau `http://localhost:5000`

## Conturi de Test

Aplicația vine cu date seed pre-populate:

### Administrator
- Email: `admin@sports.com`
- Parolă: `Admin123!`

### Utilizatori
- **Ion Popescu** (Public): `ion.popescu@sports.com` / `User123!`
- **Maria Ionescu** (Privat): `maria.ionescu@sports.com` / `User123!`
- **Andrei Stan** (Public): `andrei.stan@sports.com` / `User123!`

## Structura Proiectului

```
SportsAPP/
├── Controllers/         # Controller-e MVC
├── Models/             # Entități și modele de date
├── Views/              # View-uri Razor
├── Data/               # DbContext și Migrări
├── Areas/Identity/     # Pagini de autentificare
├── wwwroot/            # Fișiere statice (CSS, JS, uploads)
└── Services/           # Servicii (AI Moderation, etc.)
```

## Arhitectură și Design Patterns

- **MVC Pattern:** Separare clară între logică, date și prezentare
- **Repository Pattern:** Acces la date prin Entity Framework Core
- **Dependency Injection:** Servicii injectate prin ASP.NET Core DI
- **Service Layer:** Logică business separată (ex: Content Moderation)

## Privacy și Securitate

- Validare server-side și client-side pentru toate formularele
- Protecție CSRF cu Anti-Forgery Tokens
- Autorizare bazată pe roluri și ownership
- Privacy control pentru profiluri și postări
- Filtrare automată conținut ofensator

## Performance

- Eager Loading pentru entități relacionate (Include/ThenInclude)
- Paginare pentru listări mari de utilizatori
- Index-uri pe cheile străine în baza de date
- CSS/JS minificat în producție

## Browser Support

- Chrome/Edge (recomandat)
- Firefox
- Safari
- Dark mode funcționează pe toate browser-ele moderne

## Contribuții

Acest proiect a fost dezvoltat ca parte a cursului de Dezvoltarea Aplicațiilor Web.

## Licență

Acest proiect este dezvoltat în scop educațional.
