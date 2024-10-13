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


//TODO: Refactor usage of CrosshairData class in MainPage since we are storing the crosshair data in the CrosshairProfileCollection class 

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
        [JsonProperty]
        public bool ShowBorder { get; set; }
        [JsonProperty]
        public double BorderSize { get; set; }
        [JsonProperty]
        public string BorderColor { get; set; }
        [JsonProperty]
        public double Opacity { get; set; }


        public CrosshairData()
        {
            Length = 6.0;
            Thickness = 3.0;
            DotSize = 0.0;
            Color = "#06E8FF";
            ShowDot = true;
            Gap = 6.0;
            ShowBorder = true;
            BorderSize = 1.0;
            BorderColor = "#000000";
            Opacity = 1.0;
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
        // private CrosshairData crosshairData;
        private XboxGameBarWidget _widget;
        private CrosshairProfileCollection mProfileCollection;
        private bool initilized = false;
        private const string ProfileFileName = "crosshair_profile.json";


        public MainPage()
        {
            InitializeComponent();
            mProfileCollection = new CrosshairProfileCollection();
            // crosshairData = new CrosshairData();
            Crosshair.ScriptNotify += Crosshair_ScriptNotify;
        }

        public MainPage(XboxGameBarWidget widget) : this()
        {
            _widget = widget;
            _widget.GameBarDisplayModeChanged += Widget_DisplayModeChanged;
            UpdateSettingsPanelVisibility(_widget.GameBarDisplayMode);
             LoadProfile();
            LoadProfilesUI();
           
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
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

            if(Guide != null)
            {
                Guide.Visibility = (displayMode == XboxGameBarDisplayMode.PinnedOnly) ? Visibility.Collapsed : Visibility.Visible;
            }


        }

        private async void LengthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.Length = e.NewValue;
                    if (initilized)
                    {
                        await UpdateCrosshair();

                    }
                   
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("crosshairData is null.");
                }
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
                    if (initilized)
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
                    if (initilized)
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
                    if (initilized)
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
                    if (initilized)
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
                    if (initilized)
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
                    if (initilized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }

        private async void checkBoxBorder_Checked(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder = true;
                    if (initilized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }
        private async void checkBoxBorder_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                if (mProfileCollection != null && mProfileCollection.Profiles[selectedIndex].Crosshair != null)
                {
                    mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder = false;
                    if (initilized)
                    {
                        await UpdateCrosshair();
                    }
                }
            }
        }
        private async Task UpdateCrosshair()
        {
            if (ProfileSelector != null)
            {
                int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
                try
                {
                    await Crosshair.InvokeScriptAsync("updateCrosshair", new string[]
                    {
                mProfileCollection.Profiles[selectedIndex].Crosshair.Length.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.Thickness.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.DotSize.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.Color,
                mProfileCollection.Profiles[selectedIndex].Crosshair.ShowDot.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.Gap.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.BorderSize.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.BorderColor.ToString(),
                mProfileCollection.Profiles[selectedIndex].Crosshair.Opacity.ToString(),
                    });

                   await SaveProfile(true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating crosshair: {ex.Message}");
                }
            }
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
                    StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
                    StorageFile file = await roamingFolder.CreateFileAsync("crosshair_profiles.json", CreationCollisionOption.ReplaceExisting);
                    string json = JsonConvert.SerializeObject(mProfileCollection, Formatting.Indented);

                    await FileIO.WriteTextAsync(file, json);

                    Debug.WriteLine("Profiles saved successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving profiles: {ex.Message}");
                } }
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

       private void  RefreshProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                 LoadProfilesUI();
            }catch (Exception ex)
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
                Debug.WriteLine(json);

                try
                {
                    CrosshairProfileCollection profileCollection = JsonConvert.DeserializeObject<CrosshairProfileCollection>(json);
                    Debug.WriteLine(profileCollection);
                    if (profileCollection == null)
                    {
                     
                        mProfileCollection.Profiles.Add(new CrosshairProfile());
                        mProfileCollection.Profiles[0].Name = "Autosave";
                        CrosshairData DefaultCrosshair = new CrosshairData()
                        {
                            Length = 6.0,
                            Thickness = 3.0,
                            DotSize = 0.0,
                            Color = "#06E8FF",
                            ShowDot = true,
                            Gap = 6.0,
                            ShowBorder = true,
                            BorderSize = 1.0,
                            BorderColor = "#000000"
                        };
                        mProfileCollection.Profiles[0].Crosshair = DefaultCrosshair;
                        await SaveProfile();

                      
                    }else
                    {
                        mProfileCollection = profileCollection;
                        Debug.WriteLine(profileCollection);
                      // await UpdateCrosshair();
                    }



                        profileCollection.Profiles[selectedIndex].Crosshair = profileCollection.Profiles[selectedIndex].Crosshair;
                        Debug.WriteLine($"Profile '{profileCollection.Profiles[selectedIndex].Name}' loaded successfully.");
                        UpdateUIControls();
                        await UpdateCrosshair();
                        initilized = true;
                  
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
                initilized = true;
   

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile: {ex.Message}");
                Debug.WriteLine(selectedIndex);
               // mProfileCollection.Profiles[selectedIndex].Crosshair = new CrosshairData();
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
            LengthSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Length;
            ThicknessSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Thickness;
            DotSizeSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.DotSize;
            GapSlider.Value = mProfileCollection.Profiles[selectedIndex].Crosshair.Gap;
            checkBoxBorder.IsChecked = mProfileCollection.Profiles[selectedIndex].Crosshair.ShowBorder;
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


        private  void ExportCrosshairButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = (ProfileSelector.SelectedIndex == -1) ? 0 : ProfileSelector.SelectedIndex;
            try
            {
                string crosshairJson = Newtonsoft.Json.JsonConvert.SerializeObject(mProfileCollection.Profiles[selectedIndex].Crosshair);

                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(crosshairJson);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                System.Diagnostics.Debug.WriteLine("Crosshair settings exported to clipboard.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting crosshair: {ex.Message}");
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
                    string crosshairJson = await dataPackageView.GetTextAsync();

                    CrosshairData importedCrosshair = Newtonsoft.Json.JsonConvert.DeserializeObject<CrosshairData>(crosshairJson);

                    if (importedCrosshair != null)
                    {
                        mProfileCollection.Profiles[selectedIndex].Crosshair = importedCrosshair;
                        await UpdateCrosshair();

                        System.Diagnostics.Debug.WriteLine("Crosshair settings imported successfully.");
                    }
                    else
                    {
                        ImportStatusMessage.Text = "Invalid Code!";
                        ImportStatusMessage.Visibility = Visibility.Visible;
                        System.Diagnostics.Debug.WriteLine("Invalid crosshair data.");
                    }
                }
                else
                {
                    ImportStatusMessage.Text = "No valid text in clipboard.";
                    ImportStatusMessage.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("Clipboard does not contain text.");
                }
            }
            catch (Exception ex)
            {
                ImportStatusMessage.Text = "Error importing code!";
                ImportStatusMessage.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Error importing crosshair: {ex.Message}");
            }
        }

    
    }

 
}
