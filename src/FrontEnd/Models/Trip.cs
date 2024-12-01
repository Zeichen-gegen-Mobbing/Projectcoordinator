using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Models
{
    public record Trip
    {
        public required UserId UserId;
        public required PlaceId PlaceId;
        public TimeSpan time;
    }
}
