using SQLite;
namespace AgroGuid.Models.Local;
[Table("fields")]
public class LocalField {
 [PrimaryKey, AutoIncrement] public int Id { get; set; }
 [Indexed] public int UserId { get; set; }
 public string Name { get; set; } = "";
 public string CropName { get; set; } = "";
 public string GeometryJson { get; set; } = "";
 public double AreaHa { get; set; }
 public string Notes { get; set; } = "";
}
