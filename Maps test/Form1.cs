using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Collections.ObjectModel;
using BrightIdeasSoftware;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using CameraControl.Devices;
using CameraControl.Devices.Classes;


namespace Maps_test
{

    public partial class Form1 : Form
    {
        private PointLatLng point;
        private double lat = 0;
        private double lng = 0;
        private int zoommax = 32;
        private int zoommin = 2;
        private int zoomlevel = 2;
        private GMapOverlay markers = new GMapOverlay("markers");
        private GMapOverlay polygons = new GMapOverlay("polygons");
        private ArrayList roots = new ArrayList();
        private static bool _connected = false;
        Thread gpsClientThread;
        public static double _bearing;
        public static double _xoverlap;
        public static double _xdistance;
        public static double _yoverlap;
        public static double _ydistance;
        public static double _radius;
        public CameraDeviceManager DeviceManager { get; set; }
        public string FolderForPhotos { get; set; }
        List<CameraTrigger> globalTriggers = new List<CameraTrigger>();

        public Form1()
        {
            DeviceManager = new CameraDeviceManager();
            DeviceManager.CameraSelected += DeviceManager_CameraSelected;
            DeviceManager.CameraConnected += DeviceManager_CameraConnected;
            DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
            DeviceManager.UseExperimentalDrivers = true;
            DeviceManager.DisableNativeDrivers = false;
            FolderForPhotos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Map Test Photos");
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DeviceManager.ConnectToCamera();
            map.ShowCenter = false;
            //Set lat and long to a point
            point = new PointLatLng(lat, lng);
            populateCombo();
            initialiseMap();
            centerMapToPoint(point);

            treeListView1.CanExpandGetter = delegate (object x) 
            {
                return ((ModelClass)x).HasChildren();
            };
            treeListView1.ChildrenGetter = delegate (object x) 
            {
                return ((ModelClass)x).GetChildren();
            };

            treeListView1.Roots = roots;
        }

        private void populateCombo()
        {
            var type = typeof(GMapProviders);
            foreach (var p in type.GetFields())
            {
                var v = p.GetValue(null) as GMapProvider;
                if(v != null)
                {
                    comboBox1.Items.Add(v);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            map.MapProvider = cmb.SelectedItem as GMapProvider;
        }
       

        private void initialiseMap()
        {
            //Set button that drags map
            map.DragButton = MouseButtons.Left;
            //Set zoom levels
            map.MinZoom = zoommin;
            map.MaxZoom = zoommax;
            map.Zoom = zoomlevel;
        }

        private void centerMapToPoint(PointLatLng p)
        {
            map.Overlays.Remove(markers);
            map.Overlays.Add(markers);
            map.Position = p;
        }

        private void map_OnMapDrag()
        {
            point = map.Position;
            toolStripStatusLabel1.Text = "Lat: " + point.Lat.ToString();
            toolStripStatusLabel2.Text = "Long: " + point.Lng.ToString();
            toolStripStatusLabel3.Text = "Height above map: " + getAltitudeAboveMap().ToString() + "m";
        }

        private void map_OnMapClick(PointLatLng PointClick, MouseEventArgs e)
        {
            PointItem point = new PointItem(PointClick.Lat, PointClick.Lng, 1);
            point.DrawOnMap(map, polygons, markers);
            treeListView1.AddObject(point);
            centerMapToPoint(map.Position);
        }

        private void btnDeleteSelected_Click(object sender, EventArgs e)
        {
            treeListView1.RemoveObjects(treeListView1.SelectedObjects);
            RedrawObjects();
        }

        private void RedrawObjects()
        {
            map.Overlays.Clear();
            markers.Clear();
            polygons.Clear();
            foreach(ModelClass i in treeListView1.Objects)
            {
                i.DrawOnMap(map, polygons, markers);
            }
            centerMapToPoint(map.Position);
        }

        private void btnDrawPolygon_Click(object sender, EventArgs e)
        {
            List<PointItem> list = new List<PointItem>();
            object item;
            foreach (var o in treeListView1.Objects)
            {
                if (o is PointItem)
                {
                    list.Add((PointItem)o);
                }
            }
            if (list.Count > 1)
            {
                if (list.Count == 2)
                {
                    item = new LineItem(list, 1);
                    ((LineItem)item).DrawOnMap(map, polygons, markers);
                }
                else
                {
                    item = new PolygonItem(list, 1);
                    ((PolygonItem)item).DrawOnMap(map, polygons, markers);
                }
                treeListView1.AddObject(item);
                centerMapToPoint(map.Position);
                treeListView1.RemoveObjects(list);
            }
        }

        public static class RequestConstants
        {
            public const string Url = "http://api.openweathermap.org/data/2.5/weather?lat=";
            public const string APIKey = "&APPID=b1581bc021b9ab6497a723ad49525732";
            public const string UserAgent = "User-Agent";
            public const string UserAgentValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:71.0) Gecko/20100101 Firefox/71.0";
        }

        private Tuple<double, double> getWindSpeedDirection(PointLatLng p)
        {
            var client = new WebClient();
            client.Headers.Add(RequestConstants.UserAgent, RequestConstants.UserAgentValue);
            var response = client.DownloadString(RequestConstants.Url + p.Lat.ToString() + "&lon=" + p.Lng.ToString() + RequestConstants.APIKey);
            JObject joResponse = JObject.Parse(response);
            JObject ojObject = (JObject)joResponse["wind"];
            JValue speed = (JValue)ojObject["speed"];
            JValue direct = (JValue)ojObject["deg"];
            double windSpeed = (double)speed;
            double windDirect = (double)direct;
            var tuple = new Tuple<double, double>(windSpeed, windDirect);
            return tuple;
        }

        private void btnWind_Click(object sender, EventArgs e)
        {
            Tuple<double, double> t = getWindSpeedDirection(map.Position);
            lblWindSpeed.Text = t.Item1.ToString();
            lblWindDirect.Text = t.Item2.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            foreach (var i in treeListView1.SelectedObjects)
            {
                if (i is PolygonItem)
                {
                    DialogResult result = f2.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        ((PolygonItem)i).CreateFlightLines(_bearing, _xoverlap, _xdistance, _yoverlap, _ydistance, _radius, map, polygons, markers);
                    }
                }
            }
            
        }

