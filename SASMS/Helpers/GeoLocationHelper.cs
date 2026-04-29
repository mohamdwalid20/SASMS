using System;

namespace SASMS.Helpers
{
    public static class GeoLocationHelper
    {
        /// <summary>
        /// Calculates the distance between two points on the Earth's surface using the Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of point 1</param>
        /// <param name="lon1">Longitude of point 1</param>
        /// <param name="lat2">Latitude of point 2</param>
        /// <param name="lon2">Longitude of point 2</param>
        /// <returns>Distance in meters</returns>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // Earth radius in meters
            var phi1 = lat1 * Math.PI / 180;
            var phi2 = lat2 * Math.PI / 180;
            var deltaPhi = (lat2 - lat1) * Math.PI / 180;
            var deltaLambda = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        /// <summary>
        /// Checks if a point is within a certain range of a reference point.
        /// </summary>
        public static bool IsWithinRange(double lat1, double lon1, double lat2, double lon2, double rangeInMeters)
        {
            var distance = CalculateDistance(lat1, lon1, lat2, lon2);
            return distance <= rangeInMeters;
        }
    }
}
