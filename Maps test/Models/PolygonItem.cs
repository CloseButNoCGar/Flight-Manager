using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Maps_test
{
    public class PolygonItem : IModelClass
    {
        protected Color c;
        protected string _label;
        private List<FlightLineItem> _flightlines = new List<FlightLineItem>();

        public int Id { get; set; }

        /// <summary>
        /// List of children objects for use in objectlistview controls
        /// </summary>
        public List<object> Children { get; private set; }

        /// <summary>
        /// The GMapPolygon for this polygon
        /// </summary>
        public GMapPolygon Poly { get; private set; }

        /// <summary>
        /// List of PointItems that make up the polygon.
        /// </summary>
        public List<PointItem> PointItems { get; protected set; }

        /// <summary>
        /// Label to be displayed in objectlistview controls.
        /// </summary>
        public virtual string Label
        {
            get { return _label; }
            set { _label = "Polygon " + value; }
        }

        /// <summary>
        /// List of flight lines.
        /// </summary>
        public List<FlightLineItem> FlightLines
        {
            get { return _flightlines; }
            private set { _flightlines = value; RefreshChildren(); }
        }

        /// <summary>
        /// Empty constructor for inheritance.
        /// </summary>
        protected internal PolygonItem() { }

        /// <summary>
        /// Constructor, requires 3+ points.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="id"></param>
        public PolygonItem(IEnumerable<PointItem> p, int id)
        {
            if (!(this is LineItem) && p.Count() < 3)
            {
                throw new InvalidOperationException("Use LineItem for Polygons with 2 points, handle single point");
            }

            Id = id;
            Label = Id.ToString();
            c = Color.FromArgb(Id * new Random().Next());
            PointItems = p.ToList();
            CreateGMapPolygon();
            RefreshChildren();
        }

        /// <summary>
        /// Creates a GMapPolygon that can be drawn.
        /// </summary>
        public void CreateGMapPolygon()
        {
            Poly = new GMapPolygon(PointLatLngs(), Id.ToString())
            {
                Stroke = new Pen(Color.FromArgb(255, c)),
                Fill = new SolidBrush(Color.FromArgb(120, c))
            };
        }

        /// <summary>
        /// Returns all PointItems as a list of PointLatLngs
        /// </summary>
        /// <returns></returns>
        private List<PointLatLng> PointLatLngs()
        {
            List<PointLatLng> l = new List<PointLatLng>();

            foreach (PointItem i in PointItems)
            {
                l.Add(i.Coords);
            }
            return l;
        }

        /// <summary>
        /// Creates flight lines with triggers to cover polygon area. 
        /// Requires camera coverage parameters along with the map and overlays to draw on.
        /// </summary>
        /// <param name="gradient"></param>
        /// <param name="xoverlap"></param>
        /// <param name="xdistance"></param>
        /// <param name="yoverlap"></param>
        /// <param name="ydistance"></param>
        /// <param name="radius"></param>
        /// <param name="map"></param>
        /// <param name="polygons"></param>
        /// <param name="markers"></param>
        public void CreateFlightLines(double gradient, double xoverlap, double xdistance, double yoverlap, double ydistance, double radius, GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            const int maxLines = 200;
            double flineLatDelta;
            double flineLonDelta;
            List<PointItem> list = IntersectingPoints(gradient);

            // Length of flight line. Difference in lat and lon between bounding box lines.
            flineLatDelta = list[1].Latitude - list[0].Latitude;
            flineLonDelta = list[1].Longitude - list[0].Longitude;

            // Calculate number of flight lines required to statisfy overlap and camera coverage.
            double linesNeeded = Math.Ceiling(list[0].DistanceBetweenPoints(list[3]) / (xdistance - xoverlap));

            if (linesNeeded > maxLines)
            {
                throw new Exception("Flightlines over cap, adjust camera settings or select a smaller area.");
            }

            // Unneeded points.
            list.RemoveAt(2);
            list.RemoveAt(1);

            double latComponentDelta;
            double lonComponentDelta;

            // Difference of latitudes and longitudes for bounding box where flight lines will start from.
            latComponentDelta = list[1].Latitude - list[0].Latitude;
            lonComponentDelta = list[1].Longitude - list[0].Longitude;

            double lat;
            double lng;

            // Creates points for start of flight lines.
            for (int i = 0; i + 1 < linesNeeded; i++)
            {
                lng = list[i].Longitude + (lonComponentDelta / linesNeeded);
                lat = list[i].Latitude + (latComponentDelta / linesNeeded);

                list.Insert(i + 1, new PointItem(lat, lng, i));
            }
            
            List<LineItem> lines = new List<LineItem>();

            // Creates lines from start points.
            foreach (var i in list)
            {
                LineItem item = new LineItem(i, flineLatDelta, flineLonDelta, i.Id);
                lines.Add(item);
            }

            List<FlightLineItem> flines = new List<FlightLineItem>();

            // Creates flight lines using lines previously created. Resize flight lines to remove unnecessary trigger points.
            foreach (var i in lines)
            {
                try
                {
                    FlightLineItem item = new FlightLineItem(i, i.CreateTriggerPoints(ydistance, yoverlap, radius), lines.IndexOf(i));
                    item.ResizeToPoly(this);
                    if (item.Triggers.Count > 0)
                    {
                        item.DrawOnMap(map, polygons, markers);
                        flines.Add(item);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }

            FlightLines = flines;
            RefreshChildren();
        }

        

        /// <summary>
        /// Calculates the corner points of a rectangle that surrounds the polygon with a given gradient.
        /// The lines that are used to calculate distance are tangents of an ellipse that surrounds
        /// the world map coordinate system with equation of x^2/255^2 + y^2/130^2 = 1. Therefore
        /// every location on the map can use this function.
        /// </summary>
        /// <param name="gradient"></param>
        /// <returns></returns>
        private List<PointItem> IntersectingPoints(double gradient)
        {
            double distance;
            double perpdistance;
            int indexmax = 0;
            int indexmin = 0;
            int indexmaxperp = 0;
            int indexminperp = 0;
            double min = 0;
            double max = 0;
            double minp = 0;
            double maxp = 0;
            double perpendiculargradient = 1 / -gradient;

            foreach (PointItem i in PointItems)
            {
                int j = PointItems.IndexOf(i);
                distance = i.DistanceToEllipseTangent(false, gradient);
                perpdistance = i.DistanceToEllipseTangent(true, perpendiculargradient);

                // Saves index of closest point to tangent 
                if (min == 0 || distance < min)
                {
                    min = distance;
                    indexmin = j;
                }

                // Saves index of furthest point from tangent
                if (max == 0 || distance > max)
                {
                    max = distance;
                    indexmax = j;
                }

                // Saves index of closest point to perpendicular tangent
                if (minp == 0 || perpdistance < minp)
                {
                    minp = perpdistance;
                    indexminperp = j;
                }

                // Saves index of furthest point from perpendicular tangent
                if (maxp == 0 || perpdistance > maxp)
                {
                    maxp = perpdistance;
                    indexmaxperp = j;
                }
            }

            // Add indexes to array to allow iteration
            int[] indexes = new int[] { indexmin, indexmax, indexminperp, indexmaxperp };

            double x;
            double y;
            List<PointItem> list = new List<PointItem>();

            // Creates points at the intersection of lines through closest and furthest points to tangents
            // i / 2 = 0,0,1,1 for values of i = 0,1,2,3
            // i % 2 + 2 = 2,3,2,3 for values of i = 0,1,2,3
            for (int i = 0; i < 4; i++)
            {
                if (gradient == 0)
                {
                    x = PointItems[indexes[i % 2 + 2]].Longitude;
                    y = PointItems[indexes[i / 2]].Latitude;
                }
                else if (perpendiculargradient == 0)
                {
                    x = PointItems[indexes[(i / 2)]].Longitude;
                    y = PointItems[indexes[i % 2 + 2]].Latitude;
                }
                else
                {
                    x = ((gradient * PointItems[indexes[i / 2]].Longitude) - (perpendiculargradient * PointItems[indexes[i % 2 + 2]].Longitude) + PointItems[indexes[i % 2 + 2]].Latitude - PointItems[indexes[i / 2]].Latitude) / (gradient - perpendiculargradient);
                    y = gradient * (x - PointItems[indexes[i / 2]].Longitude) + PointItems[indexes[i / 2]].Latitude;
                }

                list.Add(new PointItem(y, x, i));
            }

            // Rearranges list for use in CreateFlightLines()
            list.Reverse(2, 2);
            return list;
        }

        /// <summary>
        /// Returns whether this item has children. Always true.
        /// </summary>
        /// <returns></returns>
        public bool HasChildren()
        {
            return true;
        }

        /// <summary>
        /// Returns all children items.
        /// </summary>
        /// <returns></returns>
        public List<object> GetChildren()
        {
            return Children;
        }

        /// <summary>
        /// Refreshes the list of children.
        /// </summary>
        protected void RefreshChildren()
        {
            List<object> list = new List<object>();

            foreach (var point in PointItems)
            {
                list.Add(point);
            }
            foreach (var fline in FlightLines)
            {
                list.Add(fline);
            }
            Children = list;
        }

        /// <summary>
        /// Draws polygon and all children items on given map and overlays.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="polygons"></param>
        /// <param name="markers"></param>
        public void DrawOnMap(GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            map.Overlays.Remove(polygons);
            polygons.Polygons.Add(Poly);
            map.Overlays.Add(polygons);
            foreach (PointItem point in PointItems)
            {
                point.DrawOnMap(map, polygons, markers);
            }
            foreach (FlightLineItem fline in FlightLines)
            {
                fline.DrawOnMap(map, polygons, markers);
            }
        }

        /// <summary>
        /// Changes trigger to captured if given trigger is a child.
        /// </summary>
        /// <param name="trigger"></param>
        public void ChangeTrigger(CameraTrigger trigger)
        {
            var query = from i in FlightLines
                        from j in i.Triggers
                        where j == trigger
                        select j;

            foreach (var j in query)
            {
                j.Captured = true;
                return;
            }
        }
    }
}
