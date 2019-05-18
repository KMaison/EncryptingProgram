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
using System.Threading.Tasks;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public CipherMode aesType = CipherMode.ECB;
        public int clientID;
        private TcpClient client;
        Dictionary<int, string> Clients =
            new Dictionary<int, string>();


        public MainWindow()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => MainFunction());
        }

        private void MainFunction()
        {
            TcpListener Listener = null;
            NetworkStream netstream = null;


            try
            {
                Listener = new TcpListener(IPAddress.Any, 5001);
                Listener.Start();
            }
            catch (Exception ex)
            {
                return;
            }

            for (; ; )
            {
                try
                {
                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();
                        byte[] buffer = new byte[client.ReceiveBufferSize];
                        // read file name
                        int bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
                        //---convert the data received into a string---
                        string pubKey = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

                        bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
                        int userID = BitConverter.ToInt32(buffer, 0);
                        netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

                        Clients.Add(userID, pubKey);
                    }
                }
                catch (Exception ex)
                {
                }
            }
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
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int BufferSize = 1024, tries = 0;
            byte[] SendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            try
            {
                while (tries++ < 100)
                {
                    try
                    {
                        client = new TcpClient("127.0.0.1", 5000);
                        netstream = client.GetStream();
                        break;
                    }
                    catch (Exception e1) { }
                }

                // Wysyłanie nazwy pliku
                string extension = System.IO.Path.GetExtension(selectedFileTextBox.Text);
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(filenameTextBox.Text + extension);
                netstream.Write(bytesToSend, 0, bytesToSend.Length);
                // Potwierdzenie
                while (netstream.ReadByte() != 'O') { };

                // Odbieranie UserID //~pozbyc sie tego
                byte[] buffer = new byte[client.ReceiveBufferSize];
                var bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
                int userID = BitConverter.ToInt32(buffer, 0);
                netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);



                FileStream Fs = new FileStream(selectedFileTextBox.Text, FileMode.Open, FileAccess.Read);
                int CurrentPacketLength;

                netstream.WriteTimeout = 5000;

                // Inicjalizacja enkryptora
                byte[] genKey, genIV;
                var encryptor = new Encryption();
                StreamReader srr= new StreamReader("../../../../Keys/PrivateKeys/" + clientID); //~klucze prywatne są miedzy apkami bo to bardzo bezpieczne...
                string privKeyString  = srr.ReadToEnd();
                byte[] privKey = Encoding.ASCII.GetBytes(privKeyString);

                encryptor.Initialize(out genKey, out genIV, aesType,privKey); //Przekazuje klucz prywatny 

                // Wysłanie CipherMode
                bytesToSend = ASCIIEncoding.ASCII.GetBytes(aesType.ToString());
                netstream.Write(bytesToSend, 0, bytesToSend.Length);
                // Potwierdzenie
                while (netstream.ReadByte() != 'O') { };

                // Odczytanie klucza publicznego ze słownika Clients
                string pubKeyString = Clients[userID];
                var csp = new RSACryptoServiceProvider();

                var sr = new System.IO.StringReader(pubKeyString);
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                var pubKey = (RSAParameters)xs.Deserialize(sr);
                csp.ImportParameters(pubKey);

                //Zaszyfrowanie klucza sesyjnego kluczem publicznym
                var bytesCypherText = csp.Encrypt(genKey, false);

                //Wysłanie zaszyfrowanego klucza publicznego
                netstream.Write(bytesCypherText, 0, bytesCypherText.Length);
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

                var encryptedDataSize = BitConverter.GetBytes(TotalLength);
                netstream.Write(encryptedDataSize, 0, encryptedDataSize.Length);
                // Potwierdzenie odbioru wielkości pliku
                while (netstream.ReadByte() != 'O') { };


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

        private void ecbRB_Checked(object sender, RoutedEventArgs e)
        {
            aesType = CipherMode.ECB;
        }

        private void cbcRB_Checked(object sender, RoutedEventArgs e)
        {
            aesType = CipherMode.CBC;
        }

        private void cfbRB_Checked(object sender, RoutedEventArgs e)
        {
            aesType = CipherMode.CFB;
        }

        private void ofbRB_Checked(object sender, RoutedEventArgs e)
        {
            aesType = CipherMode.OFB;
        }

        private void User1_Checked(object sender, RoutedEventArgs e)
        {
            clientID = 1;
        }

        private void User2_Checked(object sender, RoutedEventArgs e)
        {
            clientID = 2;
        }

        private void User3_Checked(object sender, RoutedEventArgs e)
        {
            clientID = 3;
        }
    }
}
