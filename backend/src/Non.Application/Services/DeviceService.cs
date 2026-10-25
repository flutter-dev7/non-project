using Non.Application.Common;
using Non.Application.DTOs.Devices.Request;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class DeviceService : IDeviceService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeviceService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> SetFcmTokenAsync(Guid userId, RegisterFcmTokenRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetTrackedByIdAsync(userId, ct);
        if (user is null)
            return Result.Failure("Пользователь не найден.", ErrorType.NotFound);

        var token = request.Token.Trim();

        var sameToken = await _userRepository.GetTrackedByFcmTokenAsync(token, ct);

        foreach (var other in sameToken.Where(u => u.Id != userId))
            other.FcmToken = null;

        user.FcmToken = token;

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> ClearFcmTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetTrackedByIdAsync(userId, ct);
        if (user is null)
            return Result.Failure("Пользователь не найден.", ErrorType.NotFound);

        user.FcmToken = null;

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}