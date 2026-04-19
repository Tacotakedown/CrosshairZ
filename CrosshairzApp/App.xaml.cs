using CrosshairZ.Services;
using Microsoft.Gaming.XboxGameBar;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CrosshairZ
{
    sealed partial class App : Application
    {
        private XboxGameBarWidget _widget;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            if (args is XboxGameBarWidgetActivatedEventArgs widgetArgs && widgetArgs.IsLaunchActivation && widgetArgs.AppExtensionId == "ExtensionCrosshair")
            {
                var frame = EnsureRootFrame();
                _widget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, frame);

                frame.Content = new MainPage(_widget);
                Window.Current.Activate();

                await _widget.CenterWindowAsync();
                return;
            }
            base.OnActivated(args);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var frame = EnsureRootFrame();
            if (frame.Content == null)
            {
                frame.Content = new MainPage(null);
            }

            SetPreferredWindowSize(1180, 760);
            Window.Current.Activate();
        }

        private Frame EnsureRootFrame()
        {
            if (Window.Current.Content is Frame frame)
            {
                return frame;
            }

            frame = new Frame();
            frame.NavigationFailed += (_, e) =>
            {
                throw new Exception($"Failed to load page {e.SourcePageType.FullName}");
            };

            Window.Current.Content = frame;
            return frame;
        }

        private static void SetPreferredWindowSize(double width, double height)
        {
            var size = new Size(width, height);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(size);
            ApplicationView.GetForCurrentView().TryResizeView(size);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            e.SuspendingOperation.GetDeferral().Complete();
        }
    }
}
