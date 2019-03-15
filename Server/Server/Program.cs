using System;
using System.Collections.Generic;
using System.Globalization;
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

            var cultureInfo = CultureInfo.GetCultureInfo("en-GB");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
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
                        netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);
                        int totalrecbytes = 0;

                        while (client.Available == 0) { }


                        FileStream Fs = new FileStream(".\\UploadedFiles\\" + SaveFileName, FileMode.OpenOrCreate, FileAccess.Write);

                        while (netstream.DataAvailable)
                        {
                            while (netstream.DataAvailable)
                            {
                                RecBytes = netstream.Read(RecData, 0, RecData.Length);
                                Fs.Write(RecData, 0, RecBytes);
                                totalrecbytes += RecBytes;
                            }
                            System.Threading.Thread.Sleep(300);
                        }

                        netstream.Write(ASCIIEncoding.ASCII.GetBytes("E"), 0, ASCIIEncoding.ASCII.GetBytes("E").Length);



                        Fs.Flush();
                        Fs.Close();
                        client.Client.Disconnect(false);
                        client.Close();
                        netstream.Close();


                        Console.WriteLine("File saved");

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
