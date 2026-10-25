using Non.Application.Common;
using Non.Application.DTOs.Devices.Request;

namespace Non.Application.Interfaces.Services;

public interface IDeviceService
{
    Task<Result> SetFcmTokenAsync(Guid userId, RegisterFcmTokenRequest request, CancellationToken ct = default);

    Task<Result> ClearFcmTokenAsync(Guid userId, CancellationToken ct = default);
}