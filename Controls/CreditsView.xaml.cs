using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Grex.Services;

namespace Grex.Controls
{
    public sealed partial class CreditsView : UserControl
    {
        public CreditsView()
        {
            this.InitializeComponent();
            LoadLicenses();
            RefreshLocalization();
            this.Loaded += CreditsView_Loaded;
            this.Unloaded += CreditsView_Unloaded;
        }

        private sealed class LicenseManifest
        {
            [JsonPropertyName("licenses")]
            public Dictionary<string, string> Licenses { get; set; } = new();

            [JsonPropertyName("components")]
            public List<LicenseComponent> Components { get; set; } = new();
        }

        private sealed class LicenseComponent
        {
            [JsonPropertyName("name")] public string Name { get; set; } = "";
            [JsonPropertyName("version")] public string Version { get; set; } = "";
            [JsonPropertyName("license")] public string License { get; set; } = "";
            [JsonPropertyName("copyright")] public string Copyright { get; set; } = "";
            [JsonPropertyName("url")] public string Url { get; set; } = "";
            [JsonPropertyName("category")] public string Category { get; set; } = "";
        }

        private void LoadLicenses()
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "third-party-licenses.json");
                if (!File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"CreditsView: manifest not found at {path}");
                    return;
                }

                var json = File.ReadAllText(path);
                var manifest = JsonSerializer.Deserialize<LicenseManifest>(json);
                if (manifest == null)
                {
                    return;
                }

                ComponentsItemsControl.Items.Clear();
                foreach (var c in manifest.Components)
                {
                    ComponentsItemsControl.Items.Add(BuildComponentExpander(c, manifest.Licenses));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreditsView: Failed to load licenses: {ex.Message}");
            }
        }

        private Expander BuildComponentExpander(LicenseComponent c, Dictionary<string, string> licenses)
        {
            var expander = new Expander
            {
                Header = $"{c.Name}  v{c.Version} — {c.License}",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4),
            };

            var panel = new StackPanel { Spacing = 8, Padding = new Thickness(4, 8, 4, 4) };

            if (!string.IsNullOrWhiteSpace(c.Copyright))
            {
                panel.Children.Add(new TextBlock { Text = c.Copyright, TextWrapping = TextWrapping.Wrap });
            }

            if (!string.IsNullOrWhiteSpace(c.Url))
            {
                var link = new HyperlinkButton { Content = c.Url };
                if (Uri.TryCreate(c.Url, UriKind.Absolute, out var uri))
                {
                    link.NavigateUri = uri;
                }
                link.PointerEntered += HyperlinkButton_PointerEntered;
                link.PointerExited += HyperlinkButton_PointerExited;
                panel.Children.Add(link);
            }

            var body = licenses.TryGetValue(c.License, out var text) ? text : string.Empty;
            panel.Children.Add(new TextBox
            {
                Text = body,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                IsSpellCheckEnabled = false,
                FontFamily = new FontFamily("Consolas"),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                MaxHeight = 360,
            });

            expander.Content = panel;
            return expander;
        }

        private void CreditsView_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ThemeChanged += OnThemeChanged;
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                ApplyCurrentThemeColors();
            });
        }

        private void CreditsView_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            try
            {
                if (DispatcherQueue == null || !DispatcherQueue.TryEnqueue(() => ApplyThemeColors(e)))
                {
                    ApplyThemeColors(e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnThemeChanged ERROR: {ex}");
            }
        }

        private void ApplyCurrentThemeColors()
        {
            try
            {
                var currentTheme = MainWindow.CurrentTheme;
                if (!IsHighContrastTheme(currentTheme))
                {
                    ClearHighContrastColors();
                    return;
                }

                var colors = MainWindow.GetCurrentThemeColors();
                ApplyThemeColors(new ThemeChangedEventArgs(currentTheme, colors.background, colors.secondary, colors.tertiary, colors.text, colors.accent));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyCurrentThemeColors ERROR: {ex}");
            }
        }

        public void ApplyThemeFromHost(ThemeChangedEventArgs e)
        {
            ApplyThemeColors(e);
        }

        private static bool IsHighContrastTheme(Services.ThemePreference preference)
        {
            return preference == Services.ThemePreference.BlackKnight ||
                   preference == Services.ThemePreference.Paranoid ||
                   preference == Services.ThemePreference.Diamond ||
                   preference == Services.ThemePreference.Subspace ||
                   preference == Services.ThemePreference.RedVelvet ||
                   preference == Services.ThemePreference.Dreams ||
                   preference == Services.ThemePreference.Tiefling ||
                   preference == Services.ThemePreference.Vibes;
        }

        private void ApplyThemeColors(ThemeChangedEventArgs e)
        {
            try
            {
                if (!IsHighContrastTheme(e.Theme))
                {
                    ClearHighContrastColors();
                    return;
                }

                ApplyForegroundToAllTextBlocks(this, e.TextBrush, e.AccentBrush);
                this.Background = e.BackgroundBrush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyThemeColors ERROR: {ex}");
            }
        }

        private void ApplyForegroundToAllTextBlocks(DependencyObject parent, SolidColorBrush foreground, SolidColorBrush accent)
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = foreground;
                }
                else if (child is TextBox textBox)
                {
                    textBox.Foreground = foreground;
                    textBox.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
                else if (child is ContentPresenter contentPresenter)
                {
                    contentPresenter.Foreground = foreground;
                }
                else if (child is Button button)
                {
                    button.Foreground = foreground;
                }

                ApplyForegroundToAllTextBlocks(child, foreground, accent);
            }
        }

        private void ClearHighContrastColors()
        {
            try
            {
                this.ClearValue(BackgroundProperty);
                ClearForegroundFromVisualTree(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearHighContrastColors ERROR: {ex}");
            }
        }

        private void ClearForegroundFromVisualTree(DependencyObject parent)
        {
            try
            {
                var count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is TextBlock textBlock)
                    {
                        textBlock.ClearValue(TextBlock.ForegroundProperty);
                    }
                    else if (child is TextBox textBox)
                    {
                        textBox.ClearValue(TextBox.ForegroundProperty);
                        textBox.ClearValue(TextBox.BackgroundProperty);
                    }
                    else if (child is ContentPresenter contentPresenter)
                    {
                        contentPresenter.ClearValue(ContentPresenter.ForegroundProperty);
                    }
                    else if (child is Button button)
                    {
                        button.ClearValue(Button.ForegroundProperty);
                        button.ClearValue(Button.BackgroundProperty);
                    }

                    ClearForegroundFromVisualTree(child);
                }
            }
            catch
            {
                // Ignore errors during visual tree traversal
            }
        }

        public void RefreshLocalization()
        {
            try
            {
                var locService = LocalizationService.Instance;

                if (CreditsHeadingTextBlock != null)
                {
                    CreditsHeadingTextBlock.Text = locService.GetLocalizedString("CreditsHeadingTextBlock.Text");
                }

                if (CreditsIntroTextBlock != null)
                {
                    CreditsIntroTextBlock.Text = locService.GetLocalizedString("CreditsIntroTextBlock.Text");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreditsView: RefreshLocalization error: {ex.Message}");
            }
        }

        private void HyperlinkButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                try
                {
                    var prop = typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var cursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
                        prop.SetValue(element, cursor);
                    }
                }
                catch
                {
                    // If reflection fails, do nothing
                }
            }
        }

        private void HyperlinkButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                try
                {
                    var prop = typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var cursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
                        prop.SetValue(element, cursor);
                    }
                }
                catch
                {
                    // If reflection fails, do nothing
                }
            }
        }
    }
}
