using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Maps_test
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            txtFlightLines.Text = "" + trackBar1.Value;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Form1._xoverlap = Double.Parse(txtXOverlap.Text);
                Form1._yoverlap = Double.Parse(txtYOverlap.Text);
                Form1._xdistance = Double.Parse(txtXDistance.Text);
                Form1._ydistance = Double.Parse(txtYDistance.Text);
                Form1._radius = Double.Parse(txtRadius.Text);
                Form1._bearing = Double.Parse(txtFlightLines.Text);
                this.Dispose();
            }
            catch
            {
                MessageBox.Show("Incorrect inputs");
            }
            
        }
    }
}
