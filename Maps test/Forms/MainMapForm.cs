using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Net;
using BrightIdeasSoftware;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace Maps_test
{

    public partial class MainMapForm : Form
    {
        // Initial map position variables
        private double lat = 0;
        private double lng = 0;
        private PointLatLng point;
        // Max, min and initial zoom level
        private int zoommax = 32;
        private int zoommin = 2;
        private int zoomlevel = 16;
        // Overlays for map, to show polygons and markers
        private GMapOverlay markers = new GMapOverlay("markers");
        private GMapOverlay polygons = new GMapOverlay("polygons");
        // Treelist roots
        private ArrayList roots = new ArrayList();
        // TCP/IP connection variables and thread
        private static bool _connected = false;
        Thread gpsClientThread;
        // Variables for flightline input
        public static double _gradient;
        public static double _xoverlap;
        public static double _xdistance;
        public static double _yoverlap;
        public static double _ydistance;
        public static double _radius;
        // Camera control variables
        public CameraDeviceManager DeviceManager { get; set; }
        public string FolderForPhotos { get; set; }
        // Triggers to compare, and have been taken
        List<CameraTrigger> globalTriggers = new List<CameraTrigger>();
        List<CameraTrigger> capturedTriggers = new List<CameraTrigger>();

        public MainMapForm()
        {
            // Camera control events
            DeviceManager = new CameraDeviceManager();
            DeviceManager.CameraSelected += DeviceManager_CameraSelected;
            DeviceManager.CameraConnected += DeviceManager_CameraConnected;
            DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
            DeviceManager.UseExperimentalDrivers = true;
            DeviceManager.DisableNativeDrivers = false;
            // Path to save photos taken
            FolderForPhotos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Map Test Photos");

            InitializeComponent();
        }

        private void MainMapForm_Load(object sender, EventArgs e)
        {
            // Connect to camera
            DeviceManager.ConnectToCamera();
            // Remove map center reticle
            map.ShowCenter = false;
            // Set the initial lat and lon to a point
            point = new PointLatLng(lat, lng);
            // Add all possible maps to combobox
            PopulateCombo(comboBox1);
            // Initialises the map control
            InitialiseMap();
            // Sets center of map to initial lat lon
            CenterMapToPoint(point);

            // TreeListView child expander functions
            treeListView1.CanExpandGetter = delegate (object x) 
            {
                return ((IModelClass)x).HasChildren();
            };
            treeListView1.ChildrenGetter = delegate (object x) 
            {
                return ((IModelClass)x).GetChildren();
            };
            // Sets roots of the TreeListView
            treeListView1.Roots = roots;
        }

        /// <summary>
        /// Populates a given combobox with all possible GMapProviders
        /// </summary>
        /// <param name="cmb"></param>
        private void PopulateCombo(ComboBox cmb)
        {
            var type = typeof(GMapProviders);

            foreach (var p in type.GetFields())
            {
                if (p.GetValue(null) is GMapProvider v)
                {
                    cmb.Items.Add(v);
                }
            }
        }

        /// <summary>
        /// Event for combobox1 selected item change. Sets the map provider to the selected map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            map.MapProvider = cmb.SelectedItem as GMapProvider;
        }
       
        /// <summary>
        /// Initialises map control
        /// </summary>
        private void InitialiseMap()
        {
            //Set button that drags map
            map.DragButton = MouseButtons.Left;
            //Set zoom levels
            map.MinZoom = zoommin;
            map.MaxZoom = zoommax;
            map.Zoom = zoomlevel;
        }

        /// <summary>
        /// Sets the map position to a given point. 
        /// Removes and readds the marker overlay to ensure 
        /// markers are on top of polygons.
        /// </summary>
        /// <param name="p"></param>
        private void CenterMapToPoint(PointLatLng p)
        {
            map.Overlays.Remove(markers);
            map.Overlays.Add(markers);
            map.Position = p;
        }

        /// <summary>
        /// Event when map control is dragged.
        /// Sets the point to center of map. 
        /// Displays lat lon and altitude on form. 
        /// </summary>
        private void Map_OnMapDrag()
        {
            point = map.Position;
            toolStripStatusLabel1.Text = "Lat: " + point.Lat.ToString();
            toolStripStatusLabel2.Text = "Long: " + point.Lng.ToString();
            toolStripStatusLabel3.Text = "Height above map: ~" + GetAltitudeAboveMap(map.Zoom).ToString() + "m";
        }

        /// <summary>
        /// Event when map is clicked. 
        /// Creates a new marker at location clicked, 
        /// adds it to the treelistview and draws on the map
        /// </summary>
        /// <param name="PointClick"></param>
        /// <param name="e"></param>
        private void Map_OnMapClick(PointLatLng PointClick, MouseEventArgs e)
        {
            PointItem point = new PointItem(PointClick.Lat, PointClick.Lng, 1);
            point.DrawOnMap(map, polygons, markers);
            treeListView1.AddObject(point);
            CenterMapToPoint(map.Position);
        }

        /// <summary>
        /// Event for delete selected button.
        /// Removes the root objects selected in the treelistview.
        /// Redraws the map with current items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            treeListView1.RemoveObjects(treeListView1.SelectedObjects);
            RedrawObjects();
        }

        /// <summary>
        /// Redraws the map with objects in the treelistview.
        /// </summary>
        private void RedrawObjects()
        {
            map.Overlays.Clear();
            markers.Clear();
            polygons.Clear();

            foreach (IModelClass i in treeListView1.Objects)
            {
                i.DrawOnMap(map, polygons, markers);
            }

            CenterMapToPoint(map.Position);
        }

        /// <summary>
        /// Event for draw polygon button.
        /// Gets all root points in treelistview and creates a 
        /// polygon if more than 2 points, a line if exactly
        /// 2 points, and nothing if there is 1 or less.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDrawPolygon_Click(object sender, EventArgs e)
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
                CenterMapToPoint(map.Position);
                treeListView1.RemoveObjects(list);
            }
        }

        /// <summary>
        /// Constants for wind info retrieval
        /// </summary>
        public static class RequestConstants
        {
            public const string Url = "http://api.openweathermap.org/data/2.5/weather?lat=";
            public const string APIKey = "&APPID=b1581bc021b9ab6497a723ad49525732";
            public const string UserAgent = "User-Agent";
            public const string UserAgentValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:71.0) Gecko/20100101 Firefox/71.0";
        }

        /// <summary>
        /// Gets the weather at a given point and parses out the wind information from JSON.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Tuple<double, double> GetWindSpeedDirection(PointLatLng p)
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

        /// <summary>
        /// Gets wind info and sets it to form controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnWind_Click(object sender, EventArgs e)
        {
            Tuple<double, double> t = GetWindSpeedDirection(map.Position);
            lblWindSpeed.Text = t.Item1.ToString();
            lblWindDirect.Text = t.Item2.ToString();
        }

        /// <summary>
        /// Function to start the GPSClient.
        /// Adjusts button control to give information of status.
        /// Gets IP connection address from textboxes.
        /// </summary>
        private void GPSClientStart()
        {
            Invoke(new Action(() =>
            {
                butTCPConnection.Text = "Working...";
                butTCPConnection.BackColor = Color.Orange;
            }));

            try
            {
                TcpClient client = new TcpClient($"{textBox1.Text}.{textBox2.Text}.{textBox3.Text}.{textBox5.Text}", 8000);

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

        /// <summary>
        /// Function to read line from tcp/ip connection.
        /// Compares to all camera triggers. If the GPS 
        /// coordinate is close enough to the trigger the camera
        /// is triggered and the camera point changes from red to green.
        /// </summary>
        /// <param name="client"></param>
        public void Read(TcpClient client)
        {
            try
            {
                while (_connected)
                {
                    try
                    {
                        StreamReader reader = new StreamReader(client.GetStream());
                        string delimited = reader.ReadLine();
                        NMEAHandler handler = new NMEAHandler(delimited.Split(','));
                        System.Diagnostics.Debug.WriteLine(delimited);
                        if (handler.ValidSentence())
                        {
                            foreach (var i in globalTriggers)
                            {
                                double j = i.Point.DistanceBetweenPoints(handler.Point);

                                if (j < _radius)
                                {
                                    Thread thread = new Thread(Capture);
                                    thread.Start();

                                    globalTriggers.Remove(i);
                                    capturedTriggers.Add(i);
                                    break;
                                }
                            }
                        }

                        Invoke(new Action(() => GreenTrigger()));
                    }
                    catch (EndOfStreamException)
                    {
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        System.Diagnostics.Debug.WriteLine("Happening here");
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"IOException reading from socket: {e.Message}");
            }
        }

        /// <summary>
        /// Changes all triggers that have been captured from green to red.
        /// </summary>
        private void GreenTrigger()
        {
            MethodInvoker method = delegate
            {
                foreach (var i in treeListView1.Objects)
                {
                    if (i is PolygonItem)
                    {
                        foreach (var j in capturedTriggers)
                        {
                            ((PolygonItem)i).ChangeTrigger(j);
                        }
                    }
                }

                RedrawObjects();
            };

            if (InvokeRequired)
            {
                BeginInvoke(method);
            }
            else
            {
                method.Invoke();
            }
        }

        /// <summary>
        /// Starts GPSClient thread if not already to connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButTCPConnection_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                _connected = true;
                gpsClientThread = new Thread(() => GPSClientStart())
                {
                    IsBackground = true
                };
                gpsClientThread.Start();
            }
            else
            {
                _connected = false;
            }
        }

        /// <summary>
        /// Changes the displayed item in the 
        /// objectlistview to selected items in treelistview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeListView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            objectListView1.ClearObjects();
            objectListView1.AddObjects(treeListView1.SelectedObjects);
        }

        /// <summary>
        /// Event for adding flightlines to polygon.
        /// Opens a dialog to retrieve settings for 
        /// creating flightlines and then creates them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click_1(object sender, EventArgs e)
        {
            CameraViewSettingsForm f2 = new CameraViewSettingsForm();
            foreach (var i in treeListView1.SelectedObjects)
            {
                if (i is PolygonItem)
                {
                    if (f2.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            ((PolygonItem)i).CreateFlightLines(_gradient, _xoverlap, _xdistance, _yoverlap, _ydistance, _radius, map, polygons, markers);
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

            TreeListView1_ItemsChanged(treeListView1, new ItemsChangedEventArgs());
            RedrawObjects();
        }

        /// <summary>
        /// Close the application handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Camera combobox updater with connected cameras
        /// </summary>
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

        /// <summary>
        /// Camera capture handler, retrys 
        /// taking the photo if device is busy.
        /// </summary>
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

        /// <summary>
        /// Method to save captured photos to file
        /// </summary>
        /// <param name="o"></param>
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
                // If file exist try to generate a new filename to prevent file lost. 
                // This is useful when camera is set to record in ram and the all file names are same.
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
                // Only display non RAW images
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

        /// <summary>
        /// Unused. Required for swapping connected cameras
        /// </summary>
        /// <param name="oldcameraDevice"></param>
        /// <param name="newcameraDevice"></param>
        void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {

        }

        /// <summary>
        /// When a new camera is connected the display refreshes.
        /// </summary>
        /// <param name="cameraDevice"></param>
        void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// When a photo is taken from the selected camera handles on a new thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            Thread thread = new Thread(PhotoCaptured);
            thread.Start(eventArgs);
        }

        /// <summary>
        /// When a camera is disconnected refreshes the display.
        /// </summary>
        /// <param name="cameraDevice"></param>
        void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// Camera device combobox selection handler.
        /// Sets camera device to selected option.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceManager.SelectedCameraDevice = (ICameraDevice)comboBox2.SelectedItem;
        }

        /// <summary>
        /// Calculates how high the camera is at the given zoom level using google
        /// </summary>
        /// <returns></returns>
        private float GetAltitudeAboveMap(double zoomlevel)
        {
            float googleearthaltitude;
            float firstPartOfEq = (float)(.05 * (591657550.5 / Math.Pow(2, zoomlevel - 1) / 2));
            googleearthaltitude = firstPartOfEq * ((float)Math.Cos(85.362 / 2 * Math.PI / 180) / ((float)Math.Sin(85.362 / 2 * Math.PI / 180)));
            return googleearthaltitude;
        }

        /// <summary>
        /// Manual camera capture button event handler.
        /// Starts camera capture on new thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButCamera_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(Capture);
            thread.Start();
        }

        /// <summary>
        /// Handler for when items are added or removed from the treelistview. 
        /// Updates global list of camera triggers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeListView1_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            List<CameraTrigger> list = new List<CameraTrigger>();

            foreach (var i in treeListView1.Objects)
            {
                if (i is PolygonItem)
                {
                    foreach (var j in ((PolygonItem)i).FlightLines)
                    {
                        foreach (var k in j.Triggers)
                        {
                            if (!k.Captured)
                            list.Add(k);
                        }
                    }
                }
            }

            globalTriggers = list;
        }
    }
}
