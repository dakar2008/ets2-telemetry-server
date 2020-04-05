using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Controllers;
using Funbit.Ets.Telemetry.Server.Data;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Setup;
using Microsoft.Owin.Hosting;
using Funbit.Ets.Telemetry.Server.Models;
using SCSSdkClient;
using SCSSdkClient.Object;
using System.Collections.Generic;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Funbit.Ets.Telemetry.Server
{
    public partial class MainForm : Form
    {
        private static SCSSdkTelemetry Telemetry;
        protected internal static SCSTelemetry data;
        private DBContext db = new DBContext();

        IDisposable _server;
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly HttpClient _broadcastHttpClient = new HttpClient();
        static readonly Encoding Utf8 = new UTF8Encoding(false);
        static readonly string BroadcastUrl = ConfigurationManager.AppSettings["BroadcastUrl"];
        static readonly string BroadcastUserId = Convert.ToBase64String(
            Utf8.GetBytes(ConfigurationManager.AppSettings["BroadcastUserId"] ?? ""));
        static readonly string BroadcastUserPassword = Convert.ToBase64String(
            Utf8.GetBytes(ConfigurationManager.AppSettings["BroadcastUserPassword"] ?? ""));
        static readonly int BroadcastRateInSeconds = Math.Min(Math.Max(1,
            Convert.ToInt32(ConfigurationManager.AppSettings["BroadcastRate"])), 86400);
        static readonly bool UseTestTelemetryData = Convert.ToBoolean(
            ConfigurationManager.AppSettings["UseEts2TestTelemetryData"]);

        private static void Telemetry_Data(SCSTelemetry data, bool newTimestamp)
        {
            if (newTimestamp)
            {
                MainForm.data = data;
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        static string IpToEndpointUrl(string host)
        {
            return $"http://{host}:{ConfigurationManager.AppSettings["Port"]}";
        }

        void Setup()
        {
            try
            {
                if (Program.UninstallMode && SetupManager.Steps.All(s => s.Status == SetupStatus.Uninstalled))
                {
                    MessageBox.Show(this, @"Server is not installed, nothing to uninstall.", @"Done",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }

                if (Program.UninstallMode || SetupManager.Steps.Any(s => s.Status != SetupStatus.Installed))
                {
                    // we wait here until setup is complete
                    var result = new SetupForm().ShowDialog(this);
                    if (result == DialogResult.Abort)
                        Environment.Exit(0);
                }

                // raise priority to make server more responsive (it does not eat CPU though!)
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ex.ShowAsMessageBox(this, @"Setup error");
            }
        }

        void Start()
        {
            try
            {
                // load list of available network interfaces
                var networkInterfaces = NetworkHelper.GetAllActiveNetworkInterfaces();
                interfacesDropDown.Items.Clear();
                foreach (var networkInterface in networkInterfaces)
                    interfacesDropDown.Items.Add(networkInterface);
                // select remembered interface or default
                var rememberedInterface = networkInterfaces.FirstOrDefault(
                    i => i.Id == Settings.Instance.DefaultNetworkInterfaceId);
                if (rememberedInterface != null)
                    interfacesDropDown.SelectedItem = rememberedInterface;
                else
                    interfacesDropDown.SelectedIndex = 0; // select default interface

                // bind to all available interfaces
                _server = WebApp.Start<Startup>(IpToEndpointUrl("+"));

                // start ETS2 process watchdog timer
                statusUpdateTimer.Enabled = true;

                // turn on broadcasting if set
                if (!string.IsNullOrEmpty(BroadcastUrl))
                {
                    _broadcastHttpClient.DefaultRequestHeaders.Add("X-UserId", BroadcastUserId);
                    _broadcastHttpClient.DefaultRequestHeaders.Add("X-UserPassword", BroadcastUserPassword);
                    broadcastTimer.Interval = BroadcastRateInSeconds * 1000;
                    broadcastTimer.Enabled = true;
                }

                // show tray icon
                trayIcon.Visible = true;
                
                // make sure that form is visible
                Activate();
                Telemetry = new SCSSdkTelemetry();
                Telemetry.Data += Telemetry_Data;
                Telemetry.Ferry += TelemetryFerry;
                Telemetry.Fined += TelemetryFine;
                Telemetry.Tollgate += TelemetryTollGate;
                Telemetry.Train += TelemetryTrainEvents;
                Telemetry.JobStarted += TelemetryJobStarted;
                Telemetry.JobDelivered += TelemetryJobEnded;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ex.ShowAsMessageBox(this, @"Network error", MessageBoxIcon.Exclamation);
            }
        }
        
        void MainForm_Load(object sender, EventArgs e)
        {
            // log current version for debugging
            Log.InfoFormat("Running application on {0} ({1}) {2}", Environment.OSVersion, 
                Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                Program.UninstallMode ? "[UNINSTALL MODE]" : "");
            Text += @" " + AssemblyHelper.Version;

            // install or uninstall server if needed
            Setup();
            ResetEventsDB();
            // start WebApi server
            Start();
        }

        void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _server?.Dispose();
            trayIcon.Visible = false;
        }
    
        void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        void statusUpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (UseTestTelemetryData)
                {
                    statusLabel.Text = @"Connected to Ets2TestTelemetry.json";
                    statusLabel.ForeColor = Color.DarkGreen;
                } 
                else if (Ets2ProcessHelper.IsEts2Running && Ets2TelemetryDataReader.Instance.IsConnected)
                {
                    statusLabel.Text = $"Connected to the simulator ({Ets2ProcessHelper.LastRunningGameName})";
                    statusLabel.ForeColor = Color.DarkGreen;
                }
                else if (Ets2ProcessHelper.IsEts2Running)
                {
                    statusLabel.Text = $"Simulator is running ({Ets2ProcessHelper.LastRunningGameName})";
                    statusLabel.ForeColor = Color.Teal;
                    trayIcon.Text = "Simulator is running...";
                }
                else
                {
                    statusLabel.Text = @"Simulator is not running";
                    trayIcon.Text = "Simulator is not running...";
                    statusLabel.ForeColor = Color.FromArgb(240, 55, 30);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ex.ShowAsMessageBox(this, @"Process error");
                statusUpdateTimer.Enabled = false;
            }
        }

        void apiUrlLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessHelper.OpenUrl(((LinkLabel)sender).Text);
        }

        void appUrlLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessHelper.OpenUrl(((LinkLabel)sender).Text);
        }
        
        void MainForm_Resize(object sender, EventArgs e)
        {
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
            if (!ShowInTaskbar && trayIcon.Tag == null)
            {
                trayIcon.ShowBalloonTip(1000, @"ETS2/ATS Telemetry Server", @"Double-click to restore.", ToolTipIcon.Info);
                trayIcon.Tag = "Already shown";
            }
        }

        void interfaceDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedInterface = (NetworkInterfaceInfo) interfacesDropDown.SelectedItem;
            apiUrlLabel.Text = IpToEndpointUrl(selectedInterface.Ip) + Ets2TelemetryController.TelemetryApiUriPath;
            linkLabel1.Text = IpToEndpointUrl(selectedInterface.Ip) + Ets2TelemetryController.TelemetryEventApiUriPath;
            ipAddressLabel.Text = selectedInterface.Ip;
            Settings.Instance.DefaultNetworkInterfaceId = selectedInterface.Id;
            Settings.Instance.Save();
        }

        async void broadcastTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                broadcastTimer.Enabled = false;
                await _broadcastHttpClient.PostAsJsonAsync(BroadcastUrl, Ets2TelemetryDataReader.Instance.Read());
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            broadcastTimer.Enabled = true;
        }
        
        void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string exeFileName = Process.GetCurrentProcess().MainModule.FileName;
            var startInfo = new ProcessStartInfo
            {
                Arguments = $"/C ping 127.0.0.1 -n 2 && \"{exeFileName}\" -uninstall",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(startInfo);
            Application.Exit();
        }

        void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessHelper.OpenUrl("http://funbit.info/ets2/donate.htm");
        }

        void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessHelper.OpenUrl("https://github.com/Funbit/ets2-telemetry-server");
        }

        void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: implement later
        }

        private void TelemetryJobStarted(object sender, EventArgs e)
        {
            JobStatus js = db.JobStatuses.FirstOrDefault();
            js.JobStarted = true;
            js.JobDelivered = false;
            db.SaveChanges();
        }

        private void TelemetryJobEnded(object sender, EventArgs e)
        {
            JobStatus js = db.JobStatuses.FirstOrDefault();
            js.JobStarted = false;
            js.JobDelivered = true;
            db.SaveChanges();
        }

        private async void TelemetryFerry(object sender, EventArgs e)
        {
            await Task.Delay(10000);
            FerryEventModel fem = new FerryEventModel();
            fem.PayAmount = data.GamePlay.FerryEvent.PayAmount;
            fem.SourceId = data.GamePlay.FerryEvent.SourceId;
            fem.SourceName = data.GamePlay.FerryEvent.SourceName;
            fem.TargetId = data.GamePlay.FerryEvent.TargetId;
            fem.TargetName = data.GamePlay.FerryEvent.TargetName;
            db.FerryEventModels.Add(fem);
            db.SaveChanges();
        }

        private async void TelemetryFine(object sender, EventArgs e)
        {
            await Task.Delay(10000);
            FineEventModel fem = new FineEventModel();
            fem.Amount = data.GamePlay.FinedEvent.Amount;
            fem.Offence = data.GamePlay.FinedEvent.Offence.ToString();
            db.FineEventModels.Add(fem);
            db.SaveChanges();
        }

        private async void TelemetryTollGate(object sender, EventArgs e)
        {
            await Task.Delay(10000);
            TollgateEventModel tem = new TollgateEventModel();
            tem.PayAmount = data.GamePlay.TollgateEvent.PayAmount;
            db.TollgateEventModels.Add(tem);
            db.SaveChanges();
        }

        private async void TelemetryTrainEvents(object sender, EventArgs e)
        {
            await Task.Delay(10000);
            TrainEventModel tem = new TrainEventModel();
            tem.PayAmount = data.GamePlay.TrainEvent.PayAmount;
            tem.SourceId = data.GamePlay.TrainEvent.SourceId;
            tem.SourceName = data.GamePlay.TrainEvent.SourceName;
            tem.TargetId = data.GamePlay.TrainEvent.TargetId;
            tem.TargetName = data.GamePlay.TrainEvent.TargetName;
            db.TrainEventModels.Add(tem);
            db.SaveChanges();
        }

        void ResetEventsDB()
        {
            db.FerryEventModels.Delete();
            db.FineEventModels.Delete();
            db.TollgateEventModels.Delete();
            db.TrainEventModels.Delete();
            JobStatus js = db.JobStatuses.FirstOrDefault();
            js.JobDelivered = false;
            js.JobStarted = false;
            db.SaveChanges();
        }

        void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessHelper.OpenUrl(((LinkLabel)sender).Text);
        }

        void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
        }
    }
}