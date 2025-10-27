using api.Models;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface ITripService
    {
        Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude);
    }
}
