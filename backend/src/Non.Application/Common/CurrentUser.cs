using Non.Domain.Enums;

namespace Non.Application.Common;

public record CurrentUser(Guid Id, UserRole Role);