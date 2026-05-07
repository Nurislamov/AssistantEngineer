using AssistantEngineer.SharedKernel.Primitives;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.Construction;

public class ConstructionAssembly
{
    private const double Rsi = 0.13;
    private const double Rse = 0.04;

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private readonly List<ConstructionLayer> _layers = new();
    public IReadOnlyCollection<ConstructionLayer> Layers => new ReadOnlyCollection<ConstructionLayer>(_layers);

    public double InternalHeatCapacityKjPerM2K
    {
        get
        {
            return _layers.Sum(layer => layer.Material.VolumetricHeatCapacityKjPerM3K * layer.ThicknessM);
        }
    }

    private ConstructionAssembly() { }

    private ConstructionAssembly(string name) => Name = name;

    public double ThermalResistanceM2KPerW => Rsi + _layers.Sum(l => l.ThermalResistanceM2KPerW) + Rse;
    public double UValueWPerM2K => _layers.Count == 0 ? 0 : 1.0 / ThermalResistanceM2KPerW;

    public static Result<ConstructionAssembly> Create(string name)
    {
        var nameResult = name.ToRequiredTrimmed("Construction assembly name", maxLength: 200);
        if (nameResult.IsFailure) return Result<ConstructionAssembly>.Failure(nameResult);

        return Result<ConstructionAssembly>.Success(new ConstructionAssembly(nameResult.Value));
    }

    public Result<ConstructionLayer> AddLayer(Material material, double thicknessM)
    {
        var thicknessCheck = Guard.AgainstZeroOrNegative(thicknessM, "Layer thickness");
        if (thicknessCheck.IsFailure) return Result<ConstructionLayer>.Failure(thicknessCheck);

        var layer = ConstructionLayer.Create(this, material, thicknessM);
        if (layer.IsFailure) return layer;

        _layers.Add(layer.Value);
        return layer;
    }
}

public class ConstructionLayer
{
    public int Id { get; private set; }
    public int ConstructionAssemblyId { get; private set; }
    public ConstructionAssembly ConstructionAssembly { get; private set; } = null!;
    public int MaterialId { get; private set; }
    public Material Material { get; private set; } = null!;
    public double ThicknessM { get; private set; }

    private ConstructionLayer() { }

    private ConstructionLayer(ConstructionAssembly constructionAssembly, Material material, double thicknessM)
    {
        ConstructionAssembly = constructionAssembly;
        Material = material;
        ThicknessM = thicknessM;
    }

    public double ThermalResistanceM2KPerW => ThicknessM / Material.ThermalConductivityWPerMK;

    public static Result<ConstructionLayer> Create(
        ConstructionAssembly constructionAssembly,
        Material material,
        double thicknessM)
    {
        var thicknessCheck = Guard.AgainstZeroOrNegative(thicknessM, "Layer thickness");
        if (thicknessCheck.IsFailure) return Result<ConstructionLayer>.Failure(thicknessCheck);

        return Result<ConstructionLayer>.Success(new ConstructionLayer(constructionAssembly, material, thicknessM));
    }
}
