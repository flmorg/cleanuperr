using Cleanuparr.Domain.Enums;
using Cleanuparr.Persistence.Models.Configuration.Arr;
using Shouldly;
using Xunit;
using ValidationException = Cleanuparr.Domain.Exceptions.ValidationException;

namespace Cleanuparr.Persistence.Tests.Models.Configuration.Arr;

public sealed class ArrConfigTests
{
    #region Validate - Valid Values

    [Theory]
    [InlineData((short)-1)]
    [InlineData((short)0)]
    [InlineData((short)3)]
    [InlineData((short)100)]
    public void Validate_WithValidStrikes_DoesNotThrow(short strikes)
    {
        ArrConfig config = new()
        {
            Type = InstanceType.Sonarr,
            FailedImportMaxStrikes = strikes,
        };

        Should.NotThrow(() => config.Validate());
    }

    #endregion

    #region Validate - Below -1

    [Theory]
    [InlineData((short)-2)]
    [InlineData((short)-100)]
    public void Validate_WithStrikesBelowMinusOne_ThrowsValidationException(short strikes)
    {
        ArrConfig config = new()
        {
            Type = InstanceType.Sonarr,
            FailedImportMaxStrikes = strikes,
        };

        ValidationException exception = Should.Throw<ValidationException>(() => config.Validate());
        exception.Message.ShouldBe("Failed import max strikes must be -1 or greater");
    }

    #endregion

    #region Validate - Between 1 and 2

    [Theory]
    [InlineData((short)1)]
    [InlineData((short)2)]
    public void Validate_WithStrikesBetween1And2_ThrowsValidationException(short strikes)
    {
        ArrConfig config = new()
        {
            Type = InstanceType.Sonarr,
            FailedImportMaxStrikes = strikes,
        };

        ValidationException exception = Should.Throw<ValidationException>(() => config.Validate());
        exception.Message.ShouldBe("The minimum value for failed import max strikes must be 3");
    }

    #endregion
}