        private void GPSClientStart()
        {
                Invoke(new Action(() =>
                {
                    butTCPConnection.Text = "Working...";
                    butTCPConnection.BackColor = Color.Orange;
                }));
            try
            {
                TcpClient client = new TcpClient(textBox1.Text + "." + textBox2.Text + "." + textBox3.Text + "." + textBox5.Text, 6000);
                Invoke(new Action(() =>
                {
                    butTCPConnection.Text = "Connected";
                    butTCPConnection.BackColor = Color.Green;
                }));

                Read(client);
                client.Close();

                Invoke(new Action(() =>
                {
                    butTCPConnection.Text = "Disconnected";
                    butTCPConnection.BackColor = Color.Red;
                }));

            }
            catch
            {
                Invoke(new Action(() =>
                {
                    butTCPConnection.Text = "Error";
                }));
                Thread.Sleep(2000);
                Invoke(new Action(() =>
                {
                    butTCPConnection.Text = "Disconnected";
                    butTCPConnection.BackColor = Color.Red;
                }));

            }
            
        }

        public void Read(TcpClient client)
        {
            try
            {
                while (_connected)
                {
                    try
                    {
                        StreamReader reader = new StreamReader(client.GetStream());
                        string line = reader.ReadLine();
                        Console.WriteLine(line);

                        foreach (var i in globalTriggers)
                        {
                            Console.WriteLine(i.Label);
                        }

                    }
                    catch (EndOfStreamException)
                    {
                        return;
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"IOException reading from socket: {e.Message}");
            }
        }

        private void butTCPConnection_Click(object sender, EventArgs e)
        {

            if (!_connected)
            {
                _connected = true;
                gpsClientThread = new Thread(() => GPSClientStart());
                gpsClientThread.IsBackground = true;
                gpsClientThread.Start();
            }
            else
            {
                _connected = false;
            }
            
        }

        private void treeListView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            objectListView1.ClearObjects();
            objectListView1.AddObjects(treeListView1.SelectedObjects);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            foreach (var i in treeListView1.SelectedObjects)
            {
                if (i is PolygonItem)
                {
                    if (f2.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            ((PolygonItem)i).CreateFlightLines(_bearing, _xoverlap, _xdistance, _yoverlap, _ydistance, _radius, map, polygons, markers);
                        }
                        catch (Exception f)
                        {
                            MessageBox.Show(f.Message);
                            ((PolygonItem)i).FlightLines.Clear();
                            break;
                        }
                    }
                }
            }
            treeListView1_ItemsChanged(treeListView1, new ItemsChangedEventArgs());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("Do you really want to exit?", "Exit", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Environment.Exit(0);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void RefreshDisplay()
        {
            MethodInvoker method = delegate
            {
                comboBox2.BeginUpdate();
                comboBox2.Items.Clear();
                foreach (ICameraDevice cameraDevice in DeviceManager.ConnectedDevices)
                {
                    comboBox2.Items.Add(cameraDevice);
                }
                comboBox2.DisplayMember = "DeviceName";
                comboBox2.SelectedItem = DeviceManager.SelectedCameraDevice;
                comboBox2.EndUpdate();
            };

            if (InvokeRequired)
                BeginInvoke(method);
            else
                method.Invoke();
        }

        private new void Capture()
        {
            bool retry;
            do
            {
                retry = false;
                try
                {
                    DeviceManager.SelectedCameraDevice.CapturePhoto();
                }
                catch (DeviceException exception)
                {
                    if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy || exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                    {
                        Thread.Sleep(100);
                        retry = true;
                    }
                    else
                    {
                        MessageBox.Show("Error occurred:" + exception.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occurred:" + ex.Message);
                }
            } while (retry);
        }

        private void PhotoCaptured(object o)
        {
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null)
            {
                return;
            }

            try
            {
                string fileName = Path.Combine(FolderForPhotos, Path.GetFileName(eventArgs.FileName));
                // if file exist try to generate a new filename to prevent file lost. 
                // This useful when camera is set to record in ram the the all file names are same.
                if (File.Exists(fileName))
                    fileName =
                      StaticHelper.GetUniqueFilename(
                        Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0,
                        Path.GetExtension(fileName));

                // check the folder of filename, if not found create it
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                   Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }
                eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
                // the IsBusy may used internally, if file transfer is done should set to false  
                eventArgs.CameraDevice.IsBusy = false;
                if (Path.GetExtension(fileName) == ".NEF")
                { }
                else
                pictureBox1.ImageLocation = fileName;

            }
            catch (Exception exception)
            {
                eventArgs.CameraDevice.IsBusy = false;
                MessageBox.Show("Error download photo from camera :\n" + exception.Message);
            }
        }

        void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {

        }

        void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }

        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            Thread thread = new Thread(PhotoCaptured);
            thread.Start(eventArgs);
        }

