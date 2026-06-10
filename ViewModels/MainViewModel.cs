#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using $safeprojectname$.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;


namespace $safeprojectname$.ViewModels
{
    public partial class ToastNotificationModel : ObservableObject
    {
        public string Message { get; set; } = "";
        public Microsoft.UI.Xaml.Controls.InfoBarSeverity Severity { get; set; } = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
    }

    public partial class MainViewModel : ObservableObject
    {
        private Views.AIAssistantWindow? _aiWindow;
        // UIスレッドへのアクセス用
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        [ObservableProperty] private string _geminiApiKey = "";
        [ObservableProperty] private int _selectedThemeIndex = 0; // 0:Default, 1:Light, 2:Dark
        [ObservableProperty] private int _selectedNavStyleIndex = 0;

        [ObservableProperty] private string _appVersion = "";

        // プロジェクトが有効かどうか（初期値は false）
        [ObservableProperty]
        private bool _isProjectActive = false;

        [ObservableProperty] private bool _isSimulatorMenuVisible = true;

        [ObservableProperty] private bool _isProjectModified = false;
        [ObservableProperty] private bool _isFriendsModified = false;
        public bool IsSettingsChangedInSession { get; set; } = false;

        [ObservableProperty] private bool _isLoading = false;

        // 通知リストのプロパティを追加
        [ObservableProperty]
        private ObservableCollection<ToastNotificationModel> _toastNotifications = new();

        partial void OnSelectedThemeIndexChanged(int value)
        {
            if (IsLoading) return; // 読み込み中はスキップ
            ApplyTheme();
            IsSettingsChangedInSession = true;
        }

        partial void OnSelectedNavStyleIndexChanged(int value)
        {
            if (IsLoading) return; // 読み込み中はスキップ
            ApplyNavStyle();
            IsSettingsChangedInSession = true;
        }

        // GeminiApiKey や Password はUIに即時影響しないがフラグだけ立てる
        partial void OnGeminiApiKeyChanged(string value) => IsSettingsChangedInSession = true;

        public MainViewModel()
        {
            // コンストラクタを空にするか、純粋なメモリ内初期化のみに留める
        }

