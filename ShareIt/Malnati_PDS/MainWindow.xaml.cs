using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Windows.Controls.Primitives;
using System.Net.NetworkInformation;

namespace Malnati_PDS
{
   
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public ObservableCollection<Person> PersonOne { get; set; }
        public ObservableCollection<File_Item> FileList { get; set; }

        static public System.Object lockk = new System.Object();
        static public int portTcp = 11000;
        int udpdataDimension = 55000;
        List<ToSend> send_list = new List<ToSend>();
        public string EnvPath { get; set; }



        Thread announcement, receivingAnnouncement,checkActive,sendImage;
        Person person;
 int port = 15000;



      

        public MainWindow()
        {
            InitializeComponent();
          
            person = new Person();

            EnvPath = Environment.CurrentDirectory;
            PersonOne = App.app.PersonOne;
            FileList = App.app.FileList;

            Persone.ItemsSource = PersonOne;
            FileControl.ItemsSource = FileList;
            this.DataContext = this;

            
            //Threads Setup
            ThreadReceivingAnnSetup();
         //   ThreadAnnouncementSetup(1000);
        //    ThreadSendImageSetup();


            
            
            //Threads start
            receivingAnnouncement.Start();
     //       sendImage.Start();
          
        //    if (Properties.Settings.Default.Incognito)
              //  announcement.Start();

        }

      
        private byte[] receiveImage(IPAddress ip) {

            TcpClient client = new TcpClient();
            client.Connect(ip,12000);
            NetworkStream ns = client.GetStream();
            byte[] data;
            
            try
            {
                using (var stream = ns)
                {
                    MemoryStream ms = new MemoryStream();
                    ns.CopyTo(ms);
                    data = ms.ToArray();
                    return data;
                 
                }
            }
            catch (Exception) {

                Console.Write("eccezione immagine");
            }


            return null;
        }
     
        private Person searchIp(string ipforSearch)
        {
            
                List<Person> listTemp = PersonOne.ToList();

                foreach (Person p in listTemp)
                    if (p.IP.Equals(ipforSearch))
                    {
                       
                        return p;
                    }
                return null;
            

        }

