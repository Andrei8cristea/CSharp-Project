using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    public class Follow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FollowerId { get; set; }  // Who is following

        [Required]
        public string FollowingId { get; set; }  // Who is being followed

        public DateTime FollowedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser? Follower { get; set; }
        public virtual ApplicationUser? Following { get; set; }
    }
}
