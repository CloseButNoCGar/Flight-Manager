using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    public class PolygonItem : ModelClass
    {
        protected Color c;
        protected string _label;
        private List<FlightLineItem> _flightlines = new List<FlightLineItem>();

        public int Id { get; set; }

        public List<object> Children { get; private set; }

        public GMapPolygon Poly { get; private set; }

        public List<PointItem> PointItems { get; protected set; }

        public virtual string Label
        {
            get { return _label; }
            set { _label = "Polygon " + value; }
        }

        public List<FlightLineItem> FlightLines
        {
            get { return _flightlines; }
            private set { _flightlines = value; RefreshChildren(); }
        }

        protected internal PolygonItem() { }

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

        public void CreateGMapPolygon()
        {
            Poly = new GMapPolygon(pointLatLngs(), Id.ToString())
            {
                Stroke = new Pen(Color.FromArgb(255, c)),
                Fill = new SolidBrush(Color.FromArgb(120, c))
            };
        }

        private List<PointLatLng> pointLatLngs()
        {
            List<PointLatLng> l = new List<PointLatLng>();

            foreach (PointItem i in PointItems)
            {
                l.Add(i.Coords);
            }
            return l;
        }

        public void CreateFlightLines(double bearing, double xoverlap, double xdistance, double yoverlap, double ydistance, double radius, GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            bearing = bearing % 180;
            List<PointItem> list = IntersectingPoints(bearing);
            double gradient = bearing * Math.PI / 180;

            double width = DistanceBetweenPoints(list[0], list[3]);
            double latheight = list[1].Latitude - list[0].Latitude;
            double lonheight = list[1].Longitude - list[0].Longitude;
            double pointsNeeded = Math.Ceiling(width / (xdistance - xoverlap));
            if (pointsNeeded > 200)
            {
                throw new Exception("Flightlines over cap, adjust camera settings or select a smaller area.");
            }
            double distanceBetweenPoints = width / pointsNeeded;
            list.RemoveAt(2);
            list.RemoveAt(1);

            double latlength = list[1].Latitude - list[0].Latitude;
            double lonlength = list[1].Longitude - list[0].Longitude;
            double lat;
            double lng;

            for (int i = 0; i + 1 < pointsNeeded; i++)
            {

                lng = list[i].Longitude + (lonlength / pointsNeeded);
                lat = list[i].Latitude + (latlength / pointsNeeded);

                list.Insert(i + 1, new PointItem(lat, lng, i));
            }

            List<LineItem> lines = new List<LineItem>();
            foreach (var i in list)
            {
                LineItem item = new LineItem(i, gradient, latheight, lonheight, i.Id);
                lines.Add(item);
            }

            List<FlightLineItem> flines = new List<FlightLineItem>();
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

        protected double DistanceBetweenPoints(PointItem p, PointItem q)
        {
            var latrad1 = p.Latitude * Math.PI/180;
            var lonrad1 = p.Longitude * Math.PI / 180;
            var latrad2 = q.Latitude * Math.PI / 180;
            var lonrad2 = q.Longitude * Math.PI / 180;
            var deltalat = (q.Latitude - p.Latitude) * Math.PI / 180;
            var deltalon = (q.Longitude - p.Longitude) * Math.PI / 180;
            double a = Math.Sin(deltalat / 2) * Math.Sin(deltalat / 2) + Math.Cos(latrad1) * Math.Cos(latrad2) * Math.Sin(deltalon / 2) * Math.Sin(deltalon / 2);
            double distance = 6371e3 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
            return distance;
        }

        //outer bounds
        private List<PointItem> IntersectingPoints(double bearing)
        {
            double gradient = 1 / Math.Tan(bearing * Math.PI/180);
            if (bearing == 90)
            {
                gradient = 0;
            }
            double distance;
            double distance2;
            int indexmax = 0;
            int indexmin = 0;
            int indexmaxoppo = 0;
            int indexminoppo = 0;
            double min = 0;
            double max = 0;
            double gradientoppo = 1 / -gradient;

            //Given bearing
            for (int i = 0; i < PointItems.Count(); i++)
            {
                if (Double.IsInfinity(gradient))
                {
                    distance = PointItems[i].Longitude + 255;
                    gradientoppo = 0;
                }
                else if (gradient == 0)
                {
                    distance = 130 - PointItems[i].Latitude;
                }
                else
                {
                    double b = PointItems[i].Latitude - (PointItems[i].Longitude / -gradient);
                    double xx = (Math.Sqrt(Math.Pow(255, 2)* Math.Pow(gradient, 2) + Math.Pow(130, 2)) - b) / ((1 / -gradient) - gradient);
                    double yy = 1 / -gradient * xx + b;
                    distance = Math.Sqrt(Math.Pow(xx - PointItems[i].Longitude, 2) + Math.Pow(yy - PointItems[i].Latitude, 2));
                }
                if (min == 0 || distance < min)
                {
                    min = distance;
                    indexmin = i;
                }
                if (max == 0 || distance > max)
                {
                    max = distance;
                    indexmax = i;
                }
            }
            min = 0;
            max = 0;

            //Opposite bearing
            for (int i = 0; i < PointItems.Count(); i++)
            {
                if (Double.IsInfinity(gradientoppo))
                {
                    distance2 = PointItems[i].Longitude + 255;
                }
                else if (gradientoppo == 0)
                {
                    distance2 = PointItems[i].Latitude + 130;
                }
                else
                {
                    // Calculates distance to the SW tangent line with opposite slope as given bearing of an ellipse that contains entire map coords
                    double b = PointItems[i].Latitude - (PointItems[i].Longitude / -gradientoppo);
                    double xx = (-Math.Sqrt(Math.Pow(255, 2) * Math.Pow(gradientoppo, 2) + Math.Pow(130, 2)) - b) / ((1 / -gradientoppo) - gradientoppo);
                    double yy = 1 / -gradientoppo * xx + b;
                    if(bearing>90)
                    {
                        distance2 = Math.Sqrt(Math.Pow(xx - PointItems[i].Latitude, 2) + Math.Pow(yy - PointItems[i].Longitude, 2));
                    }
                    else
                    {
                        distance2 = Math.Sqrt(Math.Pow(xx - PointItems[i].Longitude, 2) + Math.Pow(yy - PointItems[i].Latitude, 2));
                    }
                }

                if (min == 0 || distance2 < min)
                {
                    min = distance2;
                    indexminoppo = i;
                }
                if (max == 0 || distance2 > max)
                {
                    max = distance2;
                    indexmaxoppo = i;
                }
            }

            int[] indexes = new int[] { indexmin, indexmax, indexminoppo, indexmaxoppo };

            double x;
            double y;
            List<PointItem> list = new List<PointItem>();

            for (int i = 0; i < 4; i++)
            {
                if (gradient == 0)
                {
                    x = PointItems[indexes[i % 2 + 2]].Longitude;
                    y = PointItems[indexes[i / 2]].Latitude;
                }
                else if (gradientoppo == 0)
                {
                    x = PointItems[indexes[(i / 2)]].Longitude;
                    y = PointItems[indexes[i % 2 + 2]].Latitude;
                }
                else if (bearing < 90)
                {
                    x = ((gradient * PointItems[indexes[i / 2]].Longitude) - (gradientoppo * PointItems[indexes[i % 2 + 2]].Longitude) + PointItems[indexes[i % 2 + 2]].Latitude - PointItems[indexes[i / 2]].Latitude) / (gradient - gradientoppo);
                    y = gradient * (x - PointItems[indexes[i / 2]].Longitude) + PointItems[indexes[i / 2]].Latitude;
                }
                else
                {
                    x = ((gradient * PointItems[indexes[(i / 2) + (i % 2) + 1 - i]].Longitude) - (gradientoppo * PointItems[indexes[i % 2 + 2]].Longitude) + PointItems[indexes[i % 2 + 2]].Latitude - PointItems[indexes[(i / 2) + (i % 2) + 1 - i]].Latitude) / (gradient - gradientoppo);
                    y = gradient * (x - PointItems[indexes[(i / 2) + (i % 2) + 1 - i]].Longitude) + PointItems[indexes[(i / 2) + (i % 2) + 1 - i]].Latitude;
                }

                list.Add(new PointItem(y, x, i));
            }
            list.Reverse(2, 2);
            if (bearing > 90)
            {
                list.Add(list[0]);
                list.RemoveAt(0);
            }
            return list;
        }

        public bool HasChildren()
        {
            return true;
        }

        public List<object> GetChildren()
        {
            return Children;
        }

        protected void RefreshChildren()
        {
            List<object> list = new List<object>();

            foreach(var i in PointItems)
            {
                list.Add(i);
            }
            foreach(var i in FlightLines)
            {
                list.Add(i);
            }
            Children = list;
        }

        public void DrawOnMap(GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            map.Overlays.Remove(polygons);
            polygons.Polygons.Add(Poly);
            map.Overlays.Add(polygons);
            foreach(PointItem i in PointItems)
            {
                i.DrawOnMap(map, polygons, markers);
            }
            foreach(FlightLineItem i in FlightLines)
            {
                i.DrawOnMap(map, polygons, markers);
            }
        }
    }
}
