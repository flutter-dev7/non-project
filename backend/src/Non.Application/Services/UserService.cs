using Non.Application.Common;
using Non.Application.DTOs.Users.Request;
using Non.Application.DTOs.Users.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<UserDetailsResponse>>> GetPagedAsync(
        UserQueryRequest request, CancellationToken ct = default)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _userRepository.GetPagedAsync(
            request.Search, request.Role, request.IsActive, page, pageSize, ct);

        return Result<PagedResult<UserDetailsResponse>>.Success(new PagedResult<UserDetailsResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<UserDetailsResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDetailsResponse>.Failure("Пользователь не найден.", ErrorType.NotFound);

        return Result<UserDetailsResponse>.Success(ToResponse(user));
    }

    public async Task<Result<UserDetailsResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);
        if (phone is null)
            return Result<UserDetailsResponse>.Failure("Неверный номер телефона.");

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (firstName.Length == 0 || lastName.Length == 0)
            return Result<UserDetailsResponse>.Failure("Укажите имя и фамилию.");

        if (await _userRepository.GetByPhoneAsync(phone, ct) is not null)
            return Result<UserDetailsResponse>.Failure("Пользователь с таким номером уже существует.", ErrorType.Conflict);

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Role = request.Role,
            IsActive = true
        };

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<UserDetailsResponse>.Success(ToResponse(user));
    }

    public async Task<Result<UserDetailsResponse>> UpdateAsync(
        Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetTrackedByIdAsync(id, ct);
        if (user is null)
            return Result<UserDetailsResponse>.Failure("Пользователь не найден.", ErrorType.NotFound);

        if (id == currentUserId && (!request.IsActive || request.Role != user.Role))
            return Result<UserDetailsResponse>.Failure("Нельзя отключить себя или изменить свою роль.");

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (firstName.Length == 0 || lastName.Length == 0)
            return Result<UserDetailsResponse>.Failure("Укажите имя и фамилию.");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<UserDetailsResponse>.Success(ToResponse(user));
    }

    private static UserDetailsResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}