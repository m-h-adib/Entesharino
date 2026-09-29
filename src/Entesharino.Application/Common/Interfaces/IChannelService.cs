using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Channels.Models;

namespace Entesharino.Application.Common.Interfaces;

public interface IChannelService
{
    Task<ResultOfList<ChannelListItemDto>> GetListAsync(ChannelListRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<ChannelDetailsDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ResultDto<ChannelDetailsDto>> CreateAsync(CreateChannelRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<ChannelDetailsDto>> UpdateAsync(long id, UpdateChannelRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> UpdateConnectionAsync(long id, UpdateChannelConnectionRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<ResultDto> TestConnectionAsync(long id, CancellationToken cancellationToken = default);
}
