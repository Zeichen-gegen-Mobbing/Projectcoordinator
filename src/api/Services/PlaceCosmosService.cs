using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api.Entities;
using api.Models;
using api.Repositories;
using Microsoft.Azure.Cosmos;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public sealed class PlaceCosmosService(IPlaceRepository repository) : IPlaceService
    {

        public async Task<Place> AddPlace(PlaceRequest placeRequest)
        {
            var place = new PlaceEntity()
            {
                Id = new PlaceId(Guid.NewGuid().ToString()),
                UserId = placeRequest.UserId,
                Name = placeRequest.Name,
                Address = placeRequest.Address,
            };
            place = await repository.AddAsync(place);
            return new Place()
            {
                Id = place.Id,
                UserId = place.UserId,
                Name = place.Name
            };
        }

        public async Task<IEnumerable<Place>> GetAllPlacesAsync()
        {
            var placeEntities = await repository.GetAllAsync();
            return placeEntities.Select(entity =>
            {
                return new Place
                {
                    Id = entity.Id,
                    UserId = entity.UserId,
                    Name = entity.Name,
                };
            });
        }
    }
}
