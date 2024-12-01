using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api.Entities;

namespace api.Repositories
{
    public interface IPlaceRepository
    {
        Task<IEnumerable<PlaceEntity>> GetAllAsync();

        Task<PlaceEntity> AddAsync(PlaceEntity entity);
    }
}
