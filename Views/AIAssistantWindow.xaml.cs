using $safeprojectname$.Services;
using $safeprojectname$.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Capture;           // 権限チェック用
using Windows.Media.SpeechRecognition; // 音声認識用
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace $safeprojectname$.Views
{
    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Text { get; set; } = "";
        public bool IsLoading { get; set; } = false;

        // ユーザーかどうかの判定
        public bool IsUser => Role == "user";
        // AI（またはシステム）かどうかの判定（Loading中でない時）
        public bool IsAi => Role != "user" && !IsLoading;

        public HorizontalAlignment Alignment => Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        // 反対側に余白を作るためのマージン設定
        // ユーザー(右寄せ)なら左に60px、AI(左寄せ)なら右に60pxの空間を空ける
        public Thickness MessageMargin => Role == "user"
            ? new Thickness(60, 4, 0, 4)
            : new Thickness(0, 4, 60, 4);

        public Brush Background => Role == "user"
            ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)) // ユーザー：濃い青
            : (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"]; // AI：標準背景

        public Brush Foreground => Role == "user"
            ? new SolidColorBrush(Microsoft.UI.Colors.White)
            : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    public sealed partial class AIAssistantWindow : Window
    {
        // IME操作用のWindows API定義
        [DllImport("imm32.dll")] private static extern IntPtr ImmGetContext(IntPtr hWnd);
        [DllImport("imm32.dll")] private static extern bool ImmSetOpenStatus(IntPtr hIMC, bool fOpen);
        [DllImport("imm32.dll")] private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        private static readonly HttpClient _httpClient = new HttpClient();
        private static string? _cachedGeminiModelId;
        private ObservableCollection<ChatMessage> _messages = new();
        private CancellationTokenSource? _cts;

        // --- 音声認識用メンバ ---
        private SpeechRecognizer? _speechRecognizer;
        private bool _isListening = false;

        private string SystemInstruction =>
                    "あなたは○○ツール「$safeprojectname$」の公式専門エキスパートガイドです。\n" +
                    "以下のアプリ仕様、操作方法、およびライセンス制限に基づき、ユーザーの質問に回答してください。\n\n" +
                    "【1. ライセンスプランと制限】\n" +
                    "・無償版 / 試用版：\n" +
                    "・Pro版：\n" +
                    "【2. アプリの基本構造】\n" +
                    "・ホーム：プロジェクトの新規作成、保存、読み込みを行います。\n" +
                    "【3. 主要な操作手順】\n" +
                    "【4. 回回答のルール】\n" +
                    "・ライセンスに関する質問には、上記制限事項に基づいて正確に回答してください。\n" +
                    "・操作方法は具体的（画面名とボタン名）に案内してください。\n" +
                    "・無関係な質問には「$safeprojectname$専用アシスタントのためお答えできません」と丁寧に断ってください。";

        public AIAssistantWindow()
        {
            InitializeComponent();
            SetupWindow(); // コンストラクタで呼び出し

            Closed += AIAssistantWindow_Closed;
            ChatListView.ItemsSource = _messages;
            AppendChatMessage("ai", "$safeprojectname$アシスタントです。何かお手伝いできることはありますか？");

            Activated += (s, e) =>
            {
                if (e.WindowActivationState != WindowActivationState.Deactivated)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        await Task.Delay(100);
                        TxtInput.Focus(FocusState.Programmatic);
                    });
                }
            };
        }

        /// <summary>
        /// マイク権限を確認し、SpeechRecognizerを初期化する
        /// </summary>
        private async Task InitializeRecognizerWithPermissionAsync()
        {
            try
            {
                // 1. マイク権限の確認 (Windowsのシステムダイアログを出す)
                bool isMicAvailable = await RequestMicrophonePermission();
                if (!isMicAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("マイク権限がありません。");
                    return;
                }

                // 2. Recognizerの初期化
                if (_speechRecognizer == null)
                {
                    _speechRecognizer = new SpeechRecognizer(new Windows.Globalization.Language("ja-JP"));

                    // 制約の追加
                    var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
                    _speechRecognizer.Constraints.Add(dictationConstraint);

                    var result = await _speechRecognizer.CompileConstraintsAsync();
                    if (result.Status != SpeechRecognitionResultStatus.Success) return;

                    // 認識結果(確定)
                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += (s, e) => {
                        if (e.Result.Status == SpeechRecognitionResultStatus.Success)
                        {
                            DispatcherQueue.TryEnqueue(() => {
                                TxtInput.Text += e.Result.Text;
                                TxtInput.SelectionStart = TxtInput.Text.Length;
                            });
                        }
                    };

                    // 認識失敗やセッション終了のハンドリング
                    _speechRecognizer.StateChanged += (s, e) => {
                        System.Diagnostics.Debug.WriteLine($"Speech State: {e.State}");
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"音声認識初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Windowsの標準ダイアログでマイク権限をリクエストする
        /// </summary>
        private async Task<bool> RequestMicrophonePermission()
        {
            try
            {
                var settings = new MediaCaptureInitializationSettings { StreamingCaptureMode = StreamingCaptureMode.Audio };
                using (var capture = new MediaCapture())
                {
                    await capture.InitializeAsync(settings);
                }
                return true;
            }
            catch (UnauthorizedAccessException) { return false; }
            catch (Exception) { return false; }
        }

        private async void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            if (_speechRecognizer == null)
            {
                await InitializeRecognizerWithPermissionAsync();
                if (_speechRecognizer == null)
                {
                    AppendChatMessage("ai", "マイクの初期化に失敗しました。");
                    BtnMic.IsChecked = false;
                    return;
                }
            }

            if (BtnMic.IsChecked == true)
            {
                try
                {
                    // もし前のセッションが完全に終わっていない（停止処理中など）場合は、少し待機する
                    int retryCount = 0;
                    while (_speechRecognizer.State != SpeechRecognizerState.Idle && retryCount < 10)
                    {
                        await Task.Delay(100);
                        retryCount++;
                    }

                    if (_speechRecognizer.State == SpeechRecognizerState.Idle)
                    {
                        _isListening = true;
                        MicIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                        await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    else
                    {
                        // それでも Idle にならない場合
                        throw new Exception("音声認識エンジンがビジー状態です。再度お試しください。");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"音声認識開始エラー: {ex.Message}");
                    await StopListeningAsync();
                    BtnMic.IsChecked = false;
                    AppendChatMessage("ai", "マイクを開始できませんでした。デバイスが他で使用中か確認してください。");
                }
            }
            else
            {
                await StopListeningAsync();
            }
        }

        /// <summary>
        /// 音声認識を「確実」に停止する (Taskを返すように変更)
        /// </summary>
        private async Task StopListeningAsync()
        {
            if (_speechRecognizer != null)
            {
                try
                {
                    // 停止処理。状態が Idle 以外なら停止を試みる
                    if (_speechRecognizer.State != SpeechRecognizerState.Idle)
                    {
                        // StopAsync よりも確実にセッションを終了させるため、
                        // 動作中の場合はキャンセル処理も含めて確実に止める
                        await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"StopAsync Error: {ex.Message}");
                }
                finally
                {
                    _isListening = false;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        MicIcon.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                    });
                }
            }
        }
        /// <summary>
        /// 送信ボタンの処理
        /// </summary>
        private async Task ProcessSendMessageAsync()
        {
            // 1. 音声認識を確実に停止させて完了を待つ
            if (_isListening)
            {
                await StopListeningAsync();
                BtnMic.IsChecked = false;
            }

            string userText = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(userText) || !BtnSend.IsEnabled) return;

            // 2. ユーザーの質問を表示
            AppendChatMessage("user", userText);
            TxtInput.Text = "";
            BtnSend.IsEnabled = false;

            if (string.IsNullOrEmpty(RegistrySettings.GeminiApiKey))
            {
                AppendChatMessage("ai", "Gemini API キーが設定されていません。");
                BtnSend.IsEnabled = true;
                return;
            }

            // 3. AIの考え中（スケルトン）を追加
            _cts = new CancellationTokenSource();
            var loadingMessage = new ChatMessage { Role = "ai", IsLoading = true };
            _messages.Add(loadingMessage);
            ChatListView.ScrollIntoView(loadingMessage);

            try
            {
                // 4. API呼び出し
                string response = await CallGeminiApi(userText, _cts.Token);
                if (_cts == null || _cts.IsCancellationRequested) return;

                // 5. ローディング表示を削除して、本物の回答を追加
                _messages.Remove(loadingMessage);
                AppendChatMessage("ai", response);
            }
            catch (Exception ex)
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _messages.Remove(loadingMessage);
                    AppendChatMessage("ai", $"通信エラーが発生しました: {ex.Message}");
                }
            }
            finally
            {
                // 6. UI状態の復帰
                BtnSend.IsEnabled = true;
                TxtInput.Focus(FocusState.Programmatic);
                ForceImeOn();
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 現在のウィンドウのIMEを強制的に日本語入力モード（ON）にします
        /// </summary>
        private void ForceImeOn()
        {
            try
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                IntPtr hIMC = ImmGetContext(hWnd);
                if (hIMC != IntPtr.Zero)
                {
                    ImmSetOpenStatus(hIMC, true); // IMEをONにする
                    ImmReleaseContext(hWnd, hIMC);
                }
            }
            catch { /* 例外は無視 */ }
        }

        private void SetupWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(600, 700));

            if (System.IO.File.Exists(App.IconPath))
            {
                appWindow.SetIcon(App.IconPath);
            }
        }

        private void AIAssistantWindow_Closed(object sender, WindowEventArgs args)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_speechRecognizer != null)
            {
                try
                {
                    if (_isListening)
                    {
                        // 動作中なら強制停止（同期的に待機はできないためDisposeを優先）
                    }
                    _speechRecognizer.Dispose();
                }
                catch { }
                // ここでの null 代入は、型が ? なので警告されません
                _speechRecognizer = null;
            }
        }

        private void Skeleton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                foreach (var res in element.Resources.Values)
                {
                    if (res is Microsoft.UI.Xaml.Media.Animation.Storyboard storyboard)
                    {
                        // 修正前のシンプルな形
                        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(storyboard, element);
                        storyboard.Begin();
                    }
                }
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await ProcessSendMessageAsync();
        }

        private void AppendChatMessage(string role, string message)
        {
            _messages.Add(new ChatMessage { Role = role, Text = message });
            if (_messages.Count > 0)
                ChatListView.ScrollIntoView(_messages.Last());
        }

        public static async Task<string> DiscoverBestGeminiModelAsync()
        {
            if (!string.IsNullOrEmpty(_cachedGeminiModelId)) return _cachedGeminiModelId;
            if (string.IsNullOrEmpty(RegistrySettings.GeminiApiKey)) return "models/gemini-flash-latest";

            try
            {
                string url = "https://generativelanguage.googleapis.com/v1beta/models?key=" + RegistrySettings.GeminiApiKey;
                string resJson = await _httpClient.GetStringAsync(url);

                // Source Generation を使用してデシリアライズ
                var modelList = JsonSerializer.Deserialize(resJson, ProjectJsonContext.Default.GeminiModelList);

                if (modelList?.Models != null)
                {
                    var flashModels = modelList.Models
                        .Select(m => m.Name)
                        .Where(name => name != null &&
                                     name.Contains("-flash") &&
                                     !name.Contains("-live") &&
                                     !name.Contains("-vision") &&
                                     !name.Contains("-thinking") &&
                                     !name.Contains("-lite"))
                        .OrderByDescending(name => name)
                        .ToList();

                    if (flashModels.Count > 0)
                    {
                        _cachedGeminiModelId = flashModels.First();
                        return _cachedGeminiModelId!;
                    }
                }
            }
            catch { }

            return "models/gemini-flash-latest";
        }

        private async Task<string> CallGeminiApi(string userPrompt, CancellationToken ct)
        {
            string modelId = await DiscoverBestGeminiModelAsync();
            string apiUrl = "https://generativelanguage.googleapis.com/v1beta/" + modelId + ":generateContent?key=" + RegistrySettings.GeminiApiKey;

            // 匿名型を廃止し、定義したモデルを使用
            var body = new GeminiRequest
            {
                SystemInstruction = new GeminiSystemInstruction
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = SystemInstruction } }
                },
                Contents = new List<GeminiContent>
                {
                    new GeminiContent { Parts = new List<GeminiPart> { new GeminiPart { Text = userPrompt } } }
                }
            };

            // Source Generation を使用してシリアライズ
            var jsonBody = JsonSerializer.Serialize(body, ProjectJsonContext.Default.GeminiRequest);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, content, ct);
            var resJson = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(resJson);

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                string msg = error.TryGetProperty("message", out var m) ? m.GetString() ?? "Unknown API Error" : "Unknown API Error";
                return "APIエラー: " + msg;
            }

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var resContent) &&
                    resContent.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? "(応答が空です)";
                }
            }

            return "AIからの応答を解析できませんでした。";
        }

        private async void TxtInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                e.Handled = true;
                await ProcessSendMessageAsync();
            }
        }

        private async void LnkReport_Click(object sender, RoutedEventArgs e) =>
            await Launcher.LaunchUriAsync(new Uri("https://forms.gle/xxx"));

        /// <summary>
        /// ListViewの項目が再利用されたり、新しいデータが割り当てられた際に実行されます。
        /// これにより、二回目以降の回答が正しく更新されない問題を解決します。
        /// </summary>
        private void MessageBlock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // sender は RichTextBlock
            if (sender is RichTextBlock richText)
            {
                // 新しいデータ（ChatMessage）を取得
                if (args.NewValue is ChatMessage message)
                {
                    // AIの回答（IsAi == true）の場合のみ、Markdownをパースして表示
                    if (message.IsAi)
                    {
                        $safeprojectname$.Services.MarkdownHelper.SetMarkdownText(richText, message.Text);
                    }
                    else
                    {
                        // AI以外の場合は、念のためブロックをクリア（誤表示防止）
                        richText.Blocks.Clear();
                    }
                }
            }
        }
    }
}