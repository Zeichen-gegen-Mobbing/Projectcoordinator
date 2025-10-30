namespace FrontEnd.Services
{
    public interface IRoleService
    {
        Task<bool> HasRole(string roleName);
    }
}
