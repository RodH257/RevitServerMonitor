using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PacketDotNet;
using RevitServerMonitor.Properties;
using SharpPcap;

namespace RevitServerMonitor
{
    public partial class RevitServerMonitorForm : Form
    {
        //delgate for updating ui
        private delegate void UpdateUiHandler(bool communicating);
        //timer for turning off the communication
        private Timer _endCommunicatingTimer = new Timer();

        public RevitServerMonitorForm()
        {
            InitializeComponent();

            //setup timer
            _endCommunicatingTimer.Interval = 5 * 1000;
            _endCommunicatingTimer.Tick += new EventHandler(timer_Tick);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //start capturing for all interfaces
            foreach (ICaptureDevice device in CaptureDeviceList.Instance)
            {
                    device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
                    device.Open(DeviceMode.Normal, 1000);
                    device.StartCapture();
            }
        }

      
        //check the packet is port 808
        void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (e.Packet.Data.Length < 20)
                return;

            Packet p = null;
            TcpPacket tcp = null;
            try
            {
                p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
                tcp = TcpPacket.GetEncapsulated(p);

            }
            catch
            {
                return;
            }
            if (tcp == null)
                return;
            ushort portToFind = 808;
            if (tcp.DestinationPort == portToFind || tcp.DestinationPort == portToFind)
            {
                UpdateUiHandler handler = new UpdateUiHandler(SetCommunicating);
                Object[] args = new Object[] { true };
                this.BeginInvoke(handler, args);
            }


        }

      
        /// <summary>
        /// Sets communicating display on or off
        /// </summary>
        void SetCommunicating(bool communicating)
        {
            if (communicating)
            {

                this.lblCommunicating.Text = "COMMUNICATING - DO NOT DISCONNECT";
                this.lblCommunicating.ForeColor = Color.Red;
                this.trayIcon.Icon = Resources.RedArrow;
                //set an event for in 5 seconds time to swap it to not communicating.
                _endCommunicatingTimer.Stop();
                _endCommunicatingTimer.Start();
            }
            else
            {
                this.lblCommunicating.ForeColor = Color.Green;
                this.trayIcon.Icon = Resources.GreenArrow;
                this.lblCommunicating.Text = "Not communicating. Safe to disconnect";
                _endCommunicatingTimer.Stop();
            }
        }


        /// <summary>
        /// Sets it to not communicating
        /// </summary>
        void timer_Tick(object sender, EventArgs e)
        {
            SetCommunicating(false);
        }

        /// <summary>
        /// Stop Capturing on Close
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (ICaptureDevice device in CaptureDeviceList.Instance)
            {
                device.StopCapture();
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
        
        private void trayIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void RevitServerMonitorForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        }
    }
}
