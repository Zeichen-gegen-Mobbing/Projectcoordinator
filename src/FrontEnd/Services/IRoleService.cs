namespace FrontEnd.Services
{
    public interface IRoleService
    {
        Task<bool> HasRole(string roleName);
        Task<string[]> GetRolesAsync();
    }
}
