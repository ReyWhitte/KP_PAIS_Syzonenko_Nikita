using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AgroGuid.API.Models;
[Table("auth_sessions")]
public class AuthSession
{
    [Key, Column("id")] public long Id { get; set; }
    [Column("user_id")] public long UserId { get; set; }
    [Required, Column("token_hash")] public string TokenHash { get; set; } = "";
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
