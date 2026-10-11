namespace WukongBench.Presentation.Rendering;

using System.Text.Json;
using System.Text.Json.Serialization;
using WukongBench.Domain.Models;

/// <summary>Serialises the report to JSON so it can be consumed by other tools.</summary>
public sealed class JsonReportRenderer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Render(BenchmarkReport report) => JsonSerializer.Serialize(report, Options);

    public string RenderDiagnostics(DiagnosticsSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Options);
}
