namespace MultiverseCRM.Services;

public static class Format
{
    public static string Money(decimal? v, string sym = "₹")
    {
        if (v is null or 0) return "—";
        var n = v.Value;
        if (n >= 10_000_000) return $"{sym}{n / 10_000_000:0.##} Cr";
        if (n >= 100_000) return $"{sym}{n / 100_000:0.##} L";
        return $"{sym}{n:N0}";
    }

    public static string Range(decimal? min, decimal? max, string sym = "₹") =>
        min is null && max is null ? "—" :
        min is null ? $"up to {Money(max, sym)}" :
        max is null ? $"from {Money(min, sym)}" : $"{Money(min, sym)} – {Money(max, sym)}";

    public static DateTime Local(DateTime utc, string tz) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            TimeZoneInfo.FindSystemTimeZoneById(tz));

    public static string Stamp(DateTime? utc, string tz, string fmt = "dd MMM yyyy, hh:mm tt") =>
        utc is null ? "—" : Local(utc.Value, tz).ToString(fmt);

    public static string Ago(DateTime utc)
    {
        var d = DateTime.UtcNow - DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        if (d.TotalMinutes < 1) return "just now";
        if (d.TotalHours < 1) return $"{(int)d.TotalMinutes}m ago";
        if (d.TotalDays < 1) return $"{(int)d.TotalHours}h ago";
        if (d.TotalDays < 7) return $"{(int)d.TotalDays}d ago";
        return utc.ToString("dd MMM yy");
    }

    public static string Initials(string? name)
    {
        var p = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return p.Length == 0 ? "?" : p.Length == 1 ? p[0][..1].ToUpper() : (p[0][..1] + p[^1][..1]).ToUpper();
    }
}