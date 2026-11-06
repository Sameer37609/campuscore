namespace CampusCore.Models;

public record DirectoryRow(string Id, string Title, string Subtitle, string Department, string Year, string Status, string Detail, string Url, string Initials, int? Occupied = null, int? Capacity = null);
public class DirectoryPage
{
    public string Kind { get; init; } = "";
    public string? Q { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
    public string? Year { get; set; }
    public string Sort { get; set; } = "name";
    public int Page { get; set; } = 1;
    public int Total { get; set; }
    public int AllCount { get; set; }
    public int Pages => Math.Max(1, (int)Math.Ceiling(Total / 10d));
    public IReadOnlyList<DirectoryRow> Rows { get; set; } = [];
    public IReadOnlyList<string> Departments { get; set; } = [];
    public IReadOnlyList<string> Statuses { get; set; } = [];
    public IReadOnlyList<string> Years { get; set; } = [];
}
public record DetailPage(string Eyebrow, string Title, string Subtitle, string Status, IReadOnlyList<KeyValuePair<string, string>> Facts, IReadOnlyList<DirectoryRow> Related, string RelatedHeading, string BackUrl);
