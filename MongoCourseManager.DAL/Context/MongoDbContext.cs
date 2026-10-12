using Microsoft.Extensions.Options;
using MongoCourseManager.DAL.Configuration;
using MongoCourseManager.DAL.Entities;
using MongoDB.Driver;

namespace MongoCourseManager.DAL.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<CourseEntity> Courses =>
        _database.GetCollection<CourseEntity>("courses");
}
