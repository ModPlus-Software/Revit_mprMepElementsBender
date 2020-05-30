namespace mprMepElementsBender.Views
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using Autodesk.Revit.UI;
    using ModPlusStyle.Controls;

    /// <summary>
    /// Главное окно плагина
    /// </summary>
    public partial class MainView : ModPlusWindow
    {
        private static MainView _instance;
        private static bool _isOpen;

        /// <summary>
        /// Создает экземпляр класса <see cref="MainView"/>
        /// </summary>
        /// <param name="uiApplication"><see cref="UIApplication"/></param>
        public MainView(UIApplication uiApplication)
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(ModPlusConnector.Instance);
            Loaded += (sender, args) =>
            {
                _instance = this;
                _isOpen = true;
            };
            Closed += (sender, args) =>
            {
                _instance = null;
                _isOpen = false;
            };
            MouseEnter += (sender, args) => Focus();
            MouseLeave += (sender, args) =>
            {
#if R2017 || R2018
                SetForegroundWindow(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
#else
                SetForegroundWindow(uiApplication.MainWindowHandle);
#endif
            };
        }

        /// <summary>
        /// Экземпляр окна уже открыт
        /// </summary>
        public static bool IsOpen
        {
            get
            {
                if (_isOpen)
                {
                    if (_instance.WindowState == WindowState.Minimized)
                        _instance.WindowState = WindowState.Normal;

                    _instance.Focus();
                }

                return _isOpen;
            }
        }
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
