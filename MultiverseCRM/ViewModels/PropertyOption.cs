namespace MultiverseCRM.ViewModels;

public class PropertyOption
{
    public int Id { get; set; }
    public string UnitNumber { get; set; } = "";
    public string? Title { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? BHK { get; set; }
    public string? City { get; set; }
    public string? Location { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? CarpetArea { get; set; }
    public decimal? BuiltUpArea { get; set; }
    public string? Status { get; set; }
    public string? PropertyType { get; set; }
    public string? AreaUnit { get; set; }
}