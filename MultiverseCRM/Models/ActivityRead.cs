using System.ComponentModel.DataAnnotations;

namespace MultiverseCRM.Models;

public class ActivityRead
{
    public int Id { get; set; }

    [MaxLength(128)]
    public string UserName { get; set; } = "";

    public int LastReadEventId { get; set; }
}