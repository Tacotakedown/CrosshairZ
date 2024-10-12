using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using System.Drawing;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.Gaming.XboxGameBar;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;
using Windows.Storage;


namespace CrosshairZ
{

    public class CrosshairData
    {
        [JsonProperty]
        public double Length { get; set; }
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

        public CrosshairData()
        {
            Length = 10;
            Thickness = 1;
            DotSize = 5;
            Color = "#FFFFFF";
            ShowDot = true;
            Gap = 4;
        }
    }


    public sealed partial class MainPage : Page
    {
        private CrosshairData crosshairData;
        private XboxGameBarWidget _widget;

        private const string ProfileFileName = "crosshair_profile.json";


        public MainPage()
        {
            InitializeComponent();
            crosshairData = new CrosshairData();
            Crosshair.ScriptNotify += Crosshair_ScriptNotify;
        }

        public MainPage(XboxGameBarWidget widget) : this()
        {
            _widget = widget;
            _widget.GameBarDisplayModeChanged += Widget_DisplayModeChanged;
            UpdateSettingsPanelVisibility(_widget.GameBarDisplayMode);
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

        private void Crosshair_ScriptNotify(object sender, NotifyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"JavaScript Log: {e.Value}");
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
            Debug.WriteLine($"UpdateSettingsPanelVisibility called with mode: {displayMode}");

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

           
        }

        private async void LengthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (crosshairData != null)
            {
                crosshairData.Length = e.NewValue;
                await UpdateCrosshair();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("crosshairData is null.");
            }
        }

        private async void ThicknessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (crosshairData != null)
            {
                crosshairData.Thickness = e.NewValue;
                await UpdateCrosshair();
            }

        }

        private async void DotSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (crosshairData != null)
            {
                crosshairData.DotSize = e.NewValue;
                await UpdateCrosshair();
            }
        }

        private async void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (crosshairData != null)
            {
                crosshairData.Color = "#" + args.NewColor.R.ToString("X2") + args.NewColor.G.ToString("X2") + args.NewColor.B.ToString("X2");
                await UpdateCrosshair();
            }
        }



        private async void GapSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (crosshairData != null)
            {
                crosshairData.Gap = e.NewValue;
                await UpdateCrosshair();
            }
        }

        private async Task UpdateCrosshair()
        {
            try
            {
                await Crosshair.InvokeScriptAsync("updateCrosshair", new string[]
                {
                crosshairData.Length.ToString(),
                crosshairData.Thickness.ToString(),
                crosshairData.DotSize.ToString(),
                crosshairData.Color,
                crosshairData.ShowDot.ToString(),
                crosshairData.Gap.ToString()
                });

                await SaveProfile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating crosshair: {ex.Message}");
            }
        }

        private async Task SaveProfile()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.CreateFileAsync("crosshair_profile.json", CreationCollisionOption.ReplaceExisting);
                string json = JsonConvert.SerializeObject(crosshairData);
                await FileIO.WriteTextAsync(file, json);
                Debug.WriteLine("Profile saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile: {ex.Message}");
            }
        }

        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveProfile();
        }

        private async void LoadProfileButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadProfile();
            await UpdateCrosshair();
            UpdateUIControls();
        }

        private async Task LoadProfile()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.GetFileAsync("crosshair_profile.json");
                string json = await FileIO.ReadTextAsync(file);
                crosshairData = JsonConvert.DeserializeObject<CrosshairData>(json);
                Debug.WriteLine("Profile loaded successfully");
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("No profile found, using default settings");
                crosshairData = new CrosshairData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile: {ex.Message}");
                crosshairData = new CrosshairData();
            }
        }

        private void UpdateUIControls()
        {
            LengthSlider.Value = crosshairData.Length;
            ThicknessSlider.Value = crosshairData.Thickness;
            DotSizeSlider.Value = crosshairData.DotSize;
            GapSlider.Value = crosshairData.Gap;

            Windows.UI.Color color = ColorHelper.FromArgb(
                255,
                byte.Parse(crosshairData.Color.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(crosshairData.Color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(crosshairData.Color.Substring(5, 2), System.Globalization.NumberStyles.HexNumber)
            );
            ColorPicker.Color = color;
        }
    }
}
