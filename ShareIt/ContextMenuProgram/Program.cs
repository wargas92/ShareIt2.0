using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContextMenuProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            NamedPipeClientStream namedPipeClient = new NamedPipeClientStream("test-pipe");
            Console.WriteLine("Wait for connection to main program..." );
            namedPipeClient.Connect();
            string s="";       
            foreach (string s1 in args) {
                s= s+" "+s1;
                    }
                FileAttributes attr = File.GetAttributes(s);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                 string dir = Path.GetFileName(s);
                

                string path = Path.GetDirectoryName(s);


                for (int indexFile = 1; File.Exists(path + "\\" + dir + ".zip"); dir = dir + "(" + indexFile + ")", indexFile++) ;

                
               
                string filename =path+"\\" + dir + ".zip";
                Console.WriteLine("Wait for compression..." +filename);
                ZipFile.CreateFromDirectory(s, filename);
                    s =filename;

                }



                byte[] x = Encoding.UTF8.GetBytes(s);
                namedPipeClient.Write(x, 0, x.Length);
                namedPipeClient.Dispose();
    
           
        }
    }
}
