using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Reports.Models;

namespace Entesharino.Application.Features.Reports;

public interface IReportService
{
    Task<ResultDto<DashboardDto>> GetDashboardAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default);
}
