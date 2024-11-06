using FrontEnd.Models.Id;

namespace FrontEnd.Models
{
    public record Trip
    {
        public required UserId UserId;
        public required PlaceId PlaceId;
        public TimeSpan time;
    }
}
