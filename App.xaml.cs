using AgroGuid.Pages;
using AgroGuid.Services;
#if WINDOWS
using Microsoft.UI.Windowing;
#endif
namespace AgroGuid;
public partial class App : Application
{
    public App() { InitializeComponent(); }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loading = new ContentPage {
            BackgroundColor = Color.FromArgb("#F3F6F4"),
            Content = new VerticalStackLayout {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children = { new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#174B32") },
                    new Label { Text = "AgroGuid — перевірка входу...", TextColor = Color.FromArgb("#174B32") } }
            }
        };
        var window = new Window(loading);
        window.Created += async (_, _) =>
        {
#if WINDOWS
            var native = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (native?.AppWindow.Presenter is OverlappedPresenter presenter)
                presenter.Maximize();
#endif
            try
            {
                var user = await SessionStore.RestoreAsync();
                window.Page = user == null ? new LoginPage() : new HomePage(user.Name, user.Id);
            }
            catch (Exception)
            {
                // Якщо виникла помилка локального сховища, відкриваємо вхід.
                window.Page = new LoginPage();
            }
        };
        return window;
    }
}
