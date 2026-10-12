using AutoMapper;
using MongoCourseManager.BLL.Models;
using MongoCourseManager.BLL.Services.Interfaces;
using MongoCourseManager.DAL.Entities;
using MongoCourseManager.DAL.Repositories.Interfaces;

namespace MongoCourseManager.BLL.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _repository;
    private readonly IMapper _mapper;

    public CourseService(
        ICourseRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CourseModel?> GetByIdAsync(string id)
    {
        var entity = await _repository.GetByIdAsync(id);

        return entity is null
            ? null
            : _mapper.Map<CourseModel>(entity);
    }

    public async Task<PagedResultModel<CourseModel>> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int page,
        int pageSize,
        bool sortDescending)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        pageSize = pageSize > 50 ? 50 : pageSize;

        var result = await _repository.SearchAsync(
            text,
            category,
            isPublished,
            page,
            pageSize,
            sortDescending);

        return new PagedResultModel<CourseModel>
        {
            Items = _mapper.Map<IReadOnlyCollection<CourseModel>>(result.Items),
            TotalItems = result.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CourseModel> CreateAsync(CreateCourseModel model)
    {
        ValidateCourse(
            model.Code,
            model.Title,
            model.Category,
            model.DurationHours);

        var normalizedCode = model.Code.Trim().ToUpperInvariant();

        var existingCourse = await _repository.GetByCodeAsync(
            normalizedCode);

        if (existingCourse is not null)
        {
            throw new InvalidOperationException(
                $"Esiste già un corso con codice '{model.Code}'.");
        }

        var entity = _mapper.Map<CourseEntity>(model);

        entity.Code = normalizedCode;
        entity.Title = entity.Title.Trim();
        entity.Category = entity.Category.Trim();
        entity.Tags = NormalizeTags(entity.Tags);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.CreateAsync(entity);

        return _mapper.Map<CourseModel>(created);
    }

    public async Task<CourseModel?> UpdateAsync(
        string id,
        UpdateCourseModel model)
    {
        ValidateCourse(
            model.Code,
            model.Title,
            model.Category,
            model.DurationHours);

        var currentEntity = await _repository.GetByIdAsync(id);

        if (currentEntity is null)
            return null;

        var courseWithSameCode = await _repository.GetByCodeAsync(
            model.Code.Trim().ToUpperInvariant());

        if (courseWithSameCode is not null &&
            courseWithSameCode.Id != id)
        {
            throw new InvalidOperationException(
                $"Esiste già un altro corso con codice '{model.Code}'.");
        }

        _mapper.Map(model, currentEntity);

        currentEntity.Code = currentEntity.Code.Trim().ToUpperInvariant();
        currentEntity.Title = currentEntity.Title.Trim();
        currentEntity.Category = currentEntity.Category.Trim();
        currentEntity.Tags = NormalizeTags(currentEntity.Tags);
        currentEntity.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(currentEntity);

        return updated
            ? _mapper.Map<CourseModel>(currentEntity)
            : null;
    }

    public Task<bool> DeleteAsync(string id)
    {
        return _repository.DeleteAsync(id);
    }

    public Task<bool> SetPublishedAsync(
        string id,
        bool isPublished)
    {
        return _repository.SetPublishedAsync(id, isPublished);
    }

    public async Task<bool> AddTagAsync(string id, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Il tag è obbligatorio.");

        var normalizedTag = tag.Trim().ToLowerInvariant();

        return await _repository.AddTagAsync(id, normalizedTag);
    }

    public async Task<bool> RemoveTagAsync(string id, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Il tag è obbligatorio.");

        var normalizedTag = tag.Trim().ToLowerInvariant();

        return await _repository.RemoveTagAsync(id, normalizedTag);
    }

    private static void ValidateCourse(
        string code,
        string title,
        string category,
        int durationHours)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Il codice è obbligatorio.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Il titolo è obbligatorio.");

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("La categoria è obbligatoria.");

        if (durationHours <= 0)
            throw new ArgumentException(
                "La durata deve essere maggiore di zero.");
    }

    private static List<string> NormalizeTags(
        IEnumerable<string>? tags)
    {
        return tags?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList()
            ?? [];
    }
}
