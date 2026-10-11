namespace WukongBench.Presentation.Rendering;

using System.Globalization;

/// <summary>Small formatting helpers shared by the report renderers.</summary>
internal static class Format
{
    public const string NotAvailable = "n/a";

    public static string Fps(double? value) =>
        value.HasValue ? value.Value.ToString("0.#", CultureInfo.InvariantCulture) : NotAvailable;

    public static string Millis(double? value) =>
        value.HasValue ? $"{value.Value.ToString("0.##", CultureInfo.InvariantCulture)} ms" : NotAvailable;

    public static string MegaBytes(double? value) =>
        value.HasValue ? $"{value.Value.ToString("0", CultureInfo.InvariantCulture)} MB" : NotAvailable;

    public static string GigaBytes(double? value) =>
        value.HasValue ? $"{value.Value / 1024.0:0.##} GB" : NotAvailable;

    public static string Percent(double? value) =>
        value.HasValue ? $"{value.Value.ToString("0.#", CultureInfo.InvariantCulture)} %" : NotAvailable;

    public static string Text(string value) => string.IsNullOrWhiteSpace(value) ? NotAvailable : value;

    public static string Timestamp(DateTimeOffset value) =>
        value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    public static string Duration(TimeSpan value) =>
        value.TotalMinutes >= 1
            ? $"{value.TotalMinutes:0.#} min"
            : $"{value.TotalSeconds:0.#} s";
}
