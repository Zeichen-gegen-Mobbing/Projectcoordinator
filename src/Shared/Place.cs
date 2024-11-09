using System.ComponentModel;
using System.Security.AccessControl;
using System.Text.Json.Serialization;

namespace ZgM.ProjectCoordinator.Shared
{
    [JsonConverter(typeof(IdJsonConverter<PlaceId, string>))]
    public class PlaceId(string value) : AbstractId<PlaceId, string>(value)
    {
    }

    [JsonConverter(typeof(IdJsonConverter<UserId, Guid>))]
    public class UserId(Guid value) : AbstractId<UserId, Guid>(value)
    {
    }

    public struct Place
    {
        public required UserId UserId { get; set; }

        public required PlaceId Id { get; set; }
        public required string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
