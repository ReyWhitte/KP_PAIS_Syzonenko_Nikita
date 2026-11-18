using SQLite;
namespace AgroGuid.Models.Local;
[Table("crops")]
public class LocalCrop {
 [PrimaryKey, AutoIncrement] public int Id { get; set; }
 [Indexed(Unique = true)] public string Name { get; set; } = "";
 public string Category { get; set; } = "";
 public string Description { get; set; } = "";
 public string Features { get; set; } = "";
 public string CropType { get; set; } = "";
}
