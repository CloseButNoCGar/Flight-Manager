using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    public class CameraTrigger : ModelClass
    {
        private string _label;

        public PointItem Point { get; set; }

        public List<object> Child { get; set; }

        private double Bearing { get; set; }

        public double Radius { get; set; }

        public int Id { get; set; }

        public string Label
        {
            get { return _label; }
            set { _label = "Trigger " + Id; }
        }

        public CameraTrigger(PointItem point, double bearing, double radius, int id)
        {
            Point = point;
            Bearing = bearing;
            Radius = radius;
            Id = id;
            Label = Id.ToString();
            Child = new List<object>();
            Child.Add(Point);
        }

        public bool IsOverPolygon(PolygonItem p)
        {
            return p.Poly.IsInside(Point.Coords);
        }

        public bool WithinBounds(PointLatLng p)
        {
            return p.Lat < Point.Latitude + Radius && p.Lat > Point.Latitude - Radius && p.Lng < Point.Longitude + Radius && p.Lng > Point.Longitude - Radius;
        }

        public bool HasChildren()
        {
            return true;
        }

        public List<object> GetChildren()
        {
            return Child;
        }

        public void DrawOnMap(GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            GMapMarker marker = new GMarkerGoogle(Point.Coords, GMarkerGoogleType.red_dot);
            map.Overlays.Remove(markers);
            markers.Markers.Add(marker);
            map.Overlays.Add(markers);
        }
    }
}
