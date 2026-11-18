using SQLite;
namespace AgroGuid.Models.Local;
[Table("users")]
public class LocalUser {
 [PrimaryKey, AutoIncrement] public int Id { get; set; }
 [Indexed(Unique = true)] public string Login { get; set; } = "";
 [Indexed(Unique = true)] public string Email { get; set; } = "";
 public string Name { get; set; } = "";
 public string PasswordHash { get; set; } = "";
}
