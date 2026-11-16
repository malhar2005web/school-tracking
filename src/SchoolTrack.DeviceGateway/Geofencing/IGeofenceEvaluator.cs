namespace SchoolTrack.DeviceGateway.Geofencing;

public sealed record GeofenceEvaluationResult(
    bool IsWithinGeofence,
    double DistanceMeters,
    int GeofenceRadiusMeters);

public interface IGeofenceEvaluator
{
    GeofenceEvaluationResult Evaluate(
        decimal busLat,
        decimal busLon,
        decimal stopLat,
        decimal stopLon,
        int geofenceRadiusMeters);
}

public sealed class GeofenceEvaluator : IGeofenceEvaluator
{
    public GeofenceEvaluationResult Evaluate(
        decimal busLat,
        decimal busLon,
        decimal stopLat,
        decimal stopLon,
        int geofenceRadiusMeters)
    {
        var distance = HaversineDistanceCalculator.CalculateDistanceMeters(busLat, busLon, stopLat, stopLon);
        var isWithin = distance <= geofenceRadiusMeters;

        return new GeofenceEvaluationResult(isWithin, distance, geofenceRadiusMeters);
    }
}

