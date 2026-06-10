using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace $safeprojectname$
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public static string AppName { get; } = "$safeprojectname$";

        public static string IconPath { get; } = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "MainWindow.ico");

        public static bool IsDialogOpen { get; private set; }
        public static XamlRoot? MainPageXamlRoot { get; set; }
        public static IntPtr MainWindowHandle { get; private set; }
        public static Window? MainWindowInstance { get; private set; }


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                _window = new MainWindow();
                _window.Activate();

                MainWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
                SetMainWindowHandle(MainWindowHandle, _window);

                var dispatcher = _window.DispatcherQueue;
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
                    {
                        try
                        {
                            if (MainWindowInstance is MainWindow mainWindow)
                            {
                                mainWindow.ViewModel.InitializeStartup();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Startup Background Error: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL LAUNCH ERROR: {ex.Message}");
            }
        }

        // ハンドルを外からセットするためのメソッド
        public static void SetMainWindowHandle(IntPtr handle, Window window)
        {
            MainWindowHandle = handle;
            MainWindowInstance = window;
        }

        // ダイアログを安全に表示するためのラッパーメソッド
        public static async Task<ContentDialogResult> ShowDialogSafeAsync(ContentDialog dialog)
        {
            if (IsDialogOpen) return ContentDialogResult.None;

            // XamlRoot が設定されていない場合のフォールバックロジック
            if (dialog.XamlRoot == null)
            {
                if (MainPageXamlRoot != null)
                {
                    dialog.XamlRoot = MainPageXamlRoot;
                }
                else if (MainWindowInstance?.Content?.XamlRoot != null)
                {
                    dialog.XamlRoot = MainWindowInstance.Content.XamlRoot;
                }
                else
                {
                    // それでも取得できない場合は表示不可
                    System.Diagnostics.Debug.WriteLine("Error: XamlRoot is null. Cannot show dialog.");
                    return ContentDialogResult.None;
                }
            }

            IsDialogOpen = true;
            try
            {
                return await dialog.ShowAsync();
            }
            finally
            {
                IsDialogOpen = false;
            }
        }
    }
}
