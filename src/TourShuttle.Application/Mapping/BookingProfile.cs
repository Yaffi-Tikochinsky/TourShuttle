using AutoMapper;
using TourShuttle.Application.DTOs.Bookings;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Mapping;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Booking, BookingResponse>()
            .ForMember(dest => dest.DestinationName, opt => opt.MapFrom(src => src.Tour.Destination.Name))
            .ForMember(dest => dest.DepartureAt, opt => opt.MapFrom(src => src.Tour.DepartureAt))
            .ForMember(dest => dest.ReturnAt, opt => opt.MapFrom(src => src.Tour.ReturnAt))
            .ForMember(dest => dest.PickupPointName, opt => opt.MapFrom(src => src.TourPickupPoint.PickupPoint.Name))
            .ForMember(dest => dest.PickupPointAddress, opt => opt.MapFrom(src => src.TourPickupPoint.PickupPoint.Address))
            // Both depend on the clock or on the estimator, so the service fills them.
            .ForMember(dest => dest.TourDisplayStatus, opt => opt.Ignore())
            .ForMember(dest => dest.EstimatedPickupTimeUtc, opt => opt.Ignore());
    }
}
