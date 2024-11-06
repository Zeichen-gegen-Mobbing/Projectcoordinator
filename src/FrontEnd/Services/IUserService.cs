using FrontEnd.Models;
using FrontEnd.Models.Id;

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
    }
}
