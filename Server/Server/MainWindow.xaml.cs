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
using System.Security.Cryptography;
using System.Threading;
using Client;

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
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "png Files (*.png)|*.png|mp3 Files (*.mp3)|*.mp3|avi Files (*.avi)|*.avi|txt Files (*.txt)|*.txt";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                selectedFileTextBox.Text = filename;
            }

            //var abc = "witam serdecznie";
            //System.Windows.MessageBox.Show(abc, "Aes");
            //System.Windows.MessageBox.Show(Encrypt(abc, "haslo"), "Aes");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int BufferSize = 1024, tries = 0;
            byte[] SendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            try
            {
                while (tries++ < 100) {
                    try
                    {
                        client = new TcpClient("127.0.0.1", 5000);
                        netstream = client.GetStream();
                        break;
                    }
                    catch (Exception e1) { }
                }

                // Wysyłanie nazwy
                string extension = System.IO.Path.GetExtension(selectedFileTextBox.Text);
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(filenameTextBox.Text+extension);
                netstream.Write(bytesToSend, 0, bytesToSend.Length);
                // Potwierdzenie
                while (netstream.ReadByte() != 'O') { };
                
                FileStream Fs = new FileStream(selectedFileTextBox.Text, FileMode.Open, FileAccess.Read);
                int CurrentPacketLength;

                netstream.WriteTimeout = 5000;

                // Inicjalizacja enkryptora
                byte[] genKey, genIV;
                var encryptor = new Encryption();
                encryptor.Initialize(out genKey, out genIV);

                netstream.Write(genKey, 0, genKey.Length);
                // Potwierdzenie odbioru Key
                while (netstream.ReadByte() != 'O') { };

                netstream.Write(genIV, 0, genIV.Length);
                // Potwierdzenie odbioru IV
                while (netstream.ReadByte() != 'O') { };

                // Enkrypcja
                var dataToEncrypt = new byte[Fs.Length];
                Fs.Read(dataToEncrypt, 0, dataToEncrypt.Length);
                byte[] encyptedData = encryptor.Encrypt(dataToEncrypt);

                int TotalLength = encyptedData.Length;
                int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(encyptedData.Length) / Convert.ToDouble(BufferSize)));
                //progressBar.Maximum = NoOfPackets;

                for (int i = 0; i < NoOfPackets; i++)
                {
                    if (TotalLength > BufferSize)
                        CurrentPacketLength = BufferSize;
                    else
                        CurrentPacketLength = TotalLength;

                    SendingBuffer = new byte[CurrentPacketLength];
                    Array.Copy(encyptedData, encyptedData.Length - TotalLength, SendingBuffer, 0, CurrentPacketLength);

                    TotalLength = TotalLength - CurrentPacketLength;

                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);

                    //if (progressBar.Value >= progressBar.Maximum)
                    //    progressBar.Value = progressBar.Minimum;
                    //progressBar.Value++;
                    //progressBar.UpdateLayout();
                }
                netstream.Flush();
                while (netstream.ReadByte() != 'O') { }

                Fs.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error");
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
