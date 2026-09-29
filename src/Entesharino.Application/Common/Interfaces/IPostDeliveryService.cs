using Entesharino.Application.Common.Models;

namespace Entesharino.Application.Common.Interfaces;

public interface IPostDeliveryService
{
    Task<ResultDto> RetryAsync(
        long postChannelId,
        CancellationToken cancellationToken = default);
}
