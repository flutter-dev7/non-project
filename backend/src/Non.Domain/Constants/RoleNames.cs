using Non.Domain.Enums;

namespace Non.Domain.Constants;

public static class RoleNames
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Agent = nameof(UserRole.Agent);
    public const string Courier = nameof(UserRole.Courier);
    public const string Shop = nameof(UserRole.Shop);

    public const string AdminOrAgent = Admin + "," + Agent;
}