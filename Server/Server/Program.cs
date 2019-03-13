using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {

        static void Main(string[] args)
        {
            TcpListener Listener = null;
            try
            {
                Listener = new TcpListener(IPAddress.Loopback, 5000);
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            byte[] RecData = new byte[1024];
            int RecBytes;

            for (; ; )
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                try
                {
                    if (Listener.Pending())
                    {
                        Console.WriteLine("Incoming File");

                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();

                        byte[] buffer = new byte[client.ReceiveBufferSize];

                        //---read incoming stream---
                        int bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);

                        //---convert the data received into a string---
                        string SaveFileName = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("File name: " + SaveFileName);
                        netstream.Write(ASCIIEncoding.ASCII.GetBytes("OK"), 0, ASCIIEncoding.ASCII.GetBytes("OK").Length);
                        int totalrecbytes = 0;
                        while (true)
                        {
                            if (Listener.Pending())
                                break;
                        }
                        FileStream Fs = new FileStream("D:\\Projekty\\Visual Studio C#\\BSK Proj\\ServerFiles\\" + SaveFileName, FileMode.OpenOrCreate, FileAccess.Write);
                        while ((RecBytes = netstream.Read(RecData, 0, RecData.Length)) > 0)
                        {
                            Fs.Write(RecData, 0, RecBytes);
                            totalrecbytes += RecBytes;
                        }
                        Fs.Close();

                        netstream.Close();
                        client.Close();

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    netstream.Close();
                }
            }
        }


    }
}
