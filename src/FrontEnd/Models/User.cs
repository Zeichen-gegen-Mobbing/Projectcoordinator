using System.Text.Json.Serialization;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Models
{
    public class User(UserId id)
    {
        public  UserId Id { get; init; } = id;

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        public User(UserId id, string displayName) : this(id)
        {
            this.DisplayName = displayName;
        }

        public override string ToString()
        {
            return DisplayName ?? $"Unknwon User ({Id})";
        }
    }
}
