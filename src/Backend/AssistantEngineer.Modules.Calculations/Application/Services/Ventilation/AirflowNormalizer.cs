using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public static class AirflowNormalizer
{
    public static Result<double> M3PerHourToM3PerSecond(double airflowM3PerHour)
    {
        if (airflowM3PerHour < 0)
            return Result<double>.Validation("Airflow cannot be negative.");

        return Result<double>.Success(airflowM3PerHour / 3600.0);
    }

    public static Result<double> LitersPerSecondToM3PerSecond(double airflowLitersPerSecond)
    {
        if (airflowLitersPerSecond < 0)
            return Result<double>.Validation("Airflow cannot be negative.");

        return Result<double>.Success(airflowLitersPerSecond / 1000.0);
    }

    public static Result<double> LitersPerSecondToM3PerHour(double airflowLitersPerSecond)
    {
        var m3PerSecond = LitersPerSecondToM3PerSecond(airflowLitersPerSecond);
        return m3PerSecond.IsFailure
            ? Result<double>.Failure(m3PerSecond)
            : Result<double>.Success(m3PerSecond.Value * 3600.0);
    }

    public static Result<double> AirChangesPerHourToM3PerHour(
        double roomVolumeM3,
        double airChangesPerHour)
    {
        if (airChangesPerHour < 0)
            return Result<double>.Validation("Air changes per hour cannot be negative.");

        if (roomVolumeM3 <= 0)
            return Result<double>.Validation("Room volume must be greater than zero for ACH airflow conversion.");

        return Result<double>.Success(roomVolumeM3 * airChangesPerHour);
    }

    public static Result<double> AirflowPerPersonToLitersPerSecond(
        double airflowPerPersonLps,
        int people)
    {
        if (airflowPerPersonLps < 0)
            return Result<double>.Validation("Airflow per person cannot be negative.");

        if (people < 0)
            return Result<double>.Validation("People count cannot be negative.");

        return Result<double>.Success(airflowPerPersonLps * people);
    }

    public static Result<double> AirflowPerAreaToLitersPerSecond(
        double airflowPerAreaLpsM2,
        double areaM2)
    {
        if (airflowPerAreaLpsM2 < 0)
            return Result<double>.Validation("Airflow per area cannot be negative.");

        if (areaM2 <= 0)
            return Result<double>.Validation("Room area must be greater than zero for area airflow conversion.");

        return Result<double>.Success(airflowPerAreaLpsM2 * areaM2);
    }
}
