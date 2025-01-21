using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api
{
    public record OpenRouteServiceOptions
    {
        public required string Title { get; init; } = "OpenRouteService";
        public required string BaseUrl { get; init; } = "https://api.openrouteservice.org";

        [Required]
        public required string ApiKey { get; init; }
    }
}
