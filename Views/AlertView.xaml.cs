using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Controls.Shapes;
using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class AlertView : ContentView, IRefreshable
{
    private EmergencyType _type = EmergencyType.Panic;
    private bool _messageIsDefault = true;
    private bool _refreshingLocation;

    public event Action<int>? NavigateRequested;
    public event Action? AlertCreated;

    public AlertView()
    {
        InitializeComponent();
        MessageEditor.Text = EmergencyCatalog.DefaultMessage(_type);
        _messageIsDefault = true;
        CharacterCountLabel.Text = $"{MessageEditor.Text.Length} car.";
    }

    public void RefreshView()
    {
        UpdateLocationLabels();
        BuildRecipientAvatars();

        if (string.IsNullOrWhiteSpace(MessageEditor.Text) || _messageIsDefault)
        {
            MessageEditor.Text = EmergencyCatalog.DefaultMessage(_type);
            _messageIsDefault = true;
            CharacterCountLabel.Text = $"{MessageEditor.Text.Length} car.";
        }
    }

    public void SelectType(EmergencyType type)
    {
        _type = type switch
        {
            EmergencyType.Medical => EmergencyType.Medical,
            EmergencyType.Harassment or EmergencyType.WalkSafe => EmergencyType.Harassment,
            EmergencyType.Robbery or EmergencyType.Police => EmergencyType.Robbery,
            _ => EmergencyType.Panic
        };

        UpdateTypeSelection();
        MessageEditor.Text = EmergencyCatalog.DefaultMessage(_type);
        _messageIsDefault = true;
        CharacterCountLabel.Text = $"{MessageEditor.Text.Length} car.";
    }

    private void UpdateTypeSelection()
    {
        ApplyCard(PanicCard, PanicIcon, PanicTitle, PanicSubtitle, "#B91C1C", _type == EmergencyType.Panic);
        ApplyCard(MedicalCard, MedicalIcon, MedicalTitle, MedicalSubtitle, "#DC2626", _type == EmergencyType.Medical);
        ApplyCard(HarassmentCard, HarassmentIcon, HarassmentTitle, HarassmentSubtitle, "#0074D5", _type == EmergencyType.Harassment);
        ApplyCard(RobberyCard, RobberyIcon, RobberyTitle, RobberySubtitle, "#1F3864", _type == EmergencyType.Robbery);
    }

    private static void ApplyCard(
        Border card,
        Label icon,
        Label title,
        Label subtitle,
        string accent,
        bool active)
    {
        card.BackgroundColor = Color.FromArgb(active ? "#B91C1C" : "#EDEEF0");
        card.Stroke = new SolidColorBrush(Color.FromArgb(active ? "#B91C1C" : "#EDEEF0"));
        icon.TextColor = active ? Colors.White : Color.FromArgb(accent);
        title.TextColor = active ? Colors.White : Color.FromArgb("#111827");
        subtitle.TextColor = active ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#4B5563");
    }

    private void UpdateLocationLabels()
    {
        MapPrecisionLabel.Text = LocationService.PrecisionText;
        AddressLabel.Text = LocationService.Address;
        CoordinatesLabel.Text = LocationService.CoordinatesText;
        NearbyLabel.Text = $"• {LocationService.Nearby}";
        UpdateMapImage();
    }

    private async void UpdateMapImage()
    {
        try
        {
            var lat = LocationService.Latitude.ToString("0.000000", CultureInfo.InvariantCulture);
            var lon = LocationService.Longitude.ToString("0.000000", CultureInfo.InvariantCulture);
            var url = $"https://staticmap.openstreetmap.de/staticmap.php?center={lat},{lon}&zoom=16&size=640x380&maptype=mapnik&markers={lat},{lon},red-pushpin";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return;

            MapImage.Source = ImageSource.FromUri(new Uri(url));
        }
        catch
        {
            // Si el mapa en línea no está disponible se conserva la imagen local.
        }
    }

    private async void CallEmergency_Tapped(object sender, TappedEventArgs e)
    {
        var call = await Ui.Confirm(
            "Llamar al 911",
            "Se abrirá el marcador del teléfono con el número 911. La llamada la confirmas tú.",
            "Abrir marcador",
            "Cancelar");

        if (!call)
            return;

        try
        {
            PhoneDialer.Default.Open("911");
        }
        catch
        {
            await Ui.Alert("Teléfono", "No se pudo abrir el marcador en este dispositivo.", "Aceptar");
        }
    }

    private async void OpenMap_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(
                SharingService.BuildLocationLink(LocationService.Latitude, LocationService.Longitude)));
        }
        catch
        {
            await Ui.Alert("Mapa", "No se pudo abrir la aplicación de mapas.", "Aceptar");
        }
    }

    private void BuildRecipientAvatars()
    {
        RecipientAvatars.Children.Clear();

        var contacts = AppSession.Account?.Contacts.OrderBy(c => c.Order).ToList() ?? new List<EmergencyContact>();
        var visible = contacts.Take(3).ToList();

        for (var i = 0; i < visible.Count; i++)
        {
            var contact = visible[i];
            RecipientAvatars.Children.Add(new Border
            {
                WidthRequest = 34,
                HeightRequest = 34,
                BackgroundColor = Color.FromArgb(contact.AvatarColor),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 17 },
                Margin = new Thickness(i == 0 ? 0 : -9, 0, 0, 0),
                Content = new Label
                {
                    Text = contact.Initials,
                    FontFamily = "InterBold",
                    FontSize = 10,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }

        if (contacts.Count > 3)
        {
            RecipientAvatars.Children.Add(new Border
            {
                WidthRequest = 34,
                HeightRequest = 34,
                BackgroundColor = Color.FromArgb("#DBEAFE"),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 17 },
                Margin = new Thickness(-9, 0, 0, 0),
                Content = new Label
                {
                    Text = $"+{contacts.Count - 3}",
                    FontFamily = "InterBold",
                    FontSize = 10,
                    TextColor = Color.FromArgb("#1F3864"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }

        if (contacts.Count == 0)
        {
            RecipientAvatars.Children.Add(new Border
            {
                WidthRequest = 38,
                HeightRequest = 38,
                BackgroundColor = Color.FromArgb("#DBEAFE"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 19 },
                Content = new Label
                {
                    Text = Icons.PersonAdd,
                    FontFamily = "MaterialIcons",
                    FontSize = 19,
                    TextColor = Color.FromArgb("#0074D5"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }

        RecipientsLabel.Text = contacts.Count switch
        {
            0 => "Sin contactos de confianza",
            1 => "1 Contacto de Confianza",
            _ => $"{contacts.Count} Contactos de Confianza"
        };

        RecipientsSubtitle.Text = contacts.Count == 0
            ? "Agrega contactos para compartir tu ubicación"
            : "WhatsApp con ubicación y SMS de respaldo";
    }

    private async void MapRecenter_Tapped(object sender, TappedEventArgs e)
    {
        if (_refreshingLocation)
            return;

        _refreshingLocation = true;
        MapPrecisionLabel.Text = "Actualizando ubicación...";

        try
        {
            var success = await LocationService.RefreshAsync();
            UpdateLocationLabels();

            if (!success)
            {
                await Ui.Alert(
                    "Ubicación de demostración",
                    "No se pudo obtener el GPS del equipo. Se mantiene la ubicación de demostración (Av. Amazonas, Quito) para probar la alerta.",
                    "Entendido");
            }
            else
            {
                await Ui.Alert(
                    "Ubicación actualizada",
                    $"{LocationService.Address}\n{LocationService.CoordinatesText}\n{LocationService.PrecisionText}",
                    "Aceptar");
            }
        }
        finally
        {
            _refreshingLocation = false;
        }
    }

    private void SelectPanic_Tapped(object sender, TappedEventArgs e) => ChangeType(EmergencyType.Panic);

    private void SelectMedical_Tapped(object sender, TappedEventArgs e) => ChangeType(EmergencyType.Medical);

    private void SelectHarassment_Tapped(object sender, TappedEventArgs e) => ChangeType(EmergencyType.Harassment);

    private void SelectRobbery_Tapped(object sender, TappedEventArgs e) => ChangeType(EmergencyType.Robbery);

    private void ChangeType(EmergencyType type)
    {
        _type = type;
        UpdateTypeSelection();

        if (_messageIsDefault || string.IsNullOrWhiteSpace(MessageEditor.Text))
        {
            MessageEditor.Text = EmergencyCatalog.DefaultMessage(type);
            _messageIsDefault = true;
            CharacterCountLabel.Text = $"{MessageEditor.Text.Length} car.";
        }
    }

    private void MessageEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? string.Empty;
        CharacterCountLabel.Text = $"{text.Length} car.";

        if (string.IsNullOrWhiteSpace(text))
            _messageIsDefault = false;
        else
            _messageIsDefault = text.Trim() == EmergencyCatalog.DefaultMessage(_type);
    }

    private async void SendAlert_Clicked(object sender, EventArgs e)
    {
        if (AppSession.Account == null)
        {
            await Ui.Alert("Sesión requerida", "Inicia sesión para enviar una alerta.", "Aceptar");
            return;
        }

        var message = MessageEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            await Ui.Alert("Mensaje vacío", "Escribe un mensaje breve para tus contactos o selecciona un tipo de emergencia.", "Aceptar");
            return;
        }

        SendAlertButton.IsEnabled = false;

        try
        {
            var granted = await PermissionService.EnsureLocationAsync();
            if (granted)
                await LocationService.RefreshAsync();
            else
                await Ui.Alert(
                    "Permiso de ubicación",
                    "Sin permiso de ubicación se enviará la última posición conocida. Puedes concederlo desde Configuración de Android.",
                    "Entendido");

            var alert = AlertService.Send(
                _type,
                message,
                LocationService.Address,
                LocationService.Latitude,
                LocationService.Longitude,
                notes: "Alerta emitida desde el módulo de envío con mapa y ubicación compartida.");

            var account = AppSession.Account!;
            var share = await SharingService.DispatchAsync(
                account,
                message,
                LocationService.Latitude,
                LocationService.Longitude);

            var summary = share.WhatsAppSent
                ? $"WhatsApp abierto con {share.PrimaryName} y tu ubicación.\nConfirma el envío dentro de WhatsApp."
                : $"{share.Error}\nSe preparará un SMS para tus contactos.";

            if (share.HasPendingSms)
            {
                var sendSms = await Ui.Confirm(
                    "Alerta registrada",
                    $"{summary}\n\n¿Enviar SMS a {share.SmsPending.Count} contacto(s) más con el mensaje de ayuda?",
                    "Enviar SMS",
                    "Ahora no");

                if (sendSms)
                {
                    var composed = await SharingService.ComposeSmsAsync(share.SmsPending, message);
                    if (!composed)
                        await Ui.Alert("SMS", "No se pudo abrir la aplicación de mensajes.", "Aceptar");
                }
            }
            else
            {
                await Ui.Alert(
                    "Alerta registrada",
                    $"{summary}\n\nUbicación incluida: {LocationService.Address}.",
                    "Aceptar");
            }

            _messageIsDefault = true;
            _type = EmergencyType.Panic;
            UpdateTypeSelection();
            MessageEditor.Text = EmergencyCatalog.DefaultMessage(_type);
            CharacterCountLabel.Text = $"{MessageEditor.Text.Length} car.";

            AlertCreated?.Invoke();
            NavigateRequested?.Invoke(3);
        }
        catch (Exception ex)
        {
            await Ui.Alert("No se pudo enviar", ex.Message, "Aceptar");
        }
        finally
        {
            SendAlertButton.IsEnabled = true;
        }
    }

    private async void CancelAlert_Clicked(object sender, EventArgs e)
    {
        var discard = await Ui.Confirm(
            "Descartar alerta",
            "¿Deseas cancelar el envío y volver al inicio? El mensaje actual se perderá.",
            "Descartar",
            "Continuar editando");

        if (!discard)
            return;

        _type = EmergencyType.Panic;
        UpdateTypeSelection();
        MessageEditor.Text = EmergencyCatalog.DefaultMessage(_type);
        _messageIsDefault = true;
        CharacterCountLabel.Text = $"{MessageEditor.Text.Length} car.";
        NavigateRequested?.Invoke(0);
    }
}