        void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceManager.SelectedCameraDevice = (ICameraDevice)comboBox2.SelectedItem;
        }

        private float getAltitudeAboveMap()
        {
            float googleearthaltitude;
            float firstPartOfEq = (float)(.05 * (591657550.5 / Math.Pow(2, map.Zoom - 1) / 2));
            googleearthaltitude = firstPartOfEq * ((float)Math.Cos(85.362 / 2 * Math.PI / 180) / ((float)Math.Sin(85.362 / 2 * Math.PI / 180)));
            return googleearthaltitude;
        }

        private void butCamera_MouseClick(object sender, MouseEventArgs e)
        {
            Thread thread = new Thread(Capture);
            thread.Start();
        }

        public double DistanceBetweenPoints(PointItem p, PointItem q)
        {
            var latrad1 = p.Latitude * Math.PI / 180;
            var lonrad1 = p.Longitude * Math.PI / 180;
            var latrad2 = q.Latitude * Math.PI / 180;
            var lonrad2 = q.Longitude * Math.PI / 180;
            var deltalat = (q.Latitude - p.Latitude) * Math.PI / 180;
            var deltalon = (q.Longitude - p.Longitude) * Math.PI / 180;
            double a = Math.Sin(deltalat / 2) * Math.Sin(deltalat / 2) + Math.Cos(latrad1) * Math.Cos(latrad2) * Math.Sin(deltalon / 2) * Math.Sin(deltalon / 2);
            double distance = 6371e3 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return distance;
        }

        private void treeListView1_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            List<CameraTrigger> list = new List<CameraTrigger>();
            Console.WriteLine("updated");
            foreach (var i in treeListView1.Objects)
            {
                if (i is PolygonItem)
                {
                    foreach (var j in ((PolygonItem)i).FlightLines)
                    {
                        foreach (var k in j.Triggers)
                        {
                            list.Add(k);
                        }
                    }
                }
                
            }
            globalTriggers = list;

        }
    }
}
