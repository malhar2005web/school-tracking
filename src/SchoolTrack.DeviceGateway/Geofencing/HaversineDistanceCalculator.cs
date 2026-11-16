namespace SchoolTrack.DeviceGateway.Geofencing;

/// <summary>
/// High-precision spherical geodetic calculations using the Haversine great-circle formula.
/// </summary>
public static class HaversineDistanceCalculator
{
    private const double EarthRadiusMeters = 6371000.0;

    /// <summary>
    /// Computes the great-circle distance between two geographic coordinates in meters.
    /// </summary>
    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var rLat1 = ToRadians(lat1);
        var rLat2 = ToRadians(lat2);

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Computes the great-circle distance between two decimal coordinates in meters.
    /// </summary>
    public static double CalculateDistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        return CalculateDistanceMeters((double)lat1, (double)lon1, (double)lat2, (double)lon2);
    }

    /// <summary>
    /// Computes initial bearing / heading in degrees [0, 360) from origin to destination.
    /// </summary>
    public static double CalculateHeadingDegrees(double lat1, double lon1, double lat2, double lon2)
    {
        var rLat1 = ToRadians(lat1);
        var rLat2 = ToRadians(lat2);
        var dLon = ToRadians(lon2 - lon1);

        var y = Math.Sin(dLon) * Math.Cos(rLat2);
        var x = Math.Cos(rLat1) * Math.Sin(rLat2) -
                Math.Sin(rLat1) * Math.Cos(rLat2) * Math.Cos(dLon);

        var initialBearing = Math.Atan2(y, x);
        var initialBearingDegrees = ToDegrees(initialBearing);

        return (initialBearingDegrees + 360.0) % 360.0;
    }

    /// <summary>
    /// Computes bearing / heading in degrees [0, 360) from origin to destination using decimal coordinates.
    /// </summary>
    public static decimal CalculateHeadingDegrees(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        return (decimal)CalculateHeadingDegrees((double)lat1, (double)lon1, (double)lat2, (double)lon2);
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
    private static double ToDegrees(double radians) => radians * (180.0 / Math.PI);
}

