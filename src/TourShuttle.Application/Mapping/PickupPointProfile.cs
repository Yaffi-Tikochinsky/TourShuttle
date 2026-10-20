using AutoMapper;
using TourShuttle.Application.DTOs.PickupPoints;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Mapping;

public class PickupPointProfile : Profile
{
    public PickupPointProfile()
    {
        CreateMap<PickupPoint, PickupPointResponse>();

        // Used both to create a new entity and to copy a PUT body onto a tracked one.
        CreateMap<PickupPointRequest, PickupPoint>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Tours, opt => opt.Ignore());
    }
}
