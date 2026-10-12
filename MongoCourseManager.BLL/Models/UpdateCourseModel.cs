namespace MongoCourseManager.BLL.Models;

public class UpdateCourseModel
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int DurationHours { get; set; }

    public bool IsPublished { get; set; }

    public List<string> Tags { get; set; } = [];
}
