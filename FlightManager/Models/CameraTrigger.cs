using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightManager
{
    public class CameraTrigger : IModelClass
    {
        private string _label;

        private bool _captured;

        private GMarkerGoogleType mark;

        public PointItem Point { get; set; }

        public List<object> Child { get; set; }

        public double Radius { get; set; }

        public int Id { get; set; }

        public bool Captured
        {
            get { return _captured; }
            set
            {
                _captured = value;
                if (_captured)
                {
                    mark = GMarkerGoogleType.green_small;
                }
                else
                {
                    mark = GMarkerGoogleType.red_small;
                }
            }
        }

        public string Label
        {
            get { return _label; }
            set { _label = "Trigger " + Id; }
        }

        public CameraTrigger(PointItem point, double radius, int id)
        {
            Point = point;
            Radius = radius;
            Id = id;
            Label = Id.ToString();
            Child = new List<object>();
            Child.Add(Point);
            Captured = false;
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
            GMapMarker marker = new GMarkerGoogle(Point.Coords, mark);
            map.Overlays.Remove(markers);
            markers.Markers.Add(marker);
            map.Overlays.Add(markers);
        }
    }
}
