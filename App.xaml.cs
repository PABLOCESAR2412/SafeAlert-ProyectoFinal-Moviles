using SafeAlert.Services;
using SafeAlert.Views;

namespace SafeAlert;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var args = Environment.GetCommandLineArgs();

        // Permite forzar la pantalla de acceso aunque exista una sesión guardada.
        if (args.Any(arg => arg.Equals("--login", StringComparison.OrdinalIgnoreCase)))
            AppSession.Logout();

        // Restaura la sesión guardada para no pedir credenciales en cada apertura.
        Page root = AppSession.TryRestoreSession() ? new MainPage() : new LoginPage();

        // Modo de demostración para pruebas y capturas:
        // SafeAlert.exe --demo --tab=home|alert|contacts|history|profile
        if (args.Any(arg => arg.Equals("--demo", StringComparison.OrdinalIgnoreCase)) &&
            AppSession.TryLoginDemo(out _))
        {
            var tabArg = args.FirstOrDefault(arg =>
                arg.StartsWith("--tab=", StringComparison.OrdinalIgnoreCase));
            root = new MainPage(tabArg?.Split('=', 2).ElementAtOrDefault(1));
        }

        return new Window(root)
        {
            Title = "SafeAlert - Seguridad personal",
            Width = 440,
            Height = 920
        };
    }
}
