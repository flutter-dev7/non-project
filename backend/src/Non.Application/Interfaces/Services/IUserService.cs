using Non.Application.Common;
using Non.Application.DTOs.Users.Request;
using Non.Application.DTOs.Users.Response;

namespace Non.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<PagedResult<UserDetailsResponse>>> GetPagedAsync(UserQueryRequest request, CancellationToken ct = default);

    Task<Result<UserDetailsResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<UserDetailsResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<Result<UserDetailsResponse>> UpdateAsync(
        Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken ct = default);
}