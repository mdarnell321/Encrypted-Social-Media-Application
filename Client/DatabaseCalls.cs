
using K4os.Compression.LZ4.Streams.Abstractions;
using NAudio.Wave;
using NetVips;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Authentication;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Linq;

namespace ESMA
{
    public static class DatabaseCalls
    {
        public static string host = "http://127.0.0.1";
        public static string chosenprofilepicpath { get; set; }
        public static void Register(string username, string password, string passwordconfirm, byte[] profilepic, string publickey, string privatekey) 
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please enter a username.");
                }));
                return;
            }
            if (String.IsNullOrWhiteSpace(password))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please enter a password.");
                }));
                return;
            }
            if (String.IsNullOrWhiteSpace(passwordconfirm))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please confirm your password.");
                }));
                return;
            }
            if(Methods.AlphaNumerical(username) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Only alpha-numerical characters are allowed for username.");
                }));
                return;
            }
            if (Methods.RegularText(password) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Only english keyboard characters are allowed for password.");
                }));
                return;
            }
            if (password != passwordconfirm)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Password do not match.");
                }));
                return;
            }
            username = username.Trim();
            MainWindow.instance.EnableorDisableLoading("Creating account...", true);
            string post = Methods.GetPost(host + "/RegMethod.php", "name", username, "pass", password, "publickey", publickey, "privatekey", privatekey);

            if (post.Trim() != "Success")
            {
                MainWindow.instance.MessageBoxShow(post);
                MainWindow.instance.EnableorDisableLoading("Creating account...", false);
                return;
            }   
            //write private key locally
            SFTPCalls.UploadProfilePicture(profilepic, username, true);
        }
        public static void AddUserCache(int account_ID) 
        {
            if (ChatManager.UserCache.Any(x => x.account_ID == account_ID) == false)
            {
                SendPackets.GetProfilePicture(account_ID);
            }
        }
       
        public static void SaveUserMedia() 
        {
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                string t = MainWindow.instance.UserProfile_MediaTextBox.Text;
                if (t.Contains("t1h1i1s1i1s1a1t1a1p1l1i1t1") == false && t.Contains("t2h2i2s2i2s2a2s2p2l2i2t2") == false)
                {
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        SendPackets.RequestSaveProfile(t);
                    });
                }
            });
        }
        public static void OpenUserProfile(int account_ID, string _bio, string _media, string _name, string _creation, string _friends, string _roles, string _reqroles) 
        {
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.EditMyMedia.Visibility = Visibility.Visible;
                MainWindow.CurPLWindow = account_ID;
                MainWindow.instance.UserProfile_MediaTextBox.Visibility = Visibility.Hidden;
                MainWindow.instance.UserProfile_MediaListBox.Visibility = Visibility.Visible;
                MainWindow.instance.UserProfile_MediaListBox.Items.Clear();
            });


            string bio = _bio;
            string media = _media;
            string name = _name;
            string creation = _creation;
            string friends = _friends;
            string[] roles = _roles.Split('/');
            List<string> reqroles = _reqroles.Split('/').ToList();
            reqroles.RemoveAll(x => String.IsNullOrWhiteSpace(x));
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.UserProfile_Picture.Source = (ChatManager.GetUserCacheByID(account_ID) ?? new UserDataCache()).profilepic as ImageSource;
            });
            if (NetworkManager.MyAccountID == account_ID)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.addfriendfromwindow.Visibility = Visibility.Visible;
                    MainWindow.instance.removefriendfromwindow.Visibility = Visibility.Hidden;
                    MainWindow.instance.addfriendfromwindow.IsEnabled = false;
                    MainWindow.instance.dmfromwindow.IsEnabled = false;
                    MainWindow.instance.EditMyMedia.Visibility = Visibility.Visible;
                    MainWindow.instance.transferownership.Visibility = Visibility.Hidden;
                    MainWindow.instance.banfromwindow.Visibility = Visibility.Hidden;
                    MainWindow.instance.kickfromwindow.Visibility = Visibility.Hidden;
                    MainWindow.instance.mutefromwindow.Visibility = Visibility.Hidden;
                });
            }
            else
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.addfriendfromwindow.IsEnabled = true;
                    MainWindow.instance.removefriendfromwindow.IsEnabled = true;
                    MainWindow.instance.dmfromwindow.IsEnabled = true;
                    MainWindow.instance.EditMyMedia.Visibility = Visibility.Hidden;
                });
                if (ChatManager.CurrentSavedServer != null && ChatManager.UserList_FULL.Any(x => x.AccountID == account_ID))
                {
                    Visibility a = reqroles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == int.Parse(x)) ?? new RolesList(-1, "undefined", new int[6], "#FFFFFF", -1)).powers[2] == 1) ? Visibility.Visible : Visibility.Hidden;
                    Visibility b = reqroles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == int.Parse(x)) ?? new RolesList(-1, "undefined", new int[6], "#FFFFFF", -1)).powers[0] == 1) ? Visibility.Visible : Visibility.Hidden;
                    bool c = true;
                    Visibility d = ChatManager.my_room_user._Roles.Contains(1) ? Visibility.Visible : Visibility.Hidden;
                    Visibility e = reqroles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == int.Parse(x)) ?? new RolesList(-1, "undefined", new int[6], "#FFFFFF", -1)).powers[1] == 1) ? Visibility.Visible : Visibility.Hidden;

                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.banfromwindow.Visibility = a;
                        MainWindow.instance.kickfromwindow.Visibility = b;
                        MainWindow.instance.transferownership.IsEnabled = c;
                        MainWindow.instance.transferownership.Visibility = d;
                        MainWindow.instance.mutefromwindow.Visibility = e;
                    });
                }
                else
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.banfromwindow.Visibility = Visibility.Hidden;
                        MainWindow.instance.kickfromwindow.Visibility = Visibility.Hidden;
                        MainWindow.instance.mutefromwindow.Visibility = Visibility.Hidden;
                        MainWindow.instance.transferownership.Visibility = Visibility.Hidden;
                    });
                }
            }
            if (friends.Split('?').ToList().Contains(NetworkManager.MyAccountID.ToString()) == true)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.addfriendfromwindow.Visibility = Visibility.Hidden;
                    MainWindow.instance.removefriendfromwindow.Visibility = Visibility.Visible;
                });
            }
            else
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.addfriendfromwindow.Visibility = Visibility.Visible;
                    MainWindow.instance.removefriendfromwindow.Visibility = Visibility.Hidden;
                });
            }
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.UserProfile_BioText.Text = bio;
                MainWindow.instance.UserProfile_Name.Text = name;
                MainWindow.instance.UserProfile_Name_Copy.Text = "Account Creation : " + creation.Split(' ')[0];
            });
            if (String.IsNullOrEmpty(media) == false)
            {
                foreach (string s in media.Split(new string[] { "t1h1i1s1i1s1a1t1a1p1l1i1t1" }, StringSplitOptions.None))
                {
                    if (s.Contains("t2h2i2s2i2s2a2s2p2l2i2t2"))
                    {
                        string[] internalstuff = s.Split(new string[] { "t2h2i2s2i2s2a2s2p2l2i2t2" }, StringSplitOptions.None);
                        string WebsiteName = internalstuff[0].Split('/')[2].Split('.')[1].ToUpper();
                        string Raw = internalstuff[0];
                        string Title = WebsiteName + " : " + internalstuff[1];
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow.instance.UserProfile_MediaListBox.Items.Add(new MediaFormat(WebsiteName, Title, Raw));
                        });
                    }

                }

            }
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.UserProfileWindow.Visibility = Visibility.Visible;
            });
            string builtstring = "";
            if (ChatManager.CurrentSavedServer != null)
            {

                for (int i = 0; i < roles.Length; ++i)
                {
                    if (String.IsNullOrEmpty(roles[i]) == false)
                    {
                        builtstring += (ChatManager.RoleList.Find(x => x.id == int.Parse(roles[i])) ?? new RolesList(-1, "undefined", new int[6], "#FFFFFF", -1)).rolename;
                        if (i < roles.Length - 2)
                        {
                            builtstring += ", ";
                        }
                    }
                }
            }
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.ProfileWindowRole.Text = "Roles : " + builtstring;
            });
        }
        public static void RedactAccount(string username) 
        {
            string post = Methods.GetPost(host + "/RedactAccountMethod.php", "name", username);

            if (post.Trim() != "Deleted")
            {
                MainWindow.instance.MessageBoxShow("Failure in removal of account.");           
            }
            else
            {
                MainWindow.instance.MessageBoxShow("Account removal success.");
            }
        }
        public static void Login(string username, string password) 
        {

            if (String.IsNullOrWhiteSpace(username))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please enter a username.");
                }));
                return;
            }
            if (String.IsNullOrWhiteSpace(password))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please enter a password.");
                }));
                return;
            }
            if (Methods.AlphaNumerical(username) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Only alpha-numerical characters are allowed for username.");
                }));
                return;
            }
            if (Methods.RegularText(password) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Only english keyboard characters are allowed for password.");
                }));
                return;
            }
            MainWindow.instance.EnableorDisableLoading("Logging in...", true);

            string post = Methods.GetPost(host + "/LoginMethod.php", "name", username, "password", password, "v", "1");
      
            if(String.IsNullOrWhiteSpace(post))
            {
                MainWindow.instance.MessageBoxShow("Login failed.");
                MainWindow.instance.EnableorDisableLoading("Logging in...", false);
                return;
            }
            if (post.Trim() == "wv")
            {
                MainWindow.instance.MessageBoxShow("You have the wrong version of the client.");
                MainWindow.instance.EnableorDisableLoading("Logging in...", false);
                return;
            }

            string privatekey = "";


            string fetchedmd5 = post.Split('|')[0];
            string fetchedid = post.Split('|')[1];
            string fetchedname = post.Split('|')[2];
            string publickey = post.Split('|')[3];
            string fetchedprivatekey = post.Split('|')[4];
            if(fetchedprivatekey == "Local")
            {
                string thisaccountkeypath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), username + ".xml");
                if (File.Exists(thisaccountkeypath) == false)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Your private key for PMs not found. Unable to login.");
                    }));
                    return;
                }
                privatekey = File.ReadAllText(thisaccountkeypath, System.Text.Encoding.UTF8);
            }
            else
            {
                string constanta = "Thestandardconstant@#$!@#";
                string constantb = "C0n$1stent1yC0nst4nt";
                byte[] code;
                using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                {
                    byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(password));
                    code = keys;
                }
                byte[] ccode;
                using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                {
                    byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(constantb));
                    ccode = keys;
                }
                 string pk = Methods.EncryptOrDecryptExposable(Methods.DecryptString(Methods.DecryptString(fetchedprivatekey.Replace(' ', '+'), ccode), code), constanta);
                if (pk == null)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Failure fetching private key.");
                    }));
                    return;
                }
                privatekey = pk;
            }
            ChatManager.MyUserPrivateKey = privatekey;
            string exportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp/" + fetchedmd5 + "");
            if (File.Exists(exportfile) && Methods.GetFileMD5Signature(exportfile) == fetchedmd5)
            {
                goto Success;
            }

            string webString = "";
            try
            {
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    webString = webclient.DownloadString(host + "/prof/" + fetchedmd5);
                 
                }
                if (String.IsNullOrEmpty(webString) == true)
                {
                    throw new Exception();
                }
            }
            catch
            {
                goto Fail;
            }

            byte[] imgStr = Convert.FromBase64String(webString);
            if (File.Exists(exportfile))
            {
                File.Delete(exportfile);
            }
            File.WriteAllBytes(exportfile, imgStr);

        Success:;
            MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.EnableorDisableLoading("Connecting to master server...", true);
                MainWindow.instance.GoToLobby(fetchedmd5 + "", fetchedname, int.Parse(fetchedid));
                ChatManager.MyServers.Add(new ServerDirectoryElements(-1, "+", ""));
                ChatManager.MyServers.Add(new ServerDirectoryElements(-2, "+", ""));
                MainWindow.instance.ServerDirectoryListBox.ItemsSource = ChatManager.MyServers;
                MainWindow.instance.ServerDirectoryListBox.Items.Refresh();

            });
            ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
            {
                MainWindow.instance.MessageBoxShow("Login Success");
            }));
            ChatManager.MyUserPublicKey = publickey;
            goto Skip;
        Fail:;
            ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
            {
                MainWindow.instance.MessageBoxShow("Login failed.");
            }));
        Skip:;
            
        
        }
        public static void updateProfilePic(string hash, string name, bool creation) 
        {
           
            string post = Methods.GetPost(host + "/UpdateProfPicture.php", "name", name, "md5", hash);

            if (post.Trim() != "Success")
            {
                MainWindow.instance.MessageBoxShow(post);
                MainWindow.instance.EnableorDisableLoading("Creating account...", false);
                return;
            }

            if (creation == false)
            {
                SendPackets.SendNewUserProfilePicture(hash, (int)NetworkManager.MyAccountID);
            }
            MainWindow.instance.EnableorDisableLoading("Creating account...", false);
            ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
            {
                if (creation == true)
                {
                    MainWindow.instance.MessageBoxShow("Creating account success.");
                    MainWindow.instance.SetFormVisibility(1);
                }
                else
                {
                    MainWindow.instance.MessageBoxShow("Re-assigning profile picture success.");
                }

            }));

          

        }

    }
}
public struct MediaFormat
{
    public string Site { get; set; }
    public string Title { get; set; }
    public string Raw { get; set; }
    public MediaFormat(string site, string title, string raw)
    {
        Site = site;
        Title = title;
        Raw = raw;
    }
}
