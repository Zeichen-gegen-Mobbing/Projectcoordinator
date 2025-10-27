namespace FrontEnd.Services
{
    public class FakeRoleService : IRoleService
    {
        public Task<string[]> GetUserRolesAsync()
        {
            return Task.FromResult(new[] { "projectcoordination" });
        }
    }
}
