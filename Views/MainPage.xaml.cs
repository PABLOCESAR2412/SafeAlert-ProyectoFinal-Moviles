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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateHeader();

        if (AppSession.Account?.Settings.ShakeDetection == true)
            _shakeDetector.Start();
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
            return;

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
            "El acelerómetro registró una sacudida. ¿Deseas enviar una alerta SOS a tus contactos y a la central 911?",
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
            $"Se notificó a {alert.NotifiedContacts} contacto(s) y a la central 911 con tu ubicación en tiempo real.",
            "Aceptar");
    }
}
