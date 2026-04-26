using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IVentilationAnalysisFacade
{
    Task<Result<NaturalVentilationPreviewResponse>> PreviewNaturalVentilationAsync(
        int roomId,
        NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken);
}