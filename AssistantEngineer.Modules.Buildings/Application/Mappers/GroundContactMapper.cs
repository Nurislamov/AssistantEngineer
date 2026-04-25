using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;

namespace AssistantEngineer.Modules.Buildings.Application.Mappers;

public static class GroundContactMapper
{
    public static GroundContactType ToDomain(this GroundContactTypeDto dto) => dto switch
    {
        GroundContactTypeDto.SlabOnGround => GroundContactType.SlabOnGround,
        GroundContactTypeDto.BasementConditioned => GroundContactType.BasementConditioned,
        GroundContactTypeDto.BasementUnconditioned => GroundContactType.BasementUnconditioned,
        GroundContactTypeDto.CrawlSpace => GroundContactType.CrawlSpace,
        GroundContactTypeDto.VentilatedCrawlSpace => GroundContactType.VentilatedCrawlSpace,
        _ => GroundContactType.SlabOnGround
    };

    public static GroundContactTypeDto ToContract(this GroundContactType domain) => domain switch
    {
        GroundContactType.SlabOnGround => GroundContactTypeDto.SlabOnGround,
        GroundContactType.BasementConditioned => GroundContactTypeDto.BasementConditioned,
        GroundContactType.BasementUnconditioned => GroundContactTypeDto.BasementUnconditioned,
        GroundContactType.CrawlSpace => GroundContactTypeDto.CrawlSpace,
        GroundContactType.VentilatedCrawlSpace => GroundContactTypeDto.VentilatedCrawlSpace,
        _ => GroundContactTypeDto.SlabOnGround
    };

    public static RoomGroundContactResponse ToResponse(this GroundContactMetadata metadata, int roomId, string roomName) =>
        new()
        {
            RoomId = roomId,
            RoomName = roomName,
            ContactType = metadata.ContactType.ToContract(),
            ExposedPerimeterM = metadata.ExposedPerimeterM,
            BurialDepthM = metadata.BurialDepthM,
            WallHeightBelowGradeM = metadata.WallHeightBelowGradeM,
            HorizontalInsulationWidthM = metadata.HorizontalInsulationWidthM,
            PerimeterInsulationDepthM = metadata.PerimeterInsulationDepthM,
            UnderfloorVentilationAirChangesPerHour = metadata.UnderfloorVentilationAirChangesPerHour
        };
}