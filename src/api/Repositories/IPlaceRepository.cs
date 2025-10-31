using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api.Entities;

namespace api.Repositories
{
    using ZgM.ProjectCoordinator.Shared;

    public interface IPlaceRepository
    {
        Task<IEnumerable<PlaceEntity>> GetAllAsync();
        
        Task<IEnumerable<PlaceEntity>> GetAsync(UserId userId);

        Task<PlaceEntity> AddAsync(PlaceEntity entity);

        Task DeleteAsync(UserId userId, PlaceId placeId);
    }
}
