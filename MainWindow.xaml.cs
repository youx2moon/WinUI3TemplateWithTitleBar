using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using Windows.UI;
using WinRT.Interop;

namespace $safeprojectname$
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow;
        private bool _isClosingConfirmed = false;
        private string OrignTitle = App.AppName;
        public ViewModels.MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            InitializeComponent();

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            ExtendsContentIntoTitleBar = true;

            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                var appTitleBar = _appWindow.TitleBar;

                // コンテンツをタイトルバー領域まで拡張
                //appTitleBar.ExtendsContentIntoTitleBar = true;

                // 【ここが重要】タイトルバーの高さを「Tall (高い)」に設定
                // これにより、閉じるボタンなどの高さが自動的に拡張されます
                appTitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
                appTitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                appTitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

                // XAML上の TitleBar コントロールをシステムに認識させる
                SetTitleBar(titleBar);

                // 初回の色設定
                UpdateTitleBarButtonColors();
            }

            RootGrid.ActualThemeChanged += (s, e) => UpdateTitleBarButtonColors();

            Title = OrignTitle;
            App.SetMainWindowHandle(hWnd, this);

            AppWindow.Closing += AppWindow_Closing;
            ContentFrame.Navigated += ContentFrame_Navigated;
 
            // 設定を読み込んで適用
            ViewModel.LoadSettings();

            // ContentFrame.Navigate の null 警告を回避して初期化
            ContentFrame.Navigate(typeof(Views.HomePage), ViewModel);
        }

        /// <summary>
        /// 現在のテーマ（ダーク/ライト）に基づいて、システムキャプションボタンの色を更新します
        /// </summary>
        private void UpdateTitleBarButtonColors()
        {
            if (_appWindow == null || !AppWindowTitleBar.IsCustomizationSupported()) return;

            var titleBar = _appWindow.TitleBar;

            // 現在の実際のテーマを取得
            var theme = RootGrid.ActualTheme;

            if (theme == ElementTheme.Dark)
            {
                // ダークテーマ用の配色
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 255, 255, 255); // 薄い白
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(30, 255, 255, 255);

                // 非アクティブ時の設定
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
            else
            {
                // ライトテーマ用の配色
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 0, 0, 0); // 薄い黒
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(30, 0, 0, 0);

                // 非アクティブ時の設定
                titleBar.ButtonInactiveForegroundColor = Colors.LightGray;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }

        /// <summary>
        /// TitleBar の戻るボタンがクリックされた時の処理
        /// </summary>
        private void titleBar_BackRequested(TitleBar sender, object args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        /// <summary>
        /// 画面遷移が発生した後に実行される共通処理
        /// </summary>
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // 1. 戻るボタンの表示・非表示を切り替え
            // ホーム画面（履歴の最初）では戻るボタンを隠す
            titleBar.IsBackButtonVisible = ContentFrame.CanGoBack;

            // 2. NavigationView の選択項目を現在のページに合わせる（同期）
            UpdateNavigationViewSelection(e.SourcePageType);
        }

        /// <summary>
        /// 現在表示されているページの種類に基づいてサイドメニューの選択状態を更新します
        /// </summary>
        private void UpdateNavigationViewSelection(Type currentPageType)
        {
            string? tag = currentPageType.Name switch
            {
                "HomePage" => "HomePage",
                "SettingsPage" => "SettingsPage",
                _ => null
            };

            if (tag == "SettingsPage")
            {
                // SettingsPage は特殊扱い（SettingsItem）なので、直接選択状態を設定
                NavView.SelectedItem = NavView.SettingsItem;
            }
            else
            {
                var item = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == tag);
                if (item != null)
                {
                    NavView.SelectedItem = item;
                }
            }
        }


        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // UI要素が読み込まれたタイミングで XamlRoot を App に保存
            App.MainPageXamlRoot = Content.XamlRoot;
        }

        public void SetTitle(string projectName = "")
        {
            string displayTitle = string.IsNullOrEmpty(projectName)
                ? $"{OrignTitle}"
                : $"{OrignTitle} - {projectName}";

            Title = displayTitle;

            if (titleBar != null)
            {
                titleBar.Title = displayTitle;
            }
        }

        private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            if (_isClosingConfirmed) return;
            if (ViewModel == null) return;

            if (!ViewModel.IsProjectModified && !ViewModel.IsFriendsModified)
            {
                _isClosingConfirmed = true;
                Cleanup(); // ここで Cleanup を呼ぶ
                return;
            }

            args.Cancel = true;
            if (App.IsDialogOpen) return;

            try
            {
                bool canProceed = await ViewModel.ConfirmShutdownAsync();
                if (canProceed)
                {
                    _isClosingConfirmed = true;
                    Cleanup(); // 確定後に呼ぶ
                    Close();
                }
            }
            catch
            {
                _isClosingConfirmed = true;
                Cleanup();
                Close();
            }
        }

        public void Cleanup()
        {
            try
            {
                // 現在表示されているページがシミュレータなら、個別に Close を呼ぶ
                if (ContentFrame != null)
                {
                    ContentFrame.Content = null;
                    ContentFrame.BackStack.Clear();
                }

                ViewModel?.CloseAIAssistant();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定されたタグのページへプログラムから遷移します
        /// </summary>
        public async void NavigateToPage(string tag)
        {
             Type? pageType = tag switch
            {
                "HomePage" => typeof(Views.HomePage),
                "SettingsPage" => typeof(Views.SettingsPage),
                _ => null
            };

            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType, ViewModel);

                // NavigationViewの選択状態を同期
                var item = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == tag);

                if (item != null) NavView.SelectedItem = item;
                else if (tag == "SettingsPage") NavView.SelectedItem = NavView.SettingsItem;
            }
        }

        /// <summary>
        /// ナビゲーションの戻る履歴をすべて削除します
        /// </summary>
        public void ClearNavigationHistory()
        {
            // Frameの履歴スタックを空にする
            ContentFrame.BackStack.Clear();

            // 履歴がなくなったので戻るボタンを非表示にする
            if (titleBar != null)
            {
                titleBar.IsBackButtonVisible = false;
            }
        }

        // 既存のイベントハンドラもこの NavigateToPage を使うように整理するとコードが綺麗になります
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigateToPage("SettingsPage");
                return;
            }

            if (args.InvokedItemContainer?.Tag is string tag)
            {
                NavigateToPage(tag);
            }
        }


        private void titleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void BtnOpenAI_Click(object sender, RoutedEventArgs e) => ViewModel.OpenAIAssistant();
    }
}