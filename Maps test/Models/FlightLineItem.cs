using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    public class FlightLineItem : IModelClass
    {
        private LineItem _line;
        private List<CameraTrigger> _triggers;
        private string _label;

        public int Id { get; set; }

        public List<object> Children { get; private set; }

        public LineItem Line
        {
            get { return _line; }
            set { _line = value; RefreshChildren(); }
        }

        public List<CameraTrigger> Triggers
        {
            get { return _triggers; }
            set { _triggers = value; RefreshChildren(); }
        }

        public string Label
        {
            get { return _label; }
            set { _label = "Flight Line " + value; }
        }

        public FlightLineItem(LineItem l, List<CameraTrigger> t, int id)
        {
            Line = l;
            Triggers = t;
            Id = id;
            Label = id.ToString();
        }

        /// <summary>
        /// Resizes the flight line to the given polygon (usually parent).
        /// Find if trigger points are over polygon. If they are then the 
        /// previous trigger and next trigger will be kept. Removes any 
        /// that are not over polygon or not next to one that is over polygon.
        /// </summary>
        /// <param name="p"></param>
        public void ResizeToPoly(PolygonItem p)
        {
            bool mem1 = false;
            bool mem2 = true;
            List<int> indexesToRemove = new List<int>();

            for (int i = Triggers.Count - 1; i > -1; i--)
            {
                if (Triggers[i].IsOverPolygon(p))
                {
                    mem2 = mem1;
                    mem1 = true;
                }
                else
                {
                    if (!mem1 && !mem2)
                    {
                        Triggers.RemoveAt(i + 1);
                    }
                    mem2 = mem1;
                    mem1 = false;
                }
            }
            if (!mem1 && !mem2)
            {
                Triggers.RemoveAt(0);
            }
            if (Triggers.Count != 0)
            {
                Line.PointItems[0] = Triggers[0].Point;
                Line.PointItems[1] = Triggers.Last().Point;
                Line.CreateGMapPolygon();
            }
            RefreshChildren();
        }

        public bool HasChildren()
        {
            return true;
        }

        public List<object> GetChildren()
        {
            return Children;
        }

        private void RefreshChildren()
        {
            List<object> list = new List<object>();

            list.Add(Line);
            
            if(Triggers != null)
            {
                foreach (var i in Triggers)
                {
                    list.Add(i);
                }
            }

            Children = list;
        }

        public void DrawOnMap(GMapControl map, GMapOverlay polygons, GMapOverlay markers)
        {
            Line.DrawOnMap(map, polygons, markers);
            foreach (CameraTrigger o in Triggers)
            {
                o.DrawOnMap(map, polygons, markers);
            }
        }
    }
}
