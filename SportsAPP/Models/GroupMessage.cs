using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    public class GroupMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesajul nu poate fi gol")]
        [StringLength(2000, ErrorMessage = "Mesajul nu poate avea mai mult de 2000 de caractere")]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        // Navigation properties
        public virtual Group? Group { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
