using FrontEnd.Models;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Retrieves information about a User based on the UserId and replaces all other Properties with the new UserInformation
        /// </summary>
        /// <param name="id">The Id of the User</param>
        /// <returns>User object with UserInformation</returns>
        public Task<User> GetUserAsync(UserId Id);

        /// <summary>
        /// Searches for users matching the query string
        /// </summary>
        /// <param name="query">Search query</param>
        /// <returns>List of matching GraphUser objects</returns>
        public Task<IEnumerable<GraphUser>> SearchUsersAsync(string query);
    }
}
