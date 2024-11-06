using FrontEnd.Models.Id;

namespace FrontEnd.Models
{
    public class Place
    {
        public required UserId UserId { get; set; }

        public required PlaceId Id { get; set; }
        public required string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
