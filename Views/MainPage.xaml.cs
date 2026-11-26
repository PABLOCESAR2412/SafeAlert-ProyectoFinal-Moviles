using Microsoft.Maui.ApplicationModel;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class MainPage : ContentPage
{
    private readonly HomeView _homeView = new();
    private readonly AlertView _alertView = new();
    private readonly ContactsView _contactsView = new();
    private readonly HistoryView _historyView = new();
    private readonly ProfileView _profileView = new();
    private readonly View[] _views;
    private readonly Label[] _tabIcons;
    private readonly Label[] _tabLabels;
    private readonly BoxView[] _tabIndicators;
    private readonly ShakeDetector _shakeDetector = new();

    private int _currentTab = -1;
    private bool _shakePromptOpen;
    private bool _sensorInfoShown;
    private UpdateInfo? _pendingUpdate;

    public MainPage(string? initialTab = null)
    {
        InitializeComponent();

        _views = new View[] { _homeView, _alertView, _contactsView, _historyView, _profileView };
        _tabIcons = new[] { Icon0, Icon1, Icon2, Icon3, Icon4 };
        _tabLabels = new[] { TabLabel0, TabLabel1, TabLabel2, TabLabel3, TabLabel4 };
        _tabIndicators = new[] { Indicator0, Indicator1, Indicator2, Indicator3, Indicator4 };

        _homeView.NavigateRequested += SwitchTab;
        _homeView.EmergencyTypeRequested += type =>
        {
            _alertView.SelectType(type);
            SwitchTab(1);
        };
        _homeView.AlertSent += () => _historyView.RefreshView();

        _alertView.NavigateRequested += SwitchTab;
        _alertView.AlertCreated += () =>
        {
            _homeView.RefreshView();
            _historyView.RefreshView();
        };

        _contactsView.ContactsChanged += () => _homeView.RefreshView();
        _contactsView.AlertCreated += () =>
        {
            _homeView.RefreshView();
            _historyView.RefreshView();
        };

        _profileView.LogoutRequested += Logout;
        _profileView.ShakeSettingChanged += ShakeSettingChanged;
        _profileView.SimulateShakeRequested += SimulateShake;
        _profileView.UpdateCheckRequested += () => _ = CheckUpdateAsync();
        _profileView.ProfileUpdated += () =>
        {
            UpdateHeader();
            _homeView.RefreshView();
        };

        _shakeDetector.Detected += ShakeDetected;

        UpdateHeader();

        var initial = initialTab?.Trim().ToLowerInvariant() switch
        {
            "alert" or "alerta" => 1,
            "contacts" or "contactos" => 2,
            "history" or "historial" => 3,
            "profile" or "perfil" => 4,
            _ => 0
        };

        SwitchTab(initial);
    }

    public void SwitchTab(int index)
    {
        if (_views.Length == 0)
            return;

        if (index < 0 || index >= _views.Length)
            index = 0;

        _currentTab = index;
        ContentHost.Content = _views[index];

        for (var i = 0; i < _views.Length; i++)
        {
            var active = i == index;
            _tabIcons[i].TextColor = Color.FromArgb(active ? "#1F3864" : "#6B7280");
            _tabLabels[i].TextColor = Color.FromArgb(active ? "#1F3864" : "#6B7280");
            _tabLabels[i].FontFamily = active ? "InterBold" : "InterMedium";
            _tabIndicators[i].Color = active ? Color.FromArgb("#0074D5") : Colors.Transparent;
        }

        if (_views[index] is IRefreshable refreshable)
            refreshable.RefreshView();
    }

    private void Tab_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string text && int.TryParse(text, out var index))
            SwitchTab(index);
    }

    private void HeaderAvatar_Tapped(object sender, TappedEventArgs e) => SwitchTab(4);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateHeader();

        if (AppSession.Account?.Settings.ShakeDetection == true)
            _shakeDetector.Start();

        await RefreshLocationAsync();
        await CheckUpdateAsync();
    }

    private async Task RefreshLocationAsync()
    {
        if (!await PermissionService.EnsureLocationAsync())
            return;

        await LocationService.RefreshAsync();
        _homeView.RefreshView();
    }

    private async Task CheckUpdateAsync()
    {
        _pendingUpdate = await UpdateService.CheckAsync();

        if (_pendingUpdate == null)
        {
            UpdateBanner.IsVisible = false;
            return;
        }

        UpdateBannerText.Text = $"Versión {_pendingUpdate.LatestVersion} disponible.";
        UpdateBanner.IsVisible = true;
    }

    private async void UpdateBanner_Clicked(object sender, EventArgs e)
    {
        if (_pendingUpdate == null)
        {
            UpdateBanner.IsVisible = false;
            return;
        }

        var info = _pendingUpdate;
        UpdateService.MarkSeen(info.LatestVersion);
        UpdateBanner.IsVisible = false;

        var open = await DisplayAlert(
            "Actualizar SafeAlert",
            $"Versión instalada: {AppInfo.Current.VersionString}\nVersión disponible: {info.LatestVersion}\n\n{info.Message}\n\nDescarga la nueva APK desde el enlace del administrador y ábrela. Android actualizará SafeAlert conservando tus datos.",
            "Descargar",
            "Ahora no");

        if (!open || string.IsNullOrWhiteSpace(info.DownloadUrl))
            return;

        try
        {
            await Launcher.Default.OpenAsync(new Uri(info.DownloadUrl));
        }
        catch
        {
            await DisplayAlert("Actualización", "No se pudo abrir el enlace de descarga.", "Aceptar");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _shakeDetector.Stop();
    }

    private void UpdateHeader()
    {
        if (AppSession.Account is { } account)
            HeaderAvatarLabel.Text = account.Profile.Initials;
    }

    private async void Logout()
    {
        var confirm = await DisplayAlert(
            "Cerrar sesión",
            "Tu sesión se cerrará de forma segura en este dispositivo.",
            "Cerrar sesión",
            "Cancelar");

        if (!confirm)
            return;

        _shakeDetector.Stop();
        AppSession.Logout();

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new LoginPage();
    }

    private async void ShakeSettingChanged(bool enabled)
    {
        if (!enabled)
        {
            _shakeDetector.Stop();
            return;
        }

        if (_shakeDetector.Start())
        {
            if (_sensorInfoShown)
                return;

            _sensorInfoShown = true;
            await DisplayAlert(
                "Sensor de movimiento activo",
                "SafeAlert usará el acelerómetro para detectar sacudidas fuertes. En Android el acelerómetro no requiere un permiso especial: solo se lee mientras la app está abierta y no se guarda el movimiento continuo.",
                "Entendido");
            return;
        }

        if (_sensorInfoShown)
            return;

        _sensorInfoShown = true;
        await DisplayAlert(
            "Acelerómetro no disponible",
            "Este equipo Windows no reporta un acelerómetro. La detección de sacudidas ya está implementada y se activará automáticamente en un teléfono Android o iOS. Puedes usar \"Probar sensor\" para simular el evento.",
            "Entendido");
    }

    private void SimulateShake() => _ = HandleShakeAsync();

    private async void ShakeDetected(object? sender, EventArgs e) => await HandleShakeAsync();

    private async Task HandleShakeAsync()
    {
        if (_shakePromptOpen)
            return;

        _shakePromptOpen = true;
        var send = await DisplayAlert(
            "Movimiento brusco detectado",
            "El acelerómetro registró una sacudida. ¿Deseas registrar una alerta SOS para tus contactos? No se realizarán llamadas ni envíos externos.",
            "Enviar SOS",
            "Cancelar");
        _shakePromptOpen = false;

        if (!send)
            return;

        var alert = AlertService.Send(
            EmergencyType.Panic,
            "Alerta SOS activada por detección de sacudida (acelerómetro).",
            LocationService.Address,
            LocationService.Latitude,
            LocationService.Longitude);

        _homeView.RefreshView();
        _historyView.RefreshView();
        SwitchTab(3);

        await DisplayAlert(
            "Alerta enviada",
            $"Alerta SOS registrada para {alert.NotifiedContacts} contacto(s). No se realizó una llamada ni envío externo.",
            "Aceptar");
    }
}
