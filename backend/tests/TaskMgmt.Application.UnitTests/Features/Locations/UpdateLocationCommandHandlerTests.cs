using TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Locations;

public class UpdateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesCheckInRadiusMeters()
    {
        using var context = TestDbContextFactory.Create();
        var location = TestDataFactory.CreateLocation();
        context.Locations.Add(location);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), TaskMgmt.Domain.Enums.SystemRole.Manager);
        var handler = new UpdateLocationCommandHandler(context, currentUser, new FakeCacheService());

        var result = await handler.Handle(
            new UpdateLocationCommand(location.Id, location.Name, location.Address, location.Latitude, location.Longitude, 250, true, null),
            default);

        Assert.Equal(250, result.CheckInRadiusMeters);
        var saved = await context.Locations.FindAsync(location.Id);
        Assert.Equal(250, saved!.CheckInRadiusMeters);
    }
}
