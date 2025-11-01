namespace ZgM.ProjectCoordinator.Shared
{
    /// <summary>
    /// Transport mode for calculating travel time
    /// </summary>
    public enum TransportMode
    {
        /// <summary>
        /// Calculate time using car/driving profile
        /// </summary>
        Car = 0,

        /// <summary>
        /// Calculate time using public transport (train) profile
        /// </summary>
        Train = 1
    }
}
