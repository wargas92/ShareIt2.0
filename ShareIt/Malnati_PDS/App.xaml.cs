using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using System.Net.NetworkInformation;
using System.Windows.Media.Imaging;
using System.IO.Pipes;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;

namespace Malnati_PDS
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
       
       public int change { get; set; }
        public IPAddress broadcast { get; set; }
        public List<IPAddress> myIp{ get; set; }
        Thread tcpConnection,sendImage,namedPipe,pingConnection;
        Thread announcement { get; set; }
        public ObservableCollection<Person> PersonOne { get; set; }
        public ObservableCollection<File_Item> FileList { get; set; }
        int portTcp = 11000;
        internal static Malnati_PDS.App app;
        public string[] argument { get; set; }
       
        int port = 15000;
        System.Windows.Forms.NotifyIcon notifyIcon;
        private Thread netUp;

        public Window s { get; set; }
        public Window setting { get; set; }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
           app = this;
            myIp = new List<IPAddress>();
                PersonOne = new ObservableCollection<Person>();//new Person();
            FileList = new ObservableCollection<File_Item>();
            AddOption_ContextMenu();


            if (Malnati_PDS.Properties.Settings.Default.Profile_Image.Equals("Sconosciuto.png")) {
                Malnati_PDS.Properties.Settings.Default.Profile_Image = Path.GetDirectoryName(Application.ResourceAssembly.Location) + "\\Sconosciuto.png";

            }

            broadcastCalculation();
                ThredTcpConnectionSetup();
            ThreadNetUpSetup();
            ThreadAnnouncementSetup(1000);
            ThredNamedPipeSetup();
            ThreadPingSetup();
            change = 0;
                tcpConnection.Start();
            ThreadSendImageSetup();
            sendImage.Start();
            if (Malnati_PDS.Properties.Settings.Default.Incognito)
                announcement.Start();

       
            string path = Environment.CurrentDirectory;
                argument = e.Args;
            NotifyIconSetup();
  
        }

        private void ThreadPingSetup()
        {
            pingConnection = new Thread(() =>
            {
                Ping pingSender = new Ping();

                
                while (true)
                {
                    List<Person> listP = PersonOne.ToList();
                    Thread.Sleep(1500);
                    foreach (Person p in listP)
                    {
                        PingReply reply = pingSender.Send(IPAddress.Parse(p.IP));
                        bool b = true;
                        if (reply.Status != IPStatus.Success)
                            Dispatcher.Invoke(new Action(() =>
                            {
                                PersonOne.Remove(p);
                            }));
                    }
                }

            });
            pingConnection.Start();
            }

        public void ThreadNetUpSetup()
        {
            netUp = new Thread(() =>
            {
                bool shown = false;
                Thread.Sleep(7000);
                while (true)
                {
                    Thread.Sleep(2000);
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() == false && !shown)
                    {
                        shown = true;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            notifyIcon.ShowBalloonTip(5000, "Network Down", "Your Internet Connection is down", System.Windows.Forms.ToolTipIcon.Warning);
                        }));
                    }
                    else if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() == true && shown)
                        shown = false;
                }

            });
            netUp.Start();
        }

        public void ThredNamedPipeSetup() {
            namedPipe = new Thread(() =>
             {
                 PipeSecurity ps = new PipeSecurity();
                 byte[] tmp = new byte[1024];
                 int nRead;
                 //Is this okay to do?  Everyone Read/Write?
                 PipeAccessRule psRule = new PipeAccessRule(@"Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
                 ps.AddAccessRule(psRule);
                   while (true)
                 {
                     NamedPipeServerStream namedPipeServer = new NamedPipeServerStream("test-pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1, 1, ps);

                     namedPipeServer.WaitForConnection();
                     string filename;
                     using (var reader = new StreamReader(namedPipeServer))
                     {
                        filename = reader.ReadLine();
                     }
                     if (filename.Length == 0) continue;
                  //   namedPipeServer.Disconnect();
                     File_Item f = new File_Item();
                     //string filename = Encoding.ASCII.GetString(tmp);
                     f.fileName = Path.GetFileName(filename);
                     f.path = Path.GetDirectoryName(filename);
                     
                        app.Dispatcher.BeginInvoke(new Action(() =>
                         {
                             FileList.Add(f);
                             if (s == null)
                             {
                                 s = new MainWindow();
                                 s.Show();
                             }

                                 s.Activate();
                                 s.WindowState = WindowState.Normal;
                             
                         
                         }));
                     
                     

                 }
             });
            namedPipe.Start();

        }
        private void NotifyIconSetup()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = new System.Drawing.Icon("share.ico");

            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(2000, "ShareIt is running", "Share your files just clicking to the file or directory with RIGHT BUTTON -> Share with ShareIt", System.Windows.Forms.ToolTipIcon.Info);
            System.Windows.Forms.ContextMenu cm = new System.Windows.Forms.ContextMenu();

            System.Windows.Forms.MenuItem setting = new System.Windows.Forms.MenuItem();
            setting.Text = "Settings";
            notifyIcon.DoubleClick += Setting_Click;
            setting.Click += Setting_Click;
            cm.MenuItems.Add(0, setting);
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem();
            exit.Text = "Exit";
            exit.Click += Close_Click;
            cm.MenuItems.Add(1, exit);
            notifyIcon.ContextMenu = cm;
        }

        private void Close_Click(object sender, EventArgs e)
        {
            if (s != null)
                s.Close();
            if (setting != null)
                setting.Close();
            RemoveOption_ContextMenu();
            notifyIcon.Visible = false;
            app.Shutdown();
            announcement.Abort();
            Environment.Exit(0);
            
        }
        private void Setting_Click(object sender, EventArgs e)
        {
            if (setting != null) {
                setting.Activate();
                return;

            }
            setting = new SettingWindow();
            setting.Show();
        }

        public void ThredTcpConnectionSetup()
        {
            tcpConnection = new Thread(() =>
            {

                TcpListener server = new TcpListener(IPAddress.Parse("0.0.0.0"), portTcp);
                server.Start();
                while (true)
                {

                    Socket acceptSocket = server.AcceptSocket();
                    Thread t = new Thread(() => {

                        byte[] buffer = new byte[1024];
                        int counter = 0;
                        string fileName, fileNameWithoutExtension, extension;
                        int nRead;

                        byte[] response = Encoding.UTF8.GetBytes("OK");

                        if ((nRead = acceptSocket.Receive(buffer, 1024, SocketFlags.None)) <= 0) return;
                        acceptSocket.Send(response, response.Length, SocketFlags.None);
                        int length = BitConverter.ToInt32(buffer, 0);
                        byte[] name = new byte[length];
                        for (int j = 0; j < length; j++)
                        {
                            name[j] = buffer[j + 4];
                        }
                        counter += length;
                        fileName = Encoding.ASCII.GetString(name);
                        fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
                        extension = System.IO.Path.GetExtension(fileName);
                        MessageBoxResult result;
                        FileStream fs = null;
                        string path = Malnati_PDS.Properties.Settings.Default.Path+"\\";
                        for (int indexFile = 1; File.Exists(path+fileName); fileName = fileNameWithoutExtension + "(" + indexFile + ")" + extension, indexFile++) ;
                        //result = MessageBox.Show("Do you want to close this window?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        

                        if ((nRead = acceptSocket.Receive(buffer, 1024, SocketFlags.None)) <= 0) return;
                        length = BitConverter.ToInt32(buffer, 0);
                        result = MessageBoxResult.Yes;
                        if (Malnati_PDS.Properties.Settings.Default.Request)
                        {
                            string dim = "KB";
                            int dimension = length / 1024;
                            if (dimension >= 1024) {
                                dimension /= 1024;
                                dim = "MB";
                                }
                            result = MessageBox.Show("Do you want to receive " + fileName + " Size: " + dimension+" "+dim, "Request of sharing file ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        }
                            if (result == MessageBoxResult.No) {
                            response= Encoding.UTF8.GetBytes("NO");
                            acceptSocket.Send(response, response.Length, SocketFlags.None);
                            acceptSocket.Close();
                            return;

                        }
                            




                        fileName = path + fileName;
                        fs = File.Create(fileName);
                        //  fs.Write(name,0,name.Length
                        if (fs == null)
                        {
                            acceptSocket.Close();

                            return;
                        }

                        acceptSocket.Send(response, response.Length, SocketFlags.None);

                        byte[] file = new byte[4096];



                        try
                        {
                            while (length > 0)
                            {
                                nRead = acceptSocket.Receive(file, 4096, SocketFlags.None);
                                if (nRead == 1 && length != 1) throw new SocketException();
                                if (nRead == 0) throw new SocketException();

                                fs.Write(file, 0, nRead);
                                length -= nRead;
                            }
                        }
                        catch (SocketException se)
                        {

                            notifyIcon.ShowBalloonTip(3000, "Download Failed", "The download of File " + fileNameWithoutExtension + " is failed", System.Windows.Forms.ToolTipIcon.Error);

                        }
                        finally
                        {
                            fs.Close();
                            if (length <= 0)
                            {
                                notifyIcon.ShowBalloonTip(3000, "Download Complete", Path.GetFileName(fileName)+ " was saved", System.Windows.Forms.ToolTipIcon.Info);
                                acceptSocket.Send(response, response.Length, SocketFlags.None);
                            }
                            else
                                File.Delete(fileName);


                            acceptSocket.Close();

                        }

                    });
                    t.Start();
                }


            });
        }

        internal void AbortAnnThread()
        {
            announcement.Abort();
         
        }

        internal void StartAnnThread()
        {
            ThreadAnnouncementSetup(0);
            announcement.Start();

        }

        public IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }


     
        public IPAddress broadcastCalculation()
        {
            IPAddress ipUp = null;
            myIp.Clear();
            myIp.Add(IPAddress.Loopback);
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface Interface in Interfaces)
            {          
                if (Interface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                Console.WriteLine(Interface.Description);
                UnicastIPAddressInformationCollection UnicastIPInfoCol = Interface.GetIPProperties().UnicastAddresses;
                foreach (UnicastIPAddressInformation UnicatIPInfo in UnicastIPInfoCol)
                {
                    
                    if (UnicatIPInfo.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    myIp.Add(UnicatIPInfo.Address);
                    if (Interface.OperationalStatus != OperationalStatus.Up) break;
                    Console.WriteLine("\tIP Address is {0}", UnicatIPInfo.Address);
                    Console.WriteLine("\tSubnet Mask is {0}", UnicatIPInfo.IPv4Mask);
                    ipUp = UnicatIPInfo.Address;
                    broadcast = GetBroadcastAddress(UnicatIPInfo.Address, UnicatIPInfo.IPv4Mask);
                }
            }
            return (ipUp== null)?IPAddress.Loopback:ipUp;

        }

        public void ThreadAnnouncementSetup(int delay)
        {
            announcement = new Thread(() => {
                Thread.Sleep(delay);
                UdpClient udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;
                udpClient.Client.EnableBroadcast = true;
                udpClient.Client.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), port);
                 IPEndPoint ip1 = new IPEndPoint(IPAddress.Parse("224.0.0.2"), port);
                try
                {
                    while (true)
                    {

                        int counter = 0;
                        //var data = Encoding.UTF8.GetBytes("ABCD");
                        byte[] sendByte = new byte[1024];
                        //Name codification
                        var name = Encoding.UTF8.GetBytes(Malnati_PDS.Properties.Settings.Default.Name);
                        byte[] intBytes = BitConverter.GetBytes(name.Length);
                        Array.Copy(intBytes, 0, sendByte, counter, intBytes.Length);
                        counter += intBytes.Length;
                        Array.Copy(name, 0, sendByte, counter, name.Length);
                        counter += name.Length;
                        intBytes = BitConverter.GetBytes(change);
                        Array.Copy(intBytes, 0, sendByte, counter, intBytes.Length);
                        counter += intBytes.Length;
                        udpClient.Send(sendByte, counter, ip);
                        udpClient.Send(sendByte, counter, ip1);
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception)
                {
                    byte[] intBytes = BitConverter.GetBytes(0);
                    try
                    {
                        udpClient.Send(intBytes, 4, ip);
                        udpClient.Send(intBytes, 4, ip1);
                    }
                    catch (Exception) { };
                    udpClient.Close();
                }
            });

        }
        private void ThreadSendImageSetup()
        {
            sendImage = new Thread(() => {


                TcpListener server = new TcpListener(IPAddress.Parse("0.0.0.0"), 12000);
                server.Start();
                byte[] data;
                byte[] tmp = new byte[1024];

                byte[] intBytes;
                while (true)
                {

                    var acceptSocket = server.AcceptTcpClient();

                    var Uri = new Uri(Malnati_PDS.Properties.Settings.Default.Profile_Image, UriKind.Absolute);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(getBitmap(Uri)));


                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);

                        data = ms.ToArray();
                    }
                    intBytes = BitConverter.GetBytes(data.Length);
                    NetworkStream ns = acceptSocket.GetStream();
                    try
                    {
                        MemoryStream ms = new MemoryStream(data);
                        ms.CopyTo(ns);

                    }
                    catch (Exception)
                    { }
                    acceptSocket.Close();

                }

            });

        }
        private BitmapImage getBitmap(Uri uri)
        {

            BitmapImage bi = new BitmapImage();
            BitmapImage source = new BitmapImage(uri);
            // Begin initialization.
            bi.BeginInit();

            // Set properties.
            bi.CacheOption = BitmapCacheOption.OnDemand;
            bi.CreateOptions = BitmapCreateOptions.DelayCreation;
            //double divider = (source.Height / 350)+1;
            double divider = (source.Width / 350) + 1;
            bi.DecodePixelHeight = (int)(source.Height / divider);
            //bi.DecodePixelWidth = 10;
            bi.DecodePixelWidth = (int)(source.Width / divider);
            //bi.DecodePixelHeight = 10;
            bi.UriSource = uri;
            bi.EndInit();
            return bi;
        }
        private void AddOption_ContextMenu()
        {
            RegistryKey _key1 = Registry.ClassesRoot.OpenSubKey("Folder\\shell", true);
            RegistryKey _key = Registry.ClassesRoot.OpenSubKey("*\\shell", true);
            
            RegistryKey newkey = _key.CreateSubKey("Share with ShareIt");
            RegistryKey newkey1 = _key1.CreateSubKey("Share with ShareIt");
            RegistryKey command= newkey.CreateSubKey("Command");
            RegistryKey command1 = newkey1.CreateSubKey("Command");
            string program= Path.GetDirectoryName(Application.ResourceAssembly.Location);
            for (int i = 0; i < 3; i++)
                program = Path.GetDirectoryName(program);
            program = program+ "\\ContextMenuProgram\\bin\\Debug\\ContextMenuProgram.exe %1";
          command.SetValue("", program);
            command1.SetValue("", program);
            newkey.SetValue("DefaultIcon", Path.GetDirectoryName(Application.ResourceAssembly.Location) + "\\share.ico");

             command.Close();
            command1.Close();

            newkey1.Close();
            newkey.Close();
            _key1.Close();
            _key.Close();

        }
        private void RemoveOption_ContextMenu()
        {
            RegistryKey _key= Registry.ClassesRoot.OpenSubKey("*\\shell", true);
            RegistryKey _key1 = Registry.ClassesRoot.OpenSubKey("Folder\\shell", true);
            _key.DeleteSubKeyTree("Share with ShareIt");
            _key1.DeleteSubKeyTree("Share with ShareIt");
            _key1.Close();
            _key.Close();
        }
        private void SecondInstance()
        {
            MessageBox.Show("Second Instance Detected! Well Done");
        }
    }
}
