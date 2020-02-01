using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;


namespace FlightManager
{
    public class PointItem : IModelClass
    {
        private string _label;
        private double _latitude;
        private double _longitude;

        /// <summary>
        /// Get and Set the Id for the Point.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Get GMapPoint to be placed on a map.
        /// </summary>
        public PointLatLng Coords { get; private set; }

        /// <summary>
        /// Get and Set label of point of format "Point <value>"
        /// </summary>
        public string Label
        {
            get { return _label; }
            set { _label = "Point " + Id; }
        }
        
        /// <summary>
        /// Get the latitude of the point.
        /// Set latitude, updates GMapPoint to be placed on a map.
        /// </summary>
        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; CreateGMapPoint(); }
        }

        /// <summary>
        /// Get the longitude of the point.
        /// </summary>
        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; CreateGMapPoint(); }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lat"> Latitude of point</param>
        /// <param name="lng"> Longitude of point</param>
        /// <param name="id"> Id for point</param>
        public PointItem(double lat, double lng, int id)
        {
            Id = id;
            Latitude = lat;
            Longitude = lng;
            Label = Id.ToString();
            CreateGMapPoint();
        }

        /// <summary>
        /// Creates a GMapPoint for placement on a map
        /// </summary>
        private void CreateGMapPoint()
        {
            Coords = new PointLatLng(Latitude, Longitude);
        }

        public bool HasChildren()
        {
            return false;
        }

        public List<object> GetChildren()
        {
            return new List<object>();
        }

        public void DrawOnMap(GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            GMapMarker marker = new GMarkerGoogle(Coords, GMarkerGoogleType.blue_small);
            map.Overlays.Remove(markers);
            markers.Markers.Add(marker);
            map.Overlays.Add(markers);
        }

        /// <summary>
        /// Calculates the distance in metres to given point.
        /// </summary>
        /// <param name="q">Point</param>
        /// <returns>Distance to point</returns>
        public double DistanceBetweenPoints(PointItem q)
        {
            const double earthRadius = 6371e3;

            var latrad1 = Latitude * Math.PI / 180;
            var lonrad1 = Longitude * Math.PI / 180;
            var latrad2 = q.Latitude * Math.PI / 180;
            var lonrad2 = q.Longitude * Math.PI / 180;
            var deltalat = (q.Latitude - Latitude) * Math.PI / 180;
            var deltalon = (q.Longitude - Longitude) * Math.PI / 180;
            double a = Math.Sin(deltalat / 2) * Math.Sin(deltalat / 2) + Math.Cos(latrad1) * Math.Cos(latrad2) * Math.Sin(deltalon / 2) * Math.Sin(deltalon / 2);
            double distance = earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return distance;
        }

        /// <summary>
        /// Calculates the distance from the point to a tangent on a theoretical ellipse that surrounds the map coordinate system.
        /// The ellipse has the equation of x^2/255^2 + y^2/130^2 = 1.
        /// If the perpendicular variable is false the function will calculate distance to the tangent in the North hemisphere.
        /// If the perpendicular variable is true the function will calculate distance to the tangent in the Eastern hemisphere.
        /// </summary>
        /// <param name="perpendicular"></param>
        /// <param name="gradient"></param>
        /// <returns></returns>
        public double DistanceToEllipseTangent(bool perpendicular, double gradient)
        {
            const double ellipseXComponent = 255;
            const double ellipseYComponent = 130;
            double distance;

            if (Double.IsInfinity(gradient))
            {
                distance = Longitude + ellipseXComponent;
            }
            else if (gradient == 0)
            {
                if (perpendicular)
                {
                    distance = Latitude + ellipseYComponent;
                }
                else
                {
                    distance = ellipseYComponent - Latitude;
                }
            }
            else
            {
                double b = Latitude - (Longitude / -gradient);
                double xx = (Math.Sqrt(Math.Pow(ellipseXComponent, 2) * Math.Pow(gradient, 2) + Math.Pow(ellipseYComponent, 2)) - b) / ((1 / -gradient) - gradient);
                double yy = 1 / -gradient * xx + b;
                distance = Math.Sqrt(Math.Pow(xx - Longitude, 2) + Math.Pow(yy - Latitude, 2));

                if (perpendicular && gradient > 0)
                {
                    distance = -distance;
                }
            }

            return distance;
        }
    }
}
