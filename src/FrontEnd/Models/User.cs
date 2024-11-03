using System.Text.Json.Serialization;

namespace FrontEnd.Models
{
    public class User
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }
}
