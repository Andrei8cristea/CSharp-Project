using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportsAPP.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Proprietati de baza pentru profil
        [StringLength(50, ErrorMessage = "Prenumele nu poate avea mai mult de 50 de caractere")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Numele nu poate avea mai mult de 50 de caractere")]
        public string? LastName { get; set; }

        [StringLength(500, ErrorMessage = "Descrierea nu poate avea mai mult de 500 de caractere")]
        public string? ProfileDescription { get; set; }

        // Calea catre imaginea de profil
        public string? ProfileImagePath { get; set; }

        // Public sau privat
        public bool IsPublic { get; set; } = true; // default: public

        // Proprietate calculata pentru numele complet
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        // Relatii
        // Un user poate posta mai multe postari
        public virtual ICollection<Post> Posts { get; set; } = [];

        // Un user poate posta mai multe comentarii
        public virtual ICollection<Comment> Comments { get; set; } = [];

        // Pentru popularea unui dropdown list cu roluri
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}
