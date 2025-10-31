using System.Text.Json.Serialization;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Models
{
    public record GraphUser
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("displayName")]
        public required string DisplayName { get; init; }

        [JsonPropertyName("mail")]
        public string? Mail { get; init; }

        public User ToUser()
        {
            return new User(UserId.Parse(Id), DisplayName);
        }
    }
}
