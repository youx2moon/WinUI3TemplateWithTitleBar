using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace $safeprojectname$.Views
{
    public sealed partial class NewProjectDialog : ContentDialog
    {
        public string ResultProjectName => ProjectNameEntry.Text;
        public string ResultFolderPath => Path.Combine(LocationEntry.Text, ProjectNameEntry.Text);

        public NewProjectDialog()
        {
            InitializeComponent();

            // 初期パス設定)
            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "$safeprojectname$"
            );

            if (!Directory.Exists(defaultPath)) Directory.CreateDirectory(defaultPath);

            LocationEntry.Text = defaultPath;

            // 初回チェック実行
            Validate();
        }

        private void UpdatePathPreview(object? sender, TextChangedEventArgs? e)
        {
            Validate();
        }

        private void Validate()
        {
            if (PathPreview == null || WarningMessage == null || RootDialog == null) return;

            string projectName = ProjectNameEntry.Text.Trim();
            string baseLocation = LocationEntry.Text;

            // 1. プロジェクト名が空かチェック
            if (string.IsNullOrEmpty(projectName))
            {
                PathPreview.Text = "プロジェクト名を入力してください";
                WarningMessage.Visibility = Visibility.Collapsed;
                IsPrimaryButtonEnabled = false;
                return;
            }

            // 2. フォルダの重複チェック
            string fullPath = Path.Combine(baseLocation, projectName);
            bool folderExists = Directory.Exists(fullPath);

            PathPreview.Text = "プロジェクト は \"" + fullPath + "\" で作成されます";

            if (folderExists)
            {
                WarningMessage.Visibility = Visibility.Visible;
                IsPrimaryButtonEnabled = false;
            }
            else
            {
                WarningMessage.Visibility = Visibility.Collapsed;
                IsPrimaryButtonEnabled = true;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, App.MainWindowHandle);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                LocationEntry.Text = folder.Path;
                Validate();
            }
        }
    }
}