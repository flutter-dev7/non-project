using System.Globalization;
using Non.Application.Common;
using Non.Application.DTOs.Settings.Request;
using Non.Application.DTOs.Settings.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.Application.Services;

public class SettingService : ISettingService
{
    private readonly ISettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public SettingService(
        ISettingRepository settingRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<SettingsResponse>> GetAsync(CancellationToken ct = default)
    {
        var cutoff = await _settingRepository.GetValueAsync(SettingKeys.OrderCutoffTime, ct)
                     ?? SettingDefaults.OrderCutoffTime;

        return Result<SettingsResponse>.Success(new SettingsResponse { OrderCutoffTime = cutoff });
    }

    public async Task<Result<SettingsResponse>> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        if (!TimeOnly.TryParseExact(request.OrderCutoffTime, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var cutoff))
        {
            return Result<SettingsResponse>.Failure("Время должно быть в формате HH:mm, например 18:00.");
        }

        var value = cutoff.ToString("HH:mm", CultureInfo.InvariantCulture);

        await _settingRepository.SetValueAsync(SettingKeys.OrderCutoffTime, value, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _cacheService.RemoveAsync(CacheKeys.OrderCutoffTime);

        return Result<SettingsResponse>.Success(new SettingsResponse { OrderCutoffTime = value });
    }
}