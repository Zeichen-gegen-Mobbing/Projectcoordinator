using api.Models;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface ITripService
    {
        Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude);
        /// <summary>
        /// Validate if the given coordinates can be used.
        /// </summary>
        Task<bool> ValidateAsync(double latitude, double longitude);
    }
}
