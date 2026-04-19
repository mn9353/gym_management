using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("email_templates")]
    public class EmailTemplate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("template_key")]
        [StringLength(80)]
        public string TemplateKey { get; set; } = string.Empty;

        [Required]
        [Column("subject_template")]
        [StringLength(255)]
        public string SubjectTemplate { get; set; } = string.Empty;

        [Required]
        [Column("html_template")]
        public string HtmlTemplate { get; set; } = string.Empty;

        [Column("hero_image_url")]
        [StringLength(500)]
        public string? HeroImageUrl { get; set; }

        [Column("login_url")]
        [StringLength(500)]
        public string? LoginUrl { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

