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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => MainFunction());
        }

        public void MainFunction()
        {
            TcpListener Listener = null;
            TcpClient client = null;

            var cultureInfo = CultureInfo.GetCultureInfo("en-GB");  //Wyświetlanie angielskich błedów - nie działa
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            try
            {
                Listener = new TcpListener(IPAddress.Any, 5000);
                Listener.Start();
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() => UserConsole.Text += ex.Message + "\n");
                return;
            }

            for (; ; )
            {
                try
                {
                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        //Task.Factory.StartNew(() => ReceiveFile(client)); 
                        //Nie potrzebne taski raczej
                        ReceiveFile(client);
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() => UserConsole.Text += ex.Message + "\n"); ;
                }
            }
        }
        public int ReceiveFile(TcpClient client)
        {
            NetworkStream netstream = null;
            byte[] RecData = new byte[1024];
            byte[] Key, IV;
            int RecBytes;


            this.Dispatcher.Invoke(() => UserConsole.Text += "Incoming File\n");

            netstream = client.GetStream();

            byte[] buffer = new byte[client.ReceiveBufferSize];

            //---read incoming stream---
            int bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);

            //---convert the data received into a string---
            string SaveFileName = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "File name: " + SaveFileName + "\n");

            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);



            // read AES Key
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            Key = new byte[bytesRead];
            Array.Copy(buffer, Key, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "Key: " + buffer.ToString() + "\n");
            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

            // read AES IV
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            IV = new byte[bytesRead];
            Array.Copy(buffer, IV, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "IV: " + buffer.ToString() + "\n");
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

            this.Dispatcher.Invoke(() => UserConsole.Text += "File saved\n");

            return 0;
        }
    }
}
