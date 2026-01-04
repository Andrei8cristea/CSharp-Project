using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    public class FollowRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }  // Who sent the request

        [Required]
        public string ReceiverId { get; set; }  // Who receives the request

        public DateTime RequestedAt { get; set; }

        [Required]
        public FollowRequestStatus Status { get; set; } = FollowRequestStatus.Pending;

        // Navigation properties
        public virtual ApplicationUser? Sender { get; set; }
        public virtual ApplicationUser? Receiver { get; set; }
    }

    public enum FollowRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2
    }
}
