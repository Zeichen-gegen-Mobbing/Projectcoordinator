using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api.Entities;
using api.Repositories;
using Microsoft.Azure.Cosmos;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public sealed class PlaceCosmosService(IPlaceRepository repository) : IPlaceService
    {

        public async Task AddPlace(PlaceEntity place)
        {
            await repository.AddAsync(place);
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
