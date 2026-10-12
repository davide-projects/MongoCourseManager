namespace MongoCourseManager.BLL.Models;

public class PagedResultModel<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = [];

    public long TotalItems { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public long TotalPages =>
        PageSize <= 0
            ? 0
            : (long)Math.Ceiling(TotalItems / (double)PageSize);
}
