using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportsAPP.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Proprietati de baza pentru profil
        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [StringLength(50, ErrorMessage = "Prenumele nu poate avea mai mult de 50 de caractere")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [StringLength(50, ErrorMessage = "Numele nu poate avea mai mult de 50 de caractere")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Descrierea profilului este obligatorie")]
        [StringLength(500, ErrorMessage = "Descrierea nu poate avea mai mult de 500 de caractere")]
        public string? ProfileDescription { get; set; }

        // Calea catre imaginea de profil
        // Note: Validation for profile image is handled in controller logic
        // to allow editing other fields without re-uploading the image
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

        // Follow relationships
        // People following this user (this user's followers)
        public virtual ICollection<Follow> Followers { get; set; } = [];
        
        // People this user follows (this user is following them)
        public virtual ICollection<Follow> Following { get; set; } = [];

        // Follow requests sent by this user
        public virtual ICollection<FollowRequest> SentFollowRequests { get; set; } = [];
        
        // Follow requests received by this user
        public virtual ICollection<FollowRequest> ReceivedFollowRequests { get; set; } = [];

        // Computed properties for counts
        [NotMapped]
        public int FollowersCount => Followers?.Count ?? 0;

        [NotMapped]
        public int FollowingCount => Following?.Count ?? 0;

        [NotMapped]
        public int PendingRequestsCount => ReceivedFollowRequests?.Count(r => r.Status == FollowRequestStatus.Pending) ?? 0;

        // Pentru popularea unui dropdown list cu roluri
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}
