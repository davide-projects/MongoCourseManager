using MongoCourseManager.BLL.Services;
using MongoCourseManager.BLL.Services.Interfaces;
using MongoCourseManager.DAL.Configuration;
using MongoCourseManager.DAL.Context;
using MongoCourseManager.DAL.Repositories;
using MongoCourseManager.DAL.Repositories.Interfaces;
using MongoCourseManager.PL.Mapping;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddAutoMapper(
    config =>
    {
        var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];

        if (!string.IsNullOrWhiteSpace(licenseKey))
        {
            config.LicenseKey = licenseKey;
        }
    },
    typeof(MappingProfile));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
