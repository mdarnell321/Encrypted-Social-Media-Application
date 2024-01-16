using Microsoft.WindowsAPICodePack.Shell;
using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace ESMA
{
    public class SFTPCalls
    {
 
        public static SftpClient uploader = new SftpClient("127.0.0.1", 22, "name", "pass");
        public static string www_Directory = "/C:/xampp/htdocs/";
        public static bool cur_sftp_action = false;
        public static void progressupdate(ulong a)
        {
            MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow._ProgressBar.Value = (int)a;
            });
        }
        public static void UploadProfilePicture(byte[] bytes, string username, bool creation)
        {
            bool aborted = false;
            SFTPCalls.cur_sftp_action = true;
            if (Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
            {
                Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
            }
            try
            {
                string filepath = Path.Combine(System.IO.Path.GetTempPath(), "tempprofile");
                string filepath2 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileB");


                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
                //make the text file for the server
                File.WriteAllText(filepath, Convert.ToBase64String(bytes));
                //make the actual file so we can grab that md5 hash
                if (File.Exists(filepath2))
                {
                    File.Delete(filepath2);
                }
                File.WriteAllBytes(filepath2, bytes);
               

                string md5hash = Methods.GetFileMD5Signature(filepath2);
                try
                {
                    if (File.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash)))
                    {
                        File.Delete(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash));
                    }
                    if (File.Exists(filepath2))
                    {
                        System.IO.File.Move(filepath2, Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash));
                    }
                }
                catch { }
                uploader.Connect();
                
                if (uploader.IsConnected)
                {
                    using (FileStream stream = new FileStream(filepath, FileMode.Open))
                    {
                       
                            uploader.UploadFile(stream, www_Directory + "prof/" + md5hash,
                                (ulong val) =>
                                {
                                    if (cur_sftp_action == false)
                                    {
                                        aborted = true;
                                        stream.Close();
                                    }
                                     
                                });
                     
                    }
                    if (aborted == true)
                        throw new Exception();
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Uploading profile picture fail.");
                    }));
                    uploader.Disconnect();
                    if (creation == true)
                    {
                        DatabaseCalls.RedactAccount(username);
                    }
                    MainWindow.instance.EnableorDisableLoading("Creating account...", false);
                    SFTPCalls.cur_sftp_action = false;
                    return;
                }
               
                uploader.Disconnect();
                DatabaseCalls.updateProfilePic(md5hash, username, creation);
            }
            catch
            {
                MainWindow.instance.EnableorDisableLoading("Creating account...", false);
                if (aborted == true)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Upload aborted.");
                    }));
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Uploading profile picture fail. (2)");
                    }));
                }
                uploader.Disconnect();
                if (creation == true)
                {
                    DatabaseCalls.RedactAccount(username);
                }
            }
            SFTPCalls.cur_sftp_action = false;
        }
        public static string TextFileName(int account_ID, int otheraccount_ID)
        {
            if (account_ID > otheraccount_ID)
            {
                return  account_ID + "~" + otheraccount_ID;
            }
            else
            {
                return  otheraccount_ID + "~" + account_ID;
            }
        }
        public static string GoToDir()
        {
        
            if (ChatManager.CurrentSavedServer != null)
            {
                return "/" + ChatManager.CurrentSavedServer.ToString();
            }
            else if (ChatManager.CurTempRoom != null)
            {
                return "/" + ChatManager.CurTempRoom.ToString();
            }
            else if (ChatManager.CurrentChatWithUser != null)
            {
                return "/" + (TextFileName((int)ChatManager.CurrentChatWithUser, (int)NetworkManager.MyAccountID));
            }
            return "";
        }
        public static string UploadServerPicture(byte[] bytes)
        {
            bool aborted = false;
            SFTPCalls.cur_sftp_action = true;
            if (Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
            {
                Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
            }
            try
            {
                string filepath = Path.Combine(System.IO.Path.GetTempPath(), "tempprofile");
                string filepath2 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileB");

                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
                //make the text file for the server
                File.WriteAllText(filepath, Convert.ToBase64String(bytes));
                //make the actual file so we can grab that md5 hash
                if (File.Exists(filepath2))
                {
                    File.Delete(filepath2);
                }
                File.WriteAllBytes(filepath2, bytes);
      

                string md5hash = Methods.GetFileMD5Signature(filepath2);
                try
                {
                    if (File.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash)))
                    {
                        File.Delete(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash));
                    }
                    if (File.Exists(filepath2))
                    {
                        System.IO.File.Move(filepath2, Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash));
                    }
                }
                catch { }
                uploader.Connect();
             
                if (uploader.IsConnected)
                {
                    using (FileStream stream = new FileStream(filepath, FileMode.Open))
                    {
                      
                            uploader.UploadFile(stream, www_Directory + "serverpics/" + md5hash, (ulong val) =>
                            {
                                if (cur_sftp_action == false)
                                {
                                    aborted = true;
                                    stream.Close();
                                }
                            });
                       
                       
                    }
                    if(aborted == true)
                        throw new Exception();
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Uploading server picture fail.");
                    }));
                    uploader.Disconnect();
                    MainWindow.instance.EnableorDisableLoading("Creating account...", false);
                    MainWindow.checkinghash = false;
                    SFTPCalls.cur_sftp_action = false;
                    return "";
                }

          
                uploader.Disconnect();
                SFTPCalls.cur_sftp_action = false;
                return md5hash;
            }
            catch
            {
                MainWindow.instance.EnableorDisableLoading("Creating account...", false);
                if (aborted == true)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Upload aborted.");
                    }));
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Uploading server picture fail. (2)");
                    }));
                }
                uploader.Disconnect();
                MainWindow.checkinghash = false;
                SFTPCalls.cur_sftp_action = false;
                return "";
            }
            SFTPCalls.cur_sftp_action = false;

        }
        public static void UploadPrivateChatPic(string filepath, string msg, int curchannel)
        {

            string _filepath = filepath;
            string _msg = msg;

            BackgroundWorker.SFTP_Actions.Add((Action)(() =>
            {
                bool aborted = false;
                cur_sftp_action = true;
                try
                {
                    if (Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
                    {
                        Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
                    }
                    string md5hash = Methods.GetFileMD5Signature(_filepath);

                    string theexportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash);
                    string t = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash + "_t");
                    if (File.Exists(theexportfile) == false || File.Exists(theexportfile) == true && Methods.GetFileMD5Signature(theexportfile) != md5hash)
                    {
                        if (File.Exists(theexportfile))
                        {
                            File.Delete(theexportfile);
                        }
                        File.Copy(_filepath, theexportfile);

                    }

                    string filepath2 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileB");
                    string filepath3 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileC");
                    if (File.Exists(filepath2))
                    {
                        File.Delete(filepath2);
                    }

                    File.WriteAllText(filepath2, Methods.EncryptString(Convert.ToBase64String(File.ReadAllBytes(_filepath)), ChatManager.selectedroompassasbytes));

                    // create thumbnail
                    if (File.Exists(t + ".png"))
                    {
                        File.Delete(t + ".png");
                    }
                    using (NetVips.Image image = NetVips.Image.Thumbnail(_filepath, 512))
                    {
                        image.WriteToFile(t + ".png");
                    }
                    string t_md5hash = Methods.GetFileMD5Signature(t + ".png"); // get md5 from temp
                    string newt = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + t_md5hash); // store in cache
                    if (File.Exists(newt) == false || File.Exists(newt) == true && Methods.GetFileMD5Signature(newt) != t_md5hash)
                    {
                        if (File.Exists(newt))
                        {
                            File.Delete(newt);
                        }
                        File.Copy(t + ".png", newt);
                    }
                    File.Delete(t + ".png");
                    if (File.Exists(filepath3))
                    {
                        File.Delete(filepath3);
                    }
                    File.WriteAllText(filepath3, Methods.EncryptString(Convert.ToBase64String(File.ReadAllBytes(newt)), ChatManager.selectedroompassasbytes));
                    uploader.Connect();
             
                    if (uploader.IsConnected)
                    {
                        try
                        {

                            uploader.CreateDirectory((www_Directory +  (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir());
                        }
                        catch
                        {

                        }
                        using (FileStream stream = new FileStream(filepath2, FileMode.Open))
                        {
                            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._ProgressBar.Maximum = (int)stream.Length;
                            });
                          
                                uploader.UploadFile(stream, (www_Directory + (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir() + "/" + md5hash, (ulong val) => { 
                                    progressupdate(val);
                                    if (cur_sftp_action == false)
                                    {
                                        aborted = true;
                                        stream.Close();
                                    }
                                });
                          
                         

                        }
                        if (aborted == true)
                            throw new Exception();
                        using (FileStream stream = new FileStream(filepath3, FileMode.Open))
                        {
                            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._ProgressBar.Maximum = (int)stream.Length;
                            });
                           
                                uploader.UploadFile(stream, (www_Directory + (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir() + "/" + t_md5hash, (ulong val) => { 
                                    progressupdate(val); 
                                    if (cur_sftp_action == false)
                                    {
                                        aborted = true;
                                        stream.Close();
                                    }
                                });
                
                            

                        }
                        if (aborted == true)
                            throw new Exception();
                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Uploading picture fail.");
                        }));
                        uploader.Disconnect();
                        SFTPCalls.cur_sftp_action = false;
                        return;
                    }
                    uploader.Disconnect();
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.CurrentChatWithUser != null)
                        {
                            SendPackets.SendPictureMessage(_msg, md5hash, t_md5hash, curchannel, ChatManager.CurrentSavedServer, true);
                            SendPackets.SendPictureMessage(_msg, md5hash, t_md5hash, curchannel, ChatManager.CurrentSavedServer, false);
                        }
                        else
                        {
                            SendPackets.SendPictureMessage(_msg, md5hash, t_md5hash, curchannel, ChatManager.CurrentSavedServer, null);

                        }
                    });
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow._ProgressBar.Value = 0;
                    });
                }
                catch (System.Exception ex)
                {
                    if (aborted == true)
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Upload aborted.");
                        }));
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow._ProgressBar.Value = 0;
                        });
                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow(string.Format("Uploading picture fail. ({0})", ex));
                        }));
                    }
                    uploader.Disconnect();
                    SFTPCalls.cur_sftp_action = false;
                    return;
                }
                GC.Collect();
                cur_sftp_action = false;
            }));
        }
        public static void UploadPrivateChatVideo(string filepath, string msg, int curchannel)
        {

            string _filepath = filepath;
            string _msg = msg;

            BackgroundWorker.SFTP_Actions.Add((Action)(() =>
            {
                bool aborted = false;
                cur_sftp_action = true;
                try
                {
                    if (Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
                    {
                        Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
                    }
                    string md5hash = Methods.GetFileMD5Signature(_filepath);

                    string theexportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash + ".mp4");
                    string t = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash + "_t");
                    if (File.Exists(theexportfile) == false || File.Exists(theexportfile) == true && Methods.GetFileMD5Signature(theexportfile) != md5hash)
                    {
                        if (File.Exists(theexportfile))
                        {
                            File.Delete(theexportfile);
                        }
                        File.Copy(_filepath, theexportfile);

                    }

                    string filepath2 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileB");
                    string filepath3 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileC");
                    if (File.Exists(filepath2))
                    {
                        File.Delete(filepath2);
                    }
                    File.WriteAllText(filepath2, Methods.EncryptString(Convert.ToBase64String(File.ReadAllBytes(_filepath)), ChatManager.selectedroompassasbytes));



                    //get thumbnail
                    ShellFile shell_file = ShellFile.FromFilePath(filepath);
                    Bitmap shell_thumbnail = shell_file.Thumbnail.ExtraLargeBitmap;

                    int height = 0;
                    int width = 0;
                    float decimatevalue = 1;
                    height = shell_thumbnail.Height;
                    width = shell_thumbnail.Width;
                    if (width > 100 || height > 100)
                    {
                        if (width > height)
                        {
                            decimatevalue = width / 100;
                        }
                        else
                        {
                            decimatevalue = height / 100;
                        }
                    }
                    height = (int)((float)height / decimatevalue);
                    width = (int)((float)width / decimatevalue);

                    Bitmap image = new Bitmap(shell_thumbnail, width, height);
                    byte[] data = null;
                    using (MemoryStream stream = new MemoryStream())
                    {
                        image.Save(stream, ImageFormat.Bmp);
                        data = stream.ToArray();
                      
                    }
                    if(data == null)
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Uploading video fail.");
                        }));
                        uploader.Disconnect();
                        SFTPCalls.cur_sftp_action = false;
                        return;
                    }
                    if (File.Exists(t))
                    {
                        File.Delete(t);
                    }
                    File.WriteAllBytes(t, data);
                    if (Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
                    {
                        Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
                     }

                    string t_md5hash = Methods.GetFileMD5Signature(t);
                    string newt = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + t_md5hash);
                    if (File.Exists(newt) == false || File.Exists(newt) == true && Methods.GetFileMD5Signature(newt) != t_md5hash)
                    {
                        if (File.Exists(newt))
                        {
                            File.Delete(newt);
                        }
                        File.Copy(t, newt);
                    }
                    File.Delete(t);

                    if (File.Exists(filepath3)) // delete temp thumbnail icon
                    {
                        File.Delete(filepath3);
                    }
                    File.WriteAllText(filepath3, Methods.EncryptString(Convert.ToBase64String(data), ChatManager.selectedroompassasbytes)); // write text file for thumbnail


                    uploader.Connect();
         
                    if (uploader.IsConnected)
                    {
                        try
                        {
                            uploader.CreateDirectory((www_Directory +  (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir());
                        }
                        catch { }
                        using (FileStream stream = new FileStream(filepath2, FileMode.Open))
                        {
                            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._ProgressBar.Maximum = (int)stream.Length;
                            });
                          
                                uploader.UploadFile(stream, (www_Directory + (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir() + "/" + md5hash,
                                      (ulong val) =>
                                      {
                                          progressupdate(val);
                                          if (cur_sftp_action == false)
                                          {
                                              aborted = true;
                                              stream.Close();
                                          }
                                      });

                          

                        }
                        if (aborted == true)
                            throw new Exception();
                        using (FileStream stream = new FileStream(filepath3, FileMode.Open))
                        {
                            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._ProgressBar.Maximum = (int)stream.Length;
                            });
                          
                                uploader.UploadFile(stream, (www_Directory + (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir() + "/" + t_md5hash, (ulong val) =>
                                {
                                    progressupdate(val);
                                    if (cur_sftp_action == false)
                                    {
                                        aborted = true;
                                        stream.Close();
                                    }
                                });
                           

                        }
                        if (aborted == true)
                            throw new Exception();
                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Uploading video fail.");
                        }));
                        uploader.Disconnect();
                        SFTPCalls.cur_sftp_action = false;
                        return;
                    }
                    uploader.Disconnect();
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.CurrentChatWithUser != null)
                        {
                            SendPackets.SendVideoMessage(_msg, md5hash, t_md5hash, curchannel, ChatManager.CurrentSavedServer, true);
                            SendPackets.SendVideoMessage(_msg, md5hash, t_md5hash, curchannel, ChatManager.CurrentSavedServer, false);
                        }
                        else
                        {
                            SendPackets.SendVideoMessage(_msg, md5hash, t_md5hash, curchannel, ChatManager.CurrentSavedServer, null);

                        }
                    });
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow._ProgressBar.Value = 0;
                    });
                }
                catch (System.Exception ex)
                {
                    if (aborted == true)
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Upload aborted.");
                        }));
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow._ProgressBar.Value = 0;
                        });
                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow(string.Format("Uploading video fail. ({0})", ex));
                        }));
                    }
                    uploader.Disconnect();
                    SFTPCalls.cur_sftp_action = false;
                    return;
                }
                GC.Collect();
                cur_sftp_action = false;
            }));




        }
        public static void UploadPrivateChatFile(string filepath, string msg, string extension, long size, int curchannel)
        {

            string _filepath = filepath;
            string _msg = msg;

            BackgroundWorker.SFTP_Actions.Add((Action)(() =>
            {
                bool aborted = false;
                cur_sftp_action = true;
                try
                {
                    string md5hash = Methods.GetFileMD5Signature(_filepath);
                    if (Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
                    {
                        Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
                    }
                    //store this in the cache
                    string theexportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash);
                    if (File.Exists(theexportfile) == false || File.Exists(theexportfile) == true && Methods.GetFileMD5Signature(theexportfile) != md5hash)
                    {
                        if (File.Exists(theexportfile))
                        {
                            File.Delete(theexportfile);
                        }
                        File.Copy(_filepath, theexportfile);
                    }
                    //end of storing in cache

                    string filepath2 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileB");
                    if (File.Exists(filepath2))
                    {
                        File.Delete(filepath2);
                    }
                    File.WriteAllText(filepath2, Methods.EncryptString(Convert.ToBase64String(File.ReadAllBytes(_filepath)), ChatManager.selectedroompassasbytes));
                    uploader.Connect();
                  
                    if (uploader.IsConnected)
                    {
                        try
                        {

                            uploader.CreateDirectory((www_Directory +  (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir());
                        }
                        catch
                        {

                        }
                        using (FileStream stream = new FileStream(filepath2, FileMode.Open))
                        {
                            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._ProgressBar.Maximum = (int)stream.Length;
                            });

                          
                                uploader.UploadFile(stream, (www_Directory + (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir() + "/" + md5hash, (ulong val) =>
                                {
                                    progressupdate(val);
                                    if (cur_sftp_action == false)
                                    {
                                        aborted = true;
                                        stream.Close();
                                    }
                                });
                          

                        }
                        if (aborted == true)
                            throw new Exception();
                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Uploading file fail.");
                        }));
                        uploader.Disconnect();
                        SFTPCalls.cur_sftp_action = false;
                        return;
                    }
                    uploader.Disconnect();
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.CurrentChatWithUser != null)
                        {
                            SendPackets.SendFileMessage(_msg, md5hash, extension, size, curchannel, ChatManager.CurrentSavedServer, true);
                            SendPackets.SendFileMessage(_msg, md5hash, extension, size, curchannel, ChatManager.CurrentSavedServer, false);
                        }
                        else
                        {
                            SendPackets.SendFileMessage(_msg, md5hash, extension, size, curchannel, ChatManager.CurrentSavedServer, null);

                        }
                    });
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow._ProgressBar.Value = 0;
                    });
                }
                catch (System.Exception ex)
                {
                    if (aborted == true)
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Upload aborted.");
                        }));
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow._ProgressBar.Value = 0;
                        });
                    }

                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow(string.Format("Uploading file fail. ({0})", ex));
                        }));
                    }
                    uploader.Disconnect();
                    SFTPCalls.cur_sftp_action = false;
                    return;
                }
                GC.Collect();
                cur_sftp_action = false;
            }));
        }

        public static void UploadPrivateChatAudio(string filepath, string msg, int curchannel)
        {

            string _filepath = filepath;
            string _msg = msg;

            BackgroundWorker.SFTP_Actions.Add((Action)(() =>
            {
                bool aborted = false;
                cur_sftp_action = true;
                try
                {
                    string md5hash = Methods.GetFileMD5Signature(_filepath);
                    if(Directory.Exists(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp")) == false)
                    {
                        Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp"));
                    }

                    //store this in the cache
                    string theexportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + md5hash);
                    if (File.Exists(theexportfile) == false || File.Exists(theexportfile) == true && Methods.GetFileMD5Signature(theexportfile) != md5hash)
                    {
                        if (File.Exists(theexportfile))
                        {
                            File.Delete(theexportfile);
                        }
                        File.Copy(_filepath, theexportfile);
                    }
                    //end of storing in cache

                    string filepath2 = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/tempprofileB");
                    if (File.Exists(filepath2))
                    {
                        File.Delete(filepath2);
                    }
                    File.WriteAllText(filepath2, Methods.EncryptString(Convert.ToBase64String(File.ReadAllBytes(_filepath)), ChatManager.selectedroompassasbytes));
                    uploader.Connect();
                   
                    if (uploader.IsConnected)
                    {
                        try
                        {

                            uploader.CreateDirectory((www_Directory +  (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir());
                        }
                        catch
                        {

                        }
                        using (FileStream stream = new FileStream(filepath2, FileMode.Open))
                        {
                            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow._ProgressBar.Maximum = (int)stream.Length;
                            });
                           
                                uploader.UploadFile(stream, (www_Directory + (ChatManager.CurrentSavedServer != null ? "publicroomcontent" : (ChatManager.CurTempRoom != null ? "temproomcontent" : "pms"))) + GoToDir() + "/" + md5hash, (ulong val) =>
                                {
                                    progressupdate(val);
                                    if (cur_sftp_action == false)
                                    {
                                        aborted = true;
                                        stream.Close();
                                    }
                                });
                     

                        }
                        if (aborted == true)
                            throw new Exception();

                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Uploading audio fail.");
                        }));
                        uploader.Disconnect();
                        SFTPCalls.cur_sftp_action = false;
                        return;
                    }
                    uploader.Disconnect();
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.CurrentChatWithUser != null)
                        {
                            SendPackets.SendAudioMessage(_msg, md5hash, curchannel, ChatManager.CurrentSavedServer, true);
                            SendPackets.SendAudioMessage(_msg, md5hash, curchannel, ChatManager.CurrentSavedServer, false);
                        }
                        else
                        {
                            SendPackets.SendAudioMessage(_msg, md5hash, curchannel, ChatManager.CurrentSavedServer, null);

                        }
                    });
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow._ProgressBar.Value = 0;
                    });
                }
                catch (System.Exception ex)
                {
                    if (aborted == true)
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Upload aborted.");
                        }));
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow._ProgressBar.Value = 0;
                        });
                    }
                    else
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow(string.Format("Uploading audio fail. ({0})", ex));
                        }));
                    }
                    uploader.Disconnect();
                    SFTPCalls.cur_sftp_action = false;
                    return;
                }
                GC.Collect();
                cur_sftp_action = false;
            }));
        }
    }
}
