using CrosshairZ.Interop;
using CrosshairZ.Services;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace CrosshairZ
{
    public sealed partial class MainPage : Page
    {
        private readonly ProfileStore _store = new ProfileStore();
        private readonly WidgetController _widget;
        private readonly XboxGameBarWidget _gameBarWidget;

        private ProfilesFile _profiles;
        private CrosshairProfile _activeProfile;
        private IReadOnlyList<DrawCmd> _drawCmds = Array.Empty<DrawCmd>();
        private bool _loading;

        private enum UIMode { Customization, Placement, PinnedActive }
        private UIMode _uiMode = UIMode.Customization;

        private DispatcherTimer _animTimer;
        private float _animAngle;
        private DateTime _lastTick;

        private static readonly string[] StyleTags =
        {
            "classic", "circle", "dot", "diagonal_x", "square", "circle_cross", "custom"
        };

        public MainPage(XboxGameBarWidget widget)
        {
            InitializeComponent();
            _widget = new WidgetController(widget);
            _gameBarWidget = widget;
            if (widget != null)
                widget.GameBarDisplayModeChanged += Widget_DisplayModeChanged;
            Loaded += MainPage_Loaded;

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += AnimTimer_Tick;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _profiles = await _store.LoadAsync();
                RefreshProfileSelector();
            }
            catch (Exception ex)
            {
                _profiles = ProfileStore.CreateDefault();
                RefreshProfileSelector();
                ShowStatus($"Load error: {ex.Message}");
            }
        }


        private void AnimTimer_Tick(object sender, object e)
        {
            var now = DateTime.UtcNow;
            float dt = (float)(now - _lastTick).TotalSeconds;
            _lastTick = now;

            if (_activeProfile == null) return;
            float speed = _activeProfile.Crosshair.RotationSpeed;
            if (speed <= 0) return;

            _animAngle = (_animAngle + speed * dt) % 360f;
            RefreshPreviewWithAnim();
        }

        private void StartOrStopAnim()
        {
            bool shouldRun = _activeProfile?.Crosshair.RotationSpeed > 0;
            if (shouldRun && !_animTimer.IsEnabled)
            {
                _animAngle = 0f;
                _lastTick = DateTime.UtcNow;
                _animTimer.Start();
            }
            else if (!shouldRun && _animTimer.IsEnabled)
            {
                _animTimer.Stop();
                _animAngle = 0f;
                RefreshPreview();
            }
        }


        private void RefreshProfileSelector()
        {
            _loading = true;
            ProfileSelector.ItemsSource = null;
            ProfileSelector.ItemsSource = _profiles.Profiles;

            var active = _profiles.Profiles.FirstOrDefault(p => p.Id == _profiles.ActiveProfileId)
                         ?? _profiles.Profiles.First();

            ProfileSelector.SelectedItem = active;
            _loading = false;

            LoadProfileIntoControls(active);
        }

        private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (ProfileSelector.SelectedItem is CrosshairProfile p)
            {
                _profiles.ActiveProfileId = p.Id;
                LoadProfileIntoControls(p);
            }
        }

        private void LoadProfileIntoControls(CrosshairProfile profile)
        {
            _activeProfile = profile;
            var c = profile.Crosshair;

            _loading = true;

            EnabledToggle.IsOn = c.Enabled;

            int styleIdx = Array.IndexOf(StyleTags, c.Style ?? "classic");
            StyleCombo.SelectedIndex = styleIdx < 0 ? 0 : styleIdx;

            TopToggle.IsOn = c.ShowTop;
            BottomToggle.IsOn = c.ShowBottom;
            LeftToggle.IsOn = c.ShowLeft;
            RightToggle.IsOn = c.ShowRight;
            CenterDotToggle.IsOn = c.ShowCenterDot;

            WidthSlider.Value = c.Width;
            HeightSlider.Value = c.Height;
            ThicknessSlider.Value = c.Thickness;
            GapSlider.Value = c.Gap;
            DotSlider.Value = c.CenterDotRadius;

            CircleRadiusSlider.Value = c.CircleRadius;
            CircleThicknessSlider.Value = c.CircleThickness;

            InnerLinesToggle.IsOn = c.InnerLinesEnabled;
            ILLengthSlider.Value = c.InnerLinesLength;
            ILThicknessSlider.Value = c.InnerLinesThickness;
            ILGapSlider.Value = c.InnerLinesGap;

            RotationSlider.Value = c.Rotation;
            RotationSpeedSlider.Value = c.RotationSpeed;

            OpacitySlider.Value = c.Opacity;
            OutlineToggle.IsOn = c.OutlineEnabled;
            OutlineSlider.Value = c.OutlineThickness;
            CrosshairColorPicker.Color = Rgba8ToColor(c.Color);
            OutlineColorPicker.Color = Rgba8ToColor(c.OutlineColor);

            _loading = false;

            UpdateStylePanelVisibility();
            UpdateValueLabels();
            RefreshPreview();
            StartOrStopAnim();
        }


        private void UpdateStylePanelVisibility()
        {
            string style = GetSelectedStyleTag();
            bool hasArms = style == "classic" || style == "diagonal_x" || style == "circle_cross";
            bool hasCircle = style == "circle" || style == "square" || style == "circle_cross";

            ArmPanel.Visibility = hasArms ? Visibility.Visible : Visibility.Collapsed;
            CirclePanel.Visibility = hasCircle ? Visibility.Visible : Visibility.Collapsed;

            if (!string.Equals(style, "custom") && _editorMode)
                ExitEditorMode();
        }

        private string GetSelectedStyleTag()
        {
            if (StyleCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                return tag;
            return "classic";
        }


        private void StyleCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateStylePanelVisibility();
            OnSettingsChanged();
        }

        private void AnyControl_Changed(object sender, RoutedEventArgs e) => OnSettingsChanged();
        private void AnyControl_Changed(object sender, RangeBaseValueChangedEventArgs e) => OnSettingsChanged();
        private void AnyControl_Changed(ColorPicker sender, ColorChangedEventArgs e) => OnSettingsChanged();

        private async void OnSettingsChanged()
        {
            if (_loading || _activeProfile == null) return;
            _activeProfile.Crosshair = BuildCrosshairFromControls();
            UpdateValueLabels();
            RefreshPreview();
            StartOrStopAnim();
            await _store.SaveAsync(_profiles);
        }

        private void UpdateValueLabels()
        {
            WidthLabel.Text = $"{WidthSlider.Value:F1}";
            HeightLabel.Text = $"{HeightSlider.Value:F1}";
            ThicknessLabel.Text = $"{ThicknessSlider.Value:F1}";
            GapLabel.Text = $"{GapSlider.Value:F1}";
            DotLabel.Text = $"{DotSlider.Value:F1}";
            CircleRadiusLabel.Text = $"{CircleRadiusSlider.Value:F1}";
            CircleThicknessLabel.Text = $"{CircleThicknessSlider.Value:F1}";
            ILLengthLabel.Text = $"{ILLengthSlider.Value:F1}";
            ILThicknessLabel.Text = $"{ILThicknessSlider.Value:F1}";
            ILGapLabel.Text = $"{ILGapSlider.Value:F1}";
            RotationLabel.Text = $"{RotationSlider.Value:F0}°";
            SpinLabel.Text = RotationSpeedSlider.Value > 0
                                        ? $"{RotationSpeedSlider.Value:F0}°/s"
                                        : "off";
            OpacityLabel.Text = $"{OpacitySlider.Value * 100:F0}%";
            OutlineLabel.Text = $"{OutlineSlider.Value:F1}";
        }

        private CrosshairData BuildCrosshairFromControls()
        {
            var d = new CrosshairData
            {
                Enabled = EnabledToggle.IsOn,
                Style = GetSelectedStyleTag(),
                ShowTop = TopToggle.IsOn,
                ShowBottom = BottomToggle.IsOn,
                ShowLeft = LeftToggle.IsOn,
                ShowRight = RightToggle.IsOn,
                ShowCenterDot = CenterDotToggle.IsOn,
                Width = (float)WidthSlider.Value,
                Height = (float)HeightSlider.Value,
                Thickness = (float)ThicknessSlider.Value,
                Gap = (float)GapSlider.Value,
                CenterDotRadius = (float)DotSlider.Value,
                CircleRadius = (float)CircleRadiusSlider.Value,
                CircleThickness = (float)CircleThicknessSlider.Value,
                InnerLinesEnabled = InnerLinesToggle.IsOn,
                InnerLinesLength = (float)ILLengthSlider.Value,
                InnerLinesThickness = (float)ILThicknessSlider.Value,
                InnerLinesGap = (float)ILGapSlider.Value,
                Rotation = (float)RotationSlider.Value,
                RotationSpeed = (float)RotationSpeedSlider.Value,
                Opacity = (float)OpacitySlider.Value,
                OutlineEnabled = OutlineToggle.IsOn,
                OutlineThickness = (float)OutlineSlider.Value,
                Color = ColorToRgba8(CrosshairColorPicker.Color),
                OutlineColor = ColorToRgba8(OutlineColorPicker.Color),
                CustomShapes = _activeProfile?.Crosshair?.CustomShapes ?? new System.Collections.Generic.List<CustomShapeData>(),
            };
            return d;
        }

        private void RefreshPreview()
        {
            RefreshPreviewWithAnim();
        }

        private void RefreshPreviewWithAnim()
        {
            if (_activeProfile == null) return;

            try
            {
                float w = (float)PreviewCanvas.ActualWidth;
                float h = (float)PreviewCanvas.ActualHeight;
                if (w < 1 || h < 1) w = h = 200;

                float effectiveW = (_uiMode == UIMode.Customization && !_editorMode && w > 360) ? w - 340f : w;

                var data = _activeProfile.Crosshair;

                if (_animAngle != 0f)
                {
                    data = DeepCloneCrosshair(data);
                    data.Rotation += _animAngle;
                }

                _drawCmds = RustInterop.BuildPreview(data, effectiveW, h);
                ShowStatus(string.Empty);
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message);
                _drawCmds = Array.Empty<DrawCmd>();
            }

            PreviewCanvas.Invalidate();
        }

        private void PreviewCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            if (!_editorMode)
            {
                foreach (var cmd in _drawCmds)
                {
                    if (cmd.Line is LineCmd line)
                    {
                        ds.DrawLine(line.X1, line.Y1, line.X2, line.Y2,
                                    Rgba8ToColor(line.Color), line.Thickness);
                    }
                    else if (cmd.Circle is CircleCmd circle)
                    {
                        ds.FillCircle(circle.X, circle.Y, circle.Radius, Rgba8ToColor(circle.Color));
                    }
                    else if (cmd.StrokeCircle is StrokeCircleCmd sc)
                    {
                        ds.DrawCircle(sc.X, sc.Y, sc.Radius, Rgba8ToColor(sc.Color), sc.StrokeWidth);
                    }
                    else if (cmd.FilledRect is FilledRectCmd fr)
                    {
                        ds.FillRectangle(fr.X, fr.Y, fr.W, fr.H, Rgba8ToColor(fr.Color));
                    }
                    else if (cmd.StrokeRect is StrokeRectCmd sr)
                    {
                        ds.DrawRectangle(sr.X, sr.Y, sr.W, sr.H, Rgba8ToColor(sr.Color), sr.StrokeWidth);
                    }
                    else if (cmd.FilledTriangle is FilledTriangleCmd ft)
                    {
                        var geo = Microsoft.Graphics.Canvas.Geometry.CanvasGeometry.CreatePolygon(sender.Device,
                            new System.Numerics.Vector2[]
                            {
                                new System.Numerics.Vector2(ft.X1, ft.Y1),
                                new System.Numerics.Vector2(ft.X2, ft.Y2),
                                new System.Numerics.Vector2(ft.X3, ft.Y3),
                            });
                        ds.FillGeometry(geo, Rgba8ToColor(ft.Color));
                    }
                }
            }

            if (_editorMode)
                DrawEditorOverlay(ds);
        }


        private async void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeProfile == null) return;
            if (sender is Button btn && btn.Tag is string tag)
            {
                _activeProfile.Crosshair = BuildPreset(tag);
                LoadProfileIntoControls(_activeProfile);
                try { await _store.SaveImmediateAsync(_profiles); }
                catch (Exception ex) { ShowStatus($"Save failed: {ex.Message}"); }
            }
        }

        private static CrosshairData BuildPreset(string tag) => tag switch
        {
            "cs2" => new CrosshairData
            {
                Style = "classic",
                ShowTop = true,
                ShowBottom = true,
                ShowLeft = true,
                ShowRight = true,
                ShowCenterDot = false,
                Width = 10f,
                Height = 10f,
                Thickness = 1f,
                Gap = 3f,
                Color = new Rgba8 { R = 0, G = 255, B = 0, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 255 },
            },
            "valorant" => new CrosshairData
            {
                Style = "classic",
                ShowTop = false,
                ShowBottom = true,
                ShowLeft = true,
                ShowRight = true,
                ShowCenterDot = false,
                Width = 6f,
                Height = 6f,
                Thickness = 2f,
                Gap = 3f,
                Color = new Rgba8 { R = 255, G = 255, B = 255, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 200 },
            },
            "dot" => new CrosshairData
            {
                Style = "dot",
                ShowCenterDot = true,
                CenterDotRadius = 3f,
                Color = new Rgba8 { R = 255, G = 255, B = 255, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 255 },
            },
            "circle" => new CrosshairData
            {
                Style = "circle",
                ShowCenterDot = false,
                CircleRadius = 20f,
                CircleThickness = 2f,
                Color = new Rgba8 { R = 255, G = 255, B = 255, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 255 },
            },
            "circle_cross" => new CrosshairData
            {
                Style = "circle_cross",
                CircleRadius = 20f,
                CircleThickness = 1.5f,
                Width = 12f,
                Height = 12f,
                Thickness = 1.5f,
                Gap = 4f,
                ShowCenterDot = false,
                Color = new Rgba8 { R = 100, G = 200, B = 255, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 180 },
            },
            "x_shape" => new CrosshairData
            {
                Style = "diagonal_x",
                Width = 14f,
                Height = 14f,
                Thickness = 2f,
                Gap = 4f,
                ShowCenterDot = false,
                Color = new Rgba8 { R = 255, G = 100, B = 100, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 255 },
            },
            "box" => new CrosshairData
            {
                Style = "square",
                CircleRadius = 18f,
                CircleThickness = 1.5f,
                ShowCenterDot = true,
                CenterDotRadius = 1.5f,
                Color = new Rgba8 { R = 255, G = 220, B = 50, A = 255 },
                OutlineEnabled = true,
                OutlineThickness = 1f,
                OutlineColor = new Rgba8 { R = 0, G = 0, B = 0, A = 255 },
            },
            _ => new CrosshairData(),
        };


        private async void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var box = new TextBox { PlaceholderText = "Profile name", Text = "New Profile" };
            var dlg = new ContentDialog
            {
                Title = "New profile",
                Content = box,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var profile = new CrosshairProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = string.IsNullOrWhiteSpace(box.Text) ? "New Profile" : box.Text.Trim(),
                Crosshair = new CrosshairData()
            };

            _profiles.Profiles.Add(profile);
            _profiles.ActiveProfileId = profile.Id;
            RefreshProfileSelector();

            try { await _store.SaveImmediateAsync(_profiles); }
            catch (Exception ex) { ShowStatus($"Save failed: {ex.Message}"); }
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profiles.Profiles.Count <= 1)
            {
                ShowStatus("Cannot delete the last profile.");
                return;
            }

            _profiles.Profiles.Remove(_activeProfile);
            _profiles.ActiveProfileId = _profiles.Profiles.First().Id;
            RefreshProfileSelector();

            try { await _store.SaveImmediateAsync(_profiles); }
            catch (Exception ex) { ShowStatus($"Save failed: {ex.Message}"); }
        }

        private async void DuplicateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeProfile == null) return;

            var copy = new CrosshairProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = _activeProfile.Name + " (copy)",
                Crosshair = DeepCloneCrosshair(_activeProfile.Crosshair)
            };

            _profiles.Profiles.Add(copy);
            _profiles.ActiveProfileId = copy.Id;
            RefreshProfileSelector();

            try { await _store.SaveImmediateAsync(_profiles); }
            catch (Exception ex) { ShowStatus($"Save failed: {ex.Message}"); }
        }

        private async void RenameProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeProfile == null) return;

            var box = new TextBox { PlaceholderText = "Profile name", Text = _activeProfile.Name };
            var dlg = new ContentDialog
            {
                Title = "Rename profile",
                Content = box,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
            if (string.IsNullOrWhiteSpace(box.Text)) return;

            _activeProfile.Name = box.Text.Trim();
            RefreshProfileSelector();

            try { await _store.SaveImmediateAsync(_profiles); }
            catch (Exception ex) { ShowStatus($"Save failed: {ex.Message}"); }
        }

        private void CopyShareCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeProfile == null) return;
            try
            {
                string code = RustInterop.EncodeShareCode(_activeProfile.Crosshair);
                var dp = new DataPackage();
                dp.SetText(code);
                Clipboard.SetContent(dp);
                ShowStatus("Share code copied.");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}");
            }
        }

        private async void PasteShareCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var content = Clipboard.GetContent();
                if (!content.Contains(StandardDataFormats.Text)) return;

                string code = await content.GetTextAsync();
                var data = RustInterop.DecodeShareCode(code.Trim());
                if (data == null)
                {
                    ShowStatus("Invalid share code.");
                    return;
                }

                if (_activeProfile == null) return;
                _activeProfile.Crosshair = data;
                LoadProfileIntoControls(_activeProfile);
                await _store.SaveAsync(_profiles);
                ShowStatus("Share code applied.");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}");
            }
        }

        private async void CenterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gameBarWidget != null)
                await _widget.CenterAsync();
        }

        private void ShowStatus(string message) => StatusText.Text = message;

        private static Color Rgba8ToColor(Rgba8 c) =>
            Color.FromArgb(c.A, c.R, c.G, c.B);

        private static Rgba8 ColorToRgba8(Color c) =>
            new Rgba8 { R = c.R, G = c.G, B = c.B, A = c.A };

        private static CrosshairData DeepCloneCrosshair(CrosshairData src) => new CrosshairData
        {
            Enabled = src.Enabled,
            Style = src.Style,
            ShowTop = src.ShowTop,
            ShowBottom = src.ShowBottom,
            ShowLeft = src.ShowLeft,
            ShowRight = src.ShowRight,
            ShowCenterDot = src.ShowCenterDot,
            Width = src.Width,
            Height = src.Height,
            Thickness = src.Thickness,
            Gap = src.Gap,
            CenterDotRadius = src.CenterDotRadius,
            CircleRadius = src.CircleRadius,
            CircleThickness = src.CircleThickness,
            InnerLinesEnabled = src.InnerLinesEnabled,
            InnerLinesLength = src.InnerLinesLength,
            InnerLinesThickness = src.InnerLinesThickness,
            InnerLinesGap = src.InnerLinesGap,
            Rotation = src.Rotation,
            RotationSpeed = src.RotationSpeed,
            Opacity = src.Opacity,
            Color = new Rgba8 { R = src.Color.R, G = src.Color.G, B = src.Color.B, A = src.Color.A },
            OutlineEnabled = src.OutlineEnabled,
            OutlineThickness = src.OutlineThickness,
            OutlineColor = new Rgba8 { R = src.OutlineColor.R, G = src.OutlineColor.G, B = src.OutlineColor.B, A = src.OutlineColor.A },
            CustomShapes = src.CustomShapes != null
                               ? new System.Collections.Generic.List<CustomShapeData>(src.CustomShapes)
                               : new System.Collections.Generic.List<CustomShapeData>(),
        };

        private enum EditorToolMode { Select, Line, Circle, Rect, Triangle }

        private sealed class EditorShape
        {
            public ShapeType Type;
            public float X1, Y1, X2, Y2;
            public float Cx, Cy, Radius;
            public float Rx1, Ry1, Rx2, Ry2;
            public float X3, Y3;
            public bool Filled;
            public Color Color = Color.FromArgb(255, 255, 255, 255);
            public float Thickness = 2f;
            public bool OutlineEnabled = true;
            public float OutlineThickness = 1f;
            public Color OutlineColor = Color.FromArgb(255, 0, 0, 0);

            public EditorShape Clone() => (EditorShape)MemberwiseClone();
        }

        private enum ShapeType { Line, Circle, Rect, Triangle }

        private bool _editorMode;
        private readonly List<EditorShape> _editorShapes = new List<EditorShape>();
        private int _selectedShapeIdx = -1;
        private EditorToolMode _editorTool = EditorToolMode.Select;
        private float _zoom = 4.0f;
        private bool _snapEnabled = true;
        private float _snapAngle = 10.0f;
        private bool _showGrid = true;

        private bool _drawing;
        private float _drawStartCx, _drawStartCy;
        private EditorShape _inProgressShape;

        private bool _dragging;
        private float _dragPointerStartCx, _dragPointerStartCy;
        private int _dragHandleIdx = -1;
        private float _dragX1, _dragY1, _dragX2, _dragY2;
        private float _dragCx, _dragCy, _dragRadius;
        private float _dragRx1, _dragRy1, _dragRx2, _dragRy2;
        private float _dragX3, _dragY3;

        private readonly float _gridStep = 5.0f;

        private readonly Stack<List<EditorShape>> _undoStack = new Stack<List<EditorShape>>();

        private async void Widget_DisplayModeChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender.GameBarDisplayMode == XboxGameBarDisplayMode.PinnedOnly)
                    SetUIMode(UIMode.PinnedActive);
                else
                    SetUIMode(UIMode.Customization);
            });
        }

        private void SetUIMode(UIMode mode)
        {
            _uiMode = mode;
            bool isFull = mode == UIMode.Customization;
            bool isPlacement = mode == UIMode.Placement;
            bool isPinned = mode == UIMode.PinnedActive;

            if (NavBar != null)
                NavBar.Visibility = isFull ? Visibility.Visible : Visibility.Collapsed;

            if (HomeContent != null)
                HomeContent.Visibility = (isFull && HomeTabBtn?.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            if (CustomContent != null)
                CustomContent.Visibility = (isFull && CustomTabBtn?.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            if (CommunityContent != null)
                CommunityContent.Visibility = (isFull && CommunityTabBtn?.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;

            if (ExitPlacementButton != null)
                ExitPlacementButton.Visibility = isPlacement ? Visibility.Visible : Visibility.Collapsed;

            bool transparent = isPinned || isPlacement;
            RootPage.Background = transparent
                ? new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent)
                : (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["PageBackgroundBrush"];
            if (ContentGrid != null)
                ContentGrid.Background = transparent
                    ? new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent)
                    : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 13, 18));

            if (!isFull && _editorMode) ExitEditorMode();
            RefreshPreview();
        }

        private void PlacementModeButton_Click(object sender, RoutedEventArgs e) => SetUIMode(UIMode.Placement);
        private void ExitPlacementButton_Click(object sender, RoutedEventArgs e) => SetUIMode(UIMode.Customization);


        private void Tab_Changed(object sender, RoutedEventArgs e)
        {
            if (HomeContent == null) return;
            bool isCustom = CustomTabBtn?.IsChecked == true;
            bool isCommunity = CommunityTabBtn?.IsChecked == true;
            bool isHome = HomeTabBtn?.IsChecked == true;

            HomeContent.Visibility = isHome ? Visibility.Visible : Visibility.Collapsed;
            CustomContent.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            CommunityContent.Visibility = isCommunity ? Visibility.Visible : Visibility.Collapsed;

            if (isCustom && !_editorMode) EnterEditorMode();
            else if (!isCustom && _editorMode) ExitEditorMode();

            if (isCommunity) _ = LoadCommunityAsync();
        }

        private void CloseEditorButton_Click(object sender, RoutedEventArgs e)
        {
            if (HomeTabBtn != null) HomeTabBtn.IsChecked = true;
        }

        private void EnterEditorMode()
        {
            _editorMode = true;
            LoadShapesFromProfile();
            UpdateEditorShapePropsUI();
            ShapeCountLabel.Text = ShapeCountText();
            PreviewCanvas.Invalidate();
        }

        private void ExitEditorMode()
        {
            _editorMode = false;
            _drawing = false;
            _dragging = false;
            _inProgressShape = null;
            _selectedShapeIdx = -1;
            PreviewCanvas.Invalidate();
        }

        private void LoadShapesFromProfile()
        {
            _editorShapes.Clear();
            if (_activeProfile?.Crosshair?.CustomShapes == null) return;
            foreach (var cs in _activeProfile.Crosshair.CustomShapes)
                _editorShapes.Add(ProtocolShapeToEditor(cs));
        }

        private void SyncShapesToProfile()
        {
            if (_activeProfile == null) return;
            _activeProfile.Crosshair.CustomShapes = _editorShapes
                .Select(s => EditorShapeToProtocol(s))
                .ToList();
            ShapeCountLabel.Text = ShapeCountText();
            RefreshPreview();
        }

        private string ShapeCountText() =>
            _editorShapes.Count == 0 ? "No shapes" :
            _editorShapes.Count == 1 ? "1 shape" :
            $"{_editorShapes.Count} shapes";

        private static EditorShape ProtocolShapeToEditor(CustomShapeData cs)
        {
            var s = new EditorShape
            {
                Color = Color.FromArgb(cs.Color.A, cs.Color.R, cs.Color.G, cs.Color.B),
                Thickness = cs.Thickness,
                OutlineEnabled = cs.OutlineEnabled,
                OutlineThickness = cs.OutlineThickness,
                OutlineColor = Color.FromArgb(cs.OutlineColor.A, cs.OutlineColor.R, cs.OutlineColor.G, cs.OutlineColor.B),
            };
            var k = cs.Kind;
            switch (k.Type)
            {
                case "line":
                    s.Type = ShapeType.Line;
                    s.X1 = k.X1 ?? 0; s.Y1 = k.Y1 ?? 0;
                    s.X2 = k.X2 ?? 0; s.Y2 = k.Y2 ?? 0;
                    break;
                case "circle":
                    s.Type = ShapeType.Circle;
                    s.Cx = k.Cx ?? 0; s.Cy = k.Cy ?? 0;
                    s.Radius = k.Radius ?? 8;
                    s.Filled = k.Filled ?? false;
                    break;
                case "rect":
                    s.Type = ShapeType.Rect;
                    s.Rx1 = k.X ?? 0; s.Ry1 = k.Y ?? 0;
                    s.Rx2 = s.Rx1 + (k.W ?? 10);
                    s.Ry2 = s.Ry1 + (k.H ?? 10);
                    s.Filled = k.Filled ?? false;
                    break;
                case "triangle":
                    s.Type = ShapeType.Triangle;
                    s.X1 = k.X1 ?? 0; s.Y1 = k.Y1 ?? 0;
                    s.X2 = k.X2 ?? 0; s.Y2 = k.Y2 ?? 0;
                    s.X3 = k.X3 ?? 0; s.Y3 = k.Y3 ?? 0;
                    s.Filled = k.Filled ?? false;
                    break;
            }
            return s;
        }

        private static CustomShapeData EditorShapeToProtocol(EditorShape s) => new CustomShapeData
        {
            Color = new Rgba8 { R = s.Color.R, G = s.Color.G, B = s.Color.B, A = s.Color.A },
            Thickness = s.Thickness,
            OutlineEnabled = s.OutlineEnabled,
            OutlineThickness = s.OutlineThickness,
            OutlineColor = new Rgba8 { R = s.OutlineColor.R, G = s.OutlineColor.G, B = s.OutlineColor.B, A = s.OutlineColor.A },
            Kind = s.Type switch
            {
                ShapeType.Line => new CustomShapeKind { Type = "line", X1 = s.X1, Y1 = s.Y1, X2 = s.X2, Y2 = s.Y2 },
                ShapeType.Circle => new CustomShapeKind { Type = "circle", Cx = s.Cx, Cy = s.Cy, Radius = s.Radius, Filled = s.Filled },
                ShapeType.Rect => new CustomShapeKind
                {
                    Type = "rect",
                    X = s.Rx1,
                    Y = s.Ry1,
                    W = Math.Abs(s.Rx2 - s.Rx1),
                    H = Math.Abs(s.Ry2 - s.Ry1),
                    Filled = s.Filled
                },
                ShapeType.Triangle => new CustomShapeKind
                {
                    Type = "triangle",
                    X1 = s.X1,
                    Y1 = s.Y1,
                    X2 = s.X2,
                    Y2 = s.Y2,
                    X3 = s.X3,
                    Y3 = s.Y3,
                    Filled = s.Filled
                },
                _ => new CustomShapeKind { Type = "line" },
            },
        };

        private (float cx, float cy) CanvasToCrosshair(Windows.Foundation.Point pt)
        {
            float halfW = (float)PreviewCanvas.ActualWidth / 2f;
            float halfH = (float)PreviewCanvas.ActualHeight / 2f;
            return (((float)pt.X - halfW) / _zoom, ((float)pt.Y - halfH) / _zoom);
        }

        private (float px, float py) CrosshairToCanvas(float cx, float cy)
        {
            float halfW = (float)PreviewCanvas.ActualWidth / 2f;
            float halfH = (float)PreviewCanvas.ActualHeight / 2f;
            return (halfW + cx * _zoom, halfH + cy * _zoom);
        }

        private (float x, float y) SnapToGrid(float x, float y)
        {
            if (!_snapEnabled) return (x, y);
            return ((float)Math.Round(x / _gridStep) * _gridStep,
                    (float)Math.Round(y / _gridStep) * _gridStep);
        }

        private (float x2, float y2) ApplyAngleSnap(float x1, float y1, float x2, float y2)
        {
            if (!_snapEnabled) return (x2, y2);
            float dx = x2 - x1;
            float dy = y2 - y1;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return (x2, y2);
            float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            float snapped = (float)(Math.Round(angle / _snapAngle) * _snapAngle);
            float rad = (float)(snapped * Math.PI / 180.0);
            return (x1 + dist * (float)Math.Cos(rad), y1 + dist * (float)Math.Sin(rad));
        }

        private void PushUndo() =>
            _undoStack.Push(_editorShapes.Select(s => s.Clone()).ToList());

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count == 0) return;
            _editorShapes.Clear();
            _editorShapes.AddRange(_undoStack.Pop());
            _selectedShapeIdx = -1;
            UpdateEditorShapePropsUI();
            SyncShapesToProfile();
            _ = _store.SaveAsync(_profiles);
        }

        private async void DeleteShapeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShapeIdx < 0 || _selectedShapeIdx >= _editorShapes.Count) return;
            PushUndo();
            _editorShapes.RemoveAt(_selectedShapeIdx);
            _selectedShapeIdx = -1;
            UpdateEditorShapePropsUI();
            SyncShapesToProfile();
            await _store.SaveAsync(_profiles);
        }

        private async void ClearShapesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editorShapes.Count == 0) return;
            var dlg = new ContentDialog
            {
                Title = "Clear all shapes",
                Content = "Remove all custom shapes from this profile?",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
            PushUndo();
            _editorShapes.Clear();
            _selectedShapeIdx = -1;
            UpdateEditorShapePropsUI();
            SyncShapesToProfile();
            await _store.SaveAsync(_profiles);
        }

        private void EditorTool_Changed(object sender, RoutedEventArgs e)
        {
            if (ToolSelect.IsChecked == true) _editorTool = EditorToolMode.Select;
            else if (ToolLine.IsChecked == true) _editorTool = EditorToolMode.Line;
            else if (ToolCircle.IsChecked == true) _editorTool = EditorToolMode.Circle;
            else if (ToolRect.IsChecked == true) _editorTool = EditorToolMode.Rect;
            else if (ToolTriangle.IsChecked == true) _editorTool = EditorToolMode.Triangle;
            _drawing = false;
            _inProgressShape = null;
        }

        private void EditorOption_Changed(object sender, RoutedEventArgs e)
        {
            if (AngleSnapToggle == null || GridToggle == null || SnapAngleCombo == null || PreviewCanvas == null) return;
            _snapEnabled = AngleSnapToggle.IsOn;
            _showGrid = GridToggle.IsOn;
            if (SnapAngleCombo.SelectedItem is ComboBoxItem snap && snap.Tag is string sv
                && float.TryParse(sv, System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture, out float sv2))
                _snapAngle = sv2;
            PreviewCanvas.Invalidate();
        }

        private void SnapAngleCombo_Changed(object sender, SelectionChangedEventArgs e)
            => EditorOption_Changed(sender, null);

        private void ZoomCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ZoomCombo.SelectedItem is ComboBoxItem z && z.Tag is string zv
                && float.TryParse(zv, out float zf))
                _zoom = zf;
            PreviewCanvas.Invalidate();
        }

        private void EditorCanvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!_editorMode) return;
            var pt = e.GetCurrentPoint(PreviewCanvas).Position;
            var (rawCx, rawCy) = CanvasToCrosshair(pt);
            var (cx, cy) = SnapToGrid(rawCx, rawCy);

            if (_editorTool == EditorToolMode.Select)
            {
                if (_selectedShapeIdx >= 0)
                {
                    int h = HitTestHandle(_selectedShapeIdx, cx, cy);
                    if (h >= 0)
                    {
                        _dragging = true;
                        _dragHandleIdx = h;
                        _dragPointerStartCx = cx;
                        _dragPointerStartCy = cy;
                        StoreDragBaseline(_selectedShapeIdx);
                        PreviewCanvas.CapturePointer(e.Pointer);
                        return;
                    }
                }
                int hit = HitTestShapes(cx, cy);
                _selectedShapeIdx = hit;
                if (hit >= 0)
                {
                    _dragging = true;
                    _dragHandleIdx = -1;
                    _dragPointerStartCx = cx;
                    _dragPointerStartCy = cy;
                    StoreDragBaseline(hit);
                    PreviewCanvas.CapturePointer(e.Pointer);
                }
                UpdateEditorShapePropsUI();
                PreviewCanvas.Invalidate();
                return;
            }

            _drawing = true;
            _drawStartCx = cx;
            _drawStartCy = cy;
            _inProgressShape = new EditorShape
            {
                Type = _editorTool == EditorToolMode.Line ? ShapeType.Line
                                 : _editorTool == EditorToolMode.Circle ? ShapeType.Circle
                                 : _editorTool == EditorToolMode.Triangle ? ShapeType.Triangle
                                                                          : ShapeType.Rect,
                Thickness = 2f,
                OutlineEnabled = true,
                OutlineThickness = 1f,
                X1 = cx,
                Y1 = cy,
                X2 = cx,
                Y2 = cy,
                Cx = cx,
                Cy = cy,
                Radius = 0,
                Rx1 = cx,
                Ry1 = cy,
                Rx2 = cx,
                Ry2 = cy,
                X3 = cx,
                Y3 = cy,
            };
            if (_selectedShapeIdx >= 0 && _selectedShapeIdx < _editorShapes.Count)
            {
                var src = _editorShapes[_selectedShapeIdx];
                _inProgressShape.Color = src.Color;
                _inProgressShape.Thickness = src.Thickness;
                _inProgressShape.Filled = src.Filled;
                _inProgressShape.OutlineEnabled = src.OutlineEnabled;
                _inProgressShape.OutlineThickness = src.OutlineThickness;
                _inProgressShape.OutlineColor = src.OutlineColor;
            }
            PreviewCanvas.CapturePointer(e.Pointer);
            PreviewCanvas.Invalidate();
        }

        private void EditorCanvas_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!_editorMode) return;
            var pt = e.GetCurrentPoint(PreviewCanvas).Position;
            var (rawCx, rawCy) = CanvasToCrosshair(pt);
            var (cx, cy) = SnapToGrid(rawCx, rawCy);

            if (_drawing && _inProgressShape != null)
            {
                UpdateInProgressShape(cx, cy);
                PreviewCanvas.Invalidate();
            }
            else if (_dragging && _selectedShapeIdx >= 0)
            {
                ApplyDrag(_selectedShapeIdx, cx, cy);
                SyncShapesToProfile();
                PreviewCanvas.Invalidate();
            }
        }

        private async void EditorCanvas_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!_editorMode) return;
            var pt = e.GetCurrentPoint(PreviewCanvas).Position;
            var (rawCx, rawCy) = CanvasToCrosshair(pt);
            var (cx, cy) = SnapToGrid(rawCx, rawCy);

            if (_drawing && _inProgressShape != null)
            {
                UpdateInProgressShape(cx, cy);
                float size = EstimateShapeSize(_inProgressShape);
                var committed = _inProgressShape;
                _drawing = false;
                _inProgressShape = null;
                PreviewCanvas.ReleasePointerCapture(e.Pointer);

                if (size > 0.5f)
                {
                    PushUndo();
                    _editorShapes.Add(committed);
                    _selectedShapeIdx = _editorShapes.Count - 1;
                    UpdateEditorShapePropsUI();
                    SyncShapesToProfile();
                    await _store.SaveAsync(_profiles);
                }
                PreviewCanvas.Invalidate();
            }
            else if (_dragging)
            {
                _dragging = false;
                PreviewCanvas.ReleasePointerCapture(e.Pointer);
                SyncShapesToProfile();
                await _store.SaveAsync(_profiles);
            }
        }

        private void UpdateInProgressShape(float cx, float cy)
        {
            var s = _inProgressShape;
            switch (s.Type)
            {
                case ShapeType.Line:
                    (s.X2, s.Y2) = ApplyAngleSnap(s.X1, s.Y1, cx, cy);
                    break;
                case ShapeType.Circle:
                    float dx = cx - _drawStartCx;
                    float dy = cy - _drawStartCy;
                    s.Cx = _drawStartCx; s.Cy = _drawStartCy;
                    s.Radius = (float)Math.Sqrt(dx * dx + dy * dy);
                    break;
                case ShapeType.Rect:
                    s.Rx1 = _drawStartCx; s.Ry1 = _drawStartCy;
                    s.Rx2 = cx; s.Ry2 = cy;
                    break;
                case ShapeType.Triangle:
                    {
                        s.X1 = _drawStartCx; s.Y1 = _drawStartCy;
                        s.X2 = cx; s.Y2 = cy;
                        float mx = (s.X1 + s.X2) * 0.5f;
                        float my = (s.Y1 + s.Y2) * 0.5f;
                        float edx = s.X2 - s.X1;
                        float edy = s.Y2 - s.Y1;
                        float h = (float)Math.Sqrt(edx * edx + edy * edy) * (float)(Math.Sqrt(3.0) / 2.0);
                        float len = (float)Math.Sqrt(edx * edx + edy * edy);
                        if (len < 0.001f) { s.X3 = mx; s.Y3 = my; break; }
                        float nx = -edy / len, ny = edx / len;
                        s.X3 = mx + nx * h;
                        s.Y3 = my + ny * h;
                        break;
                    }
            }
        }

        private static float EstimateShapeSize(EditorShape s) => s.Type switch
        {
            ShapeType.Line => (float)Math.Sqrt(Math.Pow(s.X2 - s.X1, 2) + Math.Pow(s.Y2 - s.Y1, 2)),
            ShapeType.Circle => s.Radius,
            ShapeType.Rect => Math.Max(Math.Abs(s.Rx2 - s.Rx1), Math.Abs(s.Ry2 - s.Ry1)),
            ShapeType.Triangle => (float)Math.Sqrt(Math.Pow(s.X2 - s.X1, 2) + Math.Pow(s.Y2 - s.Y1, 2)),
            _ => 0,
        };

        private int HitTestShapes(float cx, float cy)
        {
            const float lineTol = 5f;
            const float handleTol = 8f;

            for (int i = _editorShapes.Count - 1; i >= 0; i--)
            {
                var s = _editorShapes[i];
                switch (s.Type)
                {
                    case ShapeType.Line:
                        if (DistToSegment(cx, cy, s.X1, s.Y1, s.X2, s.Y2) < lineTol / _zoom)
                            return i;
                        break;
                    case ShapeType.Circle:
                        {
                            float d = (float)Math.Sqrt(Math.Pow(cx - s.Cx, 2) + Math.Pow(cy - s.Cy, 2));
                            if (s.Filled ? d <= s.Radius : Math.Abs(d - s.Radius) < handleTol / _zoom)
                                return i;
                            break;
                        }
                    case ShapeType.Rect:
                        {
                            float minX = Math.Min(s.Rx1, s.Rx2), maxX = Math.Max(s.Rx1, s.Rx2);
                            float minY = Math.Min(s.Ry1, s.Ry2), maxY = Math.Max(s.Ry1, s.Ry2);
                            float edge = handleTol / _zoom;
                            if (s.Filled)
                            {
                                if (cx >= minX && cx <= maxX && cy >= minY && cy <= maxY) return i;
                            }
                            else
                            {
                                if (cx >= minX - edge && cx <= maxX + edge &&
                                    cy >= minY - edge && cy <= maxY + edge &&
                                    (cx <= minX + edge || cx >= maxX - edge ||
                                     cy <= minY + edge || cy >= maxY - edge))
                                    return i;
                            }
                            break;
                        }
                    case ShapeType.Triangle:
                        {
                            float tol = lineTol / _zoom;
                            if (s.Filled)
                            {
                                if (PointInTriangle(cx, cy, s.X1, s.Y1, s.X2, s.Y2, s.X3, s.Y3)) return i;
                            }
                            else
                            {
                                if (DistToSegment(cx, cy, s.X1, s.Y1, s.X2, s.Y2) < tol ||
                                    DistToSegment(cx, cy, s.X2, s.Y2, s.X3, s.Y3) < tol ||
                                    DistToSegment(cx, cy, s.X3, s.Y3, s.X1, s.Y1) < tol) return i;
                            }
                            break;
                        }
                }
            }
            return -1;
        }

        private int HitTestHandle(int idx, float cx, float cy)
        {
            if (idx < 0 || idx >= _editorShapes.Count) return -1;
            float tol = 8f / _zoom;
            var s = _editorShapes[idx];
            List<(float, float)> handles = GetHandles(s);
            for (int i = 0; i < handles.Count; i++)
            {
                float d = (float)Math.Sqrt(Math.Pow(cx - handles[i].Item1, 2) +
                                           Math.Pow(cy - handles[i].Item2, 2));
                if (d < tol) return i;
            }
            return -1;
        }

        private static List<(float, float)> GetHandles(EditorShape s) => s.Type switch
        {
            ShapeType.Line => new List<(float, float)> { (s.X1, s.Y1), (s.X2, s.Y2) },
            ShapeType.Circle => new List<(float, float)>
            {
                (s.Cx, s.Cy),
                (s.Cx + s.Radius, s.Cy),
                (s.Cx, s.Cy - s.Radius),
            },
            ShapeType.Rect => new List<(float, float)>
            {
                (s.Rx1, s.Ry1), (s.Rx2, s.Ry1),
                (s.Rx1, s.Ry2), (s.Rx2, s.Ry2),
            },
            ShapeType.Triangle => new List<(float, float)> { (s.X1, s.Y1), (s.X2, s.Y2), (s.X3, s.Y3) },
            _ => new List<(float, float)>(),
        };

        private void StoreDragBaseline(int idx)
        {
            var s = _editorShapes[idx];
            _dragX1 = s.X1; _dragY1 = s.Y1; _dragX2 = s.X2; _dragY2 = s.Y2;
            _dragCx = s.Cx; _dragCy = s.Cy; _dragRadius = s.Radius;
            _dragRx1 = s.Rx1; _dragRy1 = s.Ry1; _dragRx2 = s.Rx2; _dragRy2 = s.Ry2;
            _dragX3 = s.X3; _dragY3 = s.Y3;
        }

        private void ApplyDrag(int idx, float cx, float cy)
        {
            float ddx = cx - _dragPointerStartCx;
            float ddy = cy - _dragPointerStartCy;
            var s = _editorShapes[idx];

            if (_dragHandleIdx < 0)
            {
                switch (s.Type)
                {
                    case ShapeType.Line:
                        s.X1 = _dragX1 + ddx; s.Y1 = _dragY1 + ddy;
                        s.X2 = _dragX2 + ddx; s.Y2 = _dragY2 + ddy;
                        break;
                    case ShapeType.Circle:
                        s.Cx = _dragCx + ddx; s.Cy = _dragCy + ddy;
                        break;
                    case ShapeType.Rect:
                        s.Rx1 = _dragRx1 + ddx; s.Ry1 = _dragRy1 + ddy;
                        s.Rx2 = _dragRx2 + ddx; s.Ry2 = _dragRy2 + ddy;
                        break;
                    case ShapeType.Triangle:
                        s.X1 = _dragX1 + ddx; s.Y1 = _dragY1 + ddy;
                        s.X2 = _dragX2 + ddx; s.Y2 = _dragY2 + ddy;
                        s.X3 = _dragX3 + ddx; s.Y3 = _dragY3 + ddy;
                        break;
                }
            }
            else
            {
                switch (s.Type)
                {
                    case ShapeType.Line:
                        if (_dragHandleIdx == 0) { s.X1 = _dragX1 + ddx; s.Y1 = _dragY1 + ddy; }
                        else { s.X2 = _dragX2 + ddx; s.Y2 = _dragY2 + ddy; }
                        if (_dragHandleIdx == 0)
                            (s.X1, s.Y1) = ApplyAngleSnap(s.X2, s.Y2, s.X1, s.Y1);
                        else
                            (s.X2, s.Y2) = ApplyAngleSnap(s.X1, s.Y1, s.X2, s.Y2);
                        break;
                    case ShapeType.Circle:
                        if (_dragHandleIdx == 0) { s.Cx = _dragCx + ddx; s.Cy = _dragCy + ddy; }
                        else
                        {
                            float hx = (_dragHandleIdx == 1 ? _dragCx + _dragRadius : _dragCx) + ddx;
                            float hy = (_dragHandleIdx == 2 ? _dragCy - _dragRadius : _dragCy) + ddy;
                            s.Radius = (float)Math.Sqrt(Math.Pow(hx - s.Cx, 2) + Math.Pow(hy - s.Cy, 2));
                        }
                        break;
                    case ShapeType.Rect:
                        switch (_dragHandleIdx)
                        {
                            case 0: s.Rx1 = _dragRx1 + ddx; s.Ry1 = _dragRy1 + ddy; break;
                            case 1: s.Rx2 = _dragRx2 + ddx; s.Ry1 = _dragRy1 + ddy; break;
                            case 2: s.Rx1 = _dragRx1 + ddx; s.Ry2 = _dragRy2 + ddy; break;
                            case 3: s.Rx2 = _dragRx2 + ddx; s.Ry2 = _dragRy2 + ddy; break;
                        }
                        break;
                    case ShapeType.Triangle:
                        if (_dragHandleIdx == 0) { s.X1 = _dragX1 + ddx; s.Y1 = _dragY1 + ddy; }
                        else if (_dragHandleIdx == 1) { s.X2 = _dragX2 + ddx; s.Y2 = _dragY2 + ddy; }
                        else { s.X3 = _dragX3 + ddx; s.Y3 = _dragY3 + ddy; }
                        break;
                }
            }
        }

        private static float DistToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float len2 = dx * dx + dy * dy;
            if (len2 < 0.0001f)
                return (float)Math.Sqrt(Math.Pow(px - ax, 2) + Math.Pow(py - ay, 2));
            float t = ((px - ax) * dx + (py - ay) * dy) / len2;
            t = Math.Max(0, Math.Min(1, t));
            return (float)Math.Sqrt(Math.Pow(px - (ax + t * dx), 2) + Math.Pow(py - (ay + t * dy), 2));
        }

        private static bool PointInTriangle(float px, float py,
            float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = Sign(px, py, ax, ay, bx, by);
            float d2 = Sign(px, py, bx, by, cx, cy);
            float d3 = Sign(px, py, cx, cy, ax, ay);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static float Sign(float px, float py, float ax, float ay, float bx, float by)
            => (px - bx) * (ay - by) - (ax - bx) * (py - by);

        private void DrawEditorOverlay(Microsoft.Graphics.Canvas.CanvasDrawingSession ds)
        {
            float cw = (float)PreviewCanvas.ActualWidth;
            float ch = (float)PreviewCanvas.ActualHeight;
            float hx = cw / 2f, hy = ch / 2f;

            if (_showGrid)
            {
                float gridUnit = 10f * _zoom;
                var gridColor = Color.FromArgb(40, 100, 140, 200);
                for (float gx = hx % gridUnit; gx < cw; gx += gridUnit)
                    ds.DrawLine(gx, 0, gx, ch, gridColor, 0.5f);
                for (float gy = hy % gridUnit; gy < ch; gy += gridUnit)
                    ds.DrawLine(0, gy, cw, gy, gridColor, 0.5f);
                ds.DrawLine(hx, 0, hx, ch, Color.FromArgb(80, 100, 160, 255), 1f);
                ds.DrawLine(0, hy, cw, hy, Color.FromArgb(80, 100, 160, 255), 1f);
            }

            ds.DrawLine(hx - 6, hy, hx + 6, hy, Color.FromArgb(180, 255, 80, 80), 1.5f);
            ds.DrawLine(hx, hy - 6, hx, hy + 6, Color.FromArgb(180, 255, 80, 80), 1.5f);

            for (int i = 0; i < _editorShapes.Count; i++)
            {
                DrawEditorShape(ds, _editorShapes[i]);
                if (i == _selectedShapeIdx)
                    DrawHandles(ds, _editorShapes[i]);
            }

            if (_drawing && _inProgressShape != null)
                DrawEditorShape(ds, _inProgressShape);
        }

        private void DrawEditorShape(Microsoft.Graphics.Canvas.CanvasDrawingSession ds, EditorShape s)
        {
            float hx = (float)PreviewCanvas.ActualWidth / 2f;
            float hy = (float)PreviewCanvas.ActualHeight / 2f;
            float z = _zoom;

            switch (s.Type)
            {
                case ShapeType.Line:
                    {
                        float px1 = hx + s.X1 * z, py1 = hy + s.Y1 * z;
                        float px2 = hx + s.X2 * z, py2 = hy + s.Y2 * z;
                        if (s.OutlineEnabled)
                            ds.DrawLine(px1, py1, px2, py2, s.OutlineColor, (s.Thickness + s.OutlineThickness * 2f) * z / 4f);
                        ds.DrawLine(px1, py1, px2, py2, s.Color, s.Thickness * z / 4f);
                        break;
                    }
                case ShapeType.Circle:
                    {
                        float pcx = hx + s.Cx * z, pcy = hy + s.Cy * z;
                        float pr = s.Radius * z;
                        if (s.Filled)
                        {
                            if (s.OutlineEnabled)
                                ds.FillCircle(pcx, pcy, pr + s.OutlineThickness * z / 4f, s.OutlineColor);
                            ds.FillCircle(pcx, pcy, pr, s.Color);
                        }
                        else
                        {
                            float sw = s.Thickness * z / 4f;
                            if (s.OutlineEnabled)
                                ds.DrawCircle(pcx, pcy, pr, s.OutlineColor, sw + s.OutlineThickness * z / 2f);
                            ds.DrawCircle(pcx, pcy, pr, s.Color, sw);
                        }
                        break;
                    }
                case ShapeType.Rect:
                    {
                        float minX = (hx + Math.Min(s.Rx1, s.Rx2) * z);
                        float minY = (hy + Math.Min(s.Ry1, s.Ry2) * z);
                        float rw = Math.Abs(s.Rx2 - s.Rx1) * z;
                        float rh = Math.Abs(s.Ry2 - s.Ry1) * z;
                        if (s.Filled)
                        {
                            if (s.OutlineEnabled)
                            {
                                float ot = s.OutlineThickness * z / 4f;
                                ds.FillRectangle(minX - ot, minY - ot, rw + ot * 2f, rh + ot * 2f, s.OutlineColor);
                            }
                            ds.FillRectangle(minX, minY, rw, rh, s.Color);
                        }
                        else
                        {
                            float sw = s.Thickness * z / 4f;
                            if (s.OutlineEnabled)
                                ds.DrawRectangle(minX, minY, rw, rh, s.OutlineColor, sw + s.OutlineThickness * z / 2f);
                            ds.DrawRectangle(minX, minY, rw, rh, s.Color, sw);
                        }
                        break;
                    }
                case ShapeType.Triangle:
                    {
                        float px1 = hx + s.X1 * z, py1 = hy + s.Y1 * z;
                        float px2 = hx + s.X2 * z, py2 = hy + s.Y2 * z;
                        float px3 = hx + s.X3 * z, py3 = hy + s.Y3 * z;
                        var verts = new System.Numerics.Vector2[] {
                        new System.Numerics.Vector2(px1, py1),
                        new System.Numerics.Vector2(px2, py2),
                        new System.Numerics.Vector2(px3, py3),
                    };
                        var geo = Microsoft.Graphics.Canvas.Geometry.CanvasGeometry.CreatePolygon(ds.Device, verts);
                        if (s.Filled)
                        {
                            if (s.OutlineEnabled)
                            {
                                using var strokeGeo = geo.Stroke(s.OutlineThickness * z / 4f * 2f + s.Thickness * z / 4f);
                                ds.FillGeometry(strokeGeo, s.OutlineColor);
                            }
                            ds.FillGeometry(geo, s.Color);
                        }
                        else
                        {
                            float sw = s.Thickness * z / 4f;
                            if (s.OutlineEnabled)
                            {
                                ds.DrawLine(px1, py1, px2, py2, s.OutlineColor, sw + s.OutlineThickness * z / 2f);
                                ds.DrawLine(px2, py2, px3, py3, s.OutlineColor, sw + s.OutlineThickness * z / 2f);
                                ds.DrawLine(px3, py3, px1, py1, s.OutlineColor, sw + s.OutlineThickness * z / 2f);
                            }
                            ds.DrawLine(px1, py1, px2, py2, s.Color, sw);
                            ds.DrawLine(px2, py2, px3, py3, s.Color, sw);
                            ds.DrawLine(px3, py3, px1, py1, s.Color, sw);
                        }
                        break;
                    }
            }
        }

        private void DrawHandles(Microsoft.Graphics.Canvas.CanvasDrawingSession ds, EditorShape s)
        {
            var handles = GetHandles(s);
            float hx = (float)PreviewCanvas.ActualWidth / 2f;
            float hy = (float)PreviewCanvas.ActualHeight / 2f;
            var fill = Color.FromArgb(230, 255, 255, 255);
            var border = Color.FromArgb(230, 30, 120, 220);
            foreach (var (hcx, hcy) in handles)
            {
                float px = hx + hcx * _zoom;
                float py = hy + hcy * _zoom;
                ds.FillCircle(px, py, 6f, fill);
                ds.DrawCircle(px, py, 6f, border, 1.5f);
            }
            var selColor = Color.FromArgb(60, 30, 120, 255);
            switch (s.Type)
            {
                case ShapeType.Circle:
                    ds.DrawCircle(hx + s.Cx * _zoom, hy + s.Cy * _zoom,
                                  s.Radius * _zoom, selColor, 1f);
                    break;
                case ShapeType.Rect:
                    float rx = hx + Math.Min(s.Rx1, s.Rx2) * _zoom;
                    float ry = hy + Math.Min(s.Ry1, s.Ry2) * _zoom;
                    ds.DrawRectangle(rx, ry, Math.Abs(s.Rx2 - s.Rx1) * _zoom,
                                     Math.Abs(s.Ry2 - s.Ry1) * _zoom, selColor, 1f);
                    break;
                case ShapeType.Triangle:
                    {
                        float tx1 = hx + s.X1 * _zoom, ty1 = hy + s.Y1 * _zoom;
                        float tx2 = hx + s.X2 * _zoom, ty2 = hy + s.Y2 * _zoom;
                        float tx3 = hx + s.X3 * _zoom, ty3 = hy + s.Y3 * _zoom;
                        ds.DrawLine(tx1, ty1, tx2, ty2, selColor, 1f);
                        ds.DrawLine(tx2, ty2, tx3, ty3, selColor, 1f);
                        ds.DrawLine(tx3, ty3, tx1, ty1, selColor, 1f);
                        break;
                    }
            }
        }

        private bool _shapePropsLoading;

        private void UpdateEditorShapePropsUI()
        {
            if (_selectedShapeIdx < 0 || _selectedShapeIdx >= _editorShapes.Count)
            {
                EditorHintText.Text = _editorTool == EditorToolMode.Select
                    ? "Click a shape to select it."
                    : "Click and drag on the canvas to draw.";
                ShapePropsContent.Visibility = Visibility.Collapsed;
                return;
            }

            EditorHintText.Text = $"Shape {_selectedShapeIdx + 1} of {_editorShapes.Count} selected.";
            ShapePropsContent.Visibility = Visibility.Visible;

            _shapePropsLoading = true;
            var s = _editorShapes[_selectedShapeIdx];
            ShapeColorPicker.Color = s.Color;
            ShapeColorSwatch.Background = new Windows.UI.Xaml.Media.SolidColorBrush(s.Color);
            ShapeThicknessSlider.Value = s.Thickness;
            ShapeThicknessLabel.Text = $"{s.Thickness:F1}";
            ShapeFilledToggle.IsOn = s.Filled;
            ShapeFilledToggle.Visibility = s.Type != ShapeType.Line
                                               ? Visibility.Visible : Visibility.Collapsed;
            ShapeOutlineToggle.IsOn = s.OutlineEnabled;
            ShapeOutlineThicknessSlider.Value = s.OutlineThickness;
            ShapeOutlineThicknessLabel.Text = $"{s.OutlineThickness:F1}";
            ShapeOutlineColorPicker.Color = s.OutlineColor;
            ShapeOutlineColorSwatch.Background = new Windows.UI.Xaml.Media.SolidColorBrush(s.OutlineColor);
            _shapePropsLoading = false;
        }

        private async void ShapeProp_Changed(object sender, RoutedEventArgs e) => await ApplyShapeProps();
        private async void ShapeProp_Changed(object sender, RangeBaseValueChangedEventArgs e) => await ApplyShapeProps();

        private async void ShapeColor_Changed(ColorPicker sender, ColorChangedEventArgs e)
        {
            if (_shapePropsLoading || _selectedShapeIdx < 0) return;
            _editorShapes[_selectedShapeIdx].Color = e.NewColor;
            ShapeColorSwatch.Background = new Windows.UI.Xaml.Media.SolidColorBrush(e.NewColor);
            SyncShapesToProfile();
            await _store.SaveAsync(_profiles);
        }

        private async void ShapeSwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShapeIdx < 0) return;
            if (sender is Button btn && btn.Tag is string hex)
            {
                var color = HexToColor(hex);
                _editorShapes[_selectedShapeIdx].Color = color;
                ShapeColorSwatch.Background = new Windows.UI.Xaml.Media.SolidColorBrush(color);
                _shapePropsLoading = true;
                ShapeColorPicker.Color = color;
                _shapePropsLoading = false;
                SyncShapesToProfile();
                await _store.SaveAsync(_profiles);
                PreviewCanvas.Invalidate();
            }
        }

        private static Windows.UI.Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 8)
                return Windows.UI.Color.FromArgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            if (hex.Length == 6)
                return Windows.UI.Color.FromArgb(255,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            return Windows.UI.Colors.White;
        }

        private async void ShapeOutlineColor_Changed(ColorPicker sender, ColorChangedEventArgs e)
        {
            if (_shapePropsLoading || _selectedShapeIdx < 0) return;
            _editorShapes[_selectedShapeIdx].OutlineColor = e.NewColor;
            ShapeOutlineColorSwatch.Background = new Windows.UI.Xaml.Media.SolidColorBrush(e.NewColor);
            SyncShapesToProfile();
            await _store.SaveAsync(_profiles);
        }

        private async Task ApplyShapeProps()
        {
            if (_shapePropsLoading || _selectedShapeIdx < 0) return;
            var s = _editorShapes[_selectedShapeIdx];
            s.Thickness = (float)ShapeThicknessSlider.Value;
            s.Filled = ShapeFilledToggle.IsOn;
            s.OutlineEnabled = ShapeOutlineToggle.IsOn;
            s.OutlineThickness = (float)ShapeOutlineThicknessSlider.Value;
            ShapeThicknessLabel.Text = $"{s.Thickness:F1}";
            ShapeOutlineThicknessLabel.Text = $"{s.OutlineThickness:F1}";
            SyncShapesToProfile();
            await _store.SaveAsync(_profiles);
        }

        private async Task LoadCommunityAsync()
        {
            CommunityStatusText.Text = "Loading...";
            CommunityTilesPanel.Items.Clear();
            CommunityOfflineBanner.Visibility = Visibility.Collapsed;

            bool online = await CommunityService.IsServerReachableAsync();
            if (!online)
            {
                CommunityStatusText.Text = "Server offline.";
                CommunityOfflineBanner.Visibility = Visibility.Visible;
                return;
            }

            var result = await CommunityService.GetCrosshairsAsync();
            CommunityTilesPanel.Items.Clear();

            if (result.Items.Count == 0)
            {
                CommunityStatusText.Text = "No community crosshairs yet. Be the first to share!";
                return;
            }

            CommunityStatusText.Text = $"{result.Total} crosshair{(result.Total == 1 ? "" : "s")} shared by the community.";

            foreach (var ch in result.Items)
                CommunityTilesPanel.Items.Add(BuildCommunityTile(ch));
        }

        private Border BuildCommunityTile(CommunityCrosshair ch)
        {
            var tagsText = ch.Tags != null && ch.Tags.Count > 0
                ? string.Join("  ", ch.Tags)
                : "";

            var loadBtn = new Button { Content = "Load", HorizontalAlignment = HorizontalAlignment.Stretch };
            loadBtn.Click += (s, e) => LoadCommunityCode(ch.Code);

            var likeBtn = new Button { Content = $"Like  {ch.Likes}", HorizontalAlignment = HorizontalAlignment.Stretch };
            likeBtn.Click += async (s, e) =>
            {
                bool ok = await CommunityService.LikeCrosshairAsync(ch.Id);
                if (ok) likeBtn.Content = $"Like  {ch.Likes + 1}";
            };

            var panel = new StackPanel { Spacing = 6 };
            panel.Children.Add(new Border
            {
                Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 13, 18)),
                CornerRadius = new CornerRadius(8),
                Height = 80,
                Child = new TextBlock
                {
                    Text = ch.Name.Length > 0 ? ch.Name[0].ToString().ToUpper() : "?",
                    FontSize = 28,
                    Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            panel.Children.Add(new TextBlock
            {
                Text = ch.Name,
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.White),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"by {ch.Author}",
                Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255)),
                FontSize = 11
            });
            if (!string.IsNullOrEmpty(tagsText))
                panel.Children.Add(new TextBlock
                {
                    Text = tagsText,
                    Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(130, 255, 255, 255)),
                    FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            panel.Children.Add(loadBtn);
            panel.Children.Add(likeBtn);

            return new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 21, 27)),
                BorderBrush = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["PanelBorderBrush"],
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 6, 6),
                Width = 148,
                Child = panel
            };
        }

        private async void LoadCommunityCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                var data = RustInterop.DecodeShareCode(code);
                if (data == null) return;
                _activeProfile.Crosshair = data;
                LoadProfileIntoControls(_activeProfile);
                await _store.SaveAsync(_profiles);
                PreviewCanvas.Invalidate();
            }
            catch { }
        }

        private async void ShareCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Share Your Crosshair",
                PrimaryButtonText = "Share",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var nameBox = new TextBox { PlaceholderText = "Name (e.g. My Pro Aim)", Margin = new Thickness(0, 0, 0, 8) };
            var authorBox = new TextBox { PlaceholderText = "Author / Gamertag" };
            var tagsBox = new TextBox { PlaceholderText = "Tags, comma-separated (optional)", Margin = new Thickness(0, 8, 0, 0) };

            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(nameBox);
            stack.Children.Add(authorBox);
            stack.Children.Add(tagsBox);
            dialog.Content = stack;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            string name = nameBox.Text.Trim();
            string author = authorBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) { name = "Untitled"; }

            string code;
            try { code = RustInterop.EncodeShareCode(_activeProfile.Crosshair); }
            catch { return; }

            var tags = tagsBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var tagList = new List<string>();
            foreach (var t in tags) { var trimmed = t.Trim(); if (!string.IsNullOrEmpty(trimmed)) tagList.Add(trimmed); }

            long? id = await CommunityService.SubmitCrosshairAsync(name, author, code, tagList);

            var confirm = new ContentDialog
            {
                Title = id.HasValue ? "Shared!" : "Failed",
                Content = id.HasValue ? "Your crosshair has been shared with the community." : "Could not reach the server. Make sure crosshair-server is running.",
                CloseButtonText = "OK"
            };
            await confirm.ShowAsync();

            if (id.HasValue) await LoadCommunityAsync();
        }

        private async void CommunityRetry_Click(object sender, RoutedEventArgs e)
        {
            await LoadCommunityAsync();
        }
    }
}
