using SQLite;
namespace AgroGuid.Models.Local;
[Table("field_history")]
public class LocalFieldHistory {
 [PrimaryKey, AutoIncrement] public int Id { get; set; }
 [Indexed] public int UserId { get; set; }
 public int FieldId { get; set; }
 public string FieldName { get; set; } = "";
 public string Action { get; set; } = "";
 public string Description { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
