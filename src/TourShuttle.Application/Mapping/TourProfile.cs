using AutoMapper;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Mapping;

public class TourProfile : Profile
{
    public TourProfile()
    {
        // Also used by ProjectTo, which turns this map into the SELECT of the tours list.
        CreateMap<Tour, TourSummaryResponse>()
            .ForMember(dest => dest.DestinationName, opt => opt.MapFrom(src => src.Destination.Name))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Destination.City))
            .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => src.Driver != null ? src.Driver.FullName : null))
            // One city per stop, in route order; the service removes repeats (Distinct here would lose the order in SQL).
            .ForMember(dest => dest.Cities, opt => opt.MapFrom(src =>
                src.PickupPoints.OrderBy(p => p.Order).Select(p => p.PickupPoint.City).ToList()))
            // Depends on the current time, so the service sets it after the query.
            .ForMember(dest => dest.DisplayStatus, opt => opt.Ignore());

        CreateMap<Tour, TourDetailsResponse>()
            .IncludeBase<Tour, TourSummaryResponse>()
            .ForMember(dest => dest.DestinationDescription, opt => opt.MapFrom(src => src.Destination.Description))
            // The effective value needs the estimator, so the service sets it; the stored one maps straight across.
            .ForMember(dest => dest.MinutesBetweenPickupPoints, opt => opt.Ignore())
            .ForMember(dest => dest.CustomMinutesBetweenPickupPoints, opt => opt.MapFrom(src => src.MinutesBetweenPickupPoints))
            .ForMember(dest => dest.PickupPoints, opt => opt.MapFrom(src => src.PickupPoints.OrderBy(p => p.Order)));

        CreateMap<TourPickupPoint, TourPickupPointResponse>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.PickupPoint.Name))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.PickupPoint.Address))
            .ForMember(dest => dest.EstimatedPickupTimeUtc, opt => opt.Ignore());
    }
}
