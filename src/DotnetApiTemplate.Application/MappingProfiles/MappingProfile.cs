using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Domain.Entities;
using AutoMapper;

namespace DotnetApiTemplate.Application.MappingProfiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Car, CarDto>().ReverseMap();
        CreateMap<Car, CarUpsertDto>().ReverseMap();

        // Custom mapping for upsert to entity
        CreateMap<CarUpsertDto, Car>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());
    }
}
