using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class HomeView : ContentView, IRefreshable
{
    public event Action<int>? NavigateRequested;

    public HomeView()
    {
        InitializeComponent();
    }

    public void RefreshView()
    {
        if (AppSession.Account is not { } account)
            return;

        DeviceStatus.Refresh();

        AddressLabel.Text = PermissionService.LocationEnabled ? LocationService.Address : "GPS desactivado en SafeAlert";
        PrecisionLabel.Text = PermissionService.LocationEnabled && !LocationService.UsingSimulation
            ? $"Precisión GPS: {LocationService.Precision}" : "Ubicación sin verificar";
        NetworkLabel.Text = DeviceStatus.Network;
        GpsLabel.Text = DeviceStatus.Gps;
        BatteryLabel.Text = DeviceStatus.BatteryLevel;

        HomeContactsTitle.Text = account.Contacts.Count switch
        {
            0 => "Sin contactos enlazados",
            1 => "1 Contacto enlazado",
            _ => $"{account.Contacts.Count} Contactos enlazados"
        };

        HomeContactsSubtitle.Text = account.Contacts.Any(c => !string.IsNullOrWhiteSpace(c.Phone))
            ? "El SMS se prepara para tu contacto principal"
            : "Agrega un contacto con número de teléfono";

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
        if (!PermissionService.LocationEnabled)
        {
            await Ui.Alert("GPS desactivado", "Actívalo en Perfil → Sensores y permisos.", "Entendido");
            return;
        }

        RefreshLocationButton.IsEnabled = false;
        AddressLabel.Text = "Actualizando ubicación...";

        try
        {
            var granted = await PermissionService.EnsureLocationAsync();
            if (!granted)
            {
                await Ui.Alert(
                    "Permiso de ubicación",
                    "Revisa el permiso en Perfil → Sensores y permisos.",
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
            RefreshView();
            RefreshLocationButton.IsEnabled = true;
        }
    }

    private async void TestStatus_Clicked(object sender, EventArgs e)
    {
        DeviceStatus.Refresh();
        BatteryLabel.Text = DeviceStatus.BatteryLevel;
        await Ui.Alert(
            "Estado operativo",
            $"Red: {DeviceStatus.Network}\nGPS: {DeviceStatus.Gps}\nBatería: {DeviceStatus.BatteryLevel}",
            "Aceptar");
    }

    private void GoContacts_Tapped(object sender, TappedEventArgs e) => NavigateRequested?.Invoke(2);

    private async void SosDial_Clicked(object sender, EventArgs e) =>
        await OpenEmergencyDialerAsync();

    internal async Task OpenEmergencyDialerAsync()
    {
        try
        {
            PhoneDialer.Default.Open("911");
        }
        catch
        {
            await Ui.Alert("Marcador no disponible", "Abre la app Teléfono y marca 911.", "Aceptar");
        }
    }

    private void AlertContact_Clicked(object sender, EventArgs e) =>
        NavigateRequested?.Invoke(1);

}
