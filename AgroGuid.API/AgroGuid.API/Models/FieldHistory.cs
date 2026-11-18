using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroGuid.API.Models;

[Table("field_history")]
public class FieldHistory
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Required]
    [Column("field_name")]
    public string FieldName { get; set; } = "";

    [Required]
    [Column("action")]
    public string Action { get; set; } = "";

    [Column("description")]
    public string Description { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
