using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class VentilationAnalysisFacade : IVentilationAnalysisFacade
{
    private readonly NaturalVentilationPreviewService _naturalVentilationPreview;

    public VentilationAnalysisFacade(
        NaturalVentilationPreviewService naturalVentilationPreview)
    {
        _naturalVentilationPreview = naturalVentilationPreview;
    }

    public Task<Result<NaturalVentilationPreviewResponse>> PreviewNaturalVentilationAsync(
        int roomId,
        NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken) =>
        _naturalVentilationPreview.PreviewAsync(
            roomId,
            request,
            cancellationToken);
}