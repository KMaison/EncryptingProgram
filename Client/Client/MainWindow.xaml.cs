using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Security;
using System.Net.Sockets;
using System.Net;

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
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";
            dlg.Filter = "mp3 Files (*.mp3)|*.mp3|png Files (*.png)|*.png|avi Files (*.avi)|*.avi|txt Files (*.txt)|*.txt";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                selectedFileTextBox.Text = filename;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            int BufferSize = 1024;
            byte[] SendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            try
            {
                client = new TcpClient(IPAddress.Loopback.ToString(), 5000);
                netstream = client.GetStream();

                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(filenameTextBox.Text);
                netstream.Write(bytesToSend, 0, bytesToSend.Length);

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

                while (true)
                {
                    if (Listener.Pending())
                        break;
                }

                FileStream Fs = new FileStream(selectedFileTextBox.Text, FileMode.Open, FileAccess.Read);
                int NoOfPackets = Convert.ToInt32
             (Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(BufferSize)));
                progressBar.Maximum = NoOfPackets;
                int TotalLength = (int)Fs.Length;
                int CurrentPacketLength, counter = 0;
                for (int i = 0; i < NoOfPackets; i++)
                {
                    if (TotalLength > BufferSize)
                    {
                        CurrentPacketLength = BufferSize;
                        TotalLength = TotalLength - CurrentPacketLength;
                    }
                    else
                        CurrentPacketLength = TotalLength;
                    SendingBuffer = new byte[CurrentPacketLength];
                    Fs.Read(SendingBuffer, 0, CurrentPacketLength);
                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                    if (progressBar.Value >= progressBar.Maximum)
                        progressBar.Value = progressBar.Minimum;
                    progressBar.Value++;
                }
                Fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                netstream.Close();
                client.Close();
            }
        }


        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
