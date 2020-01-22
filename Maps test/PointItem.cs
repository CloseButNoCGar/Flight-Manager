using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    public class PointItem : ModelClass
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
            GMapMarker marker = new GMarkerGoogle(Coords, GMarkerGoogleType.blue_dot);
            map.Overlays.Remove(markers);
            markers.Markers.Add(marker);
            map.Overlays.Add(markers);
        }
    }
}