        private void ThreadReceivingAnnSetup()
        {
            receivingAnnouncement = new Thread(() =>

            {
                //Creates a UdpClient for reading incoming data.
                UdpClient receivingUdpClient = new UdpClient(port);
                receivingUdpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.2"));
                //Creates an IPEndPoint to record the IP Address and port number of the sender. 
                // The IPEndPoint will allow you to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
                receivingUdpClient.EnableBroadcast = true;
                receivingUdpClient.Client.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                //receivingUdpClient.MulticastLoopback = false;
                try
                {
                 while (true)
                    {

                        int i = 0;
                        byte[] name1 = null;
                        byte[] image = null;
                        int length = 0;


                        // Blocks until a message returns on this socket from a remote host.

                        Byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                        string ipforSearch = RemoteIpEndPoint.Address.ToString();
                        length = BitConverter.ToInt32(receiveBytes, i);
                        if (ipforSearch.Equals(IPAddress.Loopback.ToString()))
                            ipforSearch = App.app.broadcastCalculation().ToString();

                        bool found = false;
                        if (ipforSearch.Equals(IPAddress.Loopback.ToString()))
                            length = 0;
                            IPAddress myIp=App.app.broadcastCalculation();
                        foreach (IPAddress ip in App.app.myIp) {
                            if (ip.Equals(RemoteIpEndPoint.Address))
                                found = true;
                                }

                        if (!Properties.Settings.Default.DiscoverySelf  && found)
                            length = 0;
                        //if (!myIp.Equals(IPAddress.Parse(ipforSearch)) && found) length=0;
                        if (length == 0)
                        {
                            Ping pingSender = new Ping();
                            IPAddress address = RemoteIpEndPoint.Address;
                            PingReply reply = pingSender.Send(address);
                            bool b = true;

                            if (reply.Status == IPStatus.Success)
                                b = true;
                            else
                                b = false;
                            
                                Dispatcher.Invoke(new Action(() =>
                            {
                                List<Person> tmp = PersonOne.ToList();
                                foreach (Person p in tmp)
                                    if (p.IP.Equals(ipforSearch))
                                    {

                                        if (!p.tick || !b)
                                        {
                                            foreach (ToSend o in send_list.ToList())
                                                if (o.ip.Equals(ipforSearch))
                                                    insertDeleteSendList(o, false);
                                            PersonOne.Remove(p);
                                        }
                                        else {
                                            foreach (ToSend o in send_list.ToList())
                                                if (o.ip.Equals(ipforSearch))
                                                {
                                                    if (o.bkgWorker == null) {
                                                        insertDeleteSendList(o, false);
                                                        PersonOne.Remove(p);
                                                        break;
                                                    }
                                                    
                                                    o.o = PersonOne;
                                                    o.index = p;

                                                }



                                        }
                                    }

                            }));
                            continue;


                        }
                        i += 4;
                        name1 = new byte[length];


                        for (int j = 0; j < length; j++)
                        {
                            name1[j] = receiveBytes[i + j];
                        }
                        i += length;
                        length = BitConverter.ToInt32(receiveBytes, i);

                        //  MemoryStream stream = receiveImage(RemoteIpEndPoint.Address);
                        person = searchIp(ipforSearch);


                        
                        if (person != null)
                            if (person.tick)
                                continue;
                        if (person == null || person.change != length)
                        {


                            image = receiveImage(RemoteIpEndPoint.Address);

                            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(() =>
                            {
                                if (person != null)
                                {
                                    int index = PersonOne.IndexOf(person);
                                    bool tick = person.tick;
                                    PersonOne.RemoveAt(index);
                                    person = new Person();
                                    person.Name = Encoding.ASCII.GetString(name1);
                                    person.IP = RemoteIpEndPoint.Address.ToString();
                                    person.Image = ByteArraytoBitmap(image);//StreamtoBitmap(stream);// new BitmapImage(
                                    person.change = length;
                                    person.tick = tick;
                                    PersonOne.Insert(index, person);
                                    return;
                                }
                                person = new Person();
                                person.Name = Encoding.ASCII.GetString(name1);
                                person.change = length;
                                person.tick = false;
                                person.IP = RemoteIpEndPoint.Address.ToString();
                                person.Image = ByteArraytoBitmap(image);//StreamtoBitmap(stream);// new BitmapImage(Uri);
                                PersonOne.Add(person);


                            }));
                        }
                        else {
                            
                            if (Encoding.ASCII.GetString(name1).Equals(person.Name)) continue;
                            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(() =>
                            {
                                int index = PersonOne.IndexOf(person);
                                PersonOne.RemoveAt(index);
                                person.Name = Encoding.ASCII.GetString(name1);
                                PersonOne.Insert(index, person);
                            }));
                        }

                    }

                }
                catch (Exception)
                { }
                finally {
                    receivingUdpClient.Close();
                    }
            });


        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {


            ToggleButton tB = (ToggleButton)sender;
            if (tB.IsChecked.Value)
            {
                Properties.Settings.Default.Incognito = true;
                Properties.Settings.Default.Save();
                App.app.ThreadAnnouncementSetup(0);
                App.app.StartAnnThread();
            }
            else if (!tB.IsChecked.Value)
            {
                Properties.Settings.Default.Incognito = false;
                Properties.Settings.Default.Save();
                App.app.AbortAnnThread();

            }




        }
        
        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
            StackPanel sp = (StackPanel)sender;
            bool to_Insert = true;
            
            ToSend person = new ToSend();
            foreach (UIElement i in sp.Children){

                if (i.Uid.Equals("IP"))
                {
                    person.ip = ((TextBlock)i).Text;
                }
                else if (i.Uid.Equals("Name"))
                    person.name = ((TextBlock)i).Text;
                else if (i.Uid.Equals("Tempo"))
                    person.tempo = ((TextBlock)i);
                else if (i.Uid.Equals("Grid_image"))
                {

                    Grid g = (Grid)i;
                    foreach (UIElement j in g.Children)
                    {
                        if (j.Uid.Equals("Ellipse_tick"))
                        {
                            if (j.Visibility == Visibility.Collapsed)
                                j.Visibility = Visibility.Visible;

                            else {
                                j.Visibility = Visibility.Collapsed;
                                to_Insert = false;
                            }
                            person.tick = (Ellipse)j;
                        }

                    }

                }
                else if (i.Uid.Equals("ProgressBarGrid"))
                {

                    Grid g = (Grid)i;
                    person.progressBarGrid = g;
                    foreach (UIElement j in g.Children)
                    {
                        if (j.Uid.Equals("ProgressBar"))
                        {
                            person.progressBar = (ProgressBar)j;
                        }
                        else if (j.Uid.Equals("DeleteOperation"))
                        {
                            person.cancel = (Button)j;
                            person.cancel.Click += person.Cancel_Click;

                        }

                    }

                }

            }
            insertDeleteSendList(person, to_Insert);

        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            Button s = (Button)sender;
            if (FileList.Count == 0) {

                MessageBox.Show("Add File before Click Send","Warning");
                return;

            }
            if (s.Content.Equals("Send"))
            {
                
                if (send_list.Count == 0) {
                    MessageBox.Show("Select Someone before Click Send","Warning");
                    return;
                        }
                s.Content = "Cancel";
                
                foreach (ToSend ip in send_list)
                {
                    ip.send = s;
                    ip.Send(FileList.ToList());
                    ip.listSend = send_list;

                }
                if(Properties.Settings.Default.DropFile)
                    FileList.Clear();
            }
            else {

            
                s.Content = "Send";
                foreach (ToSend ip in send_list)
                {

                    ip.Cancel();

                }

            }

            

        
        }
        private void Setting_Click(object sender, RoutedEventArgs e) {
            if (App.app.setting != null)
                return;
            App.app.setting = new SettingWindow();
            App.app.setting.Show();


        }
        private void insertDeleteSendList(ToSend ip, bool to_Insert)
        {
            Person p = searchIp(ip.ip);
            if (to_Insert)
            {
                send_list.Add(ip);
                p.tick = true;
                
            }
            else
                foreach (ToSend i in send_list)
                    if (i.ip.Equals(ip.ip))
                    {
                        if (i.index != null)
                            PersonOne.Remove(i.index);
                        p.tick = false;

                        send_list.Remove(i);
                        break;
                        
                    }
            
        }

      
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           
                
                App.app.s = null;
                send_list.Clear();
                PersonOne.Clear();
                receivingAnnouncement.Abort();
            


        }

        private void Window_Drop(object sender, DragEventArgs e)
        {

        }

        private void Window_PreviewDragEnter(object sender, DragEventArgs e)
        {
          
          
        }

        private void Chip_DeleteClick(object sender, RoutedEventArgs e)
        {
            MaterialDesignThemes.Wpf.Chip b =(MaterialDesignThemes.Wpf.Chip)sender;
            
            List<File_Item> list = FileList.ToList();
            foreach (File_Item i in list) {
                if (b.Content == null) continue;
                if (b.Content.Equals(i.fileName) && b.ToolTip.Equals(i.path)) {
                    FileList.Remove(i);
                        return;
                        }
            }

        }

        public static BitmapImage ByteArraytoBitmap(Byte[] byteArray)
        {
            MemoryStream stream = new MemoryStream(byteArray);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();

            // Set properties.
            bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
            bitmapImage.CreateOptions = BitmapCreateOptions.DelayCreation;
            
            bitmapImage.StreamSource=stream;
            bitmapImage.EndInit();
            return bitmapImage;
        }
        public static BitmapImage StreamtoBitmap(MemoryStream ms)
        {
            MemoryStream stream = ms;
            
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();

            // Set properties.
            bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
            bitmapImage.CreateOptions = BitmapCreateOptions.DelayCreation;

            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            return bitmapImage;
        }
      
    }
}
