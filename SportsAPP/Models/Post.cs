using System.ComponentModel.DataAnnotations;

namespace SportsAPP.Models
{
    // Enum pentru tipul de media
    public enum MediaType
    {
        Text,
        Image,
        Video
    }

    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(200, ErrorMessage = "Titlul nu poate avea mai mult de 200 de caractere")]
        [MinLength(5, ErrorMessage = "Titlul trebuie sa aiba cel putin 5 caractere")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Continutul este obligatoriu")]
        public string Content { get; set; }

        // Tipul de media: Text, Image sau Video
        [Required(ErrorMessage = "Tipul de media este obligatoriu")]
        public MediaType MediaType { get; set; } = MediaType.Text;

        // Calea catre fisierul media (pentru imagini sau videoclipuri)
        public string? MediaPath { get; set; }

        public DateTime Date { get; set; }

        // Cheie externa - un post este creat de un user
        public string? UserId { get; set; }

        // Proprietatea de navigatie - un post este creat de un user
        public virtual ApplicationUser? User { get; set; }

        // Un post poate avea mai multe comentarii
        public virtual ICollection<Comment> Comments { get; set; } = [];
    }
}
