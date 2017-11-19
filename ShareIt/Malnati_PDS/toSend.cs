using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Malnati_PDS
{
    class ToSend
    {

        public List<ToSend> listSend { get; set; }
        public Button send { get; set; }
        List<File_Item> list;
      public  string ip { get; set; }
        public bool free { get; set; }
        public ObservableCollection<Person> o { get; set; }
        public Person index { get; set; }
        public string name { get; set; }
     public   Ellipse tick { get; set; }
        public ProgressBar progressBar { get; set; }
        public TextBlock tempo{ get; set; }
       internal BackgroundWorker bkgWorker { get; set; }
       public Button cancel { get; set; }
        public Grid progressBarGrid { get; set; }

        public void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (bkgWorker == null) {
                return;
            }
            bkgWorker.CancelAsync();
        }

        string filePath;
        float percentage { get; set; }
        string rimanente { get; set; }
        System.Object lockThis = new System.Object();
        private int fileIndex;

        public ToSend() {
            free = true;
           
            index = null;
        }

        private void BkgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            Thread.Sleep(1000);
            progressBarGrid.Visibility = Visibility.Collapsed;
            progressBar.Value = 0;
            tempo.Visibility = Visibility.Collapsed;
            if (index !=null) {
                o.Remove(index);
            }
            free = true;
            fileIndex++;

            if (fileIndex < list.Count)
                Send(fileIndex);
            else {
                bkgWorker = null;
                bool last = true;

              foreach(ToSend p in listSend)
                    if (p.bkgWorker != null) {
                        last = false;
                        break;
                    }
              if(last)
                    send.Content="Send";
            }


        }

        private void BkgWorker_DoWork1(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 100; i ++) { bkgWorker.ReportProgress(i); } 
        }

        private void BkgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpClient client = new TcpClient();

            byte[] name = Encoding.UTF8.GetBytes(System.IO.Path.GetFileName(filePath));
            byte[] size = BitConverter.GetBytes(name.Length);
            byte[] buffer = new byte[1024];
            Array.Copy(size, 0, buffer, 0, size.Length);
            Array.Copy(name, 0, buffer, 4, name.Length);

            IAsyncResult ar = client.BeginConnect(ip, MainWindow.portTcp,null,null);
            System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    client.Close();
                    foreach (ToSend p in listSend)
                    {
                        if (p.ip.Equals(ip))
                        {
                            listSend.Remove(p);
                            break;
                        }
                    }
                    throw new TimeoutException();
                }

                client.EndConnect(ar);
            }
            
            finally
            {
                wh.Close();
            }
            client.Client.Send(buffer, 0, name.Length + size.Length, SocketFlags.None);

            client.Client.Receive(buffer, 1024, SocketFlags.None);
            string response = Encoding.ASCII.GetString(buffer);
            if (!response.Substring(0, 2).Equals("OK")) return;
            FileStream fs = File.OpenRead(filePath);
            byte[] file = new byte[4096];
            size = BitConverter.GetBytes(fs.Length);
            client.Client.Send(size, 0, size.Length, SocketFlags.None);

            client.Client.Receive(buffer, 1024, SocketFlags.None);
            response = Encoding.ASCII.GetString(buffer);
            if (!response.Substring(0, 2).Equals("OK")) {
                client.Close();
                fs.Close();

                MessageBox.Show(this.name+" doesn't accept the file");
                return; }
            float count = 0;
            int nRead = 0;
            percentage = 0;
            rimanente = "30 seconds";
            bkgWorker.ReportProgress((int)percentage);
            
            double time1;
            int tenPeriod = 0;
            DateTime started = DateTime.Now;
            while (count < fs.Length)
            {
                
                nRead = fs.Read(file, 0, 4096);
                
                count += client.Client.Send(file, 0, nRead, SocketFlags.None);
                percentage = (float)((count / (float)fs.Length) * 100);

               
                if (tenPeriod++ > 10)
                {
                    TimeSpan elapsedTime = DateTime.Now - started;
                    TimeSpan estimatedTime =
                        TimeSpan.FromSeconds(
                            (fs.Length - count) /
                            ((double)count / elapsedTime.TotalSeconds));
                    int hours = (int)estimatedTime.Hours;
                    int minute = (int)estimatedTime.Minutes;
                    int seconds = (int)estimatedTime.Seconds;
                    if (hours > 0)
                        rimanente = hours.ToString() + " hour " + minute.ToString() + " min";
                    else if( minute>0)
                        rimanente = minute.ToString() + " minutes " + seconds.ToString() + " sec";
                    else 
                        rimanente = seconds.ToString() + " seconds";
                }
                if (bkgWorker.CancellationPending) {
                    fs.Close();
                    client.Client.Send(file,0,1,SocketFlags.None);
                    client.Close();
                    return ;
                }
                
                    bkgWorker.ReportProgress((int)percentage);
                



                // Dispatcher.BeginInvoke(new Action(() => { pb.Value += (count / file.Length) * 100; }));
            }
            fs.Close();
            client.Client.Receive(buffer, 1024, SocketFlags.None);
            response = Encoding.ASCII.GetString(buffer);
            if (!response.Substring(0, 2).Equals("OK")) return;
            client.Close();
            //   Dispatcher.BeginInvoke(new Action(() => { Thread.Sleep(3000);pib.progressBarGrid.Visibility = Visibility.Collapsed; }));

        }

        private void BkgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tempo.Text = rimanente;
                progressBar.Value = e.ProgressPercentage;
                  
        }

        public void Send(int fileIndex) {
            bkgWorker = new BackgroundWorker();
            bkgWorker.ProgressChanged += BkgWorker_ProgressChanged;
            bkgWorker.DoWork += BkgWorker_DoWork;
            bkgWorker.RunWorkerCompleted += BkgWorker_RunWorkerCompleted;
            //bkgWorker.DoWork += BkgWorker_DoWork1;
            bkgWorker.WorkerReportsProgress = true;
            bkgWorker.WorkerSupportsCancellation = true;
            free = false;
            this.filePath = list[fileIndex].path+"\\"+ list[fileIndex].fileName;
            tempo.Visibility = Visibility.Visible;
            progressBarGrid.Visibility = Visibility.Visible;
            bkgWorker.RunWorkerAsync();


        }

        internal void Send(List<File_Item> list)
        {
            fileIndex = 0;
            this.list = list;
            if(list.Count>0)
            Send(0);
        }
        public void Cancel()
        {
            if (bkgWorker == null) return;
            fileIndex = list.Count;
            bkgWorker.CancelAsync();



        }

    }
}
