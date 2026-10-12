using AutoMapper;
using MongoCourseManager.BLL.Models;
using MongoCourseManager.DAL.Entities;

namespace MongoCourseManager.PL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CourseEntity, CourseModel>();

        CreateMap<CreateCourseModel, CourseEntity>()
            .ForMember(
                destination => destination.Id,
                options => options.Ignore())
            .ForMember(
                destination => destination.CreatedAt,
                options => options.Ignore())
            .ForMember(
                destination => destination.UpdatedAt,
                options => options.Ignore());

        CreateMap<UpdateCourseModel, CourseEntity>()
            .ForMember(
                destination => destination.Id,
                options => options.Ignore())
            .ForMember(
                destination => destination.CreatedAt,
                options => options.Ignore())
            .ForMember(
                destination => destination.UpdatedAt,
                options => options.Ignore());
    }
}
