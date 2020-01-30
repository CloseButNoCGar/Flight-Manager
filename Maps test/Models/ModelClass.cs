using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    /// <summary>
    /// Interface for objects for use in TreeListView controls.
    /// </summary>
    public interface IModelClass
    {
        List<object> GetChildren();
        bool HasChildren();
        void DrawOnMap(GMapControl map, GMapOverlay overlay, GMapOverlay overlay2);
    }
}
