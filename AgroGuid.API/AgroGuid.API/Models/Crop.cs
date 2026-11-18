using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroGuid.API.Models;

[Table("crops")]
public class Crop
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("features")]
    public string? Features { get; set; }

    [Column("crop_type")]
    public string? CropType { get; set; }
}
