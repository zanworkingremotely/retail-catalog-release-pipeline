using FastFashionCatalogSync.Application.Rollouts;
using FastFashionCatalogSync.Domain.Rollouts;
using Microsoft.AspNetCore.Mvc;

namespace FastFashionCatalogSync.Api.Controllers;

[ApiController]
[Route("api/product-rollouts")]
public sealed class ProductRolloutsController : ControllerBase
{
    private readonly ProductRolloutPreviewBuilder _previewBuilder;
    private readonly ProductRolloutWorkflow _rolloutWorkflow;
    private readonly ILogger<ProductRolloutsController> _logger;

    public ProductRolloutsController(
        ProductRolloutPreviewBuilder previewBuilder,
        ProductRolloutWorkflow rolloutWorkflow,
        ILogger<ProductRolloutsController> logger)
    {
        _previewBuilder = previewBuilder;
        _rolloutWorkflow = rolloutWorkflow;
        _logger = logger;
    }

    [HttpGet("preview/latest")]
    public async Task<ActionResult<ProductRolloutPreview>> PreviewLatest(CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _previewBuilder.PreviewLatestApprovedAsync(cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ToProblem("Rollout preview could not be created.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while creating the latest product rollout preview.");
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Rollout preview failed.", "An unexpected error occurred while building the rollout preview."));
        }
    }

    [HttpGet("preview/{merchandisingVersionId}")]
    public async Task<ActionResult<ProductRolloutPreview>> PreviewVersion(
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
            return BadRequest(ToProblem("Rollout preview could not be created.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while creating product rollout preview for {MerchandisingVersionId}.", merchandisingVersionId);
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Rollout preview failed.", "An unexpected error occurred while building the rollout preview."));
        }
    }

    [HttpPost("schedule")]
    public async Task<ActionResult<ScheduleProductRolloutResult>> Schedule(
        ScheduleProductRolloutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _rolloutWorkflow.ScheduleAsync(request, cancellationToken);
            return AcceptedAtAction(nameof(List), new { id = result.RolloutId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ToProblem("Product rollout could not be scheduled.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while scheduling product rollout for {MerchandisingVersionId}.", request.MerchandisingVersionId);
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Product rollout scheduling failed.", "An unexpected error occurred while scheduling the rollout."));
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductRollout>>> List(CancellationToken cancellationToken)
    {
        try
        {
            var rollouts = await _rolloutWorkflow.ListAsync(cancellationToken);
            return Ok(rollouts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while listing product rollouts.");
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Product rollout listing failed.", "An unexpected error occurred while reading the rollout ledger."));
        }
    }

    [HttpPost("execute-due")]
    public async Task<ActionResult<ProductRolloutSweepResult>> ExecuteDue(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _rolloutWorkflow.ExecuteDueAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while executing due product rollouts.");
            return StatusCode(StatusCodes.Status500InternalServerError, ToProblem("Product rollout execution failed.", "An unexpected error occurred while executing due rollouts."));
        }
    }

    private static ProblemDetails ToProblem(string title, string detail) =>
        new()
        {
            Title = title,
            Detail = detail
        };
}
