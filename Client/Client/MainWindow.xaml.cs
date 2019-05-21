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
        string userPass;
        RSAParameters privKey, pubKey;
        string privKeyString;
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

            //Kryptografia asymetryczna
            var csp = new RSACryptoServiceProvider(2048);

            privKey = csp.ExportParameters(true);
            pubKey = csp.ExportParameters(false);
            
            string pubKeyString;
            {
                var sw = new System.IO.StringWriter();
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                xs.Serialize(sw, pubKey);
                pubKeyString = sw.ToString();
            }

           
            {
                var sw = new System.IO.StringWriter();
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                xs.Serialize(sw, privKey);
                privKeyString = sw.ToString();
            }

            Encryption encryptPrivKey = new Encryption();
            byte[] newKey = new byte[32];
            byte[] tmpIV = new byte[16];
            byte[] hash;

            SHA1 sha = new SHA1CryptoServiceProvider();
            hash = sha.ComputeHash(ASCIIEncoding.ASCII.GetBytes("user" + IdUser));
            Array.Copy(hash, newKey, hash.Length>32 ? 32 : hash.Length);

            encryptPrivKey.Initialize(newKey, tmpIV);


            //Zaszyfriwanie klucza prywatnego z AES
            byte[] encryptedKey = encryptPrivKey.Encrypt(ASCIIEncoding.ASCII.GetBytes(privKeyString));

            //Zapisanie kluczy asymetrycznych
            File.WriteAllBytes("./Keys/PrivateKeys/" + IdUser, encryptedKey);
            File.WriteAllBytes("../../../../Keys/PublicKeys/" + IdUser, ASCIIEncoding.ASCII.GetBytes(pubKeyString));


            //Próba połącznia z serwerem
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

            //// Wysyłannie IdUser
            //byte[] bytesToSend = BitConverter.GetBytes(IdUser);
            //netstream.Write(bytesToSend, 0, bytesToSend.Length);
            //// Potwierdzenie
            //while (netstream.ReadByte() != 'O') { };

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

            // Odbieranie UserID
            buffer = new byte[client.ReceiveBufferSize];
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);
            int userID = BitConverter.ToInt32(buffer, 0);

            if (userID != IdUser)
            {
                netstream.Write(ASCIIEncoding.ASCII.GetBytes("N"), 0, ASCIIEncoding.ASCII.GetBytes("N").Length);
                return 0; // nie do mnie
            }
            else
                netstream.Write(ASCIIEncoding.ASCII.GetBytes("O"), 0, ASCIIEncoding.ASCII.GetBytes("O").Length);



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
            buffer = new byte[client.ReceiveBufferSize];
            bytesRead = netstream.Read(buffer, 0, client.ReceiveBufferSize);


            byte[] bytesKey = File.ReadAllBytes("./Keys/PrivateKeys/" + IdUser);

            Decryption encryptPrivKey = new Decryption();

            byte[] shaKey = new byte[32];
            byte[] tmpIV = new byte[16];
            byte[] hash;

            SHA1 sha = new SHA1CryptoServiceProvider();
            hash = sha.ComputeHash(ASCIIEncoding.ASCII.GetBytes(userPass));
            Array.Copy(hash, shaKey, hash.Length > 32 ? 32 : hash.Length);

            byte[] decryptedKey = Decryption.Decrypt(bytesKey, shaKey, tmpIV, CipherMode.CBC);

            // Odczytanie klucza publicznego ze stringa
            string keyString = Encoding.ASCII.GetString(decryptedKey, 0, decryptedKey.Length);
            var sr = new System.IO.StringReader(keyString);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            var prvKeyD = (RSAParameters)xs.Deserialize(sr);


            var csp = new RSACryptoServiceProvider();
            csp.ImportParameters(prvKeyD);
            //decrypt and strip pkcs#1.5 padding
            Key = csp.Decrypt(buffer.Take(256).ToArray<byte>(), false);

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



                //// Dekrypcja
                //StreamReader srr = new StreamReader(".Keys/PrivateKeys/" + IdUser); //~klucze prywatne są miedzy apkami bo to bardzo bezpieczne...
                //string privKeyString = srr.ReadToEnd();
                //byte[] privKey = Encoding.ASCII.GetBytes(privKeyString);


                
                var decryptedData = Decryption.Decrypt(msEncrypted.ToArray(), Key, IV, aesType); //odszyfrowuje prywatnym

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
            userPass = password.Password;
            czyZalogowano = true;
            Login.IsEnabled = false;
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
