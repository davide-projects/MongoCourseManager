using MongoCourseManager.BLL.Models;

namespace MongoCourseManager.BLL.Services.Interfaces;

public interface ICourseService
{
    Task<CourseModel?> GetByIdAsync(string id);

    Task<PagedResultModel<CourseModel>> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int page,
        int pageSize,
        bool sortDescending);

    Task<CourseModel> CreateAsync(CreateCourseModel model);

    Task<CourseModel?> UpdateAsync(string id, UpdateCourseModel model);

    Task<bool> DeleteAsync(string id);

    Task<bool> SetPublishedAsync(string id, bool isPublished);

    Task<bool> AddTagAsync(string id, string tag);

    Task<bool> RemoveTagAsync(string id, string tag);
}
