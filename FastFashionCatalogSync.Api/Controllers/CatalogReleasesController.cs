using FastFashionCatalogSync.Application.Releases;
using FastFashionCatalogSync.Domain.Releases;
using Microsoft.AspNetCore.Mvc;

namespace FastFashionCatalogSync.Api.Controllers;

[ApiController]
[Route("api/catalog-releases")]
public sealed class CatalogReleasesController : ControllerBase
{
    private readonly CatalogReleasePreviewBuilder _previewBuilder;
    private readonly CatalogReleaseOrchestrator _releaseOrchestrator;
    private readonly ILogger<CatalogReleasesController> _logger;

    public CatalogReleasesController(
        CatalogReleasePreviewBuilder previewBuilder,
        CatalogReleaseOrchestrator releaseOrchestrator,
        ILogger<CatalogReleasesController> logger)
    {
        _previewBuilder = previewBuilder;
        _releaseOrchestrator = releaseOrchestrator;
        _logger = logger;
    }

    [HttpGet("preview/latest")]
    public async Task<ActionResult<CatalogReleasePreview>> PreviewLatest(CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _previewBuilder.PreviewLatestApprovedAsync(cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ToProblem("Catalog preview could not be created.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while creating the latest catalog release preview.");
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Catalog preview failed.", "An unexpected error occurred while building the release preview."));
        }
    }

    [HttpGet("preview/{merchandisingVersionId}")]
    public async Task<ActionResult<CatalogReleasePreview>> PreviewVersion(
        string merchandisingVersionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _previewBuilder.PreviewVersionAsync(merchandisingVersionId, cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ToProblem("Catalog preview could not be created.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while creating catalog release preview for {MerchandisingVersionId}.", merchandisingVersionId);
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Catalog preview failed.", "An unexpected error occurred while building the release preview."));
        }
    }

    [HttpPost("schedule")]
    public async Task<ActionResult<ScheduleCatalogReleaseResult>> Schedule(
        ScheduleCatalogReleaseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _releaseOrchestrator.ScheduleAsync(request, cancellationToken);
            return AcceptedAtAction(nameof(List), new { id = result.ReleaseId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ToProblem("Catalog release could not be scheduled.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while scheduling catalog release for {MerchandisingVersionId}.", request.MerchandisingVersionId);
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Catalog release scheduling failed.", "An unexpected error occurred while scheduling the release."));
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CatalogRelease>>> List(CancellationToken cancellationToken)
    {
        try
        {
            var releases = await _releaseOrchestrator.ListAsync(cancellationToken);
            return Ok(releases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while listing catalog releases.");
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Catalog release listing failed.", "An unexpected error occurred while reading the release ledger."));
        }
    }

    [HttpPost("execute-due")]
    public async Task<ActionResult<CatalogReleaseSweepResult>> ExecuteDue(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _releaseOrchestrator.ExecuteDueAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while executing due catalog releases.");
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Catalog release execution failed.", "An unexpected error occurred while executing due releases."));
        }
    }

    private static ProblemDetails ToProblem(string title, string detail) =>
        new()
        {
            Title = title,
            Detail = detail
        };
}
