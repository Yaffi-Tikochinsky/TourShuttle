using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Destinations;
using TourShuttle.Application.DTOs.PickupPoints;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Tests;

/// <summary>Destinations and pickup points have unique names: the service answers 409 before the database has to.</summary>
public class CatalogNameTests
{
    private readonly Mock<IDestinationRepository> _destinations = new();
    private readonly Mock<IPickupPointRepository> _pickupPoints = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DestinationService Destinations() =>
        new(_destinations.Object, _unitOfWork.Object, TestData.Mapper(), NullLogger<DestinationService>.Instance);

    private PickupPointService PickupPoints() =>
        new(_pickupPoints.Object, _unitOfWork.Object, TestData.Mapper(), NullLogger<PickupPointService>.Instance);

    [Fact]
    public async Task A_second_destination_with_the_same_name_is_a_conflict_even_with_extra_spaces()
    {
        _destinations.Setup(d => d.NameExistsAsync("הכותל המערבי", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Destinations().CreateAsync(
            new DestinationRequest { Name = "  הכותל המערבי ", City = "ירושלים" }, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _destinations.Verify(d => d.Add(It.IsAny<Destination>()), Times.Never);
    }

    [Fact]
    public async Task A_destination_is_saved_with_trimmed_fields()
    {
        Destination? added = null;
        _destinations.Setup(d => d.Add(It.IsAny<Destination>())).Callback<Destination>(d => added = d);

        var result = await Destinations().CreateAsync(
            new DestinationRequest { Name = " העיר העתיקה בצפת ", City = " צפת ", Description = "   " }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("העיר העתיקה בצפת", added!.Name);
        Assert.Equal("צפת", added.City);
        Assert.Null(added.Description);
    }

    [Fact]
    public async Task Renaming_a_destination_to_another_ones_name_is_a_conflict()
    {
        var existing = new Destination { Id = 2, Name = "עיר דוד", City = "ירושלים" };
        _destinations.Setup(d => d.GetForUpdateAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        // The check excludes the destination being edited, so saving it under its own name stays allowed.
        _destinations.Setup(d => d.NameExistsAsync("מגדל דוד", 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Destinations().UpdateAsync(2, new DestinationRequest { Name = "מגדל דוד", City = "ירושלים" }, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Two_simultaneous_creates_with_one_name_end_in_a_conflict()
    {
        // Both passed the check; the unique index refuses the second insert.
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintException(new Exception("duplicate key")));

        var result = await Destinations().CreateAsync(new DestinationRequest { Name = "עיר דוד", City = "ירושלים" }, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task A_second_pickup_point_with_the_same_name_is_a_conflict()
    {
        _pickupPoints.Setup(p => p.NameExistsAsync("רמת גן - הבורסה", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await PickupPoints().CreateAsync(
            new PickupPointRequest { Name = "רמת גן - הבורסה", Address = "ז'בוטינסקי 7", City = "רמת גן" }, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _pickupPoints.Verify(p => p.Add(It.IsAny<PickupPoint>()), Times.Never);
    }
}
