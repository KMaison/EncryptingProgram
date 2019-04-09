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

		private static readonly int iterations = 10;

		public static byte[] CreateKey(string password, byte[] salt)
		{
			using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
				return rfc2898DeriveBytes.GetBytes(32);
		}

		private static byte[] GetSalt()
		{
			var salt = new byte[32];
			using (var random = new RNGCryptoServiceProvider())
			{
				random.GetNonZeroBytes(salt);
			}

			return salt;
		}

		public static string Encrypt(string input, string password)
		{
			byte[] encrypted;
			byte[] IV;
			byte[] Salt = GetSalt();
			byte[] Key = CreateKey(password, Salt);

			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.Padding = PaddingMode.PKCS7;
				aesAlg.Mode = CipherMode.CBC;

				aesAlg.GenerateIV();
				IV = aesAlg.IV;

				var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (var swEncrypt = new StreamWriter(csEncrypt))
						{
							swEncrypt.Write(input);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}

			byte[] combinedIvSaltCt = new byte[Salt.Length + IV.Length + encrypted.Length];
			Array.Copy(Salt, 0, combinedIvSaltCt, 0, Salt.Length);
			Array.Copy(IV, 0, combinedIvSaltCt, Salt.Length, IV.Length);
			Array.Copy(encrypted, 0, combinedIvSaltCt, Salt.Length + IV.Length, encrypted.Length);

			return Convert.ToBase64String(combinedIvSaltCt.ToArray());
		}


		private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".png";
            dlg.Filter = "png Files (*.png)|*.png|mp3 Files (*.mp3)|*.mp3|avi Files (*.avi)|*.avi|txt Files (*.txt)|*.txt";

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
                string extension = System.IO.Path.GetExtension(selectedFileTextBox.Text);
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(filenameTextBox.Text+extension);
                netstream.Write(bytesToSend, 0, bytesToSend.Length);

                while (netstream.ReadByte() != 'O') { };
                
                FileStream Fs = new FileStream(selectedFileTextBox.Text, FileMode.Open, FileAccess.Read);
                int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(BufferSize)));
                progressBar.Maximum = NoOfPackets;
                int TotalLength = (int)Fs.Length;
                int CurrentPacketLength;

                netstream.WriteTimeout = 5000;
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
                    progressBar.UpdateLayout();
                }
                netstream.Flush();
                while (netstream.ReadByte() != 'O') { }

                Fs.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error");
                //Console.WriteLine(ex.Message);
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
