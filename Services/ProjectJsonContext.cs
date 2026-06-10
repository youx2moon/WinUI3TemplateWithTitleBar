using System.Collections.Generic;
using System.Text.Json.Serialization;
using $safeprojectname$.Models;

namespace $safeprojectname$.Services
{
   [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(GeminiRequest))]
    [JsonSerializable(typeof(GeminiModelList))]

    internal partial class ProjectJsonContext : JsonSerializerContext { }
}