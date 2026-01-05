using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportsAPP.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime LikedAt { get; set; }

        // Navigation properties
        public virtual Post? Post { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
