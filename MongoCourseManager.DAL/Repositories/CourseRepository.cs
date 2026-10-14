using MongoCourseManager.DAL.Context;
using MongoCourseManager.DAL.Entities;
using MongoCourseManager.DAL.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace MongoCourseManager.DAL.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly IMongoCollection<CourseEntity> _collection;

    public CourseRepository(MongoDbContext context)
    {
        _collection = context.Courses;
    }

    public async Task<CourseEntity?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return null;

        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<CourseEntity?> GetByCodeAsync(string code)
    {
        return await _collection
            .Find(x => x.Code == code)
            .FirstOrDefaultAsync();
    }

    public async Task<(IReadOnlyCollection<CourseEntity> Items, long Total)> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int? minDurationHours,
        string? tag,
        int page,
        int pageSize,
        bool sortDescending)
    {
        var filterBuilder = Builders<CourseEntity>.Filter;
        var filters = new List<FilterDefinition<CourseEntity>>();

        if (!string.IsNullOrWhiteSpace(text))
        {
            var regex = new BsonRegularExpression(
                Regex.Escape(text.Trim()),
                "i");

            filters.Add(
                filterBuilder.Or(
                    filterBuilder.Regex(x => x.Title, regex),
                    filterBuilder.Regex(x => x.Code, regex)
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(filterBuilder.Eq(x => x.Category, category));
        }

        if (isPublished.HasValue)
        {
            filters.Add(filterBuilder.Eq(x => x.IsPublished, isPublished.Value));
        }

        if (minDurationHours.HasValue)
        {
            filters.Add(
                filterBuilder.Gte(
                    x => x.DurationHours, minDurationHours.Value));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filters.Add(filterBuilder.AnyEq(x => x.Tags, tag));
        }

        var finalFilter = filters.Count == 0
            ? FilterDefinition<CourseEntity>.Empty
            : filterBuilder.And(filters);

        var total = await _collection.CountDocumentsAsync(finalFilter);

        var query = _collection.Find(finalFilter);

        query = sortDescending
            ? query.SortByDescending(x => x.Title)
            : query.SortBy(x => x.Title);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<CourseEntity> CreateAsync(CourseEntity entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task<bool> UpdateAsync(CourseEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id) ||
            !ObjectId.TryParse(entity.Id, out _))
        {
            return false;
        }

        var filter = Builders<CourseEntity>.Filter.Eq(x => x.Id, entity.Id);

        var update = Builders<CourseEntity>.Update
            .Set(x => x.Code, entity.Code)
            .Set(x => x.Title, entity.Title)
            .Set(x => x.Category, entity.Category)
            .Set(x => x.DurationHours, entity.DurationHours)
            .Set(x => x.IsPublished, entity.IsPublished)
            .Set(x => x.Tags, entity.Tags)
            .Set(x => x.UpdatedAt, entity.UpdatedAt);

        var result = await _collection.UpdateOneAsync(filter, update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var result = await _collection.DeleteOneAsync(x => x.Id == id);

        return result.DeletedCount > 0;
    }

    public async Task<bool> SetPublishedAsync(string id, bool isPublished)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<CourseEntity>.Update
            .Set(x => x.IsPublished, isPublished)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> AddTagAsync(string id, string tag)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<CourseEntity>.Update
            .AddToSet(x => x.Tags, tag)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> RemoveTagAsync(string id, string tag)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<CourseEntity>.Update
            .Pull(x => x.Tags, tag)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> RemoveAllTagsAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        
        var update = Builders<CourseEntity>.Update
            .Set(x => x.Tags, new List<string>())
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }
}
