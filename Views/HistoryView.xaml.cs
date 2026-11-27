using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using SafeAlert.Helpers;
using SafeAlert.Models;
using SafeAlert.Services;

namespace SafeAlert.Views;

public partial class HistoryView : ContentView, IRefreshable
{
    private HistoryFilter _filter = HistoryFilter.All;

    public HistoryView()
    {
        InitializeComponent();
    }

    public void RefreshView()
    {
        var account = AppSession.Account;
        if (account == null)
            return;

        var alerts = account.Alerts.OrderByDescending(alert => alert.CreatedAt).ToList();
        var resolved = alerts.Count(alert => alert.Status == AlertStatus.Resolved);
        var falseAlarms = alerts.Count(alert => alert.Status == AlertStatus.FalseAlarm);
        var inProgress = alerts.Count(alert => alert.Status == AlertStatus.InProgress);

        AllChipLabel.Text = $"Todas ({alerts.Count})";
        ResolvedChipLabel.Text = $"Resueltas ({resolved})";
        FalseChipLabel.Text = $"Falsas alarmas ({falseAlarms})";

        OperationalLabel.Text = inProgress > 0
            ? $"{inProgress} alerta(s) en curso. Hay una alerta en curso."
            : "Sin alertas pendientes o en riesgo actualmente.";

        UpdateFilterStyles();
        BuildAlertList(alerts);
    }

    private void BuildAlertList(List<AlertRecord> alerts)
    {
        AlertsList.Children.Clear();

        var filtered = _filter switch
        {
            HistoryFilter.Resolved => alerts.Where(alert => alert.Status == AlertStatus.Resolved).ToList(),
            HistoryFilter.FalseAlarm => alerts.Where(alert => alert.Status == AlertStatus.FalseAlarm).ToList(),
            HistoryFilter.InProgress => alerts.Where(alert => alert.Status == AlertStatus.InProgress).ToList(),
            _ => alerts
        };

        if (filtered.Count == 0)
        {
            AlertsList.Children.Add(BuildEmptyState());
            return;
        }

        for (var i = 0; i < filtered.Count; i++)
            AlertsList.Children.Add(BuildAlertCard(filtered[i], i == 0));
    }

