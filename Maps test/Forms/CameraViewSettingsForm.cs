using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlightManager
{
    public partial class CameraViewSettingsForm : Form
    {
        public CameraViewSettingsForm()
        {
            InitializeComponent();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            txtFlightLines.Text = "" + trackBar1.Value;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                MainMapForm._xoverlap = Double.Parse(txtXOverlap.Text);
                MainMapForm._yoverlap = Double.Parse(txtYOverlap.Text);
                MainMapForm._xdistance = Double.Parse(txtXDistance.Text);
                MainMapForm._ydistance = Double.Parse(txtYDistance.Text);
                MainMapForm._radius = Double.Parse(txtRadius.Text);
                MainMapForm._gradient = ConvertBearingToGradient(Double.Parse(txtFlightLines.Text));

                Dispose();
            }
            catch
            {
                MessageBox.Show("Incorrect inputs");
            }
            
        }

        /// <summary>
        /// Converts from bearing given in degrees to gradient in radians.
        /// </summary>
        /// <param name="bearing"></param>
        /// <returns></returns>
        private double ConvertBearingToGradient(double bearing)
        {
            double gradient;

            // Bearing is given in degrees (0 - 360), for the purposes of drawing flightlines 0 - 180 gives the same results.
            bearing = bearing % 180;

            // This is done due to rounding errors in Math.Tan not allowing the answer to = 0.
            if (bearing == 90)
            {
                gradient = 0;
            }
            else
            {
                gradient = 1 / Math.Tan(bearing * Math.PI / 180);
            }
            return gradient;
        }
    }
}
