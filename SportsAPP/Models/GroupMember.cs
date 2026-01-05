using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    public enum MemberStatus
    {
        Pending = 0,
        Approved = 1
    }

    public enum MemberRole
    {
        Member = 0,
        Moderator = 1
    }

    public class GroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public MemberStatus Status { get; set; } = MemberStatus.Pending;

        public MemberRole Role { get; set; } = MemberRole.Member;

        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public virtual Group? Group { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
