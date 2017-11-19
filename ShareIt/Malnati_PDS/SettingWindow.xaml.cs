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
using System.Windows.Forms;


namespace Malnati_PDS
{
    /// <summary>
    /// Logica di interazione per SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
        }

        private void Image_Profile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;

            System.Windows.Forms.OpenFileDialog op = new System.Windows.Forms.OpenFileDialog();
            op.Filter = "Image Files(*.BMP; *.JPG; *.GIF; *.PNG;)| *.BMP; *.JPG; *.GIF; *.PNG;" ;
            if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Properties.Settings.Default.Profile_Image = op.FileName;
                Properties.Settings.Default.Save();
                App.app.change++;

            }

        }

        private void Path_Button_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Properties.Settings.Default.Path = fbd.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void Name_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
            System.Windows.Controls.TextBox tb =(System.Windows.Controls.TextBox) this.FindName("NameTextBox");
            
            tb.IsEnabled = !tb.IsEnabled;
            if (tb.IsEnabled)
                button.Content = "Save";
            else
                button.Content= "Edit";
        }

        private void Incognito_Button_Click(object sender, RoutedEventArgs e)
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

    

        private void Request_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tB = (ToggleButton)sender;
            if (tB.IsChecked.Value)
            {
                Properties.Settings.Default.Request = true;
                Properties.Settings.Default.Save();
              
            }
            else if (!tB.IsChecked.Value)
            {
                Properties.Settings.Default.Request = false;
                Properties.Settings.Default.Save();
                

            }

        }

        private void SelfDiscovery_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tB = (ToggleButton)sender;
            if (tB.IsChecked.Value)
            {
                Properties.Settings.Default.DiscoverySelf = true;
                Properties.Settings.Default.Save();

            }
            else if (!tB.IsChecked.Value)
            {
                Properties.Settings.Default.DiscoverySelf= false;
                Properties.Settings.Default.Save();


            }


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.app.setting = null;
        }

        private void DropFIle_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tB = (ToggleButton)sender;
            if (tB.IsChecked.Value)
            {
                Properties.Settings.Default.DropFile = true;
                Properties.Settings.Default.Save();

            }
            else if (!tB.IsChecked.Value)
            {
                Properties.Settings.Default.DropFile= false;
                Properties.Settings.Default.Save();


            }

        }
    }
}
