using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api
{
    public record CosmosSettings
    {
        public required string ConnectionString { get; init; }
        public required string DatabaseId { get; init; } = "cosql-shared-free-zgm";
        public required string ContainerId { get; init; } = "Projectcoordinator-Places";
    }
}
