using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entesharino.Api.Authorization;
using Entesharino.Application.Features.Reports;
using Entesharino.Application.Features.Reports.Models;
using Entesharino.Domain.Constants;

namespace Entesharino.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("deliveries")]
    [HasPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] DeliveryReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDeliveryReportAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("deliveries/summary")]
    [HasPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetDeliverySummary(
        [FromQuery] DashboardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDeliverySummaryAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("dashboard")]
    [HasPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DashboardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDashboardAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
