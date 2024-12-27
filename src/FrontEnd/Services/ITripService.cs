using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface ITripService
    {
        public Task<IEnumerable<Trip>> GetTripsAsync(double latitude, double longitude);
    }
}
