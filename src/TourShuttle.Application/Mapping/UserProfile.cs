using AutoMapper;
using TourShuttle.Application.DTOs.Users;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
}
