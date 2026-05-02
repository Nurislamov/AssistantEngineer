using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Tests;

public sealed class GroundContactMapperTests
{
    [Theory]
    [InlineData(GroundContactTypeDto.SlabOnGround, GroundContactType.SlabOnGround)]
    [InlineData(GroundContactTypeDto.BasementConditioned, GroundContactType.BasementConditioned)]
    [InlineData(GroundContactTypeDto.BasementUnconditioned, GroundContactType.BasementUnconditioned)]
    [InlineData(GroundContactTypeDto.CrawlSpace, GroundContactType.CrawlSpace)]
    [InlineData(GroundContactTypeDto.VentilatedCrawlSpace, GroundContactType.VentilatedCrawlSpace)]
    public void ToDomainMapsKnownValues(
        GroundContactTypeDto dto,
        GroundContactType expected)
    {
        Assert.Equal(expected, dto.ToDomain());
    }

    [Fact]
    public void ToDomainThrowsForUnknownValue()
    {
        var unknown = (GroundContactTypeDto)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => unknown.ToDomain());
    }

    [Fact]
    public void ToContractThrowsForUnknownValue()
    {
        var unknown = (GroundContactType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => unknown.ToContract());
    }
}
