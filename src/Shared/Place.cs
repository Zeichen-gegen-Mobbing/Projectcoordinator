using System.ComponentModel;
using System.Security.AccessControl;
using System.Text.Json.Serialization;
using StronglyTypedIds;

namespace ZgM.ProjectCoordinator.Shared
{
    [StronglyTypedId(Template.String)]
    public partial struct PlaceId
    {
    }

    [StronglyTypedId]
    public partial struct UserId
    {
    }

    public struct Place
    {
        public required PlaceId Id { get; set; }
        public required UserId UserId { get; set; }
        public required string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