    private Border BuildEmptyState()
    {
        var message = _filter switch
        {
            HistoryFilter.Resolved => "Todavía no hay alertas resueltas en tu historial.",
            HistoryFilter.FalseAlarm => "No se han registrado falsas alarmas.",
            HistoryFilter.InProgress => "No hay alertas en curso.",
            _ => "Aún no has generado alertas. Cuando uses el botón SOS aparecerán aquí."
        };

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Color.FromArgb("#E5E7EB")),
            StrokeThickness = 1,
            Padding = new Thickness(22, 26),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new VerticalStackLayout
            {
                Spacing = 9,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = Icons.History,
                        FontFamily = "MaterialIcons",
                        FontSize = 40,
                        TextColor = Color.FromArgb("#0074D5"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Sin registros para mostrar",
                        FontFamily = "InterBold",
                        FontSize = 16,
                        TextColor = Color.FromArgb("#1F3864"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = message,
                        FontSize = 12,
                        TextColor = Color.FromArgb("#6B7280"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        MaximumWidthRequest = 280
                    }
                }
            }
        };
    }

    private Border BuildAlertCard(AlertRecord alert, bool isFirst)
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

        // Encabezado
        var iconBorder = new Border
        {
            WidthRequest = 40,
            HeightRequest = 40,
            BackgroundColor = Color.FromArgb(alert.IconBackground),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = alert.Icon,
                FontFamily = "MaterialIcons",
                FontSize = 21,
                TextColor = Color.FromArgb(alert.IconColor),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var title = new Label
        {
            Text = alert.TypeTitle,
            FontFamily = "InterBold",
            FontSize = 16,
            TextColor = Color.FromArgb("#111827"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var dateRow = new HorizontalStackLayout { Spacing = 4 };
        dateRow.Children.Add(new Label
        {
            Text = Icons.Schedule,
            FontFamily = "MaterialIcons",
            FontSize = 14,
            TextColor = Color.FromArgb("#6B7280"),
            VerticalOptions = LayoutOptions.Center
        });
        dateRow.Children.Add(new Label
        {
            Text = alert.DateText,
            FontSize = 11,
            TextColor = Color.FromArgb("#4B5563"),
            VerticalOptions = LayoutOptions.Center
        });

        var textStack = new VerticalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Children = { title, dateRow }
        };

        var statusBadge = new Border
        {
            BackgroundColor = Color.FromArgb(alert.StatusBackgroundColor),
            StrokeThickness = 0,
            Padding = new Thickness(8, 4),
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            MaximumWidthRequest = 98,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = alert.StatusText,
                FontFamily = "InterBold",
                FontSize = 9,
                TextColor = Color.FromArgb(alert.StatusTextColor),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 9
        };
        header.Children.Add(iconBorder);
        header.SetColumn(textStack, 1);
        header.Children.Add(textStack);
        header.SetColumn(statusBadge, 2);
        header.Children.Add(statusBadge);
        body.Children.Add(header);

        var featured = isFirst && alert.ShowMap;

        // Mapa del evento más reciente con ubicación
        if (featured)
            body.Children.Add(BuildMapThumbnail(alert));

        // Panel de datos
        var info = new Border
        {
            BackgroundColor = Color.FromArgb("#F8F9FB"),
            StrokeThickness = 0,
            Padding = new Thickness(11, 9),
            StrokeShape = new RoundRectangle { CornerRadius = 10 }
        };

        var infoStack = new VerticalStackLayout { Spacing = 7 };

        if (alert.Type == EmergencyType.Medical)
        {
        }
        else
        {
            infoStack.Children.Add(BuildInfoRow(Icons.LocationOn, alert.Address, "#B91C1C"));

            if (!string.IsNullOrWhiteSpace(alert.Notes))
            {
                var note = alert.Notes.Length > 110 ? alert.Notes[..110] + "..." : alert.Notes;
                infoStack.Children.Add(BuildInfoRow(
                    alert.Type == EmergencyType.Test ? Icons.CheckCircle : Icons.ShareLocation,
                    note,
                    alert.Type == EmergencyType.Test ? "#15803D" : "#0074D5"));
            }

        }

        info.Content = infoStack;
        body.Children.Add(info);

        // Acción principal del reporte destacado
        if (featured)
        {
            var detailButton = new Button
            {
                Text = alert.ActionText,
                FontFamily = "InterSemiBold",
                FontSize = 12,
                HeightRequest = 47,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#0074D5"),
                TextColor = Colors.White,
                ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8)
            };
            detailButton.ImageSource = new FontImageSource
            {
                FontFamily = "MaterialIcons",
                Glyph = Icons.Mic,
                Color = Colors.White,
                Size = 18
            };
            detailButton.Clicked += async (_, _) => await OpenDetail(alert);
            body.Children.Add(detailButton);
        }
        else
        {
            body.Children.Add(BuildFooter(alert));
        }

        card.Content = body;

        var cardTap = new TapGestureRecognizer();
        cardTap.Tapped += async (_, _) => await OpenDetail(alert);
        card.GestureRecognizers.Add(cardTap);

        return card;
    }

    private static Grid BuildInfoRow(string icon, string text, string color)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 7
        };

        var iconLabel = new Label
        {
            Text = icon,
            FontFamily = "MaterialIcons",
            FontSize = 16,
            TextColor = Color.FromArgb(color),
            VerticalOptions = LayoutOptions.Start
        };

        var textLabel = new Label
        {
            Text = text,
            FontSize = 11,
            TextColor = Color.FromArgb("#374151"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        grid.Children.Add(iconLabel);
        grid.SetColumn(textLabel, 1);
        grid.Children.Add(textLabel);
        return grid;
    }

    private static Border BuildMapThumbnail(AlertRecord alert)
    {
        var grid = new Grid { HeightRequest = 118 };
        grid.Children.Add(new Image
        {
            Source = "map_alert.png",
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        });

        var addressBar = new Border
        {
            BackgroundColor = Color.FromArgb("#E6FFFFFF"),
            StrokeThickness = 0,
            Padding = new Thickness(8, 6),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Margin = new Thickness(6),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End
        };

        var addressGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 5
        };
        addressGrid.Children.Add(new Label
        {
            Text = Icons.LocationOn,
            FontFamily = "MaterialIcons",
            FontSize = 14,
            TextColor = Color.FromArgb("#B91C1C"),
            VerticalOptions = LayoutOptions.Center
        });
        var addressLabel = new Label
        {
            Text = alert.Address,
            FontSize = 10,
            TextColor = Color.FromArgb("#1F3864"),
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center
        };
        addressGrid.SetColumn(addressLabel, 1);
        addressGrid.Children.Add(addressLabel);
        addressBar.Content = addressGrid;
        grid.Children.Add(addressBar);

        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = 0,
            Content = grid
        };
    }

    private Grid BuildFooter(AlertRecord alert)
    {
        var footer = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 1, 0, 0)
        };

        var contacts = AppSession.Account?.Contacts.Take(2).ToList() ?? new List<EmergencyContact>();
        var avatars = new HorizontalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
        for (var i = 0; i < contacts.Count; i++)
        {
            avatars.Children.Add(new Border
            {
                WidthRequest = 26,
                HeightRequest = 26,
                BackgroundColor = Color.FromArgb(contacts[i].AvatarColor),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 13 },
                Margin = new Thickness(i == 0 ? 0 : -8, 0, 0, 0),
                Content = new Label
                {
                    Text = contacts[i].Initials,
                    FontFamily = "InterBold",
                    FontSize = 7,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }

        footer.Children.Add(avatars);

        var statusText = alert.Status switch
        {
            AlertStatus.Resolved when alert.Type == EmergencyType.Silent => "Confirmaron a salvo",
            AlertStatus.Resolved => "Evento finalizado",
            AlertStatus.FalseAlarm => "Marcada como falsa alarma",
            _ => "Seguimiento en curso"
        };
        var statusLabel = new Label
        {
            Text = statusText,
            FontSize = 10,
            TextColor = Color.FromArgb("#4B5563"),
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        footer.SetColumn(statusLabel, 1);
        footer.Children.Add(statusLabel);

        var details = new Border
        {
            BackgroundColor = Color.FromArgb("#EDEEF0"),
            StrokeThickness = 0,
            Padding = new Thickness(9, 5),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            VerticalOptions = LayoutOptions.Center
        };
        var detailsStack = new HorizontalStackLayout { Spacing = 2 };
        detailsStack.Children.Add(new Label
        {
            Text = "Detalles",
            FontFamily = "InterBold",
            FontSize = 10,
            TextColor = Color.FromArgb("#1F3864"),
            VerticalOptions = LayoutOptions.Center
        });
        detailsStack.Children.Add(new Label
        {
            Text = Icons.ChevronRight,
            FontFamily = "MaterialIcons",
            FontSize = 15,
            TextColor = Color.FromArgb("#1F3864"),
            VerticalOptions = LayoutOptions.Center
        });
        details.Content = detailsStack;
        footer.SetColumn(details, 2);
        footer.Children.Add(details);

        return footer;
    }

    private async Task OpenDetail(AlertRecord alert)
    {
        var page = new AlertDetailPage(alert);
        page.Changed += RefreshView;
        await Ui.PushModal(page);
    }

    private void FilterAll_Tapped(object sender, TappedEventArgs e) => SetFilter(HistoryFilter.All);

    private void FilterResolved_Tapped(object sender, TappedEventArgs e) => SetFilter(HistoryFilter.Resolved);

    private void FilterFalse_Tapped(object sender, TappedEventArgs e) => SetFilter(HistoryFilter.FalseAlarm);

    private void SetFilter(HistoryFilter filter)
    {
        _filter = filter;
        RefreshView();
    }

    private void UpdateFilterStyles()
    {
        ApplyChip(AllChip, AllChipLabel, _filter == HistoryFilter.All);
        ApplyChip(ResolvedChip, ResolvedChipLabel, _filter == HistoryFilter.Resolved);
        ApplyChip(FalseChip, FalseChipLabel, _filter == HistoryFilter.FalseAlarm);
    }

    private static void ApplyChip(Border chip, Label label, bool active)
    {
        chip.BackgroundColor = active ? Color.FromArgb("#1F3864") : Color.FromArgb("#F3F4F6");
        label.TextColor = active ? Colors.White : Color.FromArgb("#4B5563");
        label.FontFamily = active ? "InterBold" : "InterSemiBold";
    }

    private async void ExportTxt_Clicked(object sender, EventArgs e)
    {
        var account = AppSession.Account;
        if (account == null)
            return;

        try
        {
            var path = HistoryExporter.ExportTxt(account);
            var open = await Ui.Confirm(
                "Historial TXT exportado",
                $"Se generó el archivo:\n{path}\n\n¿Deseas abrirlo ahora?",
                "Abrir archivo",
                "Cerrar");

            if (open)
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest(
                    "Historial SafeAlert",
                    new ReadOnlyFile(path)));
            }
        }
        catch (Exception ex)
        {
            await Ui.Alert("No se pudo exportar", ex.Message, "Aceptar");
        }
    }

    private async void ExportPdf_Clicked(object sender, EventArgs e)
    {
        var account = AppSession.Account;
        if (account == null)
            return;

        try
        {
            var path = HistoryExporter.ExportPdf(account);
            var open = await Ui.Confirm(
                "Historial certificado generado",
                $"Se generó el documento PDF con el historial completo:\n{path}\n\n¿Deseas abrirlo ahora?",
                "Abrir PDF",
                "Cerrar");

            if (open)
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest(
                    "Historial certificado SafeAlert",
                    new ReadOnlyFile(path)));
            }
        }
        catch (Exception ex)
        {
            await Ui.Alert("No se pudo generar el PDF", ex.Message, "Aceptar");
        }
    }
}
