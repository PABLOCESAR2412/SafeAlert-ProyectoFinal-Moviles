using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class HomeView : ContentView, IRefreshable
{
    private IDispatcherTimer? _sosHoldTimer;
    private int _sosHoldTicks;
    private bool _sosTriggered;

    private IDispatcherTimer? _countdownTimer;
    private int _countdown;

    private IDispatcherTimer? _walkTimer;
    private int _walkRemaining;
    private bool _walkActive;

    public event Action<int>? NavigateRequested;
    public event Action<EmergencyType>? EmergencyTypeRequested;
    public event Action? AlertSent;

    public HomeView()
    {
        InitializeComponent();
    }

    public void RefreshView()
    {
        if (AppSession.Account is not { } account)
            return;

        DeviceStatus.Refresh();

        AddressLabel.Text = LocationService.Address;
        PrecisionLabel.Text = $"Precisión exacta: {LocationService.Precision.TrimStart('±')}";
        NetworkLabel.Text = DeviceStatus.Network;
        GpsLabel.Text = "GPS Activo";
        BatteryLabel.Text = DeviceStatus.BatteryLevel;

        HomeContactsTitle.Text = account.Contacts.Count switch
        {
            0 => "Sin contactos enlazados",
            1 => "1 Contacto enlazado",
            _ => $"{account.Contacts.Count} Contactos enlazados"
        };

        HomeContactsSubtitle.Text = account.Contacts.Count >= 3
            ? "Listos para recibir alerta y mapa en vivo"
            : "Completa tu red con al menos 3 contactos";

        BuildHomeAvatars(account.Contacts);
    }

    private void BuildHomeAvatars(List<EmergencyContact> contacts)
    {
        HomeAvatars.Children.Clear();

        var visible = contacts.Take(3).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            var contact = visible[i];
            var avatar = new Border
            {
                WidthRequest = 32,
                HeightRequest = 32,
                BackgroundColor = Color.FromArgb(contact.AvatarColor),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Margin = new Thickness(i == 0 ? 0 : -8, 0, 0, 0),
                Content = new Label
                {
                    Text = contact.Initials,
                    FontFamily = "InterBold",
                    FontSize = 10,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };

            HomeAvatars.Children.Add(avatar);
        }

        if (contacts.Count > 3)
        {
            HomeAvatars.Children.Add(new Border
            {
                WidthRequest = 32,
                HeightRequest = 32,
                BackgroundColor = Color.FromArgb("#E5E7EB"),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Margin = new Thickness(-8, 0, 0, 0),
                Content = new Label
                {
                    Text = $"+{contacts.Count - 3}",
                    FontFamily = "InterBold",
                    FontSize = 10,
                    TextColor = Color.FromArgb("#4B5563"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }

        if (contacts.Count == 0)
        {
            HomeAvatars.Children.Add(new Border
            {
                WidthRequest = 34,
                HeightRequest = 34,
                BackgroundColor = Color.FromArgb("#DBEAFE"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 17 },
                Content = new Label
                {
                    Text = Icons.Add,
                    FontFamily = "MaterialIcons",
                    FontSize = 18,
                    TextColor = Color.FromArgb("#0074D5"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }
    }

    private async void RefreshLocation_Clicked(object sender, EventArgs e)
    {
        RefreshLocationButton.IsEnabled = false;
        AddressLabel.Text = "Actualizando ubicación...";

        try
        {
            var granted = await PermissionService.EnsureLocationAsync();
            if (!granted)
            {
                await Ui.Alert(
                    "Permiso de ubicación",
                    "Debes permitir la ubicación precisa para actualizar tu posición. Se conserva la ubicación demostrativa hasta que se conceda.",
                    "Entendido");
                return;
            }

            var success = await LocationService.RefreshAsync();
            AddressLabel.Text = LocationService.Address;
            PrecisionLabel.Text = $"Precisión exacta: {LocationService.Precision.TrimStart('±')}";

            if (!success)
            {
                await Ui.Alert(
                    "GPS no disponible",
                    "No se pudo leer la ubicación del dispositivo. Se mantiene la ubicación de demostración para que puedas probar la app.",
                    "Entendido");
            }
        }
        finally
        {
            RefreshLocationButton.IsEnabled = true;
        }
    }

    private async void TestStatus_Clicked(object sender, EventArgs e)
    {
        DeviceStatus.Refresh();
        var sensor = Accelerometer.Default.IsSupported
            ? "Acelerómetro disponible"
            : "Acelerómetro no disponible en este equipo";

        await Ui.Alert(
            "Estado operativo",
            $"Red: {DeviceStatus.Network}\nGPS: GPS Activo\nBatería: {DeviceStatus.BatteryLevel}\n{sensor}\nÚltima prueba: hace 5 minutos",
            "Aceptar");
    }

    private void GoContacts_Tapped(object sender, TappedEventArgs e) => NavigateRequested?.Invoke(2);

    private void Medical_Tapped(object sender, TappedEventArgs e) =>
        EmergencyTypeRequested?.Invoke(EmergencyType.Medical);

    private void Police_Tapped(object sender, TappedEventArgs e) =>
        EmergencyTypeRequested?.Invoke(EmergencyType.Police);

    private async void SilentAlert_Tapped(object sender, TappedEventArgs e)
    {
        var confirm = await Ui.Confirm(
            "Alerta Silenciosa",
            "Se enviará una alerta a tu red de apoyo sin sonido ni luces. ¿Deseas continuar?",
            "Activar alerta",
            "Cancelar");

        if (!confirm)
            return;

        var alert = AlertService.Send(
            EmergencyType.Silent,
            EmergencyCatalog.DefaultMessage(EmergencyType.Silent),
            LocationService.Address,
            LocationService.Latitude,
            LocationService.Longitude,
            notes: "Alerta silenciosa emitida desde un acceso rápido.");

        SuccessMessage.Text = $"Alerta silenciosa registrada para {alert.NotifiedContacts} contacto(s). No se enviaron mensajes externos.";
        SuccessOverlay.IsVisible = true;
        AlertSent?.Invoke();
    }

    private void WalkSafe_Tapped(object sender, TappedEventArgs e)
    {
        _walkRemaining = 60;
        _walkActive = true;
        WalkTimerLabel.Text = "01:00";
        WalkSafeOverlay.IsVisible = true;

        _walkTimer?.Stop();
        _walkTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_walkTimer == null)
            return;

        _walkTimer.Interval = TimeSpan.FromSeconds(1);
        _walkTimer.Tick += WalkTimer_Tick;
        _walkTimer.Start();
    }

    private void WalkTimer_Tick(object? sender, EventArgs e)
    {
        if (!_walkActive)
            return;

        _walkRemaining--;

        if (_walkRemaining <= 0)
        {
            StopWalkTimer();
            WalkSafeOverlay.IsVisible = false;
            SendWalkSafeAlert(
                "No confirmé mi llegada a salvo. Envíen ayuda a mi ubicación actual.",
                automatic: true);
            return;
        }

        WalkTimerLabel.Text = TimeSpan.FromSeconds(_walkRemaining).ToString(@"mm\:ss");
    }

    private void StopWalkTimer()
    {
        _walkActive = false;
        if (_walkTimer == null)
            return;

        _walkTimer.Stop();
        _walkTimer.Tick -= WalkTimer_Tick;
        _walkTimer = null;
    }

    private async void WalkSafeOk_Clicked(object sender, EventArgs e)
    {
        StopWalkTimer();
        WalkSafeOverlay.IsVisible = false;

        AlertService.Send(
            EmergencyType.WalkSafe,
            "Llegué a salvo a mi destino. Check-in completado.",
            LocationService.Address,
            LocationService.Latitude,
            LocationService.Longitude,
            AlertStatus.Resolved,
            responseMinutes: 1,
            notes: "Check-in de Camina Seguro completado por la usuaria.");

        AlertSent?.Invoke();

        await Ui.Alert(
            "Llegada confirmada",
            "Tu red de apoyo recibió la confirmación de que llegaste a salvo. El seguimiento se detuvo.",
            "Aceptar");
    }

    private void WalkSafeAlert_Clicked(object sender, EventArgs e)
    {
        StopWalkTimer();
        WalkSafeOverlay.IsVisible = false;
        SendWalkSafeAlert(
            "Activo una alerta desde Camina Seguro. Necesito apoyo en mi ubicación actual.",
            automatic: false);
    }

    private async void SendWalkSafeAlert(string message, bool automatic)
    {
        var alert = AlertService.Send(
            EmergencyType.WalkSafe,
            message,
            LocationService.Address,
            LocationService.Latitude,
            LocationService.Longitude,
            notes: automatic
                ? "Alerta automática: no se confirmó la llegada antes de la cuenta regresiva."
                : "Alerta manual desde el modo Camina Seguro.");

        await SharingService.DispatchAsync(
            AppSession.Account!,
            message,
            LocationService.Latitude,
            LocationService.Longitude);

        SuccessMessage.Text = $"Alerta de Camina Seguro registrada para {alert.NotifiedContacts} contacto(s). Revisa WhatsApp para confirmar el envío.";
        SuccessOverlay.IsVisible = true;
        AlertSent?.Invoke();
    }

    private void Sos_Pressed(object sender, EventArgs e)
    {
        if (SosCountdownOverlay.IsVisible || SuccessOverlay.IsVisible || WalkSafeOverlay.IsVisible)
            return;

        _sosTriggered = false;
        _sosHoldTicks = 0;
        SosProgress.Progress = 0;
        SosProgress.IsVisible = true;

        StopSosHold();
        _sosHoldTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_sosHoldTimer == null)
            return;

        _sosHoldTimer.Interval = TimeSpan.FromMilliseconds(100);
        _sosHoldTimer.Tick += SosHold_Tick;
        _sosHoldTimer.Start();

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch
        {
            // El equipo puede no tener retroalimentación háptica.
        }
    }

    private void SosHold_Tick(object? sender, EventArgs e)
    {
        _sosHoldTicks++;
        SosProgress.Progress = Math.Min(1, _sosHoldTicks / 30.0);

        if (_sosHoldTicks < 30)
            return;

        StopSosHold();
        _sosTriggered = true;
        SosProgress.IsVisible = false;
        StartSosCountdown();
    }

    private void StopSosHold()
    {
        if (_sosHoldTimer == null)
            return;

        _sosHoldTimer.Stop();
        _sosHoldTimer.Tick -= SosHold_Tick;
        _sosHoldTimer = null;
    }

    private void Sos_Released(object sender, EventArgs e)
    {
        StopSosHold();

        if (_sosTriggered)
            return;

        SosProgress.IsVisible = false;
        SosProgress.Progress = 0;
    }

    private void StartSosCountdown()
    {
        _countdown = 3;
        CountdownNumber.Text = "3";
        SosCountdownOverlay.IsVisible = true;

        _countdownTimer?.Stop();
        _countdownTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_countdownTimer == null)
            return;

        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += Countdown_Tick;
        _countdownTimer.Start();
    }

    private void Countdown_Tick(object? sender, EventArgs e)
    {
        _countdown--;

        if (_countdown > 0)
        {
            CountdownNumber.Text = _countdown.ToString();
            return;
        }

        StopCountdown();
        SosCountdownOverlay.IsVisible = false;
        SendSosAlert();
    }

    private void StopCountdown()
    {
        if (_countdownTimer == null)
            return;

        _countdownTimer.Stop();
        _countdownTimer.Tick -= Countdown_Tick;
        _countdownTimer = null;
    }

    private void CancelCountdown_Clicked(object sender, EventArgs e)
    {
        StopCountdown();
        _countdown = 3;
        SosCountdownOverlay.IsVisible = false;
    }

    private async void SendSosAlert()
    {
        var alert = AlertService.Send(
            EmergencyType.Panic,
            EmergencyCatalog.DefaultMessage(EmergencyType.Panic),
            LocationService.Address,
            LocationService.Latitude,
            LocationService.Longitude,
            notes: "Alerta SOS generada desde el botón de pánico tras mantener presionado 3 segundos.");

        var account = AppSession.Account!;
        var share = await SharingService.DispatchAsync(
            account,
            EmergencyCatalog.DefaultMessage(EmergencyType.Panic),
            LocationService.Latitude,
            LocationService.Longitude);

        if (share.HasPendingSms)
        {
            var sendSms = await Ui.Confirm(
                "Alerta SOS registrada",
                share.WhatsAppSent
                    ? $"WhatsApp abierto con {share.PrimaryName} y tu ubicación. Confirma el envío.\n\n¿Enviar también SMS a {share.SmsPending.Count} contacto(s)?"
                    : "No se pudo abrir WhatsApp. ¿Enviar el SMS de alerta a tus contactos?",
                "Enviar SMS",
                "Ahora no");

            if (sendSms)
                await SharingService.ComposeSmsAsync(
                    share.SmsPending,
                    EmergencyCatalog.DefaultMessage(EmergencyType.Panic));
        }

        SuccessMessage.Text = $"Alerta SOS registrada para {alert.NotifiedContacts} contacto(s). Ubicación compartida con {share.PrimaryName}.";
        SuccessOverlay.IsVisible = true;
        AlertSent?.Invoke();
    }

    private void SuccessHistory_Clicked(object sender, EventArgs e)
    {
        SuccessOverlay.IsVisible = false;
        NavigateRequested?.Invoke(3);
    }

    private void SuccessClose_Clicked(object sender, EventArgs e) => SuccessOverlay.IsVisible = false;
}
