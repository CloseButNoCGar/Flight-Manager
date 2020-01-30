using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace FlightManager
{
    public class LineItem : PolygonItem
    {
        public double Length { get; private set; }

        public override string Label
        {
            get { return _label; }
            set { _label = "Line " + Id; }
        }

        /// <summary>
        /// Constructor when given 2 points.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="id"></param>
        public LineItem(IEnumerable<PointItem> p, int id) : base(p, id)
        {
            if (p.Count() != 2)
            {
                throw new InvalidOperationException();
            }

            Length = PointItems[0].DistanceBetweenPoints(PointItems[1]);
            RefreshChildren();
        }

        /// <summary>
        /// Constructor for use when a point and bearing are known.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="deltalat"></param>
        /// <param name="deltalon"></param>
        /// <param name="id"></param>
        public LineItem(PointItem q, double deltalat, double deltalon, int id)
        {
            Id = id;
            c = Color.Black;
            PointItems = new List<PointItem>();
            PointItems.Add(q);
            PointItem item;
            Label = id.ToString();
            item = new PointItem(q.Latitude + deltalat, q.Longitude + deltalon, 1);
            PointItems.Add(item);
            Length = PointItems[0].DistanceBetweenPoints(PointItems[1]);
            CreateGMapPolygon();
            RefreshChildren();
        }

        /// <summary>
        /// Creates a list of camera trigger items. 
        /// Limited to 500 max to not hang the program. 
        /// If over 500 points for flight line it is 
        /// likely the user has inputted wrong settings.
        /// </summary>
        /// <param name="ydistance"></param>
        /// <param name="yoverlap"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public List<CameraTrigger> CreateTriggerPoints(double ydistance, double yoverlap, double radius)
        {
            const int maxTriggers = 500;

            double pointsNeeded = Math.Ceiling(Length / (ydistance - yoverlap));

            if (pointsNeeded > maxTriggers)
            {
                throw new Exception("Number of triggers over cap, adjust camera settings or select a smaller area.");
            }

            double latlength = PointItems[1].Latitude - PointItems[0].Latitude;
            double lonlength = PointItems[1].Longitude - PointItems[0].Longitude;
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
                triggers.Add(new CameraTrigger(p, radius, id));
                id++;
            }
            return triggers;
        }
    }
}
