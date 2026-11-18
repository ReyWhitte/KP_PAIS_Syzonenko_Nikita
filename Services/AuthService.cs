using System.Security.Cryptography;
using AgroGuid.Models;
using AgroGuid.Models.Local;
namespace AgroGuid.Services;
public class AuthService {
 private readonly SQLiteService _db = new();
 public async Task<string> RegisterAsync(string fullName,string email,string login,string password,string confirmPassword) {
  fullName=(fullName??"").Trim();email=(email??"").Trim().ToLowerInvariant();login=(login??"").Trim().ToLowerInvariant();
  if (string.IsNullOrWhiteSpace(fullName)||string.IsNullOrWhiteSpace(email)||string.IsNullOrWhiteSpace(login)||string.IsNullOrWhiteSpace(password)||string.IsNullOrWhiteSpace(confirmPassword)) return "Заповніть усі поля.";
  if (login.Length<3) return "Логін повинен містити щонайменше 3 символи.";
  if (password.Length<8) return "Пароль повинен містити щонайменше 8 символів.";
  if (password!=confirmPassword) return "Паролі не збігаються.";
  await _db.InitializeAsync();
  if (await _db.Database.Table<LocalUser>().Where(x=>x.Login==login||x.Email==email).CountAsync()>0) return "Користувач із таким логіном або поштою вже існує.";
  byte[] salt=RandomNumberGenerator.GetBytes(16);
  byte[] hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,210000,HashAlgorithmName.SHA256,32);
  var user=new LocalUser { Name=fullName, Email=email, Login=login, PasswordHash=$"pbkdf2-sha256:210000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}" };
  try { await _db.Database.InsertAsync(user); return ""; }
  catch(SQLite.SQLiteException) { return "Користувач із таким логіном або поштою вже існує."; }
 }
 public async Task<ApiUser?> LoginAsync(string loginOrEmail,string password) {
  if (string.IsNullOrWhiteSpace(loginOrEmail)||string.IsNullOrWhiteSpace(password)) return null;
  await _db.InitializeAsync();
  string key=loginOrEmail.Trim().ToLowerInvariant();
  var user=await _db.Database.Table<LocalUser>().Where(x=>x.Login==key||x.Email==key).FirstOrDefaultAsync();
  if(user==null) return null;
  try {
   string[] parts=user.PasswordHash.Split(':');
   if(parts.Length!=4||parts[0]!="pbkdf2-sha256") return null;
   byte[] salt=Convert.FromBase64String(parts[2]); byte[] expected=Convert.FromBase64String(parts[3]);
   byte[] actual=Rfc2898DeriveBytes.Pbkdf2(password,salt,int.Parse(parts[1]),HashAlgorithmName.SHA256,expected.Length);
   if(!CryptographicOperations.FixedTimeEquals(actual,expected)) return null;
  } catch(FormatException) { return null; }
  await SessionStore.SaveAsync(user.Id.ToString());
  return new ApiUser { Id=user.Id, Name=user.Name, Email=user.Email, Login=user.Login };
 }
}
