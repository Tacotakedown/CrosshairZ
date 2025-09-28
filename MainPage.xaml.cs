using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using System.Diagnostics;
using Newtonsoft.Json;
using Windows.Storage;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Newtonsoft.Json.Linq;


namespace CrosshairZ
{

    public class CrosshairData
    {
        [JsonProperty]
        public double Width { get; set; }
        [JsonProperty]
        public double Height { get; set; }
        [JsonProperty]
        public double Thickness { get; set; }
        [JsonProperty]
        public double DotSize { get; set; }
        [JsonProperty]
        public string Color { get; set; }
        [JsonProperty]
        public bool ShowDot { get; set; }
        [JsonProperty]
        public double Gap { get; set; }
        [JsonProperty]
        public bool ShowBorder { get; set; }
        [JsonProperty]
        public double BorderSize { get; set; }
        [JsonProperty]
        public string BorderColor { get; set; }
        [JsonProperty]
        public double Opacity { get; set; }
        [JsonProperty]
        public bool AntiAliasing { get; set; }


        public CrosshairData()
        {
            Width = 6.0;
            Height = 6.0;
            Thickness = 3.0;
            DotSize = 0.0;
            Color = "#06E8FF";
            ShowDot = true;
            Gap = 6.0;
            ShowBorder = true;
            BorderSize = 1.0;
            BorderColor = "#000000";
            Opacity = 1.0;
            AntiAliasing = false;
        }
    }

