namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public enum GroundBoundaryType
{
    Unsupported = 0,
    SlabOnGround = 1,
    SuspendedFloor = 2,
    HeatedBasementWall = 3,
    HeatedBasementFloor = 4,
    UnheatedBasementCeiling = 5,
    GenericGroundContact = 6
}
