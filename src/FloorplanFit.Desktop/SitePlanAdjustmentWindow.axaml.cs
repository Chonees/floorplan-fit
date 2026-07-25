using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop;

public partial class SitePlanAdjustmentWindow : UserControl
{
    private SitePlanAdjustmentViewModel? commissionedComparisonLoadStartedFor;

    public SitePlanAdjustmentWindow()
    {
        InitializeComponent();
        PreviewControl.FloorPlanMoveDeltaRequested += PreviewControl_OnFloorPlanMoveDeltaRequested;
        DataContextChanged += SitePlanAdjustmentWindow_OnDataContextChanged;
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        await LoadCommissionedStructuralComparisonAsync();
    }

    private async void SitePlanAdjustmentWindow_OnDataContextChanged(object? sender, EventArgs e)
        => await LoadCommissionedStructuralComparisonAsync();

    private async Task LoadCommissionedStructuralComparisonAsync()
    {
        if (DataContext is not SitePlanAdjustmentViewModel { IsCommissionedAutoFit: true } viewModel ||
            ReferenceEquals(commissionedComparisonLoadStartedFor, viewModel))
        {
            return;
        }

        void Unavailable(string reason)
        {
            if (ReferenceEquals(DataContext, viewModel))
            {
                viewModel.MarkCommissionedComparisonUnavailable(reason);
            }
        }

        if (TopLevel.GetTopLevel(this)?.DataContext is not LibraryViewModel libraryViewModel)
        {
            return;
        }

        commissionedComparisonLoadStartedFor = viewModel;

        var electricalSheets = libraryViewModel.SelectedPlanSetSheets
            .Where(sheet => string.Equals(sheet.SheetType, "ElectricalPlan", StringComparison.Ordinal))
            .ToArray();
        if (electricalSheets.Length != 1 || !electricalSheets[0].SheetRegistrationId.HasValue)
        {
            Unavailable(
                "Comparación Floor/Electrical no disponible: se requiere exactamente un ElectricalPlan con registro resuelto.");
            return;
        }

        if (Program.Host is null)
        {
            Unavailable("Comparación Floor/Electrical no disponible: los servicios de lectura no están activos.");
            return;
        }

        try
        {
            var electricalSheet = electricalSheets[0];
            using var scope = Program.Host.Services.CreateScope();
            var registration = await scope.ServiceProvider
                .GetRequiredService<ISheetRegistrationRepository>()
                .GetByIdAsync(electricalSheet.SheetRegistrationId!.Value, CancellationToken.None);
            if (registration is null ||
                registration.DependentSheetId != electricalSheet.SheetId ||
                registration.Status is not SheetRegistrationStatus.Confirmed ||
                registration.Method is not SheetRegistrationMethod.WholeSheetSimilarity)
            {
                Unavailable(
                    "Comparación Floor/Electrical no disponible: el registro Electrical no está confirmado o no corresponde a la hoja activa.");
                return;
            }

            var wholePlanProof = registration.WholePlanRegistrationProof;
            if (wholePlanProof?.IsAuthoritative != true ||
                wholePlanProof.CanonicalFloorPlanVersionId != registration.CanonicalFloorPlanVersionId ||
                wholePlanProof.DependentSheetId != registration.DependentSheetId)
            {
                Unavailable(
                    "Comparación Floor/Electrical no disponible: falta una prueba whole-plan autoritativa y ligada a las hojas activas.");
                return;
            }

            var floorSource = await scope.ServiceProvider
                .GetRequiredService<IFloorPlanExtractionSourceReader>()
                .GetByVersionAsync(registration.CanonicalFloorPlanVersionId, CancellationToken.None);
            var electricalSource = await scope.ServiceProvider
                .GetRequiredService<IPlanSheetSourceReader>()
                .GetBySheetIdAsync(electricalSheet.SheetId, CancellationToken.None);
            if (floorSource is null || electricalSource is null)
            {
                Unavailable(
                    "Comparación Floor/Electrical no disponible: no se resolvieron ambos DXF fuente.");
                return;
            }

            var canonicalSourceSha256 = await ComputeSha256Async(
                floorSource.ManagedFilePath,
                CancellationToken.None);
            var dependentSourceSha256 = await ComputeSha256Async(
                electricalSource.SourceFilePath,
                CancellationToken.None);
            if (!string.Equals(
                    canonicalSourceSha256,
                    wholePlanProof.CanonicalSourceSha256,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    dependentSourceSha256,
                    wholePlanProof.DependentSourceSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                Unavailable(
                    "Comparación Floor/Electrical no disponible: los DXF resueltos ya no coinciden con la prueba de registro confirmada.");
                return;
            }

            var extractor = scope.ServiceProvider.GetRequiredService<IWallExtractor>();
            var canonicalGeometry = RegistrationReviewData.ToGeometry(
                await extractor.ExtractAsync(floorSource.ManagedFilePath, CancellationToken.None));
            var rawElectricalGeometry = RegistrationReviewData.ToGeometry(
                await extractor.ExtractAsync(electricalSource.SourceFilePath, CancellationToken.None));
            if (canonicalGeometry.Count == 0 || rawElectricalGeometry.Count == 0)
            {
                Unavailable(
                    "Comparación Floor/Electrical no disponible: alguno de los DXF no contiene trazas estructurales WALL seguras.");
                return;
            }

            var registeredElectricalGeometry = RegistrationReviewData.TransformElectricalGeometry(
                rawElectricalGeometry,
                new(
                    registration.Transform.Scale,
                    registration.Transform.RotationDegrees,
                    registration.Transform.TranslateX,
                    registration.Transform.TranslateY));
            if (!ReferenceEquals(DataContext, viewModel))
            {
                return;
            }

            viewModel.ApplyCommissionedStructuralComparison(
                registeredElectricalGeometry,
                $"Registro {registration.Method} confirmado · confianza {registration.Confidence:0.##} · prueba whole-plan verificada.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Unavailable(
                $"Comparación Floor/Electrical no disponible: no se pudo resolver la evidencia estructural ({exception.Message}).");
        }
    }

    private static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();
    }

    private void PreviewControl_OnFloorPlanMoveDeltaRequested(
        object? sender,
        FloorPlanPreviewControl.FloorPlanMoveDeltaEventArgs e)
    {
        if (DataContext is SitePlanAdjustmentViewModel viewModel)
        {
            viewModel.MoveFloorPlanBy(e.DeltaX, e.DeltaY);
        }
    }

    private void ApplyAutoFitOptionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SitePlanAdjustmentViewModel viewModel ||
            sender is not Button { DataContext: AutoFitSuggestionOptionViewModel option })
        {
            return;
        }

        viewModel.ApplyAutoFitPlan(option);
    }

    private async void ExportAdjustedSitePlanButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SitePlanAdjustmentViewModel viewModel)
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar DXF",
            SuggestedFileName = "plano-ajustado-al-sitio.dxf",
            DefaultExtension = "dxf",
            FileTypeChoices =
            [
                new FilePickerFileType("DXF files")
                {
                    Patterns = ["*.dxf"]
                }
            ]
        });

        if (file is null)
        {
            return;
        }

        await viewModel.ExportAdjustedSitePlanAsync(file.Path.LocalPath, CancellationToken.None);
    }
}
