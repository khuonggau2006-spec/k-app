using TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Locations;

public class UpdateLocationCommandValidatorTests
{
    [Fact]
    public async Task Validate_SelfAsParent_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var location = TestDataFactory.CreateLocation();
        context.Locations.Add(location);
        await context.SaveChangesAsync(default);

        var validator = new UpdateLocationCommandValidator(context);
        var command = new UpdateLocationCommand(location.Id, "Location", null, 10, 106, true, location.Id);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateLocationCommand.ParentLocationId));
    }

    [Fact]
    public async Task Validate_DifferentExistingParent_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var location = TestDataFactory.CreateLocation("Location");
        var parent = TestDataFactory.CreateLocation("Parent");
        context.Locations.AddRange(location, parent);
        await context.SaveChangesAsync(default);

        var validator = new UpdateLocationCommandValidator(context);
        var command = new UpdateLocationCommand(location.Id, "Location", null, 10, 106, true, parent.Id);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
