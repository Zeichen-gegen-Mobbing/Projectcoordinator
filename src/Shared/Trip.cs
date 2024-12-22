using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZgM.ProjectCoordinator.Shared
{
    public readonly struct Trip
    {
        public required PlaceId PlaceId { get; init; }
        public required TimeSpan Time { get; init; }

        /// <summary>
        /// The cost of the Trip in cents.
        /// </summary>
        public required ushort Cost { get; init; }
    }
}
