using Non.Application.Common;
using Non.Domain.Entities;

namespace Non.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    JwtToken Generate(User user);
}