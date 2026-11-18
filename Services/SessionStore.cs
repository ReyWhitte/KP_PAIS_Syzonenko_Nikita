using AgroGuid.Models;
using AgroGuid.Models.Local;
namespace AgroGuid.Services;
public static class SessionStore {
 private const string Key = "agroguid_offline_user_id";
 public static Task SaveAsync(string token) { Preferences.Default.Set(Key, token); return Task.CompletedTask; }
 public static Task<string?> GetTokenAsync() => Task.FromResult<string?>(Preferences.Default.Get(Key, ""));
 public static async Task<ApiUser?> RestoreAsync() {
  string token = Preferences.Default.Get(Key, "");
  if (!int.TryParse(token, out var id) || id <= 0) return null;
  var db = new SQLiteService(); await db.InitializeAsync();
  var user = await db.Database.Table<LocalUser>().Where(x => x.Id == id).FirstOrDefaultAsync();
  if (user == null) { Preferences.Default.Remove(Key); return null; }
  return new ApiUser { Id=user.Id, Name=user.Name, Email=user.Email, Login=user.Login };
 }
 public static Task LogoutAsync() { Preferences.Default.Remove(Key); return Task.CompletedTask; }
 public static Task AuthorizeAsync(HttpRequestMessage request) => Task.CompletedTask;
}
