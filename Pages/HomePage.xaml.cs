using System.Text.Json;
using Microsoft.Maui.Layouts;
using AgroGuid.Services;

namespace AgroGuid.Pages;

public partial class HomePage : ContentPage
{
    private readonly string _userName;
    private readonly long _userId;
    private readonly FieldsApiService _fieldsApi = new FieldsApiService("https://localhost:7178/");
    private readonly HistoryApiService _historyApi = new HistoryApiService("https://localhost:7178/");

    private WebView? _mapView;
    private Label? _selectedNameLabel;
    private Label? _selectedAreaLabel;
    private Label? _selectedCropLabel;
    private Label? _selectedNotesLabel;
    private readonly System.Threading.SemaphoreSlim _fieldSaveLock = new(1, 1);
    private bool _mapReadyForSaving;

    private const string FieldsFileName = "agroguid_fields.json";

    private string FieldsFilePath => Path.Combine(FileSystem.AppDataDirectory, FieldsFileName);

    public HomePage(string userName, long userId)
    {
        InitializeComponent();

        // збереження ім'я та ID користувача
        _userName = userName;
        _userId = userId;

        ShowHome();
    }
    //текстові написи
    private Label MakeLabel(
        string text,
        double size,
        string color,
        bool bold = false)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            TextColor = Color.FromArgb(color),
            FontAttributes = bold
                ? FontAttributes.Bold
                : FontAttributes.None
        };
    }

    // картки
    private Border MakeCard(
        string title,
        string description,
        string color)
    {
        var layout = new VerticalStackLayout
        {
            Padding = 18,
            Spacing = 10,
            Children =
            {
                MakeLabel(title, 19, "#244D32", true),
                MakeLabel(description, 14, "#526052")
            }
        };

        return new Border
        {
            BackgroundColor = Color.FromArgb(color),
            Stroke = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes
                .RoundRectangle
            {
                CornerRadius = 14
            },
            Content = layout,
            Padding = 0
        };
    }

    //головна сторінка
    private void ShowHome()
    {
        ContentArea.Padding = new Thickness(28);
        ContentArea.Spacing = 20;
        ContentArea.Children.Clear();

        ContentArea.Children.Add(
            MakeLabel($"Вітаємо, {_userName}!", 30, "#174B32", true));


        // Головний банер AgroGuid із фоновим зображенням
        var bannerGrid = new Grid
        {
            HeightRequest = 220
        };

        // Фонове зображення
        var bannerImage = new Image
        {
            Source = "agroguid_banner.jpg",
            Aspect = Aspect.AspectFill
        };

        bannerGrid.Children.Add(bannerImage);

        // Затемнення для кращої читабельності тексту
        bannerGrid.Children.Add(new BoxView
        {
            Color = Color.FromArgb("#990D2918")
        });

        // Текст поверх зображення
        var bannerText = new VerticalStackLayout
        {
            Padding = new Thickness(28),
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start
        };

        bannerText.Children.Add(new Label
        {
            Text = "🌱 AgroGuid",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        });

        bannerText.Children.Add(new Label
        {
            Text = "Ефективне управління полями та агророботами.",
            FontSize = 17,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.WordWrap
        });

        bannerGrid.Children.Add(bannerText);

        // Заокруглення кутів банера
        ContentArea.Children.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 16
            },
            Padding = 0,
            Content = bannerGrid
        });

        ContentArea.Children.Add(
            MakeLabel("Основні розділи", 22, "#244D32", true));

        var cards = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 14,
            RowSpacing = 14
        };

        cards.Add(
            MakeCard("🗺 Карта полів",
                "Перегляд і розташування земельних ділянок.",
                "#E1EDE1"), 0, 0);


        cards.Add(
            MakeCard("📚 Довідник",
                "Культури, добрива та рекомендації.",
                "#ECE5F4"), 0, 1);


        cards.Add(
            MakeCard("📊 Історія",
                "Історія робіт і звіти.",
                "#F5E5E3"), 1, 0);

        ContentArea.Children.Add(cards);

        ContentArea.Children.Add(
            MakeCard(
                "AgroGuid — ваш надійний помічник!",
                "Краще планування сьогодні — ефективніша робота завтра.",
                "#E7F0E5"));
    }

    //стандартне відображення інших розділів
    private void ShowSection(string title, string description)
    {
        ContentArea.Padding = new Thickness(28);
        ContentArea.Spacing = 20;
        ContentArea.Children.Clear();

        ContentArea.Children.Add(MakeLabel(title, 30, "#174B32", true));

        ContentArea.Children.Add(MakeLabel(description, 16, "#647466"));

        ContentArea.Children.Add(MakeCard(
                title,
                "Цей розділ створено. Його функціональність, " +
                "дані та елементи керування додамо на наступних етапах.",
                "#E1EDE1"));
    }

    //інтерфейс карти
    private async void OnMapClicked(object sender, EventArgs e)
    {
        try
        {
            await ShowMapInterface();
            // Дані полів завантажуються у WebView після відкриття карти.
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка карти",
                "Не вдалося відкрити карту: " + ex.Message,
                "OK");
        }
    }

    // Перевірка отримання полів із SQLite
    private async Task TestFieldsConnectionAsync()
    {
        try
        {
            // Отримуємо список полів поточного користувача
            var fields = await _fieldsApi.GetFieldsAsync(_userId);

            int count = fields.GetArrayLength();

            await DisplayAlert(
                "SQLite",
                $"Підключення успішне!\nЗнайдено полів: {count}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка SQLite",
                "Не вдалося отримати поля: " + ex.Message,
                "OK");
        }
    }

    private async Task ShowMapInterface()
    {
        ContentArea.Children.Clear();
        ContentArea.Padding = new Thickness(18);
        ContentArea.Spacing = 12;



        // Поле пошуку
        var searchEntry = new Entry
        {
            Placeholder = "Введіть назву поля...",
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#244D32"),
            PlaceholderColor = Color.FromArgb("#849184"),
            HeightRequest = 42,
            HorizontalOptions = LayoutOptions.Fill
        };

        var searchButton = new Button
        {
            Text = "⌕ Пошук",
            BackgroundColor = Color.FromArgb("#E1EDE1"),
            TextColor = Color.FromArgb("#174B32"),
            CornerRadius = 8,
            HeightRequest = 42
        };

        searchButton.Clicked += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(searchEntry.Text))
            {
                await DisplayAlert(
                    "Пошук",
                    "Введіть назву поля для пошуку.",
                    "OK");

                return;
            }

            if (_mapView == null)
                return;

            string query = System.Text.Json.JsonSerializer
                .Serialize(searchEntry.Text);

            try
            {
                string result = await _mapView.EvaluateJavaScriptAsync($"window.agroGuidSearchField({query})");

                if (result.Contains("NOT_FOUND"))
                {
                    await DisplayAlert(
                        "Пошук",
                        "Поле з такою назвою не знайдено.",
                        "OK");
                }
                else if (result.Contains("EMPTY"))
                {
                    await DisplayAlert(
                        "Пошук",
                        "Введіть назву поля.",
                        "OK");
                }
                else
                {
                    await DisplayAlert(
                        "Пошук",
                        "Поле знайдено на карті.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Помилка пошуку",
                    ex.Message,
                    "OK");
            }
        };

        // Кнопки керування полями
        var addButton = new Button
        {
            Text = "＋ Додати поле",
            BackgroundColor = Color.FromArgb("#176239"),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 42
        };
        addButton.Clicked += async (s, e) =>
        {
            await RunMapToolAsync(".leaflet-draw-draw-polygon");
        };

        var editButton = new Button
        {
            Text = "✎ Редагувати",
            BackgroundColor = Color.FromArgb("#E1EDE1"),
            TextColor = Color.FromArgb("#244D32"),
            CornerRadius = 8,
            HeightRequest = 42
        };
        editButton.Clicked += async (s, e) =>
        {
            await RunMapToolAsync(".leaflet-draw-edit-edit");
        };

        var deleteButton = new Button
        {
            Text = "✕ Видалити",
            BackgroundColor = Color.FromArgb("#F5E5E3"),
            TextColor = Color.FromArgb("#963F35"),
            CornerRadius = 8,
            HeightRequest = 42
        };
        deleteButton.Clicked += async (s, e) =>
        {
            await RunMapToolAsync(".leaflet-draw-edit-remove");
        };

        // Панель інструментів
        var toolbar = new FlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            AlignItems = FlexAlignItems.Center,
            JustifyContent = FlexJustify.Start
        };

        toolbar.Children.Add(searchEntry);
        toolbar.Children.Add(searchButton);
        toolbar.Children.Add(addButton);
        toolbar.Children.Add(editButton);
        toolbar.Children.Add(deleteButton);

        foreach (var child in toolbar.Children)
        {
            if (child is View view)
            {
                view.Margin = new Thickness(0, 0, 8, 6);
            }
        }

        ContentArea.Children.Add(
            new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E0E8DF"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                Padding = 10,
                Content = toolbar
            });

        // Читаємо HTML-файл карти
        using var stream =await FileSystem.OpenAppPackageFileAsync("Maps/map.html");

        using var reader = new StreamReader(stream);

        string html = await reader.ReadToEndAsync();

        // Вікно карти
        _mapView = new WebView
        {
            Source = new HtmlWebViewSource
            {
                Html = html
            },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        // Збереження змін із Leaflet у SQLite
        _mapReadyForSaving = false;
        _mapView.Navigating += async (sender, args) =>
        {
            // Клік по полі передає його характеристики в праву панель.
            const string selectPrefix = "agroguid://select/?data=";
            if (args.Url != null && args.Url.StartsWith(selectPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                args.Cancel = true;
                try
                {
                    string selectedJson = Uri.UnescapeDataString(
                        args.Url.Substring(selectPrefix.Length));
                    using var selectedDoc = JsonDocument.Parse(selectedJson);
                    var selected = selectedDoc.RootElement;
                    string name = selected.GetProperty("name").GetString() ?? "Без назви";
                    string crop = selected.GetProperty("crop").GetString() ?? "Не вказана";
                    string area = selected.GetProperty("area").GetString() ?? "—";
                    if (_selectedNameLabel != null) _selectedNameLabel.Text = name;
                    if (_selectedCropLabel != null) _selectedCropLabel.Text =
                        string.IsNullOrWhiteSpace(crop) ? "Не вказана" : crop;
                    if (_selectedAreaLabel != null) _selectedAreaLabel.Text = area + " га";
                    if (_selectedNotesLabel != null) _selectedNotesLabel.Text =
                        "Натисніть інше поле для перегляду";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AgroGuid SELECT: " + ex.Message);
                }
                return;
            }

            const string prefix = "agroguid://save/?data=";
            if (args.Url == null ||
                !args.Url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return;

            args.Cancel = true;
            if (!_mapReadyForSaving)
                return;

            string json = Uri.UnescapeDataString(args.Url.Substring(prefix.Length));
            await _fieldSaveLock.WaitAsync();
            try
            {
                // Резервна копія незавершених змін окремо для кожного акаунта
                string backup = Path.Combine(FileSystem.AppDataDirectory,
                    $"agroguid_fields_pending_{_userId}.json");
                await File.WriteAllTextAsync(backup, json);

                using var document = JsonDocument.Parse(json);
                var features = document.RootElement.GetProperty("features");
                var existing = await _fieldsApi.GetFieldsAsync(_userId);
                var existingIds = existing.EnumerateArray()
                    .Select(f => f.GetProperty("id").GetInt64())
                    .ToHashSet();
                var incomingIds = new HashSet<long>();
                var assignedIds = new List<object>();

                for (int i = 0; i < features.GetArrayLength(); i++)
                {
                    var feature = features[i];
                    var props = feature.GetProperty("properties");
                    string name = props.TryGetProperty("name", out var n)
                        ? n.GetString() ?? "Без назви" : "Без назви";
                    string crop = props.TryGetProperty("crop", out var c)
                        ? c.GetString() ?? "" : "";
                    string geometry = feature.GetProperty("geometry").GetRawText();
                    decimal? areaHa = CalculateGeoJsonAreaHa(feature.GetProperty("geometry"));
                    string notes = "Культура: " + crop;

                    long id = 0;
                    if (props.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.Number)
                        idProp.TryGetInt64(out id);

                    if (id > 0)
                    {
                        if (!existingIds.Contains(id))
                            throw new InvalidOperationException(
                                $"Поле з ID {id} не належить поточному списку полів.");
                        await _fieldsApi.UpdateFieldAsync(id, _userId, name,
                            areaHa, geometry, notes);
                        incomingIds.Add(id);
                    }
                    else
                    {
                        var created = await _fieldsApi.CreateFieldAsync(_userId,
                            name, areaHa, geometry, notes);
                        id = created.GetProperty("id").GetInt64();
                        assignedIds.Add(new { index = i, id });
                        incomingIds.Add(id);
                    }
                }

                // Видаляємо тільки записи, які були завантажені з БД
                foreach (long id in existingIds.Except(incomingIds))
                    await _fieldsApi.DeleteFieldAsync(id);

                if (_mapView != null && assignedIds.Count > 0)
                {
                    string idsJson = JsonSerializer.Serialize(assignedIds);
                    await _mapView.EvaluateJavaScriptAsync(
                        $"window.agroGuidApplyIds({idsJson});");
                }

                File.Delete(backup);
                System.Diagnostics.Debug.WriteLine(
                    $"AgroGuid: синхронізовано {features.GetArrayLength()} полів із SQLite.");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка синхронізації",
                    "Зміни не вдалося повністю записати у SQLite. " +
                    "Резервну копію збережено локально.\n" + ex.Message, "OK");
            }
            finally
            {
                _fieldSaveLock.Release();
            }
        };

        // Завантаження полів поточного користувача із SQLite
        _mapView.Navigated += async (sender, args) =>
        {
            if (args.Result != WebNavigationResult.Success || _mapView == null)
                return;

            try
            {
                var records = await _fieldsApi.GetFieldsAsync(_userId);
                var features = new List<object>();
                foreach (var record in records.EnumerateArray())
                {
                    if (!record.TryGetProperty("boundaryGeojson", out var geometry) ||
                        geometry.ValueKind != JsonValueKind.Object)
                        continue;

                    string notes = record.TryGetProperty("notes", out var noteProp)
                        ? noteProp.GetString() ?? "" : "";
                    string crop = notes.StartsWith("Культура: ")
                        ? notes.Substring("Культура: ".Length) : "";
                    features.Add(new
                    {
                        type = "Feature",
                        properties = new
                        {
                            id = record.GetProperty("id").GetInt64(),
                            name = record.GetProperty("name").GetString(),
                            crop
                        },
                        geometry = geometry.Clone()
                    });
                }

                string collection = JsonSerializer.Serialize(new
                {
                    type = "FeatureCollection",
                    features
                });
                string argument = JsonSerializer.Serialize(collection);
                await _mapView.EvaluateJavaScriptAsync(
                    $"window.agroGuidLoadFields(JSON.parse({argument}));");
                // Підписуємося на вибір ділянок, не змінюючи map.html.
                const string selectionScript = """
                    (function () {
                        if (window.agroGuidSelectionInstalled) return;
                        window.agroGuidSelectionInstalled = true;
                        function sendSelection(layer) {
                            if (!layer || !layer.feature) return;
                            var p = layer.feature.properties || {};
                            var area = (typeof calculateArea === 'function')
                                ? calculateArea(layer) : '—';
                            var info = JSON.stringify({
                                name: p.name || 'Без назви',
                                crop: p.crop || '',
                                area: String(area)
                            });
                            var link = document.createElement('a');
                            link.href = 'agroguid://select/?data=' + encodeURIComponent(info);
                            link.style.display = 'none';
                            document.body.appendChild(link);
                            link.click();
                            link.remove();
                        }
                        function attach(layer) {
                            if (!layer || layer._agroGuidSelectionBound) return;
                            layer._agroGuidSelectionBound = true;
                            layer.on('click', function () { sendSelection(layer); });
                        }
                        drawnItems.eachLayer(attach);
                        drawnItems.on('layeradd', function (e) { attach(e.layer); });
                    })();
                    """;
                await _mapView.EvaluateJavaScriptAsync(selectionScript);
                _mapReadyForSaving = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка завантаження SQLite",
                    "Не вдалося завантажити поля з сервера. " +
                    "Перевір запуск локальну базу SQLite.\n" + ex.Message, "OK");
            }
        };

        // Права інформаційна панель
        var infoPanel = new VerticalStackLayout
        {
            Padding = 18,
            Spacing = 14,
            BackgroundColor = Color.FromArgb("#FFFFFF")
        };

        infoPanel.Children.Add(
            MakeLabel(
                "Інформація про поле",
                19, "#174B32", true));

        infoPanel.Children.Add(
            MakeLabel(
                "Виберіть земельну ділянку на карті, щоб переглянути її характеристики.",
                13, "#647466"));

        infoPanel.Children.Add(
            new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#E3EAE2")
            });

        _selectedNameLabel = AddInfoItem(
            infoPanel, "Назва поля", "Не вибрано");

        _selectedAreaLabel = AddInfoItem(
            infoPanel, "Площа", "—");

        _selectedCropLabel = AddInfoItem(
            infoPanel, "Культура", "—");

        AddInfoItem(
            infoPanel, "Тип ґрунту", "—");

        _selectedNotesLabel = AddInfoItem(
            infoPanel, "Примітки", "—");

        infoPanel.Children.Add(
            new Label
            {
                Text = "Інформація про ділянки з'явиться після реалізації вибору полів.",
                FontSize = 12,
                TextColor = Color.FromArgb("#829082"),
                LineBreakMode = LineBreakMode.WordWrap
            });

        // Розміщення карти та інформаційної панелі
        var mapGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(280))
            },
            ColumnSpacing = 12,
            HeightRequest = 620
        };

        mapGrid.Add(
            new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE5DA"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                Padding = 0,
                Content = _mapView
            }, 0, 0);

        mapGrid.Add(
            new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE5DA"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                Padding = 0,
                Content = infoPanel
            }, 1, 0);

        ContentArea.Children.Add(mapGrid);
    }

    // Орієнтовна площа полігона в гектарах (як у Leaflet)
    private static decimal? CalculateGeoJsonAreaHa(JsonElement geometry)
    {
        if (!geometry.TryGetProperty("type", out var type) ||
            type.GetString() != "Polygon")
            return null;

        var rings = geometry.GetProperty("coordinates");
        if (rings.GetArrayLength() == 0) return null;
        var points = rings[0].EnumerateArray().ToArray();
        if (points.Length < 3) return null;

        double area = 0;
        for (int i = 0; i < points.Length; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Length];
            double x1 = p1[0].GetDouble() * Math.PI / 180;
            double y1 = p1[1].GetDouble() * Math.PI / 180;
            double x2 = p2[0].GetDouble() * Math.PI / 180;
            double y2 = p2[1].GetDouble() * Math.PI / 180;
            area += (x2 - x1) * (2 + Math.Sin(y1) + Math.Sin(y2));
        }
        return (decimal)Math.Round(Math.Abs(area * 6378137d * 6378137d / 2) / 10000, 2);
    }

    // Запуск інструментів карти
    private async Task RunMapToolAsync(string selector)
    {
        if (_mapView == null)
        {
            await DisplayAlert(
                "Помилка",
                "Карта ще не завантажена.",
                "OK");

            return;
        }

        try
        {
            string script =
                "(function() {" +
                "if (typeof L === 'undefined') return 'NO_LEAFLET';" +
                "if (typeof L.Control.Draw === 'undefined') return 'NO_DRAW';" +
                $"var button = document.querySelector('{selector}');" +
                "if (!button) return 'NOT_FOUND';" +
                "button.click();" +
                "return 'OK';" +
                "})();";


            string result =
                await _mapView.EvaluateJavaScriptAsync(script);


            if (result.Contains("NO_LEAFLET"))
            {
                await DisplayAlert(
                    "Помилка карти",
                    "Бібліотека Leaflet не завантажилася.",
                    "OK");
            }
            else if (result.Contains("NO_DRAW"))
            {
                await DisplayAlert(
                    "Помилка карти",
                    "Бібліотека Leaflet Draw не завантажилася.",
                    "OK");
            }
            else if (result.Contains("NOT_FOUND"))
            {
                await DisplayAlert(
                    "Помилка карти",
                    "Кнопку інструмента не знайдено.",
                    "OK");
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Помилка карти",
                ex.Message,
                "OK");
        }
    }

    // Додавання рядка інформації до правої панелі
    private Label AddInfoItem(VerticalStackLayout panel, string title, string value)
    {
        var valueLabel = MakeLabel(value, 15, "#244D32", true);
        panel.Children.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                MakeLabel(title, 12, "#829082"),
                valueLabel
            }
        });
        return valueLabel;
    }

    // Кнопки керування полями
    private async void OnAddFieldClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Додавання поля",
            "Тут буде реалізовано додавання нової земельної ділянки.",
            "OK");
    }

    private async void OnEditFieldClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Редагування поля",
            "Спочатку потрібно вибрати поле на карті.",
            "OK");
    }

    private async void OnDeleteFieldClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Видалення поля",
            "Спочатку потрібно вибрати поле на карті.",
            "OK");
    }

    // Навігація
    private void OnHomeClicked(object sender, EventArgs e)
    {
        ShowHome();
    }

    private void OnFieldsClicked(object sender, EventArgs e)
    {
        ShowSection(
            "Мої поля",
            "Перегляд площі, культур та інформації про поля.");
    }

    private void OnWorksClicked(object sender, EventArgs e)
    {
        ShowSection(
            "Агророботи",
            "Облік виконаних сільськогосподарських робіт.");
    }

    private async void OnReferenceClicked(object sender, EventArgs e)
    {
        try
        {
            var crops = await new CropsApiService().GetCropsAsync();
            var loaded = new List<CropInfo>();
            foreach (var crop in crops.EnumerateArray())
            {
                static string Read(JsonElement item, string key) =>
                    item.TryGetProperty(key, out var value) &&
                    value.ValueKind == JsonValueKind.String
                        ? value.GetString() ?? "" : "";
                loaded.Add(new CropInfo(
                    Read(crop, "name"),
                    Read(crop, "category"),
                    Read(crop, "description"),
                    Read(crop, "features"),
                    Read(crop, "cropType")));
            }
            _crops.Clear();
            _crops.AddRange(loaded);
            ShowReference();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка довідника",
                "Не вдалося завантажити культури з SQLite. " +
                "Перевір, чи запущено локальну базу SQLite.\n" + ex.Message, "OK");
        }
    }

    private void OnHistoryClicked(object sender, EventArgs e)
    {
        ShowHistory();
    }

    private void OnPlanningClicked(object sender, EventArgs e)
    {
        ShowSection(
            "Планування",
            "Перегляд запланованих агроробіт.");
    }

    // Верхня панель
    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Профіль",
            $"Користувач: {_userName}",
            "OK");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Налаштування",
            "Налаштування програми додамо окремо.",
            "OK");
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Довідка",
            "AgroGuid — система обліку полів та агроробіт.",
            "OK");
    }

    // Вихід з акаунта
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert(
            "Вихід з акаунта",
            "Ви дійсно хочете вийти з акаунта?",
            "Так",
            "Ні");

        if (!answer)
            return;

        try
        {
            await SessionStore.LogoutAsync();
            Application.Current!.Windows[0].Page = new LoginPage();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка виходу", "Сервер недоступний: " + ex.Message, "OK");
            // Локальний токен уже видалено.
            Application.Current!.Windows[0].Page = new LoginPage();
        }
    }


    // Демонстрационные данные справочника
    private readonly List<CropInfo> _crops = new()
{
    new CropInfo(
        "Пшениця",
        "Зернові",
        "Зернова культура, яка використовується для виробництва борошна та круп.",
        "Потребує достатнього освітлення та родючого ґрунту.",
        "Озимі та ярі сорти"),

    new CropInfo(
        "Кукурудза",
        "Зернові",
        "Зернова та кормова культура з високою потребою в теплі.",
        "Найкраще розвивається на добре освітлених ділянках.",
        "Яра культура"),

    new CropInfo(
        "Соняшник",
        "Технічні",
        "Олійна культура, насіння якої використовується для виробництва олії.",
        "Потребує достатнього освітлення та поживних речовин.",
        "Яра культура"),

    new CropInfo(
        "Соя",
        "Технічні",
        "Бобова культура, яка використовується як харчова та кормова сировина.",
        "Має здатність до біологічної фіксації азоту.",
        "Яра культура"),

    new CropInfo(
        "Ріпак",
        "Технічні",
        "Олійна культура, що використовується у харчовій промисловості.",
        "Потребує належного зволоження та догляду.",
        "Озимі та ярі сорти"),

    new CropInfo(
        "Ячмінь",
        "Зернові",
        "Зернова культура, яка використовується у харчовій промисловості та тваринництві.",
        "Відносно невибагливий до умов вирощування.",
        "Озимі та ярі сорти")
};

    // Відображення довідника
    private void ShowReference(string category = "Усі")
    {
        ContentArea.Children.Clear();
        ContentArea.Padding = new Thickness(28);
        ContentArea.Spacing = 18;

        ContentArea.Children.Add(MakeLabel(
                "Довідник агронома", 30, "#174B32", true));

        ContentArea.Children.Add(MakeLabel(
                "Інформація про сільськогосподарські культури та особливості їх вирощування.",
                15, "#647466"));

        var searchEntry = new Entry
        {
            Placeholder = "Пошук культури...",
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#244D32"),
            PlaceholderColor = Color.FromArgb("#849184"),
            HeightRequest = 48
        };

        ContentArea.Children.Add(searchEntry);

        var categories = new FlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            AlignItems = FlexAlignItems.Center
        };

        string[] categoryNames =
        {
        "Усі", "Зернові", "Технічні"
    };

        foreach (string name in categoryNames)
        {
            var categoryButton = new Button
            {
                Text = name,
                BackgroundColor = name == category
                    ? Color.FromArgb("#174B32")
                    : Color.FromArgb("#E1EDE1"),
                TextColor = name == category
                    ? Colors.White
                    : Color.FromArgb("#244D32"),
                CornerRadius = 9,
                FontSize = 13,
                Padding = new Thickness(18, 9),
                Margin = new Thickness(0, 0, 8, 6)
            };

            categoryButton.Clicked += (s, e) =>
            {
                ShowReference(name);
            };

            categories.Children.Add(categoryButton);
        }

        ContentArea.Children.Add(categories);

        ContentArea.Children.Add(MakeLabel("Каталог культур", 21, "#244D32", true));

        var cards = new VerticalStackLayout
        {
            Spacing = 12
        };

        void RefreshCards()
        {
            cards.Children.Clear();

            var filtered = _crops.Where(c => (category == "Усі" || c.Category == category) && (string.IsNullOrWhiteSpace(searchEntry.Text) || c.Name.Contains(searchEntry.Text.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();

            if (filtered.Count == 0)
            {
                cards.Children.Add(MakeCard(
                        "Нічого не знайдено",
                        "Спробуйте змінити пошуковий запит.",
                        "#F5E5E3"));

                return;
            }

            foreach (var crop in filtered)
            {
                // Вибір фонового зображення відповідно до культури
                string imageName = crop.Name switch
                {
                    "Пшениця" => "crop_wheat.jpg",
                    "Кукурудза" => "crop_corn.jpg",
                    "Соняшник" => "crop_sunflower.jpg",
                    "Соя" => "crop_soy.jpg",
                    "Ріпак" => "crop_rapeseed.jpg",
                    "Ячмінь" => "crop_barley.jpg",
                    _ => "agroguid_banner.jpg"
                };

                // Контейнер картки
                var cardGrid = new Grid
                {
                    HeightRequest = 155
                };

                // Фонове зображення
                var backgroundImage = new Image
                {
                    Source = imageName,
                    Aspect = Aspect.AspectFill
                };

                cardGrid.Children.Add(backgroundImage);

                // Затемнення зображення
                cardGrid.Children.Add(new BoxView
                {
                    Color = Color.FromArgb("#99000000")
                });

                // Текст картки
                var textLayout = new VerticalStackLayout
                {
                    Padding = new Thickness(20),
                    Spacing = 8,
                    VerticalOptions = LayoutOptions.End,
                    HorizontalOptions = LayoutOptions.Start
                };

                textLayout.Children.Add(new Label
                {
                    Text = crop.Name,
                    FontSize = 23,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                });

                textLayout.Children.Add(new Label
                {
                    Text = crop.Category + " • Переглянути інформацію  ›",
                    FontSize = 13,
                    TextColor = Colors.White
                });

                cardGrid.Children.Add(textLayout);

                // Обробка натискання на картку
                var tapGesture = new TapGestureRecognizer();

                tapGesture.Tapped += (s, e) =>
                {
                    ShowCropDetails(crop);
                };

                cardGrid.GestureRecognizers.Add(tapGesture);

                // Заокруглення кутів
                var card = new Border
                {
                    Stroke = Colors.Transparent,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 14
                    },
                    Padding = 0,
                    Content = cardGrid,
                    HeightRequest = 155
                };

                cards.Children.Add(card);
            }
        }

        searchEntry.TextChanged += (s, e) =>
        {
            RefreshCards();
        };

        ContentArea.Children.Add(cards);

        RefreshCards();
    }

    // Детальна інформація про культуру
    private void ShowCropDetails(CropInfo crop)
    {
        ContentArea.Children.Clear();
        ContentArea.Padding = new Thickness(28);
        ContentArea.Spacing = 18;

        var backButton = new Button
        {
            Text = "← Назад до довідника",
            BackgroundColor = Color.FromArgb("#E1EDE1"),
            TextColor = Color.FromArgb("#174B32"),
            HorizontalOptions = LayoutOptions.Start
        };

        backButton.Clicked += (s, e) => ShowReference();

        ContentArea.Children.Add(backButton);

        ContentArea.Children.Add(MakeLabel(crop.Name, 30, "#174B32", true));

        ContentArea.Children.Add(MakeCard(
                "Категорія",
                crop.Category,
                "#E1EDE1"));

        ContentArea.Children.Add(MakeCard(
                "Опис культури",
                crop.Description,
                "#FFFFFF"));

        ContentArea.Children.Add(MakeCard(
                "Особливості вирощування",
                crop.Features,
                "#E0ECF5"));

        ContentArea.Children.Add(MakeCard(
                "Тип культури",
                crop.Type,
                "#ECE5F4"));

        ContentArea.Children.Add(MakeLabel(
                "Додаткові відомості та рекомендації можна буде розширити на наступному етапі.",
                13, "#829082"));
    }

    // Модель культури для інтерфейсу
    private class CropInfo
    {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public string Features { get; }
        public string Type { get; }

        public CropInfo(
            string name,
            string category,
            string description,
            string features,
            string type)
        {
            Name = name;
            Category = category;
            Description = description;
            Features = features;
            Type = type;
        }
    }


    // Реальні записи з SQLite
    private readonly List<HistoryItem> _history = new();

    // Відображення історії
    private async void ShowHistory()
    {
        ContentArea.Children.Clear();
        ContentArea.Padding = new Thickness(28);
        ContentArea.Spacing = 18;

        ContentArea.Children.Add(MakeLabel("Історія діяльності", 30, "#174B32", true));

        ContentArea.Children.Add(MakeLabel(
                "Журнал виконаних операцій та подій у системі AgroGuid.",
                15, "#647466"));

        ContentArea.Children.Add(MakeCard(
                "Журнал подій",
                "Реальні події створення, редагування та видалення полів із SQLite.",
                "#E1EDE1"));

        var searchEntry = new Entry
        {
            Placeholder = "Пошук за назвою або полем...",
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#244D32"),
            HeightRequest = 48
        };

        var filterPicker = new Picker
        {
            Title = "Тип подій",
            ItemsSource = new List<string>
        {
            "Усі записи",
            "Поля"
        },
            SelectedIndex = 0,
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#244D32"),
            HeightRequest = 48
        };

        ContentArea.Children.Add(searchEntry);
        ContentArea.Children.Add(filterPicker);

        var records = new VerticalStackLayout
        {
            Spacing = 12
        };

        void RefreshHistory()
        {
            records.Children.Clear();

            string filter =
                filterPicker.SelectedItem?.ToString() ?? "Усі записи";

            string query = searchEntry.Text?.Trim() ?? "";

            var filtered = _history
                .Where(item =>
                    (filter == "Усі записи" || item.Category == filter) &&
                    (string.IsNullOrWhiteSpace(query) ||
                     item.Title.Contains(query,
                         StringComparison.OrdinalIgnoreCase) ||
                     item.Field.Contains(query,
                         StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(item => item.Date)
                .ToList();

            if (filtered.Count == 0)
            {
                records.Children.Add(MakeCard(
                        "Записів не знайдено",
                        "Змініть параметри пошуку або фільтрації.",
                        "#F5E5E3"));

                return;
            }

            foreach (var item in filtered)
            {
                var detailsButton = new Button
                {
                    Text = "Переглянути деталі",
                    BackgroundColor = Color.FromArgb("#E1EDE1"),
                    TextColor = Color.FromArgb("#174B32"),
                    FontSize = 13,
                    HorizontalOptions = LayoutOptions.Start
                };

                detailsButton.Clicked += async (s, e) =>
                {
                    await DisplayAlert(item.Title,
                        $"Дата: {item.Date:dd.MM.yyyy}\n" +
                        $"Категорія: {item.Category}\n" +
                        $"Поле: {item.Field}\n\n" +
                        $"{item.Description}",
                        "Закрити");
                };

                var layout = new VerticalStackLayout
                {
                    Padding = 16,
                    Spacing = 8
                };

                layout.Children.Add(MakeLabel(item.Date.ToString("dd.MM.yyyy"), 12, "#829082"));

                layout.Children.Add(MakeLabel(item.Title, 18, "#244D32", true));

                layout.Children.Add(MakeLabel($"{item.Category} • {item.Field}", 13, "#647466"));

                layout.Children.Add(detailsButton);

                records.Children.Add(new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#DCE5DA"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius = 12
                    },
                    Padding = 0,
                    Content = layout
                });
            }
        }

        searchEntry.TextChanged += (s, e) =>
        {
            RefreshHistory();
        };

        filterPicker.SelectedIndexChanged += (s, e) =>
        {
            RefreshHistory();
        };

        ContentArea.Children.Add(
            MakeLabel("Записи", 21, "#244D32", true));

        ContentArea.Children.Add(records);

        // Отримуємо актуальну історію тільки після відкриття вкладки.
        try
        {
            var data = await _historyApi.GetHistoryAsync(_userId);
            _history.Clear();
            foreach (var item in data.EnumerateArray())
            {
                var date = item.GetProperty("createdAt").GetDateTime().ToLocalTime();
                _history.Add(new HistoryItem(
                    date,
                    "Поля",
                    item.GetProperty("action").GetString() ?? "Подія",
                    item.GetProperty("fieldName").GetString() ?? "Без назви",
                    item.GetProperty("description").GetString() ?? ""));
            }
            // Не перемальовуємо інший розділ, якщо користувач уже перейшов.
            if (ContentArea.Children.Contains(records))
                RefreshHistory();
        }
        catch (Exception ex)
        {
            if (ContentArea.Children.Contains(records))
                await DisplayAlert("Помилка історії",
                    "Не вдалося завантажити історію з SQLite.\n" + ex.Message,
                    "OK");
        }
    }

    // Модель запису історії
    private class HistoryItem
    {
        public DateTime Date { get; }
        public string Category { get; }
        public string Title { get; }
        public string Field { get; }
        public string Description { get; }

        public HistoryItem(
            DateTime date,
            string category,
            string title,
            string field,
            string description)
        {
            Date = date;
            Category = category;
            Title = title;
            Field = field;
            Description = description;
        }
    }

}
