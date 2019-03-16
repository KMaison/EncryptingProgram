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
            TcpClient client = null;

            var cultureInfo = CultureInfo.GetCultureInfo("en-GB");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            try
            {
                Listener = new TcpListener(IPAddress.Any, 5000);
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                return;
            }

            for (; ; )
            {
                try
                {
                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        Task.Factory.StartNew(() => receiveFile(client));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static int receiveFile(TcpClient client)
        {
            NetworkStream netstream = null;
            byte[] RecData = new byte[1024];
            int RecBytes;

            Console.WriteLine("Incoming File");

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

            netstream.ReadTimeout = 5000;

            while (netstream.DataAvailable)
            {
                while (netstream.DataAvailable)
                {
                    while (netstream.DataAvailable)
                    {
                        RecBytes = netstream.Read(RecData, 0, RecData.Length);
                        Fs.Write(RecData, 0, RecBytes);
                        totalrecbytes += RecBytes;
                    }
                    Thread.Sleep(300);
                }
                Thread.Sleep(700);
            }

            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

            Fs.Flush();
            Fs.Close();
            client.Close();
            netstream.Close();

            Console.WriteLine("File saved");

            return 0;
        }

    }
}
