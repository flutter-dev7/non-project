using Non.Application.Common;
using Non.Application.DTOs.Settings.Request;
using Non.Application.DTOs.Settings.Response;

namespace Non.Application.Interfaces.Services;

public interface ISettingService
{
    Task<Result<SettingsResponse>> GetAsync(CancellationToken ct = default);

    Task<Result<SettingsResponse>> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default);
}