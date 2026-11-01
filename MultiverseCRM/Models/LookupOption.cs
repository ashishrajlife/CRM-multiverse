namespace MultiverseCRM.Models;

public class LookupOption
{
    public int Id { get; set; }
    public string Category { get; set; } = "";  // Status, Priority, BHK, ...
    public string Value { get; set; } = "";     // what is stored in the Lead column
    public string? Label { get; set; }          // what is shown (falls back to Value)
    public string? Color { get; set; }          // hex, used for pills
    public string? Meta { get; set; }           // e.g. sqft factor for AreaUnit
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}