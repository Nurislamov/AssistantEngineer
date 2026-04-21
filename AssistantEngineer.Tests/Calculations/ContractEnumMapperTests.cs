using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Equipment.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Domain.Enums;

namespace AssistantEngineer.Tests;

public class ContractEnumMapperTests
{
    [Fact]
    public void MapsKnownEnumValuesExplicitly()
    {
        Assert.Equal(RoomType.Office, RoomTypeDto.Office.ToDomain());
        Assert.Equal(RoomTypeDto.ServerRoom, RoomType.ServerRoom.ToContract());
        Assert.Equal(CardinalDirection.SouthWest, CardinalDirectionDto.SouthWest.ToDomain());
        Assert.Equal(CardinalDirectionDto.NorthEast, CardinalDirection.NorthEast.ToContract());
        Assert.Equal(CoolingLoadCalculationMethod.Iso52016, CoolingLoadCalculationMethodDto.Iso52016.ToDomain());
        Assert.Equal(HeatingLoadCalculationMethodDto.En12831, HeatingLoadCalculationMethod.En12831.ToContract());
    }

    [Fact]
    public void ThrowsForUnsupportedEnumValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RoomTypeDto)999).ToDomain());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CardinalDirection)999).ToContract());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CoolingLoadCalculationMethodDto)999).ToDomain());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((HeatingLoadCalculationMethod)999).ToContract());
    }
}


