using AutoMapper;
using TourShuttle.Application.DTOs.Destinations;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Mapping;

public class DestinationProfile : Profile
{
    public DestinationProfile()
    {
        CreateMap<Destination, DestinationResponse>();

        // Used both to create a new entity and to copy a PUT body onto a tracked one.
        CreateMap<DestinationRequest, Destination>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Tours, opt => opt.Ignore());
    }
}
