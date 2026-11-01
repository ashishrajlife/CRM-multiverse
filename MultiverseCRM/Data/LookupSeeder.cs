using MultiverseCRM.Models;

namespace MultiverseCRM.Data;

public static class LookupSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.LookupOptions.Any()) return;
        int n = 0;
        void Add(string cat, string value, string? color = null, string? label = null, string? meta = null, bool def = false) =>
            db.LookupOptions.Add(new LookupOption { Category = cat, Value = value, Color = color, Label = label, Meta = meta, IsDefault = def, SortOrder = ++n });

        Add("Status", "Fresh Lead", "#4f46e5", def: true); Add("Status", "Contacted", "#0ea5e9");
        Add("Status", "Qualified Lead", "#8b5cf6");        Add("Status", "Visit Booked", "#f59e0b");
        Add("Status", "Visit Completed", "#14b8a6");       Add("Status", "Under Negotiation", "#ec4899");

        Add("Priority", "Low", "#64748b"); Add("Priority", "Medium", "#f59e0b", def: true); Add("Priority", "High", "#ef4444");

        foreach (var b in new[] { "1 BHK", "2 BHK", "3 BHK", "4 BHK", "5+ BHK" }) Add("BHK", b);
        foreach (var f in new[] { "Unfurnished", "Semi-Furnished", "Fully Furnished" }) Add("Furnished", f);
        foreach (var l in new[] { "Buy", "Rent", "Lease", "Invest" }) Add("LookingFor", l);
        foreach (var c in new[] { "Individual", "Corporate", "NRI", "Builder" }) Add("CustomerType", c);
        Add("CustomerCategory", "A", label: "A (High Value)"); Add("CustomerCategory", "B", label: "B (Medium Value)"); Add("CustomerCategory", "C", label: "C (Low Value)");

        // Meta = sqft per unit. Bigha/Angul vary by region, so fill those in the table yourself.
        Add("AreaUnit", "Sqft", meta: "1", def: true); Add("AreaUnit", "SqM", label: "Square Meter", meta: "10.7639");
        Add("AreaUnit", "Bigha"); Add("AreaUnit", "Angul");
        Add("AreaUnit", "Acre", meta: "43560"); Add("AreaUnit", "Guntha", meta: "1089");

        foreach (var s in new[] { "Facebook", "Instagram", "Google Ads", "99acres", "MagicBricks", "Housing.com", "Walk-in", "Referral", "Website", "Channel Partner" }) Add("Source", s);
        foreach (var s in new[] { "Meta (Facebook)", "Google", "Portal", "Offline", "Referral" }) Add("PrimarySource", s);

        db.SaveChanges();
    }
}