using JadaraClearance.Models;

namespace JadaraClearance.Helpers;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName);
}
