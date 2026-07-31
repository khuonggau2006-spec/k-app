using TaskMgmt.Application.Features.Locations.Commands.CreateLocation;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Locations;

public class CreateLocationCommandValidatorTests
{
    [Theory]
    [InlineData(90, 180, true)]
    [InlineData(-90, -180, true)]
    [InlineData(90.1, 0, false)]
    [InlineData(-90.1, 0, false)]
    [InlineData(0, 180.1, false)]
    [InlineData(0, -180.1, false)]
    public async Task Validate_CoordinateBounds(double latitude, double longitude, bool expectedValid)
    {
        using var context = TestDbContextFactory.Create();
        var validator = new CreateLocationCommandValidator(context);
        var command = new CreateLocationCommand("Location", null, latitude, longitude, null);

        var result = await validator.ValidateAsync(command);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public async Task Validate_NonExistentParentLocationId_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var validator = new CreateLocationCommandValidator(context);
        var command = new CreateLocationCommand("Location", null, 10, 106, Guid.NewGuid());

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLocationCommand.ParentLocationId));
    }

    [Fact]
    public async Task Validate_ExistingParentLocationId_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var parent = TestDataFactory.CreateLocation("Parent");
        context.Locations.Add(parent);
        await context.SaveChangesAsync(default);

        var validator = new CreateLocationCommandValidator(context);
        var command = new CreateLocationCommand("Child", null, 10, 106, parent.Id);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
