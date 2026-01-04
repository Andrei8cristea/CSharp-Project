using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul comentariului este obligatoriu")]
        [StringLength(1000, ErrorMessage = "Comentariul nu poate avea mai mult de 1000 de caractere")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        // Cheie externa - un comentariu apartine unui post
        [Required(ErrorMessage = "PostId este obligatoriu")]
        public int PostId { get; set; }

        // Proprietatea de navigatie - un comentariu apartine unui post
        public virtual Post? Post { get; set; }

        // Cheie externa - un comentariu este scris de un user
        public string? UserId { get; set; }

        // Proprietatea de navigatie - un comentariu este scris de un user
        public virtual ApplicationUser? User { get; set; }
    }
}
