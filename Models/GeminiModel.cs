using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace $safeprojectname$.Models
{
    // Gemini API 用のデータモデル
    public class GeminiRequest
    {
        [JsonPropertyName("contents")] public List<GeminiContent> Contents { get; set; } = new();
        [JsonPropertyName("system_instruction")] public GeminiSystemInstruction? SystemInstruction { get; set; }
    }

    public class GeminiSystemInstruction
    {
        [JsonPropertyName("parts")] public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiContent
    {
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("parts")] public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    // モデル一覧取得用
    public class GeminiModelList { [JsonPropertyName("models")] public List<GeminiModelInfo> Models { get; set; } = new(); }
    public class GeminiModelInfo { [JsonPropertyName("name")] public string Name { get; set; } = ""; }
}