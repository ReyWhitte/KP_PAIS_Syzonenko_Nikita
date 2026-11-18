using AgroGuid.Services;

namespace AgroGuid.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService = new();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(
        object sender, EventArgs e)
    {
        RegisterButton.IsEnabled = false;
        StatusLabel.Text = "";

        try
        {
            string error = await _authService.RegisterAsync(
                NameEntry.Text ?? "",
                EmailEntry.Text ?? "",
                LoginEntry.Text ?? "",
                PasswordEntry.Text ?? "",
                ConfirmPasswordEntry.Text ?? "");

            if (error != "")
            {
                StatusLabel.Text = error;
                return;
            }

            await DisplayAlert(
                "AgroGuid",
                "Акаунт успішно створено!",
                "Продовжити");

            Application.Current!.Windows[0].Page = new LoginPage();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка",
                "Не вдалося виконати реєстрацію: " + ex.Message,
                "OK");
        }
        finally
        {
            RegisterButton.IsEnabled = true;
        }
    }
    private void OnLoginClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new LoginPage();
    }
}
