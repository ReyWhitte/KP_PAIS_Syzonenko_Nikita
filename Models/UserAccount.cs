using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace AgroGuid.Models;

public class UserAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Email { get; set; } = "";

    [Unique]
    public string Login { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string PasswordSalt { get; set; } = "";
}