    public class CrosshairProfile
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Crosshair")]
        public CrosshairData Crosshair { get; set; }
    }

    public class CrosshairProfileCollection
    {
        public List<CrosshairProfile> Profiles { get; set; } = new List<CrosshairProfile>();
    }



    public sealed partial class MainPage : Page
    {
        private XboxGameBarWidget _widget;
        private CrosshairProfileCollection mProfileCollection;
        private bool initialized = false;
        private const string ProfileFileName = "crosshair_profile.json";
        private bool isLinked = true;


        public MainPage()
        {
            InitializeComponent();
            mProfileCollection = new CrosshairProfileCollection();
        }

        public MainPage(XboxGameBarWidget widget) : this()
        {
            _widget = widget;
            _widget.GameBarDisplayModeChanged += Widget_DisplayModeChanged;
            UpdateSettingsPanelVisibility(_widget.GameBarDisplayMode);
            LoadProfile();
            LoadProfilesUI();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is XboxGameBarWidget widget)
            {
                _widget = widget;
                _widget.GameBarDisplayModeChanged += Widget_DisplayModeChanged;
                UpdateSettingsPanelVisibility(_widget.GameBarDisplayMode);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (_widget != null)
            {
                _widget.GameBarDisplayModeChanged -= Widget_DisplayModeChanged;
            }
        }
        private async void Widget_DisplayModeChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                UpdateSettingsPanelVisibility(sender.GameBarDisplayMode);
            });
        }

        private void UpdateSettingsPanelVisibility(XboxGameBarDisplayMode displayMode)
        {
            if (settingsStackPanel != null)
            {
                settingsStackPanel.Visibility = (displayMode == XboxGameBarDisplayMode.PinnedOnly)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                Debug.WriteLine($"SettingsStackPanel visibility set to: {settingsStackPanel.Visibility}");
            }
            else
            {
                Debug.WriteLine("settingsStackPanel is null");
            }

            if (Guide != null)
            {
                Guide.Visibility = (displayMode == XboxGameBarDisplayMode.PinnedOnly) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private async void HeightSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Height = e.NewValue;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                        if (isLinked)
                        {
                            WidthSlider.Value = e.NewValue;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("crosshairData is null.");
                }
            }

        }

        private async void WidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Width = e.NewValue;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                        if (isLinked)
                        {
                            HeightSlider.Value = e.NewValue;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("crosshairData is null.");
                }
            }

        }


        private void OnLinkButtonClick(object sender, RoutedEventArgs e)
        {

            isLinked = !isLinked;


            if (isLinked)
            {
                LinkButton.Content = "🔗";
                WidthSlider.Value = HeightSlider.Value;
            }
            else
            {
                LinkButton.Content = "🔓";
            }
        }

        private async void ThicknessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Thickness = e.NewValue;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void DotSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.DotSize = e.NewValue;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void BorderSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.BorderSize = e.NewValue;
                    if (initialized)
                    {
                        await UpdateCrosshair();

                    }
                }
            }
        }

        private async void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Color = "#" + args.NewColor.R.ToString("X2") + args.NewColor.G.ToString("X2") + args.NewColor.B.ToString("X2");
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Opacity = args.NewColor.A;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void ColorPicker_ColorBorderChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor = "#" + args.NewColor.R.ToString("X2") + args.NewColor.G.ToString("X2") + args.NewColor.B.ToString("X2");
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void GapSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Gap = e.NewValue;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void CheckBoxAA_Checked(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.AntiAliasing = true;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                    else
                    {
                        await LoadProfile();
                        LoadProfilesUI();
                    }
                }
            }
        }
        private async void CheckBoxAA_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.AntiAliasing = false;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void CheckBoxBorder_Checked(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder = true;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                    else
                    {
                        await LoadProfile();
                        LoadProfilesUI();
                    }
                }
            }
        }
        private async void CheckBoxBorder_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder = false;
                    if (initialized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private void OnDrawCrosshair(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (mProfileCollection.Profiles.Count == 0)
            {
                return;
            }
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                float centerX = (float)sender.ActualWidth / 2;
                float centerY = (float)sender.ActualHeight / 2;

                float width = (float)mProfileCollection.Profiles[selectedIndex].Crosshair.Width;
                float height = (float)mProfileCollection.Profiles[selectedIndex].Crosshair.Height;
                float thickness = (float)mProfileCollection.Profiles[selectedIndex].Crosshair.Thickness;
                float gap = (float)mProfileCollection.Profiles[selectedIndex].Crosshair.Gap;
                float dotThickness = (float)mProfileCollection.Profiles[selectedIndex].Crosshair.DotSize;
                Color crosshairColor = ColorHelper.FromArgb(
                    (byte)mProfileCollection.Profiles[selectedIndex].Crosshair.Opacity,
                    byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.Color.Substring(1, 2), System.Globalization.NumberStyles.HexNumber), // R value
                    byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.Color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber), // G value
                    byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.Color.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)  // B value
                );
                bool showDot = mProfileCollection.Profiles[selectedIndex].Crosshair.ShowDot;
                bool showBorder = mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder;
                float borderSize = (float)mProfileCollection.Profiles[selectedIndex].Crosshair.BorderSize;
                Color borderColor = ColorHelper.FromArgb(
                    (byte)mProfileCollection.Profiles[selectedIndex].Crosshair.Opacity,
                    byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber), // R value
                    byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber), // G value
                    byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)  // B value
                );
                CanvasAntialiasing antiAliasing = mProfileCollection.Profiles[selectedIndex].Crosshair.AntiAliasing ? CanvasAntialiasing.Antialiased : CanvasAntialiasing.Aliased;




                float dpiScale = sender.DpiScale;

                float effectiveThickness = thickness / dpiScale;

                if (effectiveThickness < 1.0f)
                    effectiveThickness = 1.0f;

                float adjustedCenterX, adjustedCenterY;

                if (thickness <= 1.0f)
                {
                    adjustedCenterX = (float)Math.Floor(centerX) + 0.5f;
                    adjustedCenterY = (float)Math.Floor(centerY) + 0.5f;
                }
                else
                {
                    adjustedCenterX = centerX;
                    adjustedCenterY = centerY;
                }


                args.DrawingSession.Antialiasing = antiAliasing;

                // Borders ------------------------------------------
                if (showBorder)
                {
                    if (width > 0.0 && height > 0.0)
                    {
                        args.DrawingSession.DrawLine(adjustedCenterX - width - gap - borderSize / 2, adjustedCenterY, adjustedCenterX - gap + borderSize / 2, adjustedCenterY, borderColor, borderSize + effectiveThickness); // Left line
                        args.DrawingSession.DrawLine(adjustedCenterX + gap - borderSize / 2, adjustedCenterY, adjustedCenterX + width + gap + borderSize / 2, adjustedCenterY, borderColor, borderSize + effectiveThickness); // Right line
                        args.DrawingSession.DrawLine(adjustedCenterX, adjustedCenterY - height - gap - borderSize / 2, adjustedCenterX, adjustedCenterY - gap + borderSize / 2, borderColor, borderSize + effectiveThickness); // Top line
                        args.DrawingSession.DrawLine(adjustedCenterX, adjustedCenterY + gap - borderSize / 2, adjustedCenterX, adjustedCenterY + height + gap + borderSize / 2, borderColor, borderSize + effectiveThickness); // Bottom line
                    }
                    if (dotThickness != 0.0)
                    {
                        args.DrawingSession.FillCircle(adjustedCenterX, adjustedCenterY, dotThickness + borderSize / 2, borderColor);
                    }
                }

                // Crosshair ----------------------------------------
                args.DrawingSession.DrawLine(adjustedCenterX - width - gap, adjustedCenterY, adjustedCenterX - gap, adjustedCenterY, crosshairColor, effectiveThickness); // Left line
                args.DrawingSession.DrawLine(adjustedCenterX + gap, adjustedCenterY, adjustedCenterX + width + gap, adjustedCenterY, crosshairColor, effectiveThickness); // Right line
                args.DrawingSession.DrawLine(adjustedCenterX, adjustedCenterY - height - gap, adjustedCenterX, adjustedCenterY - gap, crosshairColor, effectiveThickness); // Top line
                args.DrawingSession.DrawLine(adjustedCenterX, adjustedCenterY + gap, adjustedCenterX, adjustedCenterY + height + gap, crosshairColor, effectiveThickness); // Bottom line
                args.DrawingSession.FillCircle(adjustedCenterX, adjustedCenterY, dotThickness, crosshairColor);
            }
        }

        private async Task UpdateCrosshair()
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                try
                {
                    await SaveProfile(true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating crosshair: {ex.Message}");
                }
            }
            // Easiest way that I found to force a rerender
            CrosshairCanvas.Invalidate();

        }
        private async Task SaveProfile(bool autosave = false)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                try
                {
                    if (autosave && mProfileCollection.Profiles.Count > 1)
                    {
                        mProfileCollection.Profiles[0].Crosshair = mProfileCollection.Profiles[selectedIndex].Crosshair;
                    }
                    for (int i = mProfileCollection.Profiles.Count - 1; i >= 0; i--)
                    {
                        var profile = mProfileCollection.Profiles[i];
                        if (profile.Crosshair == null || profile.Name == null)
                        {
                            mProfileCollection.Profiles.RemoveAt(i);
                        }
                    }

                    StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
                    StorageFile file = await roamingFolder.CreateFileAsync("crosshair_profiles.json", CreationCollisionOption.ReplaceExisting);
                    string json = JsonConvert.SerializeObject(mProfileCollection, Formatting.Indented);

                    int retryCount = 3;
                    for (int i = 0; i < retryCount; i++)
                    {
                        try
                        {
                            await FileIO.WriteTextAsync(file, json);
                            Debug.WriteLine("Profiles saved successfully");
                            break;
                        }
                        catch (IOException ex)
                        {
                            if (i == retryCount - 1)
                            {
                                Debug.WriteLine($"Error saving profiles after {retryCount} attempts: {ex.Message}");
                            }
                            else
                            {
                                Debug.WriteLine($"File is being used. Retrying... ({i + 1}/{retryCount})");
                                await Task.Delay(100);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving profiles: {ex.Message}");
                }
            }
        }

        private async void CenterCrosshair_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_widget != null)
                {
                    await _widget.CenterWindowAsync();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}");
            }
        }

        private void RefreshProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProfilesUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task LoadProfile()
        {
            int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
            Debug.WriteLine(selectedIndex);
            try
            {
                StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
                StorageFile file = await roamingFolder.GetFileAsync("crosshair_profiles.json");
                string json = await FileIO.ReadTextAsync(file);

                try
                {
                    CrosshairProfileCollection profileCollection = JsonConvert.DeserializeObject<CrosshairProfileCollection>(json);
                    Debug.WriteLine($"Profile Collection: '{profileCollection}");
                    if (profileCollection == null)
                    {
                        mProfileCollection.Profiles.Add(new CrosshairProfile());
                        mProfileCollection.Profiles[0].Name = "Autosave";
                        CrosshairData DefaultCrosshair = new CrosshairData()
                        {
                            Width = 1.2,
                            Height = 1.2,
                            Thickness = 0.0,
                            DotSize = 0.0,
                            Color = "#82FF43",
                            ShowDot = true,
                            Gap = 1.2,
                            ShowBorder = true,
                            BorderSize = 2.0,
                            BorderColor = "#000000",
                            AntiAliasing = false,
                        };
                        mProfileCollection.Profiles[0].Crosshair = DefaultCrosshair;
                        await SaveProfile();
                    }
                    else
                    {
                        mProfileCollection = profileCollection;
                        Debug.WriteLine(profileCollection);
                    }

                    Debug.WriteLine($"Profile '{mProfileCollection.Profiles[selectedIndex].Name}' loaded successfully.");
                    UpdateUIControls();
                    await UpdateCrosshair();
                    initialized = true;
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"Deserialization error: {ex.Message}");
                }
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("No profile file found, using default settings.");
                var defaultProfile = new CrosshairProfile
                {
                    Name = "Autosave",
                    Crosshair = new CrosshairData()
                };

                mProfileCollection.Profiles.Add(defaultProfile);
                string defaultJson = JsonConvert.SerializeObject(mProfileCollection, Formatting.Indented);
                StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
                StorageFile newFile = await roamingFolder.CreateFileAsync("crosshair_profiles.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(newFile, defaultJson);
                UpdateUIControls();
                await UpdateCrosshair();
                initialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile: {ex.Message}");
                Debug.WriteLine(selectedIndex);
            }
        }



        private void LoadProfilesUI()
        {
            if (mProfileCollection != null)
            {
                ProfileSelector.ItemsSource = mProfileCollection.Profiles.Select(p => p.Name).ToList();
            }
        }

        private async void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = ProfileSelector.SelectedIndex;

            if (selectedIndex >= 0 && selectedIndex < mProfileCollection.Profiles.Count)
            {
                mProfileCollection.Profiles[selectedIndex].Crosshair = mProfileCollection.Profiles[selectedIndex].Crosshair;
                await UpdateCrosshair();
                UpdateUIControls();
            }
            deleteButton.Visibility = ProfileSelector.SelectedIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector.SelectedIndex > 0)
            {
                mProfileCollection.Profiles.RemoveAt(ProfileSelector.SelectedIndex);
                ProfileSelector.SelectedIndex = 0;
                deleteButton.Visibility = Visibility.Collapsed;
                await SaveProfile();
                await UpdateCrosshair();
                UpdateUIControls();
                await LoadProfile();
                LoadProfilesUI();
            }
        }


        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {

            mProfileCollection = mProfileCollection ?? new CrosshairProfileCollection();
            mProfileCollection.Profiles = mProfileCollection.Profiles ?? new List<CrosshairProfile>();

            ContentDialog profileNameDialog = new ContentDialog
            {
                Title = "Enter Profile Name",
                Content = new TextBox
                {
                    PlaceholderText = "Enter a name for your profile",
                    Name = "ProfileNameTextBox"
                },
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel"
            };

            var result = await profileNameDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var textBox = (TextBox)profileNameDialog.Content;
                string profileName = textBox.Text.Trim();

                if (string.IsNullOrEmpty(profileName))
                {
                    profileName = "Unnamed Profile";
                }
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;

                var newProfile = new CrosshairProfile
                {
                    Name = profileName,
                    Crosshair = mProfileCollection.Profiles[selectedIndex].Crosshair
                };

                mProfileCollection.Profiles.Add(newProfile);

                await SaveProfile();

                LoadProfilesUI();

                ProfileStatusMessage.Text = $"Profile '{profileName}' saved!";
                ProfileStatusMessage.Visibility = Visibility.Visible;
            }
        }


        private void UpdateUIControls()
        {
            int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;

            var height = mProfileCollection.Profiles[selectedIndex].Crosshair.Height;
            var width = mProfileCollection.Profiles[selectedIndex].Crosshair.Width;
            if (Math.Round(width, 2) != Math.Round(height, 2))
            {
                isLinked = false;
                LinkButton.Content = "🔓";
            }
            else
            {
                LinkButton.Content = "🔗";
            }

            HeightSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Height;
            WidthSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Width;
            ThicknessSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Thickness;
            DotSizeSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.DotSize;
            GapSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Gap;
            CheckBoxBorder.IsChecked = mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder;
            CheckBoxAA.IsChecked = mProfileCollection.Profiles[selectedIndex].Crosshair.AntiAliasing;
            BorderSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.BorderSize;

            Windows.UI.Color color = ColorHelper.FromArgb(
                255,
                byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.Color.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.Color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.Color.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)
            );
            ColorPicker.Color = color;

            Windows.UI.Color borderColor = ColorHelper.FromArgb(
                255,
                byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)
                );
            ColorPickerBorder.Color = borderColor;



        }


        private void ExportCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
            try
            {
                var crosshair = mProfileCollection.Profiles[selectedIndex].Crosshair;

                string compactExport = $"{Math.Round(crosshair.Width, 2)};{Math.Round(crosshair.Height, 2)};" +
                                       $"{Math.Round(crosshair.Thickness, 2)};{Math.Round(crosshair.DotSize, 2)};" +
                                       $"{crosshair.Color};{crosshair.ShowDot};{Math.Round(crosshair.Gap, 2)};" +
                                       $"{crosshair.ShowBorder};{Math.Round(crosshair.BorderSize, 2)};" +
                                       $"{crosshair.BorderColor};{Math.Round(crosshair.Opacity, 2)};" +
                                       $"{crosshair.AntiAliasing}";

                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(compactExport);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                Debug.WriteLine("Crosshair settings exported to clipboard.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error exporting crosshair: {ex.Message}");
            }
        }

        private async void ImportCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
            try
            {
                ImportStatusMessage.Visibility = Visibility.Collapsed;

                var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
                {
                    string compactImport = await dataPackageView.GetTextAsync();
                    var values = compactImport.Split(';');

                    if (values.Length == 12)
                    {
                        CrosshairData importedCrosshair = new CrosshairData
                        {
                            Width = double.Parse(values[0]),
                            Height = double.Parse(values[1]),
                            Thickness = double.Parse(values[2]),
                            DotSize = double.Parse(values[3]),
                            Color = values[4],
                            ShowDot = bool.Parse(values[5]),
                            Gap = double.Parse(values[6]),
                            ShowBorder = bool.Parse(values[7]),
                            BorderSize = double.Parse(values[8]),
                            BorderColor = values[9],
                            Opacity = double.Parse(values[10]),
                            AntiAliasing = bool.Parse(values[11])
                        };

                        mProfileCollection.Profiles[selectedIndex].Crosshair = importedCrosshair;
                        await UpdateCrosshair();

                        Debug.WriteLine("Crosshair settings imported successfully.");
                    }
                    else
                    {
                        ImportStatusMessage.Text = "Invalid Code!";
                        ImportStatusMessage.Visibility = Visibility.Visible;
                        Debug.WriteLine("Invalid crosshair data.");
                    }
                }
                else
                {
                    ImportStatusMessage.Text = "No valid text in clipboard.";
                    ImportStatusMessage.Visibility = Visibility.Visible;
                    Debug.WriteLine("Clipboard does not contain text.");
                }
            }
            catch (Exception ex)
            {
                ImportStatusMessage.Text = "Error importing code!";
                ImportStatusMessage.Visibility = Visibility.Visible;
                Debug.WriteLine($"Error importing crosshair: {ex.Message}");
            }
        }
    }
}
