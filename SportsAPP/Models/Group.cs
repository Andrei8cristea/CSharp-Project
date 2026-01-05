using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele grupului este obligatoriu")]
        [StringLength(100, ErrorMessage = "Numele grupului nu poate depăși 100 de caractere")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(500, ErrorMessage = "Descrierea nu poate depăși 500 de caractere")]
        public string Description { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        // ModeratorId is set by controller, not by form
        public string? ModeratorId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser? Moderator { get; set; }
        public virtual ICollection<GroupMember> Members { get; set; } = [];
        public virtual ICollection<GroupMessage> Messages { get; set; } = [];
    }
}
