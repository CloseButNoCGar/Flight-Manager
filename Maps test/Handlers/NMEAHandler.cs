using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps_test
{
    class NMEAHandler
    {
        public PointItem Point{get; set;}

        public NMEAHandler(string[] delimited)
        {
            if (delimited[0] == "$GPRMC" && delimited[3] != "")
            {
                double lat = double.Parse(delimited[3]);
                double lon = double.Parse(delimited[5]);
                double latdec = ConvertToDecimalDegrees(lat);
                double londec = ConvertToDecimalDegrees(lon);

                // Negates latitude if in Southern Hemisphere
                if (delimited[4] == "S")
                {
                    latdec = -latdec;
                }

                //Negates longitude if in Western Hemisphere
                if (delimited[6] == "W")
                {
                    londec = -londec;
                }

                Point = new PointItem(latdec, londec, 0);
            }
        }

        /// <summary>
        /// Converts a coordinate given in Degrees, Decimal Minutes (DDM) 
        /// to Decimal Degrees (DD).
        /// </summary>
        /// <param name="ddm"></param>
        /// <returns></returns>
        public double ConvertToDecimalDegrees(double ddm)
        {
            int DD = (int)(ddm / 100);
            double SS = ddm - (DD * 100);
            double decimaldegrees = DD + (SS / 60);
            return decimaldegrees;
        }

        public bool ValidSentence()
        {
            return (Point != null);
        }
    }
}
