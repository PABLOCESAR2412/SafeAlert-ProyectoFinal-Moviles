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

    private int _currentTab = -1;
    private readonly ShakeDetector _shakeDetector = new();
    private bool _monitoringMovement;
    private bool _movementPromptOpen;

    public MainPage(string? initialTab = null)
    {
        InitializeComponent();
        _shakeDetector.Detected += MovementDetected;

        _views = new View[] { _homeView, _alertView, _contactsView, _historyView, _profileView };
        _tabIcons = new[] { Icon0, Icon1, Icon2, Icon3, Icon4 };
        _tabLabels = new[] { TabLabel0, TabLabel1, TabLabel2, TabLabel3, TabLabel4 };
        _tabIndicators = new[] { Indicator0, Indicator1, Indicator2, Indicator3, Indicator4 };

        _homeView.NavigateRequested += SwitchTab;
        _alertView.NavigateRequested += SwitchTab;
        _alertView.AlertCreated += () =>
        {
            _homeView.RefreshView();
            _historyView.RefreshView();
        };

        _contactsView.ContactsChanged += () => _homeView.RefreshView();
        _profileView.LogoutRequested += Logout;
        _profileView.MovementSettingChanged += () =>
        {
            if (PermissionService.MovementEnabled)
                StartMovementMonitoring(this, EventArgs.Empty);
            else
                StopMovementMonitoring(this, EventArgs.Empty);
        };
        _profileView.ProfileUpdated += () =>
        {
            UpdateHeader();
            _homeView.RefreshView();
        };


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
        if (Window is { } window)
        {
            window.Activated += StartMovementMonitoring;
            window.Deactivated += StopMovementMonitoring;
        }
        StartMovementMonitoring(this, EventArgs.Empty);
        UpdateHeader();


        await RefreshLocationAsync();
    }

    protected override void OnDisappearing()
    {
        StopMovementMonitoring(this, EventArgs.Empty);
        if (Window is { } window)
        {
            window.Activated -= StartMovementMonitoring;
            window.Deactivated -= StopMovementMonitoring;
        }
        base.OnDisappearing();
    }

    private void StartMovementMonitoring(object? sender, EventArgs e)
    {
        if (PermissionService.MovementEnabled && AppSession.Account is not null)
            _monitoringMovement = _shakeDetector.Start();
    }

    private void StopMovementMonitoring(object? sender, EventArgs e)
    {
        _monitoringMovement = false;
        _shakeDetector.Stop();
    }

    private async void MovementDetected(object? sender, EventArgs e)
    {
        if (!_monitoringMovement || _movementPromptOpen || AppSession.Account is null)
            return;

        _movementPromptOpen = true;
        try
        {
            var openDialer = await DisplayAlert(
                "Movimiento brusco detectado",
                "¿Necesitas ayuda? Si eliges llamar, se abrirá el marcador con el 911.",
                "Llamar al 911",
                "Estoy bien");

            if (openDialer)
                await _homeView.OpenEmergencyDialerAsync();
        }
        finally
        {
            _movementPromptOpen = false;
        }
    }

    private async Task RefreshLocationAsync()
    {
        if (!await LocationService.RefreshAsync())
            return;
        _homeView.RefreshView();
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

        AppSession.Logout();

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new LoginPage();
    }
}