        /// <summary>
        /// アプリ起動後に安全なタイミングで呼ばれる初期化メソッド
        /// </summary>
        public void InitializeStartup()
        {
            try
            {
                AppVersion = GetAppVersion();
                LoadSettings();
 
            }
            catch (Exception)
            {
                ShowToast("初期設定の読み込みに失敗しました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        }

        /// <summary>
        /// 設定をロードし、UIに反映させます
        /// </summary>
        public void LoadSettings()
        {
            // 読み込み中フラグを立てて、即時反映ロジックがループするのを防ぐ
            IsLoading = true;
            try
            {
                RegistrySettings.Load();
                GeminiApiKey = RegistrySettings.GeminiApiKey;
                SelectedThemeIndex = (int)RegistrySettings.AppTheme;
                SelectedNavStyleIndex = RegistrySettings.NavStyleIndex;

                // ロード直後の値を即適用
                ApplyTheme();
                ApplyNavStyle();

                // ロードしたばかりなので変更フラグは折る
                IsSettingsChangedInSession = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task SaveSettings()
        {
            // 保存処理の実行
            RegistrySettings.GeminiApiKey = GeminiApiKey;
            RegistrySettings.AppTheme = (ElementTheme)SelectedThemeIndex;
            RegistrySettings.NavStyleIndex = SelectedNavStyleIndex;
            RegistrySettings.Save();

            // 保存したのでフラグを折る
            IsSettingsChangedInSession = false;

            ShowToast("設定を保存しました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
        }


        public void ApplyTheme()
        {
            if (App.MainWindowInstance?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = (ElementTheme)SelectedThemeIndex;
            }
        }

        public void ApplyNavStyle()
        {
            if (App.MainWindowInstance is MainWindow mainWindow)
            {
                // ナビゲーションビューのモードを切り替え
                mainWindow.NavView.PaneDisplayMode = (SelectedNavStyleIndex == 1)
                    ? NavigationViewPaneDisplayMode.Top
                    : NavigationViewPaneDisplayMode.Left;

                // ナビゲーションスタイルが「上部」(SelectedNavStyleIndex == 1) のときは
                // TitleBar の PaneToggleButton (表示/非表示ボタン) を消す
                if (mainWindow.titleBar != null)
                {
                    mainWindow.titleBar.IsPaneToggleButtonVisible = (SelectedNavStyleIndex == 0);
                }
            }
        }

        /// <summary>
        /// アプリ内トースト通知を表示します（3秒後に自動消去）
        /// </summary>
        public void ShowToast(string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                var toast = new ToastNotificationModel { Message = message, Severity = severity };
                ToastNotifications.Add(toast);

                // 3秒待機して自動で消す
                await Task.Delay(3000);
                ToastNotifications.Remove(toast);
            });
        }

        private string GetAppVersion()
        {
            try
            {
                // パッケージ環境ではこれが失敗する可能性があるため、例外保護を強化
                var package = Windows.ApplicationModel.Package.Current;
                if (package != null && package.Id != null)
                {
                    var v = package.Id.Version;
                    return $"Build {v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                }
            }
            catch { }

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return $"Build {version?.ToString() ?? "1.0.0.0"}";
        }

        [RelayCommand]
        private async Task NewProject()
        {
            // 保存確認。キャンセルされたら中断
            if (App.IsDialogOpen) return;

            if (!await ConfirmSaveAndProceedAsync()) return;

            // ダイアログを生成
            var dialog = new Views.NewProjectDialog();

            var result = await App.ShowDialogSafeAsync(dialog);

            if (result == ContentDialogResult.Primary)
            {

                UpdateWindowTitle();

                // 初回保存 (フォルダ構成の作成)
                await SaveProject();
                ShowToast("新しいプロジェクトを作成しました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);

            }
        }

        [RelayCommand]
        private async Task SaveProject()
        {
            try
            {
                ShowToast("プロジェクトを保存しました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
            }
            catch (Exception) { ShowToast("プロジェクトの保存に失敗しました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error); }
        }

        [RelayCommand]
        private async Task OpenProject()
        {
            if (!await ConfirmSaveAndProceedAsync()) return;

            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, App.MainWindowHandle);
            picker.FileTypeFilter.Add(".proj");

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            IsLoading = true;
            try
            {
                 UpdateWindowTitle();
                 ShowToast($"プロジェクトを読み込みました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
            }
            catch (Exception) 
            {
                ShowToast("プロジェクトの読み込みに失敗しました。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// プロジェクトの切り替え（新規作成や開く）を行う前に、
        /// 未保存の変更（友だちデータおよびプロジェクト）を確認します。
        /// </summary>
        /// <returns>続行して良い場合は true、キャンセルされた場合は false</returns>
        public async Task<bool> ConfirmSaveAndProceedAsync()
        {
            // プロジェクトが非アクティブな場合は、確認なしで続行可能
            if (!IsProjectActive || App.MainPageXamlRoot == null) return true;

            // どちらにも変更がない場合は、確認なしで続行可能
            if (!IsProjectModified) return true;

            // すでにダイアログが開いている場合は二重に開かない
            if (App.IsDialogOpen) return false;

            // アプリ終了時と同じ「段階的な確認ロジック」を呼び出して実行します
            // これにより、要件5, 6, 7に基づいた順序で確認が行われます
            return await ConfirmShutdownAsync(false);
        }

         private void UpdateWindowTitle()
        {
            if (App.MainWindowInstance is MainWindow mainWindow)
            {
                mainWindow.SetTitle("Sample Project");
            }
        }


        /// <summary>
        /// アプリ終了時の段階的な保存確認プロセス
        /// </summary>
        public async Task<bool> ConfirmShutdownAsync(bool IsClose = true)
        {
            if (!IsProjectActive || App.MainPageXamlRoot == null) return true;

            // AIアシスタントが開いていれば閉じる
            CloseAIAssistant();


            // 2. プロジェクトデータの確認 (IsProjectModified == true の場合)
            if (IsProjectModified)
            {
                var result = await ShowSaveDialogAsync("プロジェクト", "プロジェクトの変更を保存しますか？", IsClose);
                if (result == ContentDialogResult.Primary) await SaveProject();
                else if (result == ContentDialogResult.None) return false; // キャンセル
            }

            return true;
        }

        public void CloseAIAssistant()
        {
            if (_aiWindow != null)
            {
                try
                {
                    _aiWindow.Close();
                }
                catch { }
                _aiWindow = null;
            }
        }

        private async Task<ContentDialogResult> ShowSaveDialogAsync(string title, string message, bool IsClose = true)
        {
            // App.MainWindowInstance が破棄されていないかチェック
            if (App.MainWindowInstance == null || App.MainWindowInstance.Content == null)
                return ContentDialogResult.Secondary; // 保存せず閉じる扱いにする

            var dialog = new ContentDialog
            {
                Title = title + "の保存確認",
                Content = message,
                PrimaryButtonText = IsClose ? "保存して閉じる" : "保存する",
                SecondaryButtonText = IsClose ? "保存しないで閉じる" : "保存しない",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary,
                // MainWindowのContentから確実にXamlRootを取得
                XamlRoot = App.MainWindowInstance.Content.XamlRoot
            };
            return await App.ShowDialogSafeAsync(dialog);
        }

        [RelayCommand]
        public void OpenAIAssistant()
        {
            // すでに開いている場合はアクティブにするだけ
            if (_aiWindow != null)
            {
                _aiWindow.Activate();
                return;
            }

            _aiWindow = new Views.AIAssistantWindow();

            // 閉じられたら参照をクリア
            _aiWindow.Closed += (s, e) => { _aiWindow = null; };

            _aiWindow.Activate();
        }
    }
}