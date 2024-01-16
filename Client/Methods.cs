using Microsoft.Win32;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ESMA
{
    public class Methods
    {
        public static Random RandomRange = new Random();
        public static int Clamp(int original, int min, int max)
        {
            int _new = original;
            if (_new > max)
            {
                _new = max;
            }
            if (_new < min)
            {
                _new = min;
            }
            return _new;
        }
        public static float Clamp(float original, float min, float max)
        {
            float _new = original;
            if (_new > max)
            {
                _new = max;
            }
            if (_new < min)
            {
                _new = min;
            }
            return _new;
        }
        public static string GetServerDir()
        {
            return (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms")) + "/" + SFTPCalls.GoToDir() + "/";
        }
        public static BitmapImage BitmapSourceToBitmapImage(BitmapSource source)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapImage img = new System.Windows.Media.Imaging.BitmapImage();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);
                img.BeginInit();
                img.StreamSource = new MemoryStream(stream.ToArray());
                img.EndInit();
                stream.Close();
                return img;
            }
        }
        public static string EncryptString(string input, byte[] hash) // Primary encryption method
        {
            if (hash == null)
            {
                return "";
            }
            byte[] input_bytes = UTF8Encoding.UTF8.GetBytes(input);
            using (TripleDESCryptoServiceProvider tripDes = new TripleDESCryptoServiceProvider() { Key = hash, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7 })
            {
                byte[] results = tripDes.CreateEncryptor().TransformFinalBlock(input_bytes, 0, input_bytes.Length);
                return Convert.ToBase64String(results, 0, results.Length);
            }
        }
        public static object DeepClone(object obj)
        {
            object result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter f = new BinaryFormatter();
                f.Serialize(stream, obj);
                stream.Position = 0;
                result = f.Deserialize(stream);
            }
            return result;
        }
        public static string AsymetricalEncryption(string input, string publicKey) // Direct messaging encryption method
        {
            byte[] input_bytes = Encoding.UTF8.GetBytes(input);
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                try
                {
                    rsa.FromXmlString(publicKey.ToString().Trim().Replace(' ', '+'));
                    string encrypted = Convert.ToBase64String(rsa.Encrypt(input_bytes, false));
                    return encrypted;
                }
                catch
                {
                    rsa.PersistKeyInCsp = false;
                    return null;
                }
            }
        }
        public static bool URLExists(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch // no response
            {
                return false;
            }
            return true;
        }
        public static string GetPost(string url, params string[] data)
        {
            string content = "";
            string temp = "";
            ASCIIEncoding ascii = new ASCIIEncoding();
            for (int i = 0; i < data.Length; i += 2)
                temp += string.Format("&{0}={1}", data[i], data[i + 1]);
            string s = temp.Remove(0, 1);
            byte[] bytes = ascii.GetBytes(s);
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = bytes.Length;

                using (Stream pageStream = webRequest.GetRequestStream())
                {
                    pageStream.Write(bytes, 0, bytes.Length);
                    pageStream.Close();
                }

                using (StreamReader streamReader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
                {
                    content = streamReader.ReadToEnd();
                    streamReader.Close();
                }


            }
            catch (Exception ex)
            {
                //
            }
            return content;
        }
        public static string AsymetricalDecryption(string input, string privateKey) // decrypt a direct message
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                try
                {
                    rsa.FromXmlString(privateKey);
                    byte[] input_bytes = Convert.FromBase64String(input);
                    byte[] bytes_decrypted = rsa.Decrypt(input_bytes, false);
                    string result = Encoding.UTF8.GetString(bytes_decrypted);
                    return result;
                }
                catch
                {
                    rsa.PersistKeyInCsp = false;
                    return null;
                }

            }
        }
        public static string DecryptString(string input, byte[] hash) // decrypt regular
        {
            if (hash == null)
            {
                return null;
            }
            try
            {
                byte[] input_bytes = Convert.FromBase64String(input);
                using (TripleDESCryptoServiceProvider triple_DES = new TripleDESCryptoServiceProvider())
                {
                    triple_DES.Key = hash; 
                    triple_DES.Mode = CipherMode.ECB;
                    triple_DES.Padding = PaddingMode.PKCS7;
                    ICryptoTransform decryptor = triple_DES.CreateDecryptor();
                    byte[] results = decryptor.TransformFinalBlock(input_bytes, 0, input_bytes.Length);
                    if (results == null)
                        return null;
                    return UTF8Encoding.UTF8.GetString(results);
                }
            }
            catch
            {
                return null;
            }
        }
        public static byte[] EncryptOrDecrypt(byte[] data, byte[] key) // easy encryption and decryption with byte array params
        {
            if (key == null)
            {
                return null;
            }
            try
            {
                byte[] encrypted_data = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    encrypted_data[i] = (byte)(data[i] ^ key[i % key.Length]);
                }
                return encrypted_data;
            }
            catch
            {
                return null;
            }
        }
        public static string EncryptOrDecryptExposable(string input, string keystring)  // easy encryption and decryption with string params
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(keystring);
                byte[] textasbyte = Encoding.UTF8.GetBytes(input);
                if (key == null || textasbyte == null)
                    return null;
                byte[] encrypted_data = new byte[textasbyte.Length];
                for (int i = 0; i < textasbyte.Length; i++)
                {
                    encrypted_data[i] = (byte)(textasbyte[i] ^ key[i % key.Length]);
                }
                return Encoding.UTF8.GetString(encrypted_data);
            }
            catch
            {
                return null;
            }
        }
        public static bool foundchannel(int channelkey)
        {
            bool found = false;
            int[] keyarray = new int[ChatManager.channels.Keys.Count];
            ChatManager.channels.Keys.CopyTo(keyarray, 0);
            foreach (int c in keyarray)
            {
                if (c == channelkey)
                {
                    found = true;
                }
            }
            return found;
        }
        public static string Show_SaveFileDialog(string[] ext, int filetype)
        {
            MainWindow.FileDialogOpened = true;
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.FileDialogLoading.Visibility = Visibility.Visible;
            });
            SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            if (ext.Length > 0)
            {
                dialog.DefaultExt = "." + ext[0].ToLower();
                string builtstring = "";
                for (int i = 0; i < ext.Length; ++i)
                {
                    if (i < ext.Length - 1)
                    {
                        builtstring += string.Format("{0} (.{1})|*.{1}|", ext[i].ToUpper(), ext[i].ToLower());
                    }
                    else
                    {
                        builtstring += string.Format("{0} (.{1})|*.{1}", ext[i].ToUpper(), ext[i].ToLower());
                    }

                }
                dialog.Filter = builtstring;
            }
            else
            {
                dialog.DefaultExt = ".";
                dialog.Filter = "All files (*.*)|*.*";
            }
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                return dialog.FileName;
            }
            return null;
        }
        public static bool AlphaNumerical(string sample)
        {
            for (int i = 0; i < sample.Length; ++i)
            {
                if (char.IsLetterOrDigit(sample[i]) == false && char.IsWhiteSpace(sample[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool Allowed(string sample)
        {
            if (sample.Contains("❶") == false && sample.Contains("❷") == false)
                return true;
            return false;
        }
        public static bool RegularText(string sample)
        {
            return Regex.IsMatch(sample, @"^[\x20-\x7E\n\p{Sc}][A-Za-z0-9\s@]*$");
        }
        public static string Show_OpenFileDialog(string[] ext, int filetype)
        {
            MainWindow.FileDialogOpened = true;
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.FileDialogLoading.Visibility = Visibility.Visible;
            });
            OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            if (ext.Length > 0)
            {
                dialog.DefaultExt = "." + ext[0].ToLower();
                string builtstring = "";
                for (int i = 0; i < ext.Length; ++i)
                {
                    if (i < ext.Length - 1)
                    {
                        builtstring += string.Format("{0} (.{1})|*.{1}|", ext[i].ToUpper(), ext[i].ToLower());
                    }
                    else
                    {
                        builtstring += string.Format("{0} (.{1})|*.{1}", ext[i].ToUpper(), ext[i].ToLower());
                    }

                }
                dialog.Filter = builtstring;
            }
            else
            {
                dialog.DefaultExt = ".";
                dialog.Filter = "All files (*.*)|*.*";
            }
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                switch (filetype)
                {
                    case 0://picture
                        try
                        {
                            BitmapImage b = new BitmapImage(new Uri(dialog.FileName));
                        }
                        catch
                        {
                            return null;
                        }
                        break;
                    case 1://video
                        MainWindow.LoadMediaResult = 0;
                        try
                        {
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._VideoElement.Source = new Uri(dialog.FileName);
                                MainWindow._VideoElement.Play();
                            });
                            while (MainWindow.LoadMediaResult == 0)
                            {

                            }
                            if (MainWindow.LoadMediaResult == 2)
                            {
                                throw new Exception();
                            }
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._VideoElement.Stop();
                                MainWindow._VideoElement.Source = null;
                            });
                        }
                        catch
                        {
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._VideoElement.Stop();
                                MainWindow._VideoElement.Source = null;
                            });
                            return null;
                        }
                        break;
                    default:

                        break;
                }
                return dialog.FileName;
            }
            return null;
        }
        public static string GetFileMD5Signature(string path)
        {
            if (File.Exists(path) == false)
            {
                return "";
            }
            using (MD5 md5 = MD5.Create())
            {
                try
                {
                    using (FileStream stream = File.OpenRead(path))
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
                    }
                }
                catch
                {
                    return "";
                }
            }
        }

        public static ImageSource BytesToImageSource(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            BitmapImage bmp_img = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(data))
            {
                bmp_img.BeginInit();
                bmp_img.StreamSource = stream;
                bmp_img.EndInit();
            }
            return bmp_img as ImageSource;
        }
        public static byte[] ImageSourceToBytes(BitmapImage img)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }
    }
}
