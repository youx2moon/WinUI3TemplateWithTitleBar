// --- Services/RegistrySettings.cs ---

using $safeprojectname$.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;

public static class RegistrySettings
{
    private const string RegKeyPath = @"Software\$safeprojectname$";

    public static string GeminiApiKey { get; set; } = "";
    public static ElementTheme AppTheme { get; set; } = ElementTheme.Default;
     public static int NavStyleIndex { get; set; } = 0;

   // 【追加】友だちページ閲覧パスワード
    public static string FriendViewPassword { get; set; } = "";


    public static void Save()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegKeyPath);
        key.SetValue("GeminiApiKey", EncryptionService.Encrypt(GeminiApiKey));
        key.SetValue("AppTheme", (int)AppTheme);
        key.SetValue("NavStyleIndex", NavStyleIndex);
        // パスワードを暗号化して保存
        key.SetValue("FriendViewPassword", EncryptionService.Encrypt(FriendViewPassword));
    }

    public static void Load()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegKeyPath);
        if (key != null)
        {
            GeminiApiKey = EncryptionService.Decrypt(key.GetValue("GeminiApiKey")?.ToString() ?? "");
            AppTheme = (ElementTheme)Convert.ToInt32(key.GetValue("AppTheme", (int)ElementTheme.Default));
            NavStyleIndex = Convert.ToInt32(key.GetValue("NavStyleIndex", 0));
            // パスワードを復号して読み込み
            FriendViewPassword = EncryptionService.Decrypt(key.GetValue("FriendViewPassword")?.ToString() ?? "");
        }
    }
}