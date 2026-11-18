
namespace AgroGuid.Models;

// Дані користувача, отримані з PostgreSQL через API
public class ApiUser
{
    public long Id { get; set; }

    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public string Login { get; set; } = "";

    public string Token { get; set; } = "";

    public string Message { get; set; } = "";
}
