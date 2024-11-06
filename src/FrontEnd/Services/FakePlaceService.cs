using System;
using FrontEnd.Models;
using FrontEnd.Models.Id;

namespace FrontEnd.Services
{
    public class FakePlaceService : IPlaceService
    {
        private readonly IEnumerable<Place> _places = new List<Place>
        {
            new Place { Id = new PlaceId("P1"), UserId= new UserId(new Guid()), Name = "Place 1" },
            new Place { Id = new PlaceId("P2"),UserId= new UserId(new Guid()), Name = "Place 2" },
            new Place { Id = new PlaceId("P3"),UserId= new UserId(new Guid()), Name = "Place 3" },
            new Place { Id = new PlaceId("P4"),UserId= new UserId(new Guid()), Name = "Place 4" },
            new Place { Id = new PlaceId("P5"),UserId= new UserId(new Guid()), Name = "Place 5" },
        };
        public async Task<IEnumerable<Place>> GetAllPlacesAsync()
        {
            Random random = new Random();
            await Task.Delay(random.Next(random.Next(10000)));
            return _places;
        }

        public async Task<TimeSpan> GetTripTimeAsync(PlaceId place, string address)
        {
            Random random = new Random();
            await Task.Delay(random.Next(random.Next(10000)));
            return new TimeSpan(random.Next(8), random.Next(59), random.Next(59));
        }
    }
}
