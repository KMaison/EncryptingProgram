using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        bool czyZalogowano = false;
        int IdUser;
        RSAParameters privKey, pubKey;
        public MainWindow()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => MainFunction());
        }

        public void MainFunction()
        {
            TcpListener Listener = null;
            TcpClient client = null;

            while (czyZalogowano == false) { };
            this.Dispatcher.Invoke(() => Login.Content = "Zalogowano");

            //lets take a new CSP with a new 2048 bit rsa key pair
            var csp = new RSACryptoServiceProvider(2048);

            //how to get the private key
            privKey = csp.ExportParameters(true);

            //and the public key ...
            pubKey = csp.ExportParameters(false);

            //converting the public key into a string representation
            string pubKeyString;
            {
                //we need some buffer
                var sw = new System.IO.StringWriter();
                //we need a serializer
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                //serialize the key into the stream
                xs.Serialize(sw, pubKey);
                //get the string from the stream
                pubKeyString = sw.ToString();
            }

            //converting the public key into a string representation
            string privKeyString;
            {
                //we need some buffer
                var sw = new System.IO.StringWriter();
                //we need a serializer
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                //serialize the key into the stream
                xs.Serialize(sw, privKey);
                //get the string from the stream
                privKeyString = sw.ToString();
            }

            Encryption encryptPrivKey = new Encryption();
            byte[] IV;
            byte[] Key;
            encryptPrivKey.Initialize(out Key, out IV);
            byte[] encryptedKey = encryptPrivKey.Encrypt(ASCIIEncoding.ASCII.GetBytes(privKeyString));
            File.WriteAllText("./Keys/PrivateKeys/" + IdUser, Encoding.ASCII.GetString(encryptedKey, 0, encryptedKey.Length));
            File.WriteAllText("./Keys/PublicKeys/" + IdUser, pubKeyString);

            int tries = 0;
            NetworkStream netstream = null;
            while (tries++ < 100)
            {
                try
                {
                    client = new TcpClient("127.0.0.1", 5001);
                    netstream = client.GetStream();
                    break;
                }
                catch (Exception e1) { }
            }
            // Wysylanie publicKey
            netstream = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(pubKeyString);
            netstream.Write(bytesToSend, 0, bytesToSend.Length);
            // Potwierdzenie
            while (netstream.ReadByte() != 'O') { };

            // Wysyłannie iduser
            bytesToSend = BitConverter.GetBytes(IdUser);
            netstream.Write(bytesToSend, 0, bytesToSend.Length);
            // Potwierdzenie
            while (netstream.ReadByte() != 'O') { };

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
                        ReceiveFile(client);
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() => UserConsole.Text += ex.Message + "\n");
                }
            }
        }
        public int ReceiveFile(TcpClient client)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            NetworkStream netstream = null;
            byte[] RecData = new byte[1024];
            byte[] Key, IV;
            CipherMode aesType;
            int RecBytes, FileSize;

            this.Dispatcher.Invoke(() => UserConsole.Text += "Incoming File\n");

            netstream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];

            // read file name
            int bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            //---convert the data received into a string---
            string SaveFileName = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "File name: " + SaveFileName + "\n");
            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

            // send UserID
            var bytesToSend = BitConverter.GetBytes(IdUser);
            netstream.Write(bytesToSend, 0, bytesToSend.Length);
            // Potwierdzenie
            while (netstream.ReadByte() != 'O') { };

            // read CipherMode
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            //---convert the data received into a string---
            string aesTypeStr = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "Cipher Mode: " + aesTypeStr + "\n");

            if (aesTypeStr.Equals("ECB"))
                aesType = CipherMode.ECB;
            else if (aesTypeStr.Equals("CBC"))
                aesType = CipherMode.CBC;
            else if (aesTypeStr.Equals("CFB"))
                aesType = CipherMode.CFB;
            else aesType = CipherMode.OFB;

            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

            // read AES Key
            //buffer = new byte[client.ReceiveBufferSize];
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            //Key = new byte[bytesRead];
            //we want to decrypt, therefore we need a csp and load our private key
            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privKey);
            //decrypt and strip pkcs#1.5 padding
            Key = csp.Decrypt(buffer.Take(256).ToArray<byte>(), false);

            //Array.Copy(buffer, Key, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "Key: " + BitConverter.ToString(buffer.Take(32).ToArray()) + "\n");
            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

            // read AES IV
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            IV = new byte[bytesRead];
            Array.Copy(buffer, IV, bytesRead);
            this.Dispatcher.Invoke(() => UserConsole.Text += "IV: " + BitConverter.ToString(buffer.Take(16).ToArray()) + "\n");
            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

            // read File Size
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            FileSize = BitConverter.ToInt32(buffer, 0);
            this.Dispatcher.Invoke(() => UserConsole.Text += "Rozmiar pliku: " + FileSize + "B\n");
            netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);
            this.Dispatcher.Invoke(() =>
            {
                FileSizePB.Maximum = FileSize;
                FileSizePB.Value = 0;
            });

            int totalrecbytes = 0;
            while (client.Available == 0) { }

            FileStream Fs = new FileStream(".\\UploadedFiles\\" + SaveFileName, FileMode.OpenOrCreate, FileAccess.Write);

            netstream.ReadTimeout = 5000;

            using (var msEncrypted = new MemoryStream())
            {
                while (netstream.DataAvailable)
                {
                    while (netstream.DataAvailable)
                    {
                        while (netstream.DataAvailable)
                        {
                            RecBytes = netstream.Read(RecData, 0, RecData.Length);
                            this.Dispatcher.Invoke(() =>
                            {
                                FileSizePB.Value += RecBytes;
                            });
                            msEncrypted.Write(RecData, 0, RecBytes);
                            totalrecbytes += RecBytes;
                        }
                        Thread.Sleep(300);
                    }
                    Thread.Sleep(700);
                }


                // Wysłanie potwierdzenia odbioru
                netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);

                // Dekrypcja
                var decryptedData = Decryption.Decrypt(msEncrypted.ToArray(), Key, IV, aesType);

                Fs.Write(decryptedData, 0, decryptedData.Length);
                Fs.Flush();
                Fs.Close();
            }
            client.Close();
            netstream.Close();

            this.Dispatcher.Invoke(() => UserConsole.Text += "File saved\n\n");
            return 0;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            switch (IdUser)
            {
                case 1:
                    if (password.Password.Equals("user1"))
                    {
                        czyZalogowano = true;
                        Login.IsEnabled = false;
                    }
                    break;
                case 2:
                    if (password.Password.Equals("user2"))
                    {
                        czyZalogowano = true;
                        Login.IsEnabled = false;
                    }
                    break;
                case 3:
                    if (password.Password.Equals("user3"))
                    {
                        czyZalogowano = true;
                        Login.IsEnabled = false;
                    }
                    break;
            }

        }

        private void User1_Checked(object sender, RoutedEventArgs e)
        {
            IdUser = 1;
        }

        private void User2_Checked(object sender, RoutedEventArgs e)
        {
            IdUser = 2;
        }

        private void User3_Checked(object sender, RoutedEventArgs e)
        {
            IdUser = 3;
        }
    }
}
