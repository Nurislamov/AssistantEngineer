namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed record GreePlusDeviceStatusSnapshot(
    int? Pow,
    int? Mod,
    int? TemUn,
    int? SetTem,
    int? TemRec,
    int? WdSpd,
    int? Quiet,
    int? Tur,
    int? SwUpDn,
    int? SwingLfRig,
    int? Air,
    int? Blo,
    int? Health,
    int? SvSt,
    int? Lig,
    int? SwhSlp,
    int? SlpMod,
    int? Dmod,
    int? Dwet,
    int? AllErr,
    int? DeviceState,
    bool? Status,
    int? Mid,
    string? Host)
{
    public bool? IsPowerOn => Pow.HasValue ? Pow.Value == 1 : null;
    public bool? HasError => AllErr.HasValue ? AllErr.Value != 0 : null;
    public bool? IsStatusOnline => Status;
}
