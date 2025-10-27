using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api.Entities;
using ZgM.ProjectCoordinator.Shared;

namespace api.Repositories
{
    public interface IPlaceRepository
    {
        Task<IEnumerable<PlaceEntity>> GetAllAsync();

        Task<IEnumerable<PlaceEntity>> GetByUserIdAsync(UserId userId);

        Task<PlaceEntity> AddAsync(PlaceEntity entity);

        Task<PlaceEntity> UpdateAsync(PlaceEntity entity);

        Task DeleteAsync(PlaceId placeId, UserId userId);
    }
}
