using Microsoft.Gaming.XboxGameBar;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace CrosshairZ.Services
{
    public sealed class WidgetController
    {
        private readonly XboxGameBarWidget _widget;

        public WidgetController(XboxGameBarWidget widget)
        {
            _widget = widget;
        }

        public bool IsAvailable => _widget != null;

        public XboxGameBarDisplayMode? DisplayMode => _widget?.GameBarDisplayMode;

        public event TypedEventHandler<XboxGameBarWidget, object> DisplayModeChanged
        {
            add
            {
                if (_widget != null) _widget.GameBarDisplayModeChanged += value;
            }
            remove
            {
                if (_widget != null) _widget.GameBarDisplayModeChanged -= value;
            }
        }

        public Task CenterAsync()
        {
            if (_widget == null) return Task.CompletedTask;
            return _widget.CenterWindowAsync().AsTask();
        }
    }
}