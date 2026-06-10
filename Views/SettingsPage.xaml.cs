using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace $safeprojectname$.Views
{
    public sealed partial class SettingsPage : Page
    {
        public ViewModels.MainViewModel ViewModel { get; private set; } = null!;

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ViewModels.MainViewModel vm)
            {
                ViewModel = vm;
            }
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// ページから離れる際の処理
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // もし「保存」ボタンが押されずに他のページへ移動しようとした場合
            if (ViewModel != null && ViewModel.IsSettingsChangedInSession)
            {
                // 保存されている元の設定を再ロードしてUIを元に戻す
                ViewModel.LoadSettings();
                ViewModel.ShowToast("設定の変更は保存されませんでした。元の状態に戻します。", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
            }
            base.OnNavigatedFrom(e);
        }
    }
}