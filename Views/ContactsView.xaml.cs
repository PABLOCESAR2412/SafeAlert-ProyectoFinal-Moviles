using Microsoft.Maui.Controls.Shapes;
using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class ContactsView : ContentView, IRefreshable
{
    public event Action? ContactsChanged;

    public ContactsView()
    {
        InitializeComponent();
    }

    public void RefreshView()
    {
        BuildContacts();
    }

    private void BuildContacts()
    {
        ContactsList.Children.Clear();

        var account = AppSession.Account;
        if (account == null)
            return;

        var contacts = account.Contacts
            .OrderBy(contact => contact.Order)
            .ThenBy(contact => contact.Name)
            .ToList();

        if (contacts.Count == 0)
        {
            ContactsList.Children.Add(BuildEmptyState());
        }
        else
        {
            for (var i = 0; i < contacts.Count; i++)
                ContactsList.Children.Add(BuildContactCard(contacts[i], i + 1));
        }

        UpdateCoverage(contacts.Count);
    }

    private void UpdateCoverage(int count)
    {
        CoverageText.Text = $"{count} de 5 asignados";
        CoverageBar.Children.Clear();

        for (var i = 0; i < 5; i++)
        {
            var segment = new BoxView
            {
                HeightRequest = 6,
                CornerRadius = new CornerRadius(3),
                Color = Color.FromArgb(i < Math.Min(count, 5) ? "#1F3864" : "#E5E7EB")
            };

            CoverageBar.SetColumn(segment, i);
            CoverageBar.Children.Add(segment);
        }

        CoverageInfoIcon.Text = count >= 3 ? Icons.CheckCircle : Icons.Info;
        CoverageInfoIcon.TextColor = Color.FromArgb(count >= 3 ? "#15803D" : "#D97706");

        CoverageInfoLabel.Text = count switch
        {
            0 => "Agrega al menos 3 contactos para garantizar respuesta inmediata.",
            < 3 => "Recomendamos mínimo 3 contactos para garantizar respuesta inmediata.",
            3 => "Cobertura adecuada. Puedes añadir hasta 5 contactos de confianza.",
            4 => "Muy buena cobertura. Un contacto más alcanza el máximo.",
            _ => "Cobertura máxima alcanzada. Tu red de apoyo está completa."
        };
    }

    private static Border BuildEmptyState()
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Color.FromArgb("#E5E7EB")),
            StrokeThickness = 1,
            Padding = new Thickness(20, 24),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = Icons.GroupAdd,
                        FontFamily = "MaterialIcons",
                        FontSize = 38,
                        TextColor = Color.FromArgb("#0074D5"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Aún no tienes contactos",
                        FontFamily = "InterBold",
                        FontSize = 16,
                        TextColor = Color.FromArgb("#1F3864"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Agrega a las personas que deben recibir tu ubicación cuando actives una alerta SOS.",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#6B7280"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        MaximumWidthRequest = 290
                    }
                }
            }
        };
    }

    private Border BuildContactCard(EmergencyContact contact, int number)
    {
        var card = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Color.FromArgb("#E5E7EB")),
            StrokeThickness = 1,
            Padding = new Thickness(14),
            StrokeShape = new RoundRectangle { CornerRadius = 16 }
        };

        var body = new VerticalStackLayout { Spacing = 9 };

        // Encabezado: avatar, nombre y modo de acción
        var avatar = new Border
        {
            WidthRequest = 52,
            HeightRequest = 52,
            BackgroundColor = Color.FromArgb(contact.AvatarColor),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 26 },
            Content = new Label
            {
                Text = contact.Initials,
                FontFamily = "InterBold",
                FontSize = 16,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var numberBadge = new Border
        {
            WidthRequest = 21,
            HeightRequest = 21,
            BackgroundColor = Color.FromArgb("#1F3864"),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 11 },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(-3, 0, 0, -3),
            Content = new Label
            {
                Text = number.ToString(),
                FontFamily = "InterBold",
                FontSize = 9,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var avatarBox = new Grid
        {
            WidthRequest = 56,
            HeightRequest = 56,
            VerticalOptions = LayoutOptions.Start,
            Children = { avatar, numberBadge }
        };

        var nameLabel = new Label
        {
            Text = contact.Name,
            FontFamily = "InterBold",
            FontSize = 15,
            TextColor = Color.FromArgb("#111827"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var relationLabel = new Label
        {
            Text = contact.RelationLine,
            FontSize = 12,
            TextColor = Color.FromArgb("#4B5563"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var actionBadge = new Border
        {
            BackgroundColor = Color.FromArgb(contact.ActionBackground),
            StrokeThickness = 0,
            Padding = new Thickness(8, 4),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            MaximumWidthRequest = 108,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = contact.ActionMode,
                FontFamily = "InterBold",
                FontSize = 9,
                TextColor = Color.FromArgb(contact.ActionTextColor),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        AddToGrid(headerGrid, avatarBox, 0, 0);
        AddToGrid(headerGrid, new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { nameLabel, relationLabel }
        }, 1, 0);
        AddToGrid(headerGrid, actionBadge, 2, 0);

        body.Children.Add(headerGrid);

        // Teléfono y acciones
        var phoneLabel = new Label
        {
            Text = Validation.NormalizePhone(contact.Phone) ?? contact.Phone,
            FontFamily = "InterSemiBold",
            FontSize = 13,
            TextColor = Color.FromArgb("#111827"),
            VerticalOptions = LayoutOptions.Center
        };

        var phoneRow = new Grid { ColumnSpacing = 8 };
        phoneRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        phoneRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        AddToGrid(phoneRow, phoneLabel, 0, 0);

        var callButton = BuildMiniIconButton(Icons.Call, "#E8F2FF", "#005BB5");
        SemanticProperties.SetDescription(callButton, $"Llamar a {contact.Name}");
        callButton.Clicked += async (_, _) => await CallContactAsync(contact);
        AddToGrid(phoneRow, callButton, 1, 0);
        body.Children.Add(phoneRow);

        // Pie: nota, editar y menú
        var footerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 4
        };

        var noteStack = new HorizontalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
        noteStack.Children.Add(new Label
        {
            Text = contact.FooterIcon,
            FontFamily = "MaterialIcons",
            FontSize = 15,
            TextColor = Color.FromArgb("#0074D5"),
            VerticalOptions = LayoutOptions.Center
        });
        noteStack.Children.Add(new Label
        {
            Text = contact.FooterNote,
            FontSize = 11,
            TextColor = Color.FromArgb("#4B5563"),
            VerticalOptions = LayoutOptions.Center
        });
        AddToGrid(footerGrid, noteStack, 0, 0);

        var editStack = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        editStack.Children.Add(new Label
        {
            Text = Icons.Edit,
            FontFamily = "MaterialIcons",
            FontSize = 15,
            TextColor = Color.FromArgb("#1F3864"),
            VerticalOptions = LayoutOptions.Center
        });
        editStack.Children.Add(new Label
        {
            Text = "Editar",
            FontFamily = "InterSemiBold",
            FontSize = 11,
            TextColor = Color.FromArgb("#1F3864"),
            VerticalOptions = LayoutOptions.Center
        });
        var editTap = new TapGestureRecognizer();
        editTap.Tapped += async (_, _) => await ShowContactForm(contact);
        editStack.GestureRecognizers.Add(editTap);
        AddToGrid(footerGrid, editStack, 1, 0);

        var moreButton = BuildMiniIconButton(Icons.MoreVert, "Transparent", "#4B5563");
        moreButton.WidthRequest = 32;
        moreButton.HeightRequest = 32;
        moreButton.FontSize = 20;
        moreButton.Clicked += async (_, _) => await ShowContactOptions(contact);
        AddToGrid(footerGrid, moreButton, 2, 0);

        body.Children.Add(footerGrid);
        card.Content = body;
        return card;
    }

    private static async Task CallContactAsync(EmergencyContact contact)
    {
        var phone = Validation.NormalizePhone(contact.Phone);
        if (phone is null)
        {
            await Ui.Alert("Teléfono no válido", "Corrige el número del contacto antes de llamar.", "Aceptar");
            return;
        }

        try
        {
            PhoneDialer.Default.Open(phone);
        }
        catch
        {
            await Ui.Alert("Marcador no disponible", $"Abre Teléfono y marca {phone}.", "Aceptar");
        }
    }

    private static Button BuildMiniIconButton(string glyph, string background, string foreground)
    {
        return new Button
        {
            Text = glyph,
            FontFamily = "MaterialIcons",
            FontSize = 17,
            TextColor = Color.FromArgb(foreground),
            BackgroundColor = background == "Transparent" ? Colors.Transparent : Color.FromArgb(background),
            WidthRequest = 33,
            HeightRequest = 33,
            CornerRadius = 17,
            Padding = 0,
            BorderWidth = 0,
            VerticalOptions = LayoutOptions.Center
        };
    }

    private async Task ShowContactForm(EmergencyContact? contact)
    {
        var page = new ContactFormPage(contact);
        page.Saved += () =>
        {
            RefreshView();
            ContactsChanged?.Invoke();
        };

        await Ui.PushModal(page);
    }

    private async Task ShowContactOptions(EmergencyContact contact)
    {
        var action = await Ui.ActionSheet(
            $"Opciones de {contact.Name}",
            "Cancelar",
            "Editar contacto",
            "Eliminar contacto",
            contact.Verified ? "Quitar verificación" : "Restablecer verificación");

        if (string.IsNullOrWhiteSpace(action) || action == "Cancelar")
            return;

        if (action == "Editar contacto")
        {
            await ShowContactForm(contact);
            return;
        }

        if (action == "Eliminar contacto")
        {
            var confirm = await Ui.Confirm(
                "Eliminar contacto",
                $"¿Deseas eliminar a {contact.Name} de tu red de apoyo? Dejará de recibir tus alertas.",
                "Eliminar",
                "Cancelar");

            if (!confirm)
                return;

            var account = AppSession.Account;
            if (account == null)
                return;

            account.Contacts.Remove(contact);
            Renumber(account);
            DataStore.Save();
            RefreshView();
            ContactsChanged?.Invoke();
            await Ui.Alert("Contacto eliminado", $"{contact.Name} ya no forma parte de tu red de apoyo.", "Aceptar");
            return;
        }

        contact.Verified = !contact.Verified;
        DataStore.Save();
        RefreshView();
        ContactsChanged?.Invoke();
        await Ui.Alert(
            contact.Verified ? "Contacto verificado" : "Verificación retirada",
            contact.Verified
                ? $"{contact.Name} quedó marcado como contacto verificado."
                : $"{contact.Name} ya no aparece como verificado.",
            "Aceptar");
    }

    private static void Renumber(UserAccount account)
    {
        var ordered = account.Contacts.OrderBy(contact => contact.Order).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Order = i + 1;
    }

    private async void AddContact_Clicked(object sender, EventArgs e) => await ShowContactForm(null);

    private async void AddSecondary_Tapped(object sender, TappedEventArgs e) => await ShowContactForm(null);

    private async void Sync_Tapped(object sender, TappedEventArgs e)
    {
        await Ui.Alert(
            "Contactos locales",
            $"Tus contactos están guardados en este dispositivo.\nÚltima revisión: {DateTime.Now:HH:mm} hrs\nContactos guardados: {AppSession.Account?.Contacts.Count ?? 0}",
            "Aceptar");
    }

    private static void AddToGrid(Grid grid, IView view, int column, int row)
    {
        grid.SetColumn(view, column);
        grid.SetRow(view, row);
        grid.Children.Add(view);
    }
}
