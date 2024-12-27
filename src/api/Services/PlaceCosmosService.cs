using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api.Entities;
using api.Exceptions;
using api.Models;
using api.Repositories;
using Microsoft.Azure.Cosmos;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public sealed class PlaceCosmosService(IPlaceRepository repository, ITripService tripService) : IPlaceService
    {

        public async Task<Place> AddPlace(PlaceRequest placeRequest)
        {
            var coordinatesValid = await tripService.ValidateAsync(placeRequest.Latitude, placeRequest.Longitude);
            if (coordinatesValid)
            {
                var place = new PlaceEntity()
                {
                    Id = new PlaceId(Guid.NewGuid().ToString()),
                    UserId = placeRequest.UserId,
                    Name = placeRequest.Name,
                    Longitude = placeRequest.Longitude,
                    Latitude = placeRequest.Latitude
                };
                place = await repository.AddAsync(place);
                return new Place()
                {
                    Id = place.Id,
                    UserId = place.UserId,
                    Name = place.Name
                };
            }
            else
            {
                throw new ProblemDetailsException(System.Net.HttpStatusCode.BadRequest, "Coordinates not valid", "Failed to find streets to snap to");
            }
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
