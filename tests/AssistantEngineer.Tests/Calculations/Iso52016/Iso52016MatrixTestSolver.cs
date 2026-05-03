using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public static class Iso52016MatrixTestSolver
{
    public static Result<Iso52016RoomHeatBalanceProfile> Solve(
        Iso52016RoomHourlyInputProfile inputProfile,
        Iso52016RoomHeatBalanceOptions options)
    {
        var modelBuilder = new Iso52016MatrixReducedRoomModelBuilder();
        var solver = new Iso52016MatrixHourlySolver();
        var mapper = new Iso52016MatrixRoomEnergySimulationResultMapper();

        var matrixRequest = modelBuilder.Build(
            new Iso52016MatrixReducedRoomModelRequest(
                HourlyInputProfile: inputProfile,
                HeatBalanceOptions: options));

        if (matrixRequest.IsFailure)
            return Result<Iso52016RoomHeatBalanceProfile>.Failure(matrixRequest);

        var matrixProfile = solver.Solve(
            matrixRequest.Value);

        if (matrixProfile.IsFailure)
            return Result<Iso52016RoomHeatBalanceProfile>.Failure(matrixProfile);

        var mapped = mapper.Map(
            new Iso52016MatrixRoomEnergySimulationResult(
                RoomCode: inputProfile.RoomCode,
                SolarGainProfile: BuildSolarGainProfile(inputProfile),
                InternalGainProfile: BuildInternalGainProfile(inputProfile),
                HourlyInputProfile: inputProfile,
                MatrixSolverRequest: matrixRequest.Value,
                MatrixSolverProfile: matrixProfile.Value));

        if (mapped.IsFailure)
            return Result<Iso52016RoomHeatBalanceProfile>.Failure(mapped);

        return Result<Iso52016RoomHeatBalanceProfile>.Success(
            mapped.Value.HeatBalanceProfile);
    }

    private static Iso52016RoomSolarGainProfile BuildSolarGainProfile(
        Iso52016RoomHourlyInputProfile inputProfile) =>
        new(
            RoomCode: inputProfile.RoomCode,
            Windows: [],
            Hours: inputProfile.Hours
                .Select(hour => new Iso52016HourlyRoomSolarGainRecord(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    BeamSolarGainW: 0,
                    DiffuseSkySolarGainW: 0,
                    GroundReflectedSolarGainW: 0,
                    TotalSolarGainW: hour.SolarGainsW,
                    WindowGains: []))
                .ToArray());

    private static Iso52016RoomInternalGainProfile BuildInternalGainProfile(
        Iso52016RoomHourlyInputProfile inputProfile) =>
        new(
            RoomCode: inputProfile.RoomCode,
            PeopleCount: 0,
            SensibleHeatGainPerPersonW: 0,
            EquipmentLoadW: 0,
            LightingLoadW: 0,
            Hours: inputProfile.Hours
                .Select(hour => new Iso52016HourlyRoomInternalGainRecord(
                    HourOfYear: hour.HourOfYear,
                    OccupancyFactor: 1,
                    EquipmentFactor: 1,
                    LightingFactor: 1,
                    PeopleGainW: 0,
                    EquipmentGainW: 0,
                    LightingGainW: hour.InternalGainsW,
                    TotalInternalGainW: hour.InternalGainsW))
                .ToArray());
}