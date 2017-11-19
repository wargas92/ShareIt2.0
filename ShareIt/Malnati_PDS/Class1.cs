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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Malnati_PDS
{
 public class Person
    {
        public override bool Equals(object obj)
        {
            if (!(obj is Person)) return false;
            return ((Person)obj).IP.Equals(IP);
        }
        public string Name { get; set; }
        public int change{ get; set; }
        public string IP { get; set; }
        public BitmapSource Image { get; set; }
        public bool tick{ get; set; }
        

        
    }
}
