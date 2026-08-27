using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<Role?> GetRoleByIdAsync(int roleId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
