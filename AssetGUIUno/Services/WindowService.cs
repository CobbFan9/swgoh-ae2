using AssetGUIUno.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using SevenZip.Compression.LZ;
using WinRT.Interop;

namespace AssetGUIUno.Services
{
    public class WindowService : IWindowService
    {
        private readonly Window _window;

        public WindowService(Window window)
        {
            _window = window;
        }

        public void SetSize(int width, int height)
        {
            _window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width, Height = height });
        }
    }
}