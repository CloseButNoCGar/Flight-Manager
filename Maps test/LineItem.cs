using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    public class LineItem : PolygonItem
    {
        public double Bearing { get; private set; }

        public double Length { get; private set; }

        public override string Label
        {
            get { return _label; }
            set { _label = "Line " + Id; }
        }

        public LineItem(IEnumerable<PointItem> p, int id) : base(p, id)
        {
            if (p.Count() != 2)
            {
                throw new InvalidOperationException();
            }

            CalculateBearing();
            Length = DistanceBetweenPoints(PointItems[0], PointItems[1]);
            RefreshChildren();
        }

        public LineItem(PointItem q, double bearing, double deltalat, double deltalon, int id)
        {
            Id = id;
            c = Color.Black;
            Bearing = bearing;
            PointItems = new List<PointItem>();
            PointItems.Add(q);
            PointItem item;

            item = new PointItem(q.Latitude + Math.Abs(deltalat), q.Longitude + Math.Abs(deltalon), 1);
            PointItems.Add(item);
            Length = DistanceBetweenPoints(PointItems[0], PointItems[1]);
            CreateGMapPolygon();
            RefreshChildren();
        }

        private void CalculateBearing()
        {
            double X;
            double Y;

            //Let:      β be Bearing
            //          θ be latitude
            //          L be longitude
            //          a denotes Point A
            //          b denotes Point B
            // Formula: β = atan2(X,Y)
            // Where:   X = cos θb * sin ∆L
            //          Y = cos θa * sin θb – sin θa * cos θb * cos ∆L
            X = Math.Cos(PointItems[1].Latitude) * Math.Sin(PointItems[0].Longitude - PointItems[1].Longitude);
            Y = Math.Cos(PointItems[0].Latitude) * Math.Sin(PointItems[1].Latitude)
                - Math.Sin(PointItems[0].Latitude) * Math.Cos(PointItems[1].Latitude)
                * Math.Cos(PointItems[0].Longitude - PointItems[1].Longitude);
            Bearing = Math.Atan2(X, Y);
        }

        public List<CameraTrigger> CreateTriggerPoints(double ydistance, double yoverlap, double radius)
        {

            double pointsNeeded = Math.Ceiling(Length / (ydistance - yoverlap));
            if (pointsNeeded > 500)
            {
                throw new Exception("Flightlines over cap, adjust camera settings or select a smaller area.");
            }
            double latlength = Math.Abs(PointItems[0].Latitude - PointItems[1].Latitude);
            double lonlength = Math.Abs(PointItems[0].Longitude - PointItems[1].Longitude);
            double distanceBetweenPoints = Length / pointsNeeded;
            List<PointItem> list = new List<PointItem>();
            list.Add(PointItems[0]);
            double lat;
            double lng;
            for (int i = 0; i < pointsNeeded; i++)
            {
                lng = list[i].Longitude + (lonlength / pointsNeeded);
                lat = list[i].Latitude + (latlength / pointsNeeded);
                list.Insert(i + 1, new PointItem(lat, lng, i));
            }

            List<CameraTrigger> triggers = new List<CameraTrigger>();
            int id = 0;
            foreach (var p in list)
            {
                triggers.Add(new CameraTrigger(p, Bearing, radius, id));
                id++;
            }
            return triggers;
        }
    }
}
