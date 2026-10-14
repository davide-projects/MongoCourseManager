using MongoCourseManager.DAL.Entities;

namespace MongoCourseManager.DAL.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<CourseEntity?> GetByIdAsync(string id);

    Task<CourseEntity?> GetByCodeAsync(string code);

    Task<(IReadOnlyCollection<CourseEntity> Items, long Total)> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int? minDurationHours,
        string? tag,
        int page,
        int pageSize,
        bool sortDescending);

    Task<CourseEntity> CreateAsync(CourseEntity entity);

    Task<bool> UpdateAsync(CourseEntity entity);

    Task<bool> DeleteAsync(string id);

    Task<bool> SetPublishedAsync(string id, bool isPublished);

    Task<bool> AddTagAsync(string id, string tag);

    Task<bool> RemoveTagAsync(string id, string tag);

    Task<bool> RemoveAllTagsAsync(string id);
}
