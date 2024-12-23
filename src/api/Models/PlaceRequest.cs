using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZgM.ProjectCoordinator.Shared;

namespace api.Models
{
    public struct PlaceRequest
    {
        public required UserId UserId { get; init; }
        public required string Name { get; init; }
        public required double Longitude { get; init; }
        public required double Latitude { get; init; }
    }
}
