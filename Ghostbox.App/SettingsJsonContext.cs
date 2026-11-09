using System.Text.Json.Serialization;
using Ghostbox.Core;

namespace Ghostbox.App;

[JsonSerializable(typeof(ScanSettings))]
[JsonSerializable(typeof(EffectSettings))]
[JsonSerializable(typeof(TranscriptResult))]
[JsonSerializable(typeof(SpeechDetection))]
[JsonSerializable(typeof(ProcessingRecipe))]
[JsonSerializable(typeof(InvestigationSession))]
internal partial class SettingsJsonContext : JsonSerializerContext;
