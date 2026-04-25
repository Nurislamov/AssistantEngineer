using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Modules.Buildings.Domain.Climate;

public class ClimateZone
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Temperature SummerDesignTemperature { get; private set; } = null!;
    public Temperature WinterDesignTemperature { get; private set; } = null!;

    private ClimateZone() { }

    private ClimateZone(string name, Temperature summerTemp, Temperature winterTemp)
    {
        Name = name;
        SummerDesignTemperature = summerTemp;
        WinterDesignTemperature = winterTemp;
    }

    public static Result<ClimateZone> Create(string name, Temperature summerTemp, Temperature winterTemp)
    {
        var nameResult = name.ToRequiredTrimmed("Climate zone name", maxLength: 100);
        if (nameResult.IsFailure) return Result<ClimateZone>.Failure(nameResult);

        return Result<ClimateZone>.Success(new ClimateZone(nameResult.Value, summerTemp, winterTemp));
    }
}
