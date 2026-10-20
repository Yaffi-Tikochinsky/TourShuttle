namespace TourShuttle.Tests;

public class MappingConfigurationTests
{
    /// <summary>
    /// Every property of every response DTO is either mapped or explicitly ignored.
    /// Catches a renamed entity property that would otherwise reach the client as null or 0.
    /// </summary>
    [Fact]
    public void Every_AutoMapper_profile_maps_every_destination_member()
    {
        TestData.MapperConfiguration.AssertConfigurationIsValid();
    }
}
