namespace api.Models
{
    public struct OpenRouteServiceMatrixResponse
    {
        public required double[][] Durations { get; init; }
        public required double[][] Distances { get; init; }
    }
}
