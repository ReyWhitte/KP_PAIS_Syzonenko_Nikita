
using AgroGuid.Services;

namespace AgroGuid.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    // Вхід до акаунта
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        LoginButton.IsEnabled = false;
        StatusLabel.Text = "";

        try
        {
            var user = await _authService.LoginAsync(
                LoginEntry.Text ?? "",
                PasswordEntry.Text ?? "");

            if (user == null)
            {
                StatusLabel.Text =
                    "Неправильний логін, пошта або пароль.";
                return;
            }

            Application.Current!.Windows[0].Page = new HomePage(user.Name, user.Id);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка",
                "Не вдалося виконати вхід: " + ex.Message,
                "OK");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    // Перехід до реєстрації
    private void OnRegisterClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new RegisterPage();
    }
}
