namespace FrontEnd.Services
{
    public interface IRoleService
    {
        Task<string[]> GetUserRolesAsync();
    }
}
