﻿using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class FakePlaceService : IPlaceService
    {
        private readonly IEnumerable<Place> _places = new List<Place>
        {
            new Place { Id = new PlaceId("P1"), UserId= new UserId(Guid.NewGuid()), Name = "Fake FE Place 1" },
            new Place { Id = new PlaceId("P2"),UserId= new UserId(Guid.NewGuid()), Name = "Fake FE Place 2" },
            new Place { Id = new PlaceId("P3"),UserId= new UserId(Guid.NewGuid()), Name = " Fake FE Place 3" },
            new Place { Id = new PlaceId("P4"),UserId= new UserId(Guid.NewGuid()), Name = "Fake FE Place 4" },
            new Place { Id = new PlaceId("P5"),UserId= new UserId(Guid.NewGuid()), Name = "Fake FE Place 5" },
        };
        public async Task<IEnumerable<Place>> GetAllPlacesAsync()
        {
            Random random = new Random();
            await Task.Delay(random.Next(random.Next(10000)));
            return _places;
        }

        public async Task<Trip> GetTripAsync(PlaceId place, string address)
        {
            Random random = new Random();
            await Task.Delay(random.Next(random.Next(10000)));
            return new Trip() {
                Cost = (ushort)random.Next(ushort.MaxValue),
                PlaceId = place,
                Time = new TimeSpan(random.Next(8), random.Next(59), random.Next(59))
            };
        }
    }
}
