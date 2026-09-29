using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Reports.Models;

namespace Entesharino.Application.Features.Reports;

public interface IReportService
{
    Task<ResultDto<DeliveryReportDto>> GetDeliveryReportAsync(DeliveryReportRequest request, CancellationToken cancellationToken = default);

    Task<ResultDto<DeliverySummaryDto>> GetDeliverySummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default);

    Task<ResultDto<DashboardDto>> GetDashboardAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default);
}
