using TaskMgmt.Application.Common;

namespace TaskMgmt.Application.UnitTests.Common;

public class GeoDistanceTests
{
    [Fact]
    public void CalculateMeters_SameCoordinates_ReturnsZero()
    {
        var distance = GeoDistance.CalculateMeters(10.0, 106.0, 10.0, 106.0);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void CalculateMeters_KnownDistance_ReturnsApproximatelyCorrectValue()
    {
        // Hồ Gươm (21.0285, 105.8542) và Sân bay Nội Bài (21.2187, 105.8048) - Haversine tính ra ~21.76km.
        var distance = GeoDistance.CalculateMeters(21.0285, 105.8542, 21.2187, 105.8048);

        Assert.InRange(distance, 21_000, 22_500);
    }

    [Fact]
    public void CalculateMeters_OneHundredMetersApart_ReturnsApproximatelyOneHundred()
    {
        // 0.0009 độ vĩ độ xấp xỉ 100m (1 độ vĩ độ ~ 111.320m).
        var distance = GeoDistance.CalculateMeters(10.0, 106.0, 10.0009, 106.0);

        Assert.InRange(distance, 95, 105);
    }
}
