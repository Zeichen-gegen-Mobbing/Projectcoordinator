﻿using ZgM.ProjectCoordinator.Shared;

namespace api.Entities
{
    public struct PlaceEntity
    {
        public required UserId UserId { get; init; }
        public required PlaceId Id { get; init; }
        public required string Name { get; init; }
        public required string Address { get; init; }
    }
}