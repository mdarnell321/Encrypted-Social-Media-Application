using ESMA;
using Google.Protobuf.WellKnownTypes;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using NAudio.Wave;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Resources;
using System.Xml.Linq;


public class ReceivePackets
{

    public static void Connected(Packet packet, ref bool error)
    {
        int public_id = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        NetworkManager.instance.public_id = public_id;
        NetworkManager.MyPublicID = public_id;
        SendPackets.GoodConnected();
    }
    public static void SendTCPReady(Packet packet, ref bool error)
    {
        packet.CheckFinalizers(ref error); if (error == true) return;
        SendPackets.TCPReady();
    }
    public static void CreateUser(Packet packet, ref bool error)
    {
        int public_id = packet.GetInt(ref error); if(error == true) return;
        string username = packet.GetString(ref error); if(error == true) return;
        int account_ID = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        NetworkManager.CreateGlobalUser(public_id, username, account_ID);
    }
    public static void SendMessage(Packet packet, ref bool error)
    {
        string msg = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
        {
            MainWindow.instance.MessageBoxShow(msg);
        }));
    }
    public static void LeaveServerNotify(Packet packet, ref bool error)
    {
        int acc = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        UserList u = ChatManager.GetChatUserByID(acc);
        if (u != null)
            u.StopTransmission();

        ChatManager.UserList.RemoveAll(x => x.AccountID == acc);
        ChatManager.UserList_FULL.RemoveAll(x => x.AccountID == acc);
        foreach (int key in ChatManager.channels.Keys)
        {
            if ((ChatManager.channels[key] as VoiceChannelsForTempRoom) != null)
            {
                (ChatManager.channels[key] as VoiceChannelsForTempRoom).UsersInRoom.RemoveAll(x => x.AccountID == acc);
            }

        }

        RefreshUserList();
        foreach (int key in ChatManager.channels.Keys)
        {
            VoiceChannelsForTempRoom vc = (ChatManager.channels[key] as VoiceChannelsForTempRoom);
            if (vc != null)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < vc.ListBoxItem.Items.Count; ++i)
                    {
                        UserList _u = vc.ListBoxItem.Items[i] as UserList;
                        if (_u != null && _u.AccountID == acc)
                        {
                            vc.ListBoxItem.Items.RemoveAt(i);
                        }
                    }
                });
            }
        }

    }
    public static void CleanUpOnExternalKick(Packet packet, ref bool error) // if we were in a room that we were removed from by the server, then we need to properly leave this room on the client side
    {
        
        int? serverid = packet.GetInt(ref error); if(error == true) return;
        string servername = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        bool boolean = false;
        if (serverid == ChatManager.CurrentSavedServer)
        {
            boolean = true;
            SendPackets.LeaveAllServers();
        }
        ChatManager.SavedServers.RemoveAll(x => x.ID == serverid);
        ChatManager.MyServers.RemoveAll(x => x.name == servername);

        if (boolean == true)
        {

            ChatManager.CurrentSavedServer = null;
            ChatManager.channels.Clear();
            ChatManager.CurrentChannel = 0;
            ChatManager.RoleList.Clear();
            if (ChatManager.my_room_user != null)
                ChatManager.my_room_user.StopTransmission();
            ChatManager.my_room_user = null;
            ChatManager.UserList.Clear();
            ChatManager.UserList_FULL.Clear();
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.ServerDirectoryListBox.Items.Refresh();
            MainWindow.instance.ChatListBox.Items.Clear();
            MainWindow.instance.UserListBox.Items.Refresh();
            MainWindow.instance.ChannelListBox.Items.Clear();
            MainWindow.instance.ChatCover.Visibility = Visibility.Visible;
        });
    }
    public static void CleanUpOnDMErase(Packet packet, ref bool error)// if we were in a dm that we were removed from by the server, then we need to properly leave this room on the client side
    {
        int otherid = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
        restart:;
            for (int ii = 0; ii < MainWindow.instance.DMSListBox.Items.Count; ++ii)
            {
                if (((UserList)MainWindow.instance.DMSListBox.Items[ii]).AccountID == otherid)
                {
                    MainWindow.instance.DMSListBox.Items.RemoveAt(ii);
                    goto restart;
                }
            }
        });
        SendPackets.LeaveAllServers();

        NetworkManager.DMS.RemoveAll(x => x.AccountID == otherid);
        ChatManager.CurrentSavedServer = null;
        ChatManager.channels.Clear();
        ChatManager.CurrentChannel = 0;
        ChatManager.RoleList.Clear();
        if (ChatManager.my_room_user != null)
            ChatManager.my_room_user.StopTransmission();
        ChatManager.my_room_user = null;
        ChatManager.UserList.Clear();
        ChatManager.UserList_FULL.Clear();
        SavedServersOnMemory ss = ChatManager.SavedServers.Find(x => x.ID == -otherid);
        if (ss != null)
        {
            ChatManager.SavedServers.Remove(ss);
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.ServerDirectoryListBox.Items.Refresh();
            MainWindow.instance.ChatListBox.Items.Clear();
            MainWindow.instance.UserListBox.Items.Refresh();
            MainWindow.instance.ChannelListBox.Items.Clear();
            MainWindow.instance.ChatCover.Visibility = Visibility.Visible;
        });

    }
    public static void SendBackCall(Packet packet, ref bool error)
    {
        int acc = packet.GetInt(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ChatManager.PotentialUserInCallWith = acc;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.IncomingCall.Visibility = Visibility.Visible;
            using (System.IO.Stream stream = ESMA.Properties.Resources.ringtonee)
            {
                MainWindow.CallSound = new System.Media.SoundPlayer(stream);
                MainWindow.CallSound.Play();
            }
            MainWindow.instance.IncomingCall_Picture.Source = MainWindow.DefaultProfilePicture;
        });
  
        if (ChatManager.GetUserCacheByID(acc) == null)
        {
            DatabaseCalls.AddUserCache(acc);
        }
        else
        {
            if (ChatManager.GetUserCacheByID(acc) != null && ChatManager.GetUserCacheByID(acc).profilepic != null)
            {
                MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.IncomingCall_Picture.Source = ChatManager.GetUserCacheByID(acc).profilepic;
                    });
                return;
            }
        }
        new Thread(delegate ()
        {
            int tries = 0;
            bool check_cache = true;
            while (check_cache == true)
            {
                Action a = (Action)(() =>
                {
                    check_cache = ChatManager.GetUserCacheByID(acc) == null && NetworkManager.MyAccountID != null || ChatManager.GetUserCacheByID(acc).profilepic == null && NetworkManager.MyAccountID != null;
                });
                MainWindow.instance.RunOnPrimaryActionThread(a);
                //
                tries++;
                if (tries == 10)
                {
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.IncomingCall_Picture.Source = MainWindow.UndefinedSource as ImageSource;
                    });
                    return;
                }
                Thread.Sleep(500);
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.GetUserCacheByID(acc) != null && ChatManager.GetUserCacheByID(acc).profilepic != null)
                {
                    ImageSource source = ChatManager.GetUserCacheByID(acc).profilepic;
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.IncomingCall_Picture.Source = source;
                    });
                }
            });
        }).Start();
    }
    public static void SendCallResponse(Packet packet, ref bool error)
    {
        int acc = packet.GetInt(ref error); if(error == true) return;
        string accname = packet.GetString(ref error); if(error == true) return;
        int port = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (acc == -1)
        {
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.IncomingCall.Visibility = Visibility.Hidden;
                MainWindow.instance.CurrentCallText.Text = "";
                MainWindow.instance.Call.Visibility = ChatManager.CurrentChatWithUser != null ? Visibility.Visible : Visibility.Hidden;
                MainWindow.instance.HangUpCall.Visibility = Visibility.Hidden;
            });
            if (ChatManager.MyTempuserWhileinCall != null && ChatManager.MyTempuserWhileinCall.wave_in != null)
            {
                ChatManager.MyTempuserWhileinCall.StopTransmission();
            }
            ChatManager.PotentialUserInCallWith = null;
            ChatManager.UserInCallWith = null;
            ChatManager.MyTempuserWhileinCall = null;
            MainWindow.PlaySound(NotificationSound.CallDisconnect);
            if (NetworkManager.instance.call_udp.endpoint != null)
            {
                NetworkManager.instance.call_udp.Disconnect();
            }
        }
        else
        {
          
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                string pass = MainWindow.instance.PasswordForRoom.Text;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    ChatManager.selectedroompass = pass;
                    using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                    {
                        byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(pass));
                        ChatManager.selectedroompassasbytes = keys;
                    }
                    ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(pass);
                    ChatManager.selectedencryptionseed = "";
                });
                MainWindow.instance.IncomingCall.Visibility = Visibility.Hidden;
                MainWindow.instance.CurrentCallText.Text = "Currently in a call with " + accname;
                MainWindow.instance.Call.Visibility = Visibility.Hidden;
                MainWindow.instance.HangUpCall.Visibility = Visibility.Visible;

            });
            ChatManager.PotentialUserInCallWith = null;
            ChatManager.UserInCallWith = new UserList(accname, acc);
            ChatManager.MyTempuserWhileinCall = new UserList(NetworkManager.myusername, (int)NetworkManager.MyAccountID);
            if (ChatManager.MyTempuserWhileinCall.wave_in == null && NAudio.Wave.WaveInEvent.DeviceCount > 0)
            {
                ChatManager.MyTempuserWhileinCall.StartTransmittingVoice(); // once i join this channel then i can start transmitting my voice
                ChatManager.MyTempuserWhileinCall.wave_in.StartRecording();
            }
            MainWindow.PlaySound(NotificationSound.CallConnect);
            if (NetworkManager.instance.call_udp.endpoint == null)
            {
                NetworkManager.instance.call_udp.Start(port);
            }
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.EnableorDisableLoading("Calling user...", false);
            try
            {
                if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                    MainWindow.CallPendingSound.Stop();
            }
            catch { }
        });
    }
    public static void CloseCallTimesUp(Packet packet, ref bool error)
    {
        packet.CheckFinalizers(ref error); if (error == true) return;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.IncomingCall.Visibility = Visibility.Hidden;
            MainWindow.instance.EnableorDisableLoading("Calling user...", false);
            if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                MainWindow.CallPendingSound.Stop();
            if (MainWindow.CallSound != null && MainWindow.CallSound.IsLoadCompleted)
                MainWindow.CallSound.Stop();
        });
        ChatManager.PotentialUserInCallWith = null;
    }
    public static void SendBackMessageSpanForSearch(Packet packet, ref bool error)
    {
        int quantity = packet.GetInt(ref error); if(error == true) return;
        int msgid = packet.GetInt(ref error); if(error == true) return;
        int roomid = packet.GetInt(ref error); if(error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return;
        bool saved = packet.GetBool(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ChannelsForTempRoom c = (ChatManager.channels[channel] as ChannelsForTempRoom);
        if (c != null)
        {
            MainWindow.ChatListBoxCount = 0;
            MainWindow.LoadedChatListBoxCount = 0;
            MainWindow.UnconditionalChatListBoxCount = 0;
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.ChatListBox.Items.Clear();
            });
            c.searchedmessages.Clear();
            c.MessagesToFetchedSearch = quantity;
            c.MessagesFetchedSearch = 0;
            SendPackets.ReceivedLoadSelectedMessageFromSearch(msgid, roomid, channel, saved);
        }
    }
    public static List<int> ChannelsKeysAsList()
    {
        var originalkeys = ChatManager.channels.Keys;
        int[] originalmykeys = new int[ChatManager.channels.Keys.Count];
        originalkeys.CopyTo(originalmykeys, 0);

        return originalmykeys.ToList();
    }
    public static List<object> ChannelsAsList()
    {
        var originalkeys = ChatManager.channels.Keys;
        int[] originalmykeys = new int[ChatManager.channels.Keys.Count];
        originalkeys.CopyTo(originalmykeys, 0);
        List<object> _channels = new List<object>();

        for (int ic = 0; ic < originalmykeys.Length; ++ic)
        {
            if (ChatManager.channels[originalmykeys[ic]].GetType() == typeof(ChannelsForTempRoom))
            {
                _channels.Add(ChatManager.channels[originalmykeys[ic]] as ChannelsForTempRoom);
            }
            if (ChatManager.channels[originalmykeys[ic]].GetType() == typeof(VoiceChannelsForTempRoom))
            {
                _channels.Add(ChatManager.channels[originalmykeys[ic]] as VoiceChannelsForTempRoom);
            }
        }
        return _channels;
    }
    public static void SendBackSavedServerChannels(Packet packet, ref bool error)
    {
        byte[] bytes = packet.GetBytes( ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (ChatManager.CurrentSavedServer == null)
        {
            return;
        }
        string channelstring = System.Text.Encoding.UTF8.GetString(bytes);
        List<string> csplit = channelstring.Split('❷').ToList();


        for (int i = 0; i < csplit.Count; ++i)
        {
            if (csplit[i].Contains("❶") == false)
            {
                csplit.RemoveAt(i);
                continue;
            }
            string[] internalcsplit = csplit[i].Split('❶');
            int channelid = int.Parse(internalcsplit[0]);
            int channeltype = int.Parse(internalcsplit[1]);
            string channelname = internalcsplit[2];
            string[] rolelist = internalcsplit[3].Split('/');

            List<object> _channels = ChannelsAsList();
            if (ChatManager.channels.ContainsKey(channelid) == false)
            {
                if (channeltype == 0)
                {
                    ChannelsForTempRoom cftr = new ChannelsForTempRoom(channelname, channelid);
                    cftr.initialized = true;
                    for (int ii = 0; ii < rolelist.Length; ++ii)
                    {
                        if (String.IsNullOrWhiteSpace(rolelist[ii]) == false)
                        {
                            cftr.Roles.Add(int.Parse(rolelist[ii]));
                        }
                    }
                    cftr.read_only = int.Parse(internalcsplit[4]) == 0 ? false : true;
                    cftr.incoming_users = int.Parse(internalcsplit[5]) == 0 ? false : true;
                    ChatManager.channels.Add(channelid, cftr);
                    if (ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer) != null)
                    {
                        ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer).channels.Add(channelid, cftr);
                    }
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        int? found = null;
                        for (int ii = 0; ii < MainWindow.instance.ChannelListBox.Items.Count; ++ii)
                        {
                            if (MainWindow.instance.ChannelListBox.Items[ii].GetType() == typeof(ChannelsForTempRoom) && (MainWindow.instance.ChannelListBox.Items[ii] as ChannelsForTempRoom).key == cftr.key)
                            {
                                found = ii;
                            }
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.my_room_user._Roles.Any(x => rolelist.ToList().Contains(x.ToString())))
                            {
                                if (found == null)
                                {
                                    MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        MainWindow.instance.ChannelListBox.Items.Add(cftr);
                                    });
                                }
                            }
                            else if (found != null)
                            {
                                MainWindow.instance.Dispatcher.Invoke(() =>
                                {
                                    MainWindow.instance.ChannelListBox.Items.RemoveAt((int)found);
                                });
                                if (ChatManager.CurrentChannel == cftr.key)
                                {
                                    MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        foreach (object item in MainWindow.instance.ChannelListBox.Items)
                                        {
                                            if (item as ChannelsForTempRoom != null)
                                            {
                                                MainWindow.instance.ChannelListBox.SelectedItem = item as ChannelsForTempRoom;
                                                break;
                                            }
                                        }
                                    });
                                }
                            }
                        });
                    });
                }
                if (channeltype == 1)
                {
                    VoiceChannelsForTempRoom cftr = new VoiceChannelsForTempRoom(channelname, channelid);
                    for (int ii = 0; ii < rolelist.Length; ++ii)
                    {
                        if (String.IsNullOrWhiteSpace(rolelist[ii]) == false)
                        {
                            cftr.Roles.Add(int.Parse(rolelist[ii]));
                        }
                    }
                    cftr.read_only = int.Parse(internalcsplit[4]) == 0 ? false : true;
                    ChatManager.channels.Add(channelid, cftr);
                    if (ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer) != null)
                    {
                        ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer).channels.Add(channelid, cftr);
                    }
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        int? found = null;
                        for (int ii = 0; ii < MainWindow.instance.ChannelListBox.Items.Count; ++ii)
                        {
                            if (MainWindow.instance.ChannelListBox.Items[ii].GetType() == typeof(VoiceChannelsForTempRoom) && (MainWindow.instance.ChannelListBox.Items[ii] as VoiceChannelsForTempRoom).key == cftr.key)
                            {
                                found = ii;
                            }
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.my_room_user._Roles.Any(x => rolelist.ToList().Contains(x.ToString())))
                            {
                                if (found == null)
                                {
                                    MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        MainWindow.instance.ChannelListBox.Items.Add(cftr);
                                    });
                                }
                            }
                            else if (found != null)
                            {
                                MainWindow.instance.Dispatcher.Invoke(() =>
                                {
                                    MainWindow.instance.ChannelListBox.Items.RemoveAt((int)found);
                                });

                            }
                         
                        });
                    });
                }
            }
            else
            {
                if (channeltype == 0)
                {
                    ChannelsForTempRoom cftr = ChatManager.channels[channelid] as ChannelsForTempRoom;
                    cftr.read_only = int.Parse(internalcsplit[4]) == 0 ? false : true;
                    cftr.incoming_users = int.Parse(internalcsplit[5]) == 0 ? false : true;
                    cftr.ChannelName = channelname;
                    cftr.Roles.Clear();
                    for (int ii = 0; ii < rolelist.Length; ++ii)
                    {
                        if (String.IsNullOrWhiteSpace(rolelist[ii]) == false)
                        {
                            cftr.Roles.Add(int.Parse(rolelist[ii]));
                        }
                    }
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        int? found = null;
                        for (int ii = 0; ii < MainWindow.instance.ChannelListBox.Items.Count; ++ii)
                        {
                            if (MainWindow.instance.ChannelListBox.Items[ii].GetType() == typeof(ChannelsForTempRoom) && (MainWindow.instance.ChannelListBox.Items[ii] as ChannelsForTempRoom).key == cftr.key)
                            {
                                found = ii;
                            }
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.my_room_user._Roles.Any(x => rolelist.ToList().Contains(x.ToString())))
                            {
                                if (found == null)
                                {
                                    int ind = _channels.OrderBy(x => (x as ChannelRoot).key).ToList().FindIndex(x => ((ChannelRoot)x).key == cftr.key);
                                    MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        try
                                        {
                                            MainWindow.instance.ChannelListBox.Items.Insert(ind, cftr);
                                        }
                                        catch { }
                                    });
                                }
                            }
                            else if (found != null)
                            {
                                MainWindow.instance.Dispatcher.Invoke(() =>
                                {
                                    MainWindow.instance.ChannelListBox.Items.RemoveAt((int)found);
                                });
                                if (ChatManager.CurrentChannel == cftr.key)
                                {
                                    MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        foreach (object item in MainWindow.instance.ChannelListBox.Items)
                                        {
                                            if (item as ChannelsForTempRoom != null)
                                            {
                                                MainWindow.instance.ChannelListBox.SelectedItem = item as ChannelsForTempRoom;
                                                break;
                                            }
                                        }
                                    });
                                }
                            }
                        });
                    });
                }
                if (channeltype == 1)
                {
                    VoiceChannelsForTempRoom cftr = ChatManager.channels[channelid] as VoiceChannelsForTempRoom;
                    cftr.read_only = int.Parse(internalcsplit[4]) == 0 ? false : true;
                    cftr.ChannelName = channelname;
                    cftr.Roles.Clear(); 
                    for (int ii = 0; ii < rolelist.Length; ++ii)
                    {
                        if (String.IsNullOrWhiteSpace(rolelist[ii]) == false)
                        {
                            cftr.Roles.Add(int.Parse(rolelist[ii]));
                        }
                    }
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        int? found = null;
                        for (int ii = 0; ii < MainWindow.instance.ChannelListBox.Items.Count; ++ii)
                        {
                            if (MainWindow.instance.ChannelListBox.Items[ii].GetType() == typeof(VoiceChannelsForTempRoom) && (MainWindow.instance.ChannelListBox.Items[ii] as VoiceChannelsForTempRoom).key == cftr.key)
                            {
                                found = ii;
                            }
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.my_room_user._Roles.Any(x => rolelist.ToList().Contains(x.ToString())))
                            {
                                if (found == null)
                                {
                                    MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        try
                                        {
                                            MainWindow.instance.ChannelListBox.Items.Insert(_channels.OrderBy(x => (x as ChannelRoot).key).ToList().FindIndex(x => ((ChannelRoot)x).key == cftr.key), cftr);
                                        }
                                        catch { }
                                    });
                                }
                            }
                            else if (found != null)
                            {
                                MainWindow.instance.Dispatcher.Invoke(() =>
                                {
                                    MainWindow.instance.ChannelListBox.Items.RemoveAt((int)found);
                                });
                              
                            }
                           
                        });
                    });
                }
            }
        }
        {
            var keys = ChatManager.channels.Keys;
            int[] mykeys = new int[ChatManager.channels.Keys.Count];
            keys.CopyTo(mykeys, 0);
            if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel  != null && ChatManager.channels.ContainsKey( ChatManager.my_room_user.CurrentVoiceChannel ))
            {
                SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
            }
            foreach (int k in mykeys)
            {
                if (csplit.Any(x => int.Parse(x.Split('❶')[0]) == k) == false)
                {
                    if (ChatManager.channels[k] as ChannelsForTempRoom != null)
                    {
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow.instance.ChannelListBox.Items.Remove(ChatManager.channels[k] as ChannelsForTempRoom);
                        });
                    }
                    if (ChatManager.channels[k] as VoiceChannelsForTempRoom != null)
                    {
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow.instance.ChannelListBox.Items.Remove(ChatManager.channels[k] as VoiceChannelsForTempRoom);
                        });
                    }
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.ChannelsEditListBox.Items.Refresh();
                    });
                    ChatManager.channels.Remove(k);
                }
            }
        }
        {
            var keys = ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer).channels.Keys;
            int[] mykeys = new int[ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer).channels.Keys.Count];
            keys.CopyTo(mykeys, 0);
            if (ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer) != null)
            {
                foreach (int k in mykeys)
                {
                    if (csplit.Any(x => int.Parse(x.Split('❶')[0]) == k) == false)
                    {
                        ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer).channels.Remove(k);
                    }
                }
            }
        }
        MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.ChannelListBox_SelectionChanged(null, null);
        });
        if (ChatManager.channels.ContainsKey(ChatManager.CurrentChannel) == false)
        {
            MainWindow.instance.Dispatcher.Invoke(() =>
            {
                foreach (object item in MainWindow.instance.ChannelListBox.Items)
                {
                    if (item as ChannelsForTempRoom != null)
                    {
                        MainWindow.instance.ChannelListBox.SelectedItem = item as ChannelsForTempRoom;
                        break;
                    }
                }
            });
        }
        MainWindow.instance.Dispatcher.Invoke(() =>
        {
            if (MainWindow.instance.ManageChannels.Visibility == Visibility.Visible)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    List<object> templist = ChannelsAsList();
                    templist.Reverse();
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.ChannelsEditListBox.ItemsSource = templist;
                        MainWindow.instance.ChannelsEditListBox.Items.Refresh();
                    });
                 
                    MainWindow.instance.RefreshChannelRoleListBox();
                });
                MainWindow.AddingRoleToChannel = false;
            }
        });
    }
    public static void SendToUserTheirMSGSearch(Packet packet, ref bool error)
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            string TheMsgs = System.Text.Encoding.UTF8.GetString(bytes);
            List<Message> m = new List<Message>();
            if (String.IsNullOrWhiteSpace(TheMsgs) == false)
            {
                string[] thesplit = TheMsgs.Split('❷');
                for (int i = 0; i < thesplit.Length; i++)
                {
                    if (thesplit[i].Contains("❶"))
                    {
                        string[] internalsplit = thesplit[i].Split('❶');
                        m.Add(new Message(int.Parse(internalsplit[2]), internalsplit[1], Methods.DecryptString(internalsplit[3], ChatManager.selectedroompassasbytes) ?? "Unable to decrypt message.", int.Parse(internalsplit[4]), true, true, internalsplit[5]));
                    }
                }
            }
            MainWindow.instance.MessageHistoryListBox.ItemsSource = m;
            MainWindow.instance.MessageHistoryListBox.Items.Refresh();
        });
    }
    public static void SendCloseLoading(Packet packet, ref bool error)
    {
        string msg = packet.GetString(ref error); if(error == true) return;
        int task = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        switch (task)
        {
            case 1:
                ChatManager.StillGettingVerificationOfJoinRoom = false;
                MainWindow.checkinghash = false;
                break;
            case 3:
                if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                    MainWindow.CallPendingSound.Stop();
                if (MainWindow.CallSound != null && MainWindow.CallSound.IsLoadCompleted)
                    MainWindow.CallSound.Stop();
                break;
            case 4:
               
                break;
            case 5:
                {
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        SendPackets.RequestOpenProfile(ChatManager.CurrentSavedServer, MainWindow.CurPLWindow);
                    });
                 
                }
                break;
            default:
                break;
        }

        MainWindow.instance.EnableorDisableLoading(msg, false);
    }
    public static void UserDisconnected(Packet packet, ref bool error)
    {
        int id = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (NetworkManager.GetGlobalUser(id) != null)
        {
            int account_ID = NetworkManager.GetGlobalUser(id).account_ID;
            if (ChatManager.CurrentChatWithUser != null)
            {
                if ((int)ChatManager.CurrentChatWithUser == account_ID)
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        MainWindow.instance.EnableorDisableLoading("Calling user...", false);
                        try
                        {
                            if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                                MainWindow.CallPendingSound.Stop();
                        }
                        catch { }
                    });
                }
            }
            if (ChatManager.UserList_FULL != null && ChatManager.UserList_FULL.Any(x => x.AccountID == (NetworkManager.GetGlobalUser(id) ?? new GlobalUser()).account_ID))
            {
                NetworkManager.global_users.Remove(NetworkManager.GetGlobalUser(id)); // Removes user from global UserList
                RefreshUserList();
            }
            else
            {
                NetworkManager.global_users.Remove(NetworkManager.GetGlobalUser(id)); // Removes user from global UserList
            }
        }
    }
    public static void ReceivedUDPCheck(Packet packet, ref bool error)
    {
        packet.CheckFinalizers(ref error); if (error == true) return;
    }
    public static void TCPCheck(Packet packet, ref bool error)
    {
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (NetworkManager.instance.MadeFullConnection == false)
        {
            NetworkManager.instance.MadeFullConnection = true;
        }
        NetworkManager.Ping_Timer = 0;
    }
    public static void SendbackRoles(Packet packet, ref bool error)
    {
        byte[] rolesbytes = packet.GetBytes( ref error); if (error == true) return;
        string roles = System.Text.Encoding.UTF8.GetString(rolesbytes);
        int removalid = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (removalid != -1)
        {
            for (int i = 0; i < ChatManager.UserList.Count; ++i)
            {
                if (ChatManager.UserList[i]._Roles.Contains(removalid))
                {
                    ChatManager.UserList[i]._Roles.Remove(removalid);
                }
            }
        }


        string[] roleslist = roles.Split('❷');
        ChatManager.RoleList.Clear();
        for (int i = 0; i < roleslist.Length; ++i)
        {
            if (roleslist[i].Contains("❶"))
            {
                string[] roleslistinternal = roleslist[i].Split('❶');
                ChatManager.RoleList.Add(new RolesList(int.Parse(roleslistinternal[0]), roleslistinternal[1], new int[] { int.Parse(roleslistinternal[2]), int.Parse(roleslistinternal[3]), int.Parse(roleslistinternal[4]), int.Parse(roleslistinternal[5]), int.Parse(roleslistinternal[6]), int.Parse(roleslistinternal[7]) }, roleslistinternal[8], int.Parse(roleslistinternal[9])));
            }
        }
        MainWindow.instance.UpdateRoleUI();
        RefreshUserList();
        MainWindow.RefreshProfileNameColor();
        if (ChatManager.my_room_user != null)
        {
            bool found = false;
            for (int i = 0; i < ChatManager.my_room_user._Roles.Count; ++i)
            {
                RolesList temp = ChatManager.RoleList.Find(x => x.id == ChatManager.my_room_user._Roles[i]);
                if (temp != null && temp.powers[4] == 1)
                {
                    found = true;
                }
            }
            if (found == false)
            {
                MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.CreateChannel.Visibility = Visibility.Hidden;
                });
                return;
            }
        }
        MainWindow.instance.Dispatcher.Invoke(() =>
        {
            if (MainWindow.instance.ManageRoles.Visibility == Visibility.Visible)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    bool found = false;
                    if (ChatManager.my_room_user != null)
                    {
                        for (int i = 0; i < ChatManager.my_room_user._Roles.Count; ++i)
                        {
                            RolesList temp = ChatManager.RoleList.Find(x => x.id == ChatManager.my_room_user._Roles[i]);
                            if (temp != null && temp.powers[5] == 1)
                            {
                                found = true;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                    if (found == false)
                    {
                        MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            MainWindow.instance.ManageRoles.Visibility = Visibility.Hidden;
                        });
                        return;
                    }
                    List<RolesList> rolelist_by_precedence = new List<RolesList>(ChatManager.RoleList.OrderBy(x => x.precedence));
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        if (MainWindow.instance.RolesEditListBox.SelectedItem != null)
                        {
                            MainWindow.LastSelectedRoleID = (MainWindow.instance.RolesEditListBox.SelectedItem as RolesList).id;
                        }
                        else
                        {
                            MainWindow.LastSelectedRoleID = -1;
                        }

                        MainWindow.instance.RolesEditListBox.ItemsSource = rolelist_by_precedence;
                        MainWindow.instance.RolesEditListBox.Items.Refresh();
                        bool did = false;
                        foreach (RolesList rl in MainWindow.instance.RolesEditListBox.Items)
                        {
                            if (rl.id == MainWindow.LastSelectedRoleID)
                            {
                                did = true;
                                MainWindow.instance.RolesEditListBox.SelectedItem = rl;
                            }
                        }
                        if (did == false)
                        {
                            MainWindow.instance.RolesEditListBox.SelectedIndex = 0;
                        }
                        RolesList _role = MainWindow.instance.RolesEditListBox.SelectedItem as RolesList;
                        if (_role != null)
                        {

                            MainWindow.instance.editkick.IsChecked = _role.powers[0] == 0 ? false : true;
                            MainWindow.instance.editmute.IsChecked = _role.powers[1] == 0 ? false : true;
                            MainWindow.instance.editban.IsChecked = _role.powers[2] == 0 ? false : true;
                            MainWindow.instance.editdelete.IsChecked = _role.powers[3] == 0 ? false : true;
                            MainWindow.instance.editmanagechannels.IsChecked = _role.powers[4] == 0 ? false : true;
                            MainWindow.instance.editmanageroles.IsChecked = _role.powers[5] == 0 ? false : true;
                        }
                    });
                });
            }
        });
    }
    public static void AddOrRemoveUserFromList(Packet packet, ref bool error)
    {
        int type = packet.GetInt(ref error); if(error == true) return;
        string name = packet.GetString(ref error); if(error == true) return;
        int pid = packet.GetInt(ref error); if(error == true) return;
        int account_ID = packet.GetInt(ref error); if(error == true) return;
        string roles = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;

        if (type == 1) //Add user to temporary room userlist
        {
            if (ChatManager.GetChatUserByID(account_ID) == null)
            {
                UserList _u = new UserList(name, account_ID);
                foreach (string s in roles.Split('/'))
                {
                    if (String.IsNullOrWhiteSpace(s) == false)
                    {
                        _u._Roles.Add(int.Parse(s));
                    }
                }
                ChatManager.UserList.Add(_u);

                if (ChatManager.UserList_FULL != null && ChatManager.UserList_FULL.Any(x => x.AccountID == account_ID) == false)
                {
                    UserList _u_full = new UserList(name, account_ID);
                    foreach (string s in roles.Split('/'))
                    {
                        if (String.IsNullOrWhiteSpace(s) == false)
                        {
                            _u_full._Roles.Add(int.Parse(s));
                        }
                    }
                    ChatManager.UserList_FULL.Add(_u_full);
                }

                MainWindow.PlaySound(NotificationSound.Online);
                if (ChatManager.UserList_FULL != null)
                {
                    UserList _u_full = ChatManager.UserList_FULL.Find(x => x.AccountID == account_ID);
                    if (_u_full != null)
                    {
                        ChatManager.UserList_FULL[ChatManager.UserList_FULL.IndexOf(_u_full)] = _u;
                    }
                }
                if (_u.AccountID == NetworkManager.MyAccountID) // If this user has the same server id as you - its you.
                {
                    _u.me = true;
                    ChatManager.my_room_user = _u;
                }
            }
        }
        if (type == 2) //Remove user from temporary room userlist
        {
            if (ChatManager.GetChatUserByID(account_ID) != null)
            {
                UserList _u = ChatManager.GetChatUserByID(account_ID);
                _u.StopTransmission();
                ChatManager.UserList.Remove(_u); // remove user from room userlist
                int[] keyarray = new int[ChatManager.channels.Keys.Count];
                ChatManager.channels.Keys.CopyTo(keyarray, 0);
                foreach (int c in keyarray)
                {
                    if (ChatManager.channels[c].GetType() == typeof(VoiceChannelsForTempRoom))
                    {
                        VoiceChannelsForTempRoom vc = (ChatManager.channels[c] as VoiceChannelsForTempRoom);
                        if (vc.UsersInRoom.Contains(_u))
                        {
                            vc.UsersInRoom.Remove(_u); //remove this pl from any channel userlist  
                        }
                    }
                }
            }
        }
        RefreshUserList();
    }
    public static void SendbackRoomCreationSuccess(Packet packet, ref bool error)
    {
        string ms = "Creating room...";
        int roomid = packet.GetInt(ref error); if(error == true) return;
        byte[] channelstringbytes = packet.GetBytes( ref error); if (error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return;
        int port = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ChatManager.SavedServers.RemoveAll(x => x.ID == -1);
        ChatManager.CurrentSavedServer = null; // since this is a temp room creation, this value will be null
        ChatManager.CurrentChannel = channel;
        ChatManager.CurTempRoom = roomid;
        ChatManager.channels.Clear();
        ChatManager.CurrentChatWithUser = null;
        ChatManager.CurrentChatWithUserKey = "";
        ChatManager.UserList_FULL = ChatManager.UserList;
        RefreshUserList();

        NetworkManager.instance.udp.Start(port);

        MainWindow.instance.UpdateRoleUI();
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.Call.Visibility = Visibility.Hidden;
            ESMA.MainWindow.ChatList.Items.Clear();
            ESMA.MainWindow.ChannelList.Items.Clear();
        });
        MainWindow.UnconditionalChatListBoxCount = 0;
        MainWindow.ChatListBoxCount = 0;
        MainWindow.LoadedChatListBoxCount = 0;


        string channelstring = System.Text.Encoding.UTF8.GetString(channelstringbytes);
        string[] channeldatachunk = channelstring.Split('❷').Reverse().ToArray(); // split at each individual channel

        for (int i = 0; i < channeldatachunk.Length; ++i)
        {
            if (channeldatachunk[i].Contains("❶"))
            {
                string[] internalstuff = channeldatachunk[i].Split('❶'); // split to get datachunk for channel
                if (internalstuff.Length > 4) // if this is a text channel
                {
                    ChannelsForTempRoom c = new ChannelsForTempRoom(internalstuff[1], int.Parse(internalstuff[0]));
                    ChatManager.channels.Add(int.Parse(internalstuff[0]), c);
                    c.MessagesToFetched = int.Parse(internalstuff[0]) == channel ? int.Parse(internalstuff[4]) : 0;
                    c.MessagesToFetchTotal = int.Parse(internalstuff[3]);
                    if (c.MessagesToFetchTotal == 0)
                    {
                        c.initialized = true;
                    }
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        ESMA.MainWindow.ChannelList.Items.Add(c);
                    });
                }
                else // then this is a voice channel
                {
                    VoiceChannelsForTempRoom v = new VoiceChannelsForTempRoom(internalstuff[1], int.Parse(internalstuff[0]));
                    ChatManager.channels.Add(int.Parse(internalstuff[0]), v);
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        ESMA.MainWindow.ChannelList.Items.Add(v);
                    });
                }
            }
        }
            (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).initialized = true; //the default channel has been initialized and the first wave of messages have been received. The other channels remain uninitalized with no received messages.
        MainWindow.instance.ChangeTheSelectionOfChannelListBox(channel);
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.ServerBrowserWindow.Visibility = Visibility.Hidden;
            MainWindow.instance.ChatCover.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendGrid.Visibility = Visibility.Hidden;
            MainWindow.instance.ChannelCover.Visibility = Visibility.Hidden;
            MainWindow.instance.DMSListBoxScroll.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendHud.Visibility = Visibility.Hidden;
        });
        MainWindow.instance.CreateSavedServerFromCurrent(true);


        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
        {
            MainWindow.instance.MessageBoxShow("You have created a private chat room. The id of this room is " + roomid);
        }));
        MainWindow.instance.EnableorDisableLoading(ms, false);
    }
    public static void SendbackRoomRoot(Packet packet, ref bool error)
    {

        int roomid = packet.GetInt(ref error); if(error == true) return;
        bool issaved = packet.GetBool(ref error); if(error == true) return;
        string root = packet.GetString(ref error); if(error == true) return;
        bool onlycheck = packet.GetBool(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;

        try //check if decryption hash works for room trying to join
        {
            string core = Methods.DecryptString(root, ChatManager.selectedroompassasbytesprejoin);
            if (core == null)
            {
                throw new Exception();
            }
            string[] SplitArray = core.Split(' ');
            for (int i = 0; i < SplitArray.Length; i++) //checks if password is correct, not by comparing anything, but seeing if it can rassemble a string array (using the same words used in encryption) back to a readable form
            {
                if (String.IsNullOrWhiteSpace(SplitArray[i]) == false)
                {
                    if (WordVerififier.Awords.Contains(SplitArray[i]) == false)
                    {
                        throw new Exception();
                    }
                }
            }
        }
        catch //wrong decryption hash
        {
            MainWindow.instance.EnableorDisableLoading("Joining room...", false);
            ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
            {
                MainWindow.instance.MessageBoxShow("Joining room failed. Unable to decrypt room info with given decryption key.");
            }));
            MainWindow.checkinghash = false;
            ChatManager.StillGettingVerificationOfJoinRoom = false;
            return;
        }
        SendPackets.JoinRoomKeySuccess(roomid, issaved);

    }
    public static void ReceiveNewProfilePicture(Packet packet, ref bool error)
    {
        int account_ID = packet.GetInt(ref error); if(error == true) return;
        string md5 = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (ChatManager.GetUserCacheByID(account_ID) != null)
        {
            ChatManager.GetUserCacheByID(account_ID).profilemd5 = md5;
            ChatManager.GetUserCacheByID(account_ID).SaveProfilePicToCacheInFiles(0);
        }
    }
    public static void SendBackProfilePicture(Packet packet, ref bool error) 
    {
        int acc = packet.GetInt(ref error); if(error == true) return;
        string md5 = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (String.IsNullOrWhiteSpace(md5))
        {
            return;
        }
        ChatManager.UserCache.Add(new UserDataCache(acc, md5));
    }
    public static void OpenUserProfile(Packet packet, ref bool error) 
    {
        int openedacc = packet.GetInt(ref error); if(error == true) return;
        string bio = packet.GetString(ref error); if(error == true) return;
        string media = packet.GetString(ref error); if(error == true) return;
        string name = packet.GetString(ref error); if(error == true) return;
        string creation = packet.GetString(ref error); if(error == true) return;
        string friends = packet.GetString(ref error); if(error == true) return;
        string roles = packet.GetString(ref error); if(error == true) return;
        string reqroles = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        DatabaseCalls.OpenUserProfile(openedacc, bio, media, name, creation, friends, roles, reqroles);
    }
    public static void SendSuccessInProfileSave(Packet packet, ref bool error)
    {
        packet.CheckFinalizers(ref error); if (error == true) return;
        SendPackets.RequestOpenProfile(ChatManager.CurrentSavedServer, (int)NetworkManager.MyAccountID);
    }
    public static void SendToUserTheirRooms(Packet packet, ref bool error) //What rooms are we members of
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        string datachunk = System.Text.Encoding.UTF8.GetString(bytes);
        string[] split = datachunk.Split('❷');
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            ChatManager.MyServers.Clear();
            ChatManager.MyServers.Add(new ServerDirectoryElements(-1, "+", "")); // <- add server button
            ChatManager.MyServers.Add(new ServerDirectoryElements(-2, "+", "")); // <- add friends button
            for (int i = 0; i < split.Length; ++i)
            {
                if (split[i].Contains("❶"))
                {
                    string[] internalsplit = split[i].Split('❶');
                    ChatManager.MyServers.Add(new ServerDirectoryElements(int.Parse(internalsplit[0]), internalsplit[1], internalsplit[2]));
                }
            }
            MainWindow.instance.ServerDirectoryListBox.ItemsSource = ChatManager.MyServers;
            MainWindow.instance.ServerDirectoryListBox.Items.Refresh();//refresh UI
        });
    }
    public static void RoomExists(Packet packet, ref bool error)
    {
        int roomid = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
    }
    public static void SendBackPMHash(Packet packet, ref bool error)
    {
        string hash = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;

        string theactualkey = Methods.AsymetricalDecryption(hash, ChatManager.MyUserPrivateKey);
        if (theactualkey != null)
        {
            ChatManager.selectedroompass = theactualkey;
            using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
            {
                byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(theactualkey));
                ChatManager.selectedroompassasbytes = keys;
            }
            ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(theactualkey);
            ChatManager.selectedencryptionseed = "";

            ChannelsForTempRoom channel = ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom;
            if (channel.MessagesToFetched > 0) // if we get messages to receive on initialization, then check this boolean
            {
                ESMA.ChatManager.StillFetchingMessages = true;
                ChatManager.StillLoadingMessages = true;
                SendPackets.RequestMessagesAfterPMVerify(); // get the messages now since i joined
            }
            else
            {
                ChatManager.StillLoadingMessages = false;
                ChatManager.StillFetchingMessages = false;
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                });
            }

            MainWindow.instance.UpdateRoleUI();
            MainWindow.instance.CreateSavedServerFromCurrent(false);
        }
        else
        {
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                MainWindow.instance.MessageBoxShow("Joining room failed. Unable to decrypt haskey.");
            });
        }
    }
    public static void PMReady(Packet packet, ref bool error)
    {
        int countofmessagestotal = packet.GetInt(ref error); if(error == true) return;
        int countofmessagestoreceive = packet.GetInt(ref error); if(error == true) return;
        int _accountid = packet.GetInt(ref error); if(error == true) return;
        string _accountname = packet.GetString(ref error); if(error == true) return;
        ChatManager.CurrentChatWithUserKey = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.Call.Visibility = ChatManager.UserInCallWith == null ? Visibility.Visible : Visibility.Hidden;
            MainWindow.instance.HangUpCall.Visibility = ChatManager.UserInCallWith != null ? Visibility.Visible : Visibility.Hidden;
            MainWindow.instance.ChatListBox.Items.Clear();
            MainWindow.instance.ChatCover.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendGrid.Visibility = Visibility.Hidden;
        });
        ChatManager.CurrentChatWithUser = _accountid;
        ChatManager.SavedServers.RemoveAll(x => x.ID == -1);
        ChatManager.CurrentSavedServer = null;
        ChatManager.CurTempRoom = null;
        ChatManager.channels.Clear();
        ChatManager.channels.Add(0, new ChannelsForTempRoom("Conversation", 0));
        ChatManager.CurrentChannel = 0;
        ChatManager.StillGettingVerificationOfJoinRoom = false;
        ChannelsForTempRoom channel = ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom;
        channel.MessagesToFetchTotal = countofmessagestotal;
        channel.MessagesFetched = 0;
        channel.MessagesToFetched = countofmessagestoreceive;
        MainWindow.UnconditionalChatListBoxCount = 0;
        MainWindow.LoadedChatListBoxCount = 0;
        MainWindow.ChatListBoxCount = 0;
        ChatManager.UserList_FULL = new List<UserList>();
        ChatManager.UserList_FULL.Add(new UserList(NetworkManager.myusername, (int)NetworkManager.MyAccountID)); // add me
        ChatManager.UserList_FULL.Add(new UserList(_accountname, _accountid)); // add other user
        RefreshUserList();
        for (int i = 0; i < ChatManager.SavedServers.Count; ++i)
        {
            int[] keyarray = new int[ChatManager.SavedServers[i].channels.Keys.Count];
            ChatManager.SavedServers[i].channels.Keys.CopyTo(keyarray, 0);
            foreach (int k in keyarray)
            {
                if ((ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom) == null)
                {
                    continue;
                }
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).messages.Clear();
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).searchedmessages.Clear();
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).MessagesFetched = 0;
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).MessagesFetchedSearch = 0;
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).MessagesToFetched = 0;
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).MessagesToFetchedSearch = 0;
                (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom).MessagesToFetchTotal = 0;
            }
        }
        //get the keys
        string password = "";
        for (int i = 0; i < 5; ++i)
        {
            password += WordVerififier.Awords[Methods.RandomRange.Next(0, WordVerififier.Awords.Length)] + Methods.RandomRange.Next(0, 9999);
        }

        string myencryptedkey = Methods.AsymetricalEncryption(password, ChatManager.MyUserPublicKey);
        string otherusersencryptedkey = Methods.AsymetricalEncryption(password, ChatManager.CurrentChatWithUserKey);
        SendPackets.ReadyForPMDatabaseWrite(_accountid, myencryptedkey, otherusersencryptedkey);
    }

    public static void RefreshUserList()
    {

        if (ChatManager.UserList_FULL != null && ChatManager.UserList_FULL.Count > 0)
        {

            foreach (int k in ChannelsKeysAsList())
            {
                VoiceChannelsForTempRoom vc = ChatManager.channels[k] as VoiceChannelsForTempRoom;
                if (vc != null)
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < vc.ListBoxItem.Items.Count; ++i)
                        {
                            if (ChatManager.UserList.Contains(vc.ListBoxItem.Items[i] as UserList) == false)
                            {
                                vc.ListBoxItem.Items.RemoveAt(i);
                            }
                        }
                    });
                }
            }
            for (int i = 0; i < ChatManager.UserList_FULL.Count; ++i)
            {
                string thecode = (ChatManager.RoleList.Find(x => ChatManager.UserList_FULL[i]._Roles.Count > 0 && x.id == (ChatManager.UserList_FULL[i]._Roles.OrderBy(xx => (ChatManager.RoleList.Find(xxx => xxx.id == xx) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).precedence).ToList()[0])) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).Hex;
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    ChatManager.UserList_FULL[i].TextColor = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(thecode));
                });
                ChatManager.UserList_FULL[i].ActivityColor = ((ChatManager.UserList.Any(x => x.AccountID == ChatManager.UserList_FULL[i].AccountID) == true) ? System.Windows.Media.Brushes.LightGreen : (NetworkManager.global_users.Any(x => x.account_ID == ChatManager.UserList_FULL[i].AccountID) == true ? System.Windows.Media.Brushes.Cyan : System.Windows.Media.Brushes.Gray));
            }
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.UserList.ItemsSource = new List<UserList>(ChatManager.UserList_FULL).OrderByDescending(x => NetworkManager.global_users.Any(xx => xx.account_ID == x.AccountID)).OrderByDescending(x => ChatManager.UserList.Any(xx => xx.AccountID == x.AccountID)).ToList();
                MainWindow.UserList.Items.Refresh();
            });
        }
        else
        {
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.UserList.ItemsSource = null;
            });
        }
    }
    public static void SendBackRoomLeftSuccess(Packet packet, ref bool error)
    {
        packet.CheckFinalizers(ref error); if (error == true) return;
        ChatManager.CurrentChannel = -1;
        ChatManager.CurTempRoom = null;
        ChatManager.CurrentSavedServer = null;
        ChatManager.my_room_user = null;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.EnableorDisableLoading("Leaving current room...", false);
        });
    }
    public static void SendBackFriends(Packet packet, ref bool error)
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        string friends = System.Text.Encoding.UTF8.GetString(bytes);
        string[] split = friends.Split('❷');
        int populatewhere = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (populatewhere == 2 || populatewhere == 1) // for friends
        {
            for (int i = 0; i < split.Length; ++i)
            {
                if (split[i].Contains("❶"))
                {
                    string[] internalsplit = split[i].Split('❶');
                    UserList _u = NetworkManager.Friends.Find(x => x.AccountID == int.Parse(internalsplit[0]));
                    if (_u == null)
                    {
                        NetworkManager.Friends.Add(new UserList(internalsplit[1], int.Parse(internalsplit[0])));
                    }
                }
            }
            for (int i = 0; i < NetworkManager.Friends.Count; ++i)
            {
                bool FoundIdInNewReturn = split.ToList().Any(x => x.Contains("❶") && (int.Parse(x.Split('❶')[0])) == NetworkManager.Friends[i].AccountID);
                if (FoundIdInNewReturn == false)
                {
                    NetworkManager.Friends.RemoveAt(i);
                }
            }
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.EnableorDisableLoading("Loading friends...", false);
                if (populatewhere == 1)
                {
                    MainWindow.instance.AddFriendTab.Visibility = Visibility.Hidden;
                    MainWindow.instance.AllFriendsTab.Visibility = Visibility.Visible;
                    MainWindow.instance.PendingFriendsTab.Visibility = Visibility.Hidden;
                    MainWindow.instance.AllFriendsListBox.Items.Refresh();
                }
                MainWindow.instance.PendingFriendsListBox.Items.Refresh();
                MainWindow.instance.AllFriendsListBox.Items.Refresh();
            });
        }
        if (populatewhere == 0) // populate dms
        {
            for (int i = 0; i < split.Length; ++i)
            {
                if (split[i].Contains("❶"))
                {
                    string[] internalsplit = split[i].Split('❶');
                    UserList _u = NetworkManager.DMS.Find(x => x.AccountID == int.Parse(internalsplit[0]));
                    if (_u == null)
                    {
                        NetworkManager.DMS.Add(new UserList(internalsplit[1], int.Parse(internalsplit[0])));
                    }
                }
            }
            for (int i = 0; i < NetworkManager.DMS.Count; ++i)
            {
                bool FoundIdInNewReturn = split.ToList().Any(x => x.Contains("❶") && (int.Parse(x.Split('❶')[0])) == NetworkManager.DMS[i].AccountID);
                if (FoundIdInNewReturn == false)
                {
                    NetworkManager.DMS.Remove(NetworkManager.DMS[i]);
                }
            }
           

            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.LoadingFriends = false;
                MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                if (populatewhere == 1)
                {
                    MainWindow.instance.AddFriendTab.Visibility = Visibility.Hidden;
                    MainWindow.instance.AllFriendsTab.Visibility = Visibility.Visible;
                    MainWindow.instance.PendingFriendsTab.Visibility = Visibility.Hidden;
                    MainWindow.instance.AllFriendsListBox.Items.Refresh();
                }
                MainWindow.instance.PendingFriendsListBox.Items.Refresh();
                MainWindow.instance.AllFriendsListBox.Items.Refresh();
            });
        }
    }
    public static void SendBackPendingFriends(Packet packet, ref bool error)
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        string friends = System.Text.Encoding.UTF8.GetString(bytes);
        string[] split = friends.Split('❷');
        ESMA.MainWindow.instance.Dispatcher.Invoke(() => 
        {
            MainWindow.instance.PendingFriendsListBox.Items.Clear();
        });
        for (int i = 0; i < split.Length; ++i)
        {
            if (split[i].Contains("❶"))
            {
                string[] internalsplit = split[i].Split('❶');
                UserList _u = NetworkManager.Friends.Find(x => x.AccountID == int.Parse(internalsplit[0]));
                if (_u == null)
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() => 
                    {
                        MainWindow.instance.PendingFriendsListBox.Items.Add(new UserList(internalsplit[1], int.Parse(internalsplit[0])));
                    });
                }
            }
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.AddFriendTab.Visibility = Visibility.Hidden;
            MainWindow.instance.AllFriendsTab.Visibility = Visibility.Hidden;
            MainWindow.instance.PendingFriendsTab.Visibility = Visibility.Visible;
            MainWindow.instance.EnableorDisableLoading("Loading pending friends...", false);
        });
    }
    public static void SendBackUserInVoiceChannel(Packet packet, ref bool error)
    {

        int clientpublicid = packet.GetInt(ref error); if(error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
       
        if (channel == -1) // user is in no voice channel
        {
            UserList _u = ChatManager.GetChatUserByID((NetworkManager.GetGlobalUser(clientpublicid) ?? new GlobalUser()).account_ID);
            if (_u != null)
            {
                _u.CurrentVoiceChannel = null;
                if (_u.AccountID == NetworkManager.MyAccountID)
                {

                    _u.StopTransmission();
                }
                foreach (int c in ChannelsKeysAsList())
                {
                    if (ChatManager.channels[c].GetType() == typeof(VoiceChannelsForTempRoom)) //if channel is a voice channel
                    {
                        VoiceChannelsForTempRoom vc = (ChatManager.channels[c] as VoiceChannelsForTempRoom);
                        if (vc.UsersInRoom.Contains(_u))
                        {
                            vc.UsersInRoom.Remove(_u); //remove user if in any voice channel
                            if (_u.AccountID == NetworkManager.MyAccountID)
                            {
                                MainWindow.PlaySound(NotificationSound.CallDisconnect);
                            }
                        }
                        if (vc.ListBoxItem != null)
                        {
                            for (int i = 0; i < vc.ListBoxItem.Items.Count; ++i)
                            {
                                if (vc.ListBoxItem.Items[i] as UserList == _u)
                                {
                                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        vc.ListBoxItem.Items.Remove(_u); //remove user from that channels userlist UI
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        else // user is in voice channel
        {
            UserList _u = ChatManager.GetChatUserByID(NetworkManager.GetGlobalUser(clientpublicid).account_ID);
            if (_u != null)
            {
                _u.CurrentVoiceChannel = channel;
                if (Methods.foundchannel(channel) == false)
                {
                    return;
                }

                foreach (int k in ChannelsKeysAsList())
                {
                    if ((ChatManager.channels[k] as VoiceChannelsForTempRoom) != null)
                    {
                        VoiceChannelsForTempRoom voicechannel = (ChatManager.channels[k] as VoiceChannelsForTempRoom);
                        if (voicechannel.UsersInRoom.Contains(_u) == true) // add to channel userlist
                        {
                            voicechannel.UsersInRoom.Remove(_u);
                        }
                        if (voicechannel.ListBoxItem != null)
                        {
                            for (int i = 0; i < voicechannel.ListBoxItem.Items.Count; ++i)
                            {
                                if (voicechannel.ListBoxItem.Items[i] as UserList == _u)
                                {
                                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        voicechannel.ListBoxItem.Items.Remove(_u); //remove user from that channels userlist UI
                                    });
                                }
                            }
                        }
                    }
                }
                VoiceChannelsForTempRoom vc = (ChatManager.channels[channel] as VoiceChannelsForTempRoom);
                if (vc.UsersInRoom.Contains(_u) == false) // add to channel userlist
                {
                    vc.UsersInRoom.Add(_u);
                    if (_u.AccountID == NetworkManager.MyAccountID)
                    {
                        if (_u.wave_in == null && NAudio.Wave.WaveInEvent.DeviceCount > 0)
                        {
                            _u.StartTransmittingVoice(); // once i join this channel then i can start transmitting my voice
                            _u.wave_in.StartRecording();
                        }
                        MainWindow.PlaySound(NotificationSound.CallConnect);
                    }
                }
                bool found = false;
                if (vc.ListBoxItem != null)
                {
                    for (int i = 0; i < vc.ListBoxItem.Items.Count; ++i) // check if UI Listbox contains the user element
                    {
                        if (vc.ListBoxItem.Items[i] as UserList == _u)
                        {
                            found = true;
                        }
                    }
                }
                if (found == false)
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        vc.ListBoxItem.Items.Add(_u); // add to channel userlist UI if already not in there
                    });
                }
            }
        }
    }
    public static void SendbackRoomJoinSuccess(Packet packet, ref bool error)
    {
        string ms = "Joining room...";
        int roomid = packet.GetInt(ref error); if(error == true) return; //Room id - regardless if temp or saved
        bool issaved = packet.GetBool(ref error); if(error == true) return; // Is this a saved room?
        int channel = packet.GetInt(ref error); if(error == true) return;
        int countofmessagestotal = packet.GetInt(ref error); if(error == true) return;
        int countofmessagestoreceive = packet.GetInt(ref error); if(error == true) return;
        byte[] channelstringasbyte = packet.GetBytes( ref error); if (error == true) return;
        string channelstring = System.Text.Encoding.UTF8.GetString(channelstringasbyte);
        string msgarray = packet.GetString(ref error); if(error == true) return;
        byte[] userarrayasbyte = packet.GetBytes( ref error); if (error == true) return;
        string userarray = System.Text.Encoding.UTF8.GetString(userarrayasbyte);
        byte[] rolesbytes = packet.GetBytes( ref error); if (error == true) return;
        int port = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        string roles = System.Text.Encoding.UTF8.GetString(rolesbytes);

        ChatManager.SavedServers.RemoveAll(x => x.ID == -1);

        if (issaved == true)
        {
            ChatManager.CurrentSavedServer = roomid;
            ChatManager.CurTempRoom = null;
            MainWindow.checkinghash = true;
        }
        else
        {
            ChatManager.CurrentSavedServer = null;
            ChatManager.CurTempRoom = roomid;
            MainWindow.checkinghash = null;
        }

        ChatManager.CurrentChatWithUser = null;
        ChatManager.CurrentChatWithUserKey = "";
        ChatManager.channels.Clear();
        ChatManager.CurrentChannel = channel;
        ChatManager.StillGettingVerificationOfJoinRoom = false;
        for (int i = 0; i < ChatManager.SavedServers.Count; ++i)
        {
            int[] keyarray = new int[ChatManager.SavedServers[i].channels.Keys.Count];
            ChatManager.SavedServers[i].channels.Keys.CopyTo(keyarray, 0);
            foreach (int k in keyarray)
            {
                ChannelsForTempRoom c = (ChatManager.SavedServers[i].channels[k] as ChannelsForTempRoom);
                if (c == null)
                {
                    continue;
                }
                c.messages.Clear();
                c.searchedmessages.Clear();
                c.MessagesFetched = 0;
                c.MessagesFetchedSearch = 0;
                c.MessagesToFetched = 0;
                c.MessagesToFetchedSearch = 0;
                c.MessagesToFetchTotal = 0;
            }
        }
        if (countofmessagestoreceive > 0) // if we get messages to receive on initialization, then check these booleans
        {
            ESMA.ChatManager.StillFetchingMessages = true;
            ChatManager.StillLoadingMessages = true;
        }

        string[] usersplit = userarray.Split('❷');
        if (issaved == true)
        {
            ChatManager.UserList_FULL = new List<UserList>();
            for (int i = 0; i < usersplit.Length; ++i)
            {
                if (usersplit[i].Contains("❶"))
                {
                    string[] internalusersplit = usersplit[i].Split('❶');
                    UserList _u_full = ChatManager.UserList.Find(x => x.AccountID == int.Parse(internalusersplit[0]));
                    //
                    string theroles = internalusersplit[1];
                    string[] rsplit = theroles.Split('/');
                    List<int> TempRL = new List<int>();
                    for (int r = 0; r < rsplit.Length; ++r)
                    {
                        if (String.IsNullOrWhiteSpace(rsplit[r]))
                        {
                            continue;
                        }
                        TempRL.Add(int.Parse(rsplit[r]));
                    }
                    //
                    if (_u_full == null)
                    {
                        UserList _u = new UserList(internalusersplit[2], int.Parse(internalusersplit[0]));
                        _u._Roles = TempRL;
                        ChatManager.UserList_FULL.Add(_u);
                    }
                    else
                    {
                        _u_full._Roles = TempRL;
                        ChatManager.UserList_FULL.Add(_u_full);
                    }
                }
            }
        }
        else
        {
            ChatManager.UserList_FULL = ChatManager.UserList;
        }

        NetworkManager.instance.udp.Start(port);

        ChatManager.RoleList.Clear();
        string[] roleslist;
        if (issaved == true)
        {
            roleslist = roles.Split('❷');

            for (int i = 0; i < roleslist.Length; ++i)
            {
                if (roleslist[i].Contains("❶"))
                {
                    string[] roleslistinternal = roleslist[i].Split('❶');
                    ChatManager.RoleList.Add(new RolesList(int.Parse(roleslistinternal[0]), roleslistinternal[1], new int[] { int.Parse(roleslistinternal[2]), int.Parse(roleslistinternal[3]), int.Parse(roleslistinternal[4]), int.Parse(roleslistinternal[5]), int.Parse(roleslistinternal[6]), int.Parse(roleslistinternal[7]) }, roleslistinternal[8], int.Parse(roleslistinternal[9])));
                }
            }
        }
        MainWindow.RefreshProfileNameColor();
        MainWindow.instance.UpdateRoleUI();


        MainWindow.LoadedChatListBoxCount = 0;
        MainWindow.ChatListBoxCount = 0;
        MainWindow.UnconditionalChatListBoxCount = 0;
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.Call.Visibility = Visibility.Hidden;
            ESMA.MainWindow.ChannelList.Items.Clear();
            ESMA.MainWindow.ChatList.Items.Clear();
        });
        string[] channeldatachunk = channelstring.Split('❷').ToArray(); // split at each individual channel
        RefreshUserList();
        for (int i = 0; i < channeldatachunk.Length; ++i)
        {
            if (channeldatachunk[i].Contains("❶"))
            {
                string[] internalstuff = channeldatachunk[i].Split('❶'); // split to get datachunk for channel

                if (internalstuff.Length > 4) // if this is a text channel
                {

                    ChannelsForTempRoom c = new ChannelsForTempRoom(internalstuff[1], int.Parse(internalstuff[0]));

                    if (issaved == true)
                    {
                        c.read_only = int.Parse(internalstuff[6]) == 0 ? false : true;
                        c.incoming_users = int.Parse(internalstuff[7]) == 0 ? false : true;
                    }
                    string[] rolelist_b = new string[1];
                    if (issaved == true)
                    {
                        rolelist_b = internalstuff[5].Split('/');
                        for (int ii = 0; ii < rolelist_b.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolelist_b[ii]) == false)
                            {
                                c.Roles.Add(int.Parse(rolelist_b[ii]));
                            }
                        }
                    }
                    ChatManager.channels.Add(int.Parse(internalstuff[0]), c);
                    c.MessagesToFetched = int.Parse(internalstuff[0]) == channel ? int.Parse(internalstuff[4]) : 0;
                    c.MessagesToFetchTotal = int.Parse(internalstuff[3]);

                    if (c.MessagesToFetchTotal == 0)
                    {
                        c.initialized = true;
                    }
                    if (ChatManager.my_room_user._Roles.Any(x => rolelist_b.ToList().Contains(x.ToString())) || ChatManager.CurrentSavedServer == null)
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            ESMA.MainWindow.ChannelList.Items.Add(c);
                        });
                    }
                }
                else // then this is a voice channel
                {
                    VoiceChannelsForTempRoom v = new VoiceChannelsForTempRoom(internalstuff[1], int.Parse(internalstuff[0]));
                    v.read_only = int.Parse(internalstuff[3]) == 0 ? false : true;
                    ChatManager.channels.Add(int.Parse(internalstuff[0]), v);
                    string[] rolelist_b = new string[1];
                    if (issaved == true)
                    {
                        rolelist_b = internalstuff[3].Split('/');
                        for (int ii = 0; ii < rolelist_b.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolelist_b[ii]) == false)
                            {
                                v.Roles.Add(int.Parse(rolelist_b[ii]));
                            }
                        }
                    }
                    if (ChatManager.my_room_user._Roles.Any(x => rolelist_b.ToList().Contains(x.ToString())) || ChatManager.CurrentSavedServer == null)
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            ESMA.MainWindow.ChannelList.Items.Add(v);
                        });
                    }
                }
            }
        }
            (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).initialized = true; //the default channel has been initialized and the first wave of messages have been received. The other channels remain uninitalized with no received messages.
        MainWindow.instance.ChangeTheSelectionOfChannelListBox(channel);
        MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.ChatCover.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendGrid.Visibility = Visibility.Hidden;
            MainWindow.instance.ChannelCover.Visibility = Visibility.Hidden;
            MainWindow.instance.DMSListBoxScroll.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendHud.Visibility = Visibility.Hidden;
            MainWindow.instance.ServerBrowserWindow.Visibility = Visibility.Hidden;
        });
        MainWindow.instance.CreateSavedServerFromCurrent(!issaved);


        ESMA.MainWindow.instance.SetFormVisibility(3);
        SendPackets.RequestUserVoiceChannels(ChatManager.CurrentSavedServer ?? -1); //fetch all users in a voice channel in this room
        //check to see if we already have these messages
        if (issaved == true && msgarray != " ")
        {
            SavedServersOnMemory ss = ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer);
            if (ss != null)
            {
                if (ss.channels[channel] != null && (ss.channels[channel] as SavedChannelsForTempRoom) != null)
                {
                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channel] as SavedChannelsForTempRoom);
                    string[] msgarraysplit = msgarray.Split('|');

                    for (int i = 0; i < msgarraysplit.Length; ++i)
                    {
                        if (String.IsNullOrWhiteSpace(msgarraysplit[i]) == false)
                        {
                            if (loadedchannel.savedmessages.Any(x => x.idofmessage == int.Parse(msgarraysplit[i])) == false)
                            {
                                goto RequestMessages;
                            }
                        }
                    }
                    for (int i = 0; i < msgarraysplit.Length; ++i)
                    {
                        if (String.IsNullOrWhiteSpace(msgarraysplit[i]) == false)
                        {
                            Message msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == int.Parse(msgarraysplit[i]));
                            if (msgobj != null)
                            {
                                ChannelsForTempRoom c = (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom);
                                if (ChatManager.CurrentChannel != channel && c.messages.Count >= 30)
                                {
                                    c.messages.RemoveAt(0);
                                }
                                c.messages.Add(msgobj);
                                loadedchannel.messages.Add(msgobj);
                                verifyfetch(channel, false, false);
                                ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, false); //add to chat UI
                            }
                        }
                    }
                    return;
                }
            }
        }
    RequestMessages:;
       
        if (countofmessagestotal > 0)
        {
            SendPackets.RequestMessagesAfterJoin(channel, issaved); // get the messages now since i joined
        }
        else
        {
            ChatManager.StillLoadingMessages = false;
            ChatManager.StillFetchingMessages = false;
            MainWindow.instance.EnableorDisableLoading(ms, false);
        }


    }
    public static void SendBackMoreMessagesToClient(Packet packet, ref bool error)
    {
        int amountnew = packet.GetInt(ref error); if(error == true) return;
        int amountcurrently = packet.GetInt(ref error); if(error == true) return;
        int indexcap = packet.GetInt(ref error); if(error == true) return;
        int channelkey = packet.GetInt(ref error); if(error == true) return;
        string msgarray = packet.GetString(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (Methods.foundchannel(channelkey) == false)
        {
            ESMA.ChatManager.StillFetchingMessages = false;
            return;
        }
        ESMA.ChatManager.StillFetchingMessages = true;
        ChatManager.StillLoadingMessages = true;
        (ChatManager.channels[channelkey] as ChannelsForTempRoom).MessagesToFetched = amountnew; //received new message count ,even though we dont got them yet. 
        //check to see if we already have these messages
        if (ChatManager.CurrentSavedServer != null && msgarray != " " || ChatManager.CurrentChatWithUser != null && msgarray != " ")
        {
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

            SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
            if (ss != null)
            {
                if (ss.channels[channelkey] != null && (ss.channels[channelkey] as SavedChannelsForTempRoom) != null)
                {
                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channelkey] as SavedChannelsForTempRoom);
                    string[] msgarraysplit = msgarray.Split('|');
                    for (int i = 0; i < msgarraysplit.Length; ++i)
                    {
                        if (String.IsNullOrWhiteSpace(msgarraysplit[i]) == false)
                        {
                            if (loadedchannel.savedmessages.Any(x => x.idofmessage == int.Parse(msgarraysplit[i])) == false)
                            {
                                goto RequestMessages;
                            }
                        }
                    }
                    for (int i = 0; i < msgarraysplit.Length; ++i)
                    {
                        if (String.IsNullOrWhiteSpace(msgarraysplit[i]) == false)
                        {
                            Message msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == int.Parse(msgarraysplit[i]));
                            if (msgobj != null)
                            {
                                if (ChatManager.CurrentChannel != channelkey && (ESMA.ChatManager.channels[channelkey] as ChannelsForTempRoom).messages.Count >= 30)
                                {
                                    (ESMA.ChatManager.channels[channelkey] as ChannelsForTempRoom).messages.RemoveAt(0);
                                }
                                (ESMA.ChatManager.channels[channelkey] as ChannelsForTempRoom).messages.Add(msgobj);
                                loadedchannel.messages.Add(msgobj);

                                verifyfetch(channelkey, false, false);

                                ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, false); //add to chat UI

                            }
                        }
                    }
                    return;
                }
            }
        }
    RequestMessages:;
        SendPackets.SendSuccessInGettingNewMessageAmount(amountcurrently, indexcap, channelkey); // now we will fetch those messages
    }

    public static void SendToSavedServerNewUsersWRole(Packet packet, ref bool error)
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        string datachunk = System.Text.Encoding.UTF8.GetString(bytes);
        string[] datachunkplit = datachunk.Split('❷');
        for (int i = 0; i < datachunkplit.Length; ++i)
        {
            if (datachunkplit[i].Contains("❶"))
            {
                string[] usersplitinternal = datachunkplit[i].Split('❶');
                UserList _u = ChatManager.GetAnyChatUserByID(int.Parse(usersplitinternal[0]));
                if (_u != null)
                {
                    string[] rolesplit = usersplitinternal[1].Split('/');
                    _u._Roles.Clear();
                    for (int ii = 0; ii < rolesplit.Length; ++ii)
                    {
                        if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                        {
                            _u._Roles.Add(int.Parse(rolesplit[ii]));
                        }
                    }
                }
            }
        }

        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.UpdateRoleUI();
            if (MainWindow.instance.ManageRoles.Visibility == Visibility.Visible)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    MainWindow.instance.RefreshUserRoleListBox();
                });
            }
        });
    }
    public static void SendToSavedServerNewChannelsWRole(Packet packet, ref bool error)
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        string datachunk = System.Text.Encoding.UTF8.GetString(bytes);
        string[] datachunkplit = datachunk.Split('❷');
        for (int i = 0; i < datachunkplit.Length; ++i)
        {
            if (datachunkplit[i].Contains("❶"))
            {
                string[] usersplitinternal = datachunkplit[i].Split('❶');
                UserList _u = ChatManager.GetChatUserByID(int.Parse(usersplitinternal[0]));
                if (_u != null)
                {
                    string[] rolesplit = usersplitinternal[1].Split('/');
                    _u._Roles.Clear();
                    for (int ii = 0; ii < rolesplit.Length; ++ii)
                    {
                        if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                        {
                            _u._Roles.Add(int.Parse(rolesplit[ii]));
                        }
                    }
                }

            }
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            if (MainWindow.instance.ManageRoles.Visibility == Visibility.Visible)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    MainWindow.instance.RefreshUserRoleListBox();
                });
            }
        });
    }
    public static void verifyfetch(int channelkey, bool searched, bool dontadd) // add count to initialized messages received
    {
        if (Methods.foundchannel(channelkey) == false)
        {
            ESMA.ChatManager.StillFetchingMessages = false;
            return;
        }
        ChannelsForTempRoom cftr = (ChatManager.channels[channelkey] as ChannelsForTempRoom);
        if (cftr == null)
        {
            return;
        }
        if (searched == false)//when populating an old message loading request
        {
            cftr.MessagesFetched++;
            if (cftr.MessagesFetched == cftr.MessagesToFetched) // we have fetched all messages we wanted to fetch
            {
                cftr.messages = cftr.messages.OrderBy(x => x.idofmessage).ToList();
                int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

                if (ChatManager.GetSavedServerByID(serverid) != null)
                {
                    (ChatManager.GetSavedServerByID(serverid).channels[channelkey] as ChannelsForTempRoom).messages = (ChatManager.GetSavedServerByID(serverid).channels[channelkey] as ChannelsForTempRoom).messages.OrderBy(x => x.idofmessage).ToList();
                }
                ESMA.ChatManager.StillFetchingMessages = false;
            }
        }
        else //when populating a searched message conversation
        {
            cftr.MessagesFetchedSearch++;
            if (cftr.MessagesFetchedSearch == cftr.MessagesToFetchedSearch) // we have fetched all messages we wanted to fetch
            {
                cftr.searchedmessages = cftr.searchedmessages.OrderBy(x => x.idofmessage).ToList();
                ESMA.ChatManager.StillFetchingMessages = false;
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.MSGSearchGrid.Visibility = Visibility.Hidden;
                });
            }
        }
    }

    public static void SavedRoomCreationSuccess(Packet packet, ref bool error) // When creating a saved room only.
    {
        byte[] channels = packet.GetBytes( ref error); if (error == true) return;
        int thechannel = packet.GetInt(ref error); if(error == true) return;
        int serverid = packet.GetInt(ref error); if(error == true) return;
        byte[] rolesbytes = packet.GetBytes( ref error); if (error == true) return;
        string roles = System.Text.Encoding.UTF8.GetString(rolesbytes);
        int port = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        ChatManager.SavedServers.RemoveAll(x => x.ID == -1);
        ChatManager.CurrentSavedServer = serverid;
        ChatManager.CurrentChannel = thechannel;
        ChatManager.channels.Clear();
        ChatManager.CurTempRoom = null;
        ChatManager.CurrentChatWithUser = null;
        ChatManager.CurrentChatWithUserKey = "";
        ChatManager.UserList_FULL = new List<UserList>();
        UserList _u_full = ChatManager.UserList.Find(x => x.AccountID == (int)NetworkManager.MyAccountID);

        if (_u_full == null)
        {
            UserList _u = new UserList(NetworkManager.myusername, (int)NetworkManager.MyAccountID);
            _u._Roles = new List<int> { 0, 1 };
            ChatManager.UserList_FULL.Add(_u);
        }
        else
        {
            _u_full._Roles = new List<int> { 0, 1 };
            ChatManager.UserList_FULL.Add(_u_full);
        }
        NetworkManager.instance.udp.Start(port);
        MainWindow.checkinghash = true;

        string[] roleslist = roles.Split('❷');
        ChatManager.RoleList.Clear();
        for (int i = 0; i < roleslist.Length; ++i)
        {
            if (roleslist[i].Contains("❶"))
            {
                string[] roleslistinternal = roleslist[i].Split('❶');
                ChatManager.RoleList.Add(new RolesList(int.Parse(roleslistinternal[0]), roleslistinternal[1], new int[] { int.Parse(roleslistinternal[2]), int.Parse(roleslistinternal[3]), int.Parse(roleslistinternal[4]), int.Parse(roleslistinternal[5]), int.Parse(roleslistinternal[6]), int.Parse(roleslistinternal[7]) }, roleslistinternal[8], int.Parse(roleslistinternal[9])));
            }
        }
        MainWindow.RefreshProfileNameColor();
        MainWindow.instance.UpdateRoleUI();
        RefreshUserList();
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            MainWindow.instance.Call.Visibility = Visibility.Hidden;
            ESMA.MainWindow.ChatList.Items.Clear();
            ESMA.MainWindow.ChannelList.Items.Clear();
        });
        MainWindow.ChatListBoxCount = 0;
        MainWindow.LoadedChatListBoxCount = 0;
        MainWindow.UnconditionalChatListBoxCount = 0;

        string[] channeldatachunk = System.Text.Encoding.UTF8.GetString(channels).Split('❷').ToArray(); // split at each individual channel

        for (int i = 0; i < channeldatachunk.Length; ++i)
        {
            if (channeldatachunk[i].Contains("❶")) // if this is a potential array
            {
                string[] internalstuff = channeldatachunk[i].Split('❶'); // split to get datachunk for channel
                if (internalstuff.Length > 4) // if this is a text channel
                {
                    ChannelsForTempRoom c = new ChannelsForTempRoom(internalstuff[1], int.Parse(internalstuff[0]));
                    c.Roles.Add(0);
                    ChatManager.channels.Add(int.Parse(internalstuff[0]), c);
                    c.MessagesToFetched = int.Parse(internalstuff[0]) == thechannel ? int.Parse(internalstuff[4]) : 0;
                    c.MessagesToFetchTotal = int.Parse(internalstuff[3]);
                    if (c.MessagesToFetchTotal == 0)
                    {
                        c.initialized = true;
                    }
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        ESMA.MainWindow.ChannelList.Items.Add(c);
                    });
                }
                else // then this is a voice channel
                {
                    VoiceChannelsForTempRoom v = new VoiceChannelsForTempRoom(internalstuff[1], int.Parse(internalstuff[0]));
                    v.Roles.Add(0);
                    ChatManager.channels.Add(int.Parse(internalstuff[0]), v);
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        ESMA.MainWindow.ChannelList.Items.Add(v);
                    });
                }
            }
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {

            MainWindow.instance.ServerBrowserWindow.Visibility = Visibility.Hidden;
            MainWindow.instance.ChatCover.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendGrid.Visibility = Visibility.Hidden;
            MainWindow.instance.ChannelCover.Visibility = Visibility.Hidden;
            MainWindow.instance.DMSListBoxScroll.Visibility = Visibility.Hidden;
            MainWindow.instance.FriendHud.Visibility = Visibility.Hidden;

        });
        (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).initialized = true; //the default channel has been initialized and the first wave of messages have been received. The other channels remain uninitalized with no received messages.

        MainWindow.instance.ChangeTheSelectionOfChannelListBox(thechannel);
        MainWindow.instance.CreateSavedServerFromCurrent(false);
    }

    const int Voice_Delay_Tolerance = 200;
    public static void ReceiveVoiceInCall(Packet packet, ref bool error) // receive voice via bytes from any user in ur current channel
    {
        byte[] bytes = packet.GetBytes(ref error); if (error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (bytes == null)
        {
            return;
        }
        UserList _u = ChatManager.UserInCallWith;
        if (_u != null)
        {
            if (_u.wave_out == null)
            {
                _u.StartReceivingVoice();
            }
            if (_u.provider.BufferedDuration > TimeSpan.FromMilliseconds(Voice_Delay_Tolerance)) //if this stream is delayed, then just clear it and start fresh.
            {
                 _u.provider.ClearBuffer();
            }
            //  try
            //  {
            bytes = Methods.EncryptOrDecrypt(bytes, ChatManager.selectedroompassasbytes2);
            if (bytes == null) //if decryption failed , then stop.
            {
                return;
            }
            _u.provider.AddSamples(bytes, 0, bytes.Length); // add to users audio stream client sided
            _u.TransmissionIconTimer = 15;
            if (_u.TransmissionStatus != null)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    _u.TransmissionStatus.Text = "🔊";
                });
            }
            { //if there is for some reason any of the voice out components are null, then re initialize.
                if (_u.wave_out == null)
                {
                    _u.wave_out = new WaveOutEvent();
                }
                if (_u.provider == null)
                {
                    _u.provider = new BufferedWaveProvider(new WaveFormat());
                    _u.wave_out.Init(_u.provider);
                }
            }
           
                if (_u.wave_out.PlaybackState != PlaybackState.Playing)
                {
                    _u.wave_out.Play(); // if this audio stream isnt playing, then play. 
                }
            
            //  }
            //  catch
            //  {
            //error writing bytes to audio stream
            // }
        }
    }
    public static void ReceiveVoice(Packet packet, ref bool error) // receive voice via bytes from any user in ur current channel
    {
        int voiceaccountid = packet.GetInt(ref error); if(error == true) return;
        byte[] bytes = packet.GetBytes( ref error); if (error == true) return;
        int channelkey = packet.GetInt(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (bytes == null)
        {
            return;
        }

        UserList _u = ChatManager.GetChatUserByID(voiceaccountid);
        if (_u != null)
        {
            if (_u.wave_out == null)
            {
                _u.StartReceivingVoice();
            }
            if (_u.provider.BufferedDuration > TimeSpan.FromMilliseconds(Voice_Delay_Tolerance)) //if this stream is delayed, then just clear it and start fresh.
            {
                _u.provider.ClearBuffer();
            }
            // try
            // {
            bytes = Methods.EncryptOrDecrypt(bytes, ChatManager.selectedroompassasbytes2);
            if (bytes == null) //if decryption failed , then stop.
            {
                return;
            }

            _u.provider.AddSamples(bytes, 0, bytes.Length); // add to users audio stream client sided
            _u.TransmissionIconTimer = 15;
            if (_u.TransmissionStatus != null)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    _u.TransmissionStatus.Text = "🔊";
                });
            }
            { //if there is for some reason any of the voice out things are null, then re initialize.
                if (_u.wave_out == null)
                {
                    _u.wave_out = new WaveOutEvent();
                }
                if (_u.provider == null)
                {
                    _u.provider = new BufferedWaveProvider(new WaveFormat());
                    _u.wave_out.Init(_u.provider);
                }
            }

                if (_u.wave_out.PlaybackState != PlaybackState.Playing)
                {
                    _u.wave_out.Play(); // if this audio stream isnt playing, then play. 
                }
            
            //}
            //  catch
            //{
            //error writing bytes to audio stream
            // }
        }
    }

    public static void SendBackMessageToAllInRoom(Packet packet, ref bool error)
    {

        int id = packet.GetInt(ref error); if(error == true) return; //whats the account id of this plyer
        string username = packet.GetString(ref error); if(error == true) return; //whats the account name of this plyer
        string msg = packet.GetString(ref error); if(error == true) return; //msg text
        int idofmessage = packet.GetInt(ref error); if(error == true) return;    //whats the index of this message
        string datetime = packet.GetString(ref error); if(error == true) return; // time as it was on the server
        int channel = packet.GetInt(ref error); if(error == true) return;    //what channel is this message on
        bool initializationmessage = packet.GetBool(ref error); if(error == true) return;   //is this message sent from initialization?
        bool searched = packet.GetBool(ref error); if(error == true) return;   //is this message a result of a message search?
        bool notmine = packet.GetBool(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (initializationmessage == false)
            MainWindow.PlaySound(NotificationSound.Message);
        if (Methods.foundchannel(channel) == false)
        {
            return;
        }
        if (id == NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == false ||
            id != NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == true)
        {

            Message msgobj = new Message(id, username, "This message doesnt belong to you...", idofmessage, true, false, datetime);
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
            if (initializationmessage)
            {
                verifyfetch(channel, searched, false);
            }
            ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched); //add to chat UI
            return;
        }
        if (ChatManager.CurrentSavedServer != null || ChatManager.CurTempRoom != null || ChatManager.CurrentChatWithUser != null)
        {
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
            SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
            if (ss != null)
            {
                if (ss.channels[channel] != null && (ss.channels[channel] as SavedChannelsForTempRoom) != null)
                {

                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channel] as SavedChannelsForTempRoom);
                    Message msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == idofmessage);
                    if (msgobj != null)
                    {

                        if (searched == false)
                        {
                            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
                            {
                                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
                            }
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
                            loadedchannel.messages.Add(msgobj);

                        }
                        else
                        {
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(msgobj);
                        }
                        if (initializationmessage)
                        {
                            verifyfetch(channel, searched, false);
                        }
                        ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched); //add to chat UI


                        return;
                    }
                }
            }
        }

        //for this group, we will create a temporary regular message placeholder, until this message loads, in which the new message will replace this one. 
        Message temp_msgobj = new Message(id, username, "Message decryption pending...", idofmessage, false, true, datetime);

        if (searched == false)
        {
            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
            {
                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
            }
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

            if (ChatManager.GetSavedServerByID(serverid) != null)
            {
                (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);
            }
        }
        else
        {
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(temp_msgobj);
        }
        ESMA.MainWindow.instance.ChatRefreshList(0, temp_msgobj, searched); //add to chat UI
        if (initializationmessage)
        {
            verifyfetch(channel, searched, false);

        }
        //We will now decrypt this message , and if successful we will replace the placeholder with this one.
        (searched ? ESMA.BackgroundWorker.Searchactions : ESMA.BackgroundWorker.Download_Actions).Add((Action)(() => // put this task on its designated background worker so that it doesnt block up this thread
        {
            if (Methods.foundchannel(channel) == false)
            {
                return;
            }
            string Decrypt = null;
            if (ChatManager.CurrentChatWithUser == null)
            {
                Decrypt = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
            }
            else // if this is a dm, then we decrypt using asymetrical decryption
            {
                Decrypt = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
            }
            Message msgobj = new Message(id, username, id != -2 ? (Decrypt ?? "Unable to decrypt.") : msg, idofmessage, true, true, datetime);
            ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched); //replace placeholder
        }));
    }
    public static void SendBackVideoMessageToAllInRoom(Packet packet, ref bool error)
    {

        int id = packet.GetInt(ref error); if(error == true) return;
        string username = packet.GetString(ref error); if(error == true) return; 
        string msg = packet.GetString(ref error); if(error == true) return;
        string md5 = packet.GetString(ref error); if(error == true) return; //whats the md5 signature of the video
        string t_md5 = packet.GetString(ref error); if(error == true) return;//whats the md5 signature of the low res(thumbnail) picture
        int idofmessage = packet.GetInt(ref error); if(error == true) return; 
        string datetime = packet.GetString(ref error); if(error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return; 
        bool initializationmessage = packet.GetBool(ref error); if(error == true) return;
        bool searched = packet.GetBool(ref error); if(error == true) return;  
        bool notmine = packet.GetBool(ref error); if(error == true) return;                        
        packet.CheckFinalizers(ref error); if (error == true) return;
        string exportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + md5 + ".mp4"); // cached video file path
        string exportfile_t = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + t_md5); // cached thumbnail file path
        if (initializationmessage == false)
            MainWindow.PlaySound(NotificationSound.Message);
        if (Methods.foundchannel(channel) == false)
        {
            return;
        }
        if (id == NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == false ||
            id != NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == true)
        {

            Message msgobj = new Message(id, username, "This message doesnt belong to you...", idofmessage, true, false, datetime);
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
            if (initializationmessage)
            {
                verifyfetch(channel, searched, false);
            }
            ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched); //add to chat UI
            return;
        }
        if (ChatManager.CurrentSavedServer != null || ChatManager.CurTempRoom != null || ChatManager.CurrentChatWithUser != null)
        {
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
            SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
            if (ss != null)
            {
                if (ss.channels[channel] != null && (ss.channels[channel] as SavedChannelsForTempRoom) != null)
                {

                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channel] as SavedChannelsForTempRoom);
                    VideoMessage msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == idofmessage) as VideoMessage;
                    if (msgobj != null)
                    {

                        if (searched == false)
                        {
                            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
                            {
                                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
                            }
                             (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
                            loadedchannel.messages.Add(msgobj);

                        }
                        else
                        {
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(msgobj);
                        }
                        if (initializationmessage)
                        {
                            verifyfetch(channel, searched, false);
                        }
                        ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched);

                        return;
                    }
                }
            }
        }
        //for this group, we will create a temporary regular message placeholder, until this message loads, in which the new message will replace this one. 
        Message temp_msgobj = new Message(id, username, "Message decryption pending...", idofmessage, false, true, datetime);

        if (searched == false)
        {
            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
            {
                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
            }
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);

            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

            if (ChatManager.GetSavedServerByID(serverid) != null)
            {
                (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);
            }
        }
        else
        {
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(temp_msgobj);
        }
        ESMA.MainWindow.instance.ChatRefreshList(0, temp_msgobj, searched);
        if (initializationmessage)
        {
            verifyfetch(channel, searched, false);
        }
        if (Methods.foundchannel(channel) == false)
        {
            GC.Collect();
            return;
        }
        string link = ESMA.DatabaseCalls.host + "/" + Methods.GetServerDir(); // the encrypted file's directory on the server
        (searched ? ESMA.BackgroundWorker.Searchactions : ESMA.BackgroundWorker.Download_Actions).Add((Action)(() =>  // put this task on its designated background worker so that it doesnt block up this thread. Fetching the web data and decrypting it may take a while
        {
            if (Methods.foundchannel(channel) == false)
            {
                return;
            }
            //Fetch the encrypted string through web call
            string encryptedString = "";
            try
            {
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {

                    encryptedString = webclient.DownloadString(link + t_md5);
                }
                if (String.IsNullOrEmpty(encryptedString) == true)
                {
                    throw new Exception();
                }
            }
            catch //fail to retrieve
            {

                string dc = null;
                if (ChatManager.CurrentChatWithUser == null)
                {
                    dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                }
                else
                {
                    dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                }

                Message msgobj = new VideoMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, "", t_md5, "", datetime);
                ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
                return;
            }



            byte[] decrypted_bytes;
            string Decrypt = ESMA.Methods.DecryptString(encryptedString, ESMA.ChatManager.selectedroompassasbytes); //Decrypt the file as string
            if (Decrypt != null)
            {
                decrypted_bytes = Convert.FromBase64String(Decrypt); //convert decrypted string back in to bytes
            }
            else //Decryption failed
            {

                string dc = null;
                if (ChatManager.CurrentChatWithUser == null)
                {
                    dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                }
                else
                {
                    dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                }

                Message msgobj = new VideoMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, "", t_md5, "", datetime);
                ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
                return;
            }

            if (File.Exists(exportfile_t) == false || File.Exists(exportfile_t) == true && ESMA.Methods.GetFileMD5Signature(exportfile_t) != t_md5) //Save file in cache
            {
                try
                {
                    if (File.Exists(exportfile_t))
                    {
                        File.Delete(exportfile_t);
                    }
                    File.WriteAllBytes(exportfile_t, decrypted_bytes);
                }
                catch //failed writing file to cache
                {
                    string dc = null;
                    if (ChatManager.CurrentChatWithUser == null)
                    {
                        dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                    }
                    else
                    {
                        dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                    }

                    Message msgobj = new VideoMessage(id, username, dc ?? "Error loading message content.", idofmessage, md5, "", t_md5, "", datetime);
                    ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
                    return;
                }
            }

            {
                string dc = null;
                if (ChatManager.CurrentChatWithUser == null)
                {
                    dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                }
                else
                {
                    dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                }

                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    Message msgobj = new VideoMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, exportfile, t_md5, exportfile_t, datetime);

                    ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
                });
            }
        }));
        GC.Collect();
    }
    public static void SendBackFileMessageToAllInRoom(Packet packet, ref bool error)
    {

        int id = packet.GetInt(ref error); if (error == true) return;
        string username = packet.GetString(ref error); if(error == true) return; 
        string msg = packet.GetString(ref error); if(error == true) return;
        string md5 = packet.GetString(ref error); if(error == true) return; //whats the md5 signature of file
        int idofmessage = packet.GetInt(ref error); if(error == true) return;  
        string datetime = packet.GetString(ref error); if(error == true) return;
        string extension = packet.GetString(ref error); if(error == true) return;
        long size = packet.GetLong(ref error); if (error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return;  
        bool initializationmessage = packet.GetBool(ref error); if(error == true) return;
        bool searched = packet.GetBool(ref error); if(error == true) return; 
        bool notmine = packet.GetBool(ref error); if(error == true) return;                            
        packet.CheckFinalizers(ref error); if (error == true) return;
        if (initializationmessage == false)
            MainWindow.PlaySound(NotificationSound.Message);
        if (Methods.foundchannel(channel) == false)
        {
            return;
        }
        if (id == NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == false ||
            id != NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == true)
        {

            Message msgobj = new Message(id, username, "This message doesnt belong to you...", idofmessage, true, false, datetime);
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
            if (initializationmessage)
            {
                verifyfetch(channel, searched, false);
            }
            ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched);

            return;
        }
        if (ChatManager.CurrentSavedServer != null || ChatManager.CurTempRoom != null || ChatManager.CurrentChatWithUser != null)
        {
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
            SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
            if (ss != null)
            {
                if (ss.channels[channel] != null && (ss.channels[channel] as SavedChannelsForTempRoom) != null)
                {

                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channel] as SavedChannelsForTempRoom);
                    FileMessage msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == idofmessage) as FileMessage;
                    if (msgobj != null)
                    {

                        if (searched == false)
                        {
                            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
                            {
                                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
                            }
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
                            loadedchannel.messages.Add(msgobj);

                        }
                        else
                        {
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(msgobj);
                        }
                        if (initializationmessage)
                        {
                            verifyfetch(channel, searched, false);

                        }
                        ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched);

                        return;
                    }
                }
            }
        }
        //for this group, we will create a temporary regular message placeholder, until this message loads, in which the new message will replace this one. 
        Message temp_msgobj = new Message(id, username, "Message decryption pending...", idofmessage, false, true, datetime);

        if (searched == false)
        {
            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
            {
                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
            }
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);

            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

            if (ChatManager.GetSavedServerByID(serverid) != null)
            {
                (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);
            }
        }
        else
        {
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(temp_msgobj);
        }
        ESMA.MainWindow.instance.ChatRefreshList(0, temp_msgobj, searched);

        if (initializationmessage)
        {
            verifyfetch(channel, searched, false);
        }

        if (Methods.foundchannel(channel) == false)
        {
            GC.Collect();
            return;
        }
        (searched ? ESMA.BackgroundWorker.Searchactions : ESMA.BackgroundWorker.Download_Actions).Add((Action)(() =>
        {

            string dc = null;
            if (ChatManager.CurrentChatWithUser == null)
            {
                dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
            }
            else
            {
                dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
            }

            Message msgobj = new FileMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, extension, size, datetime);

            ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
        }));
        GC.Collect();
    }
    public static void SendBackAudioMessageToAllInRoom(Packet packet, ref bool error)
    {

        int id = packet.GetInt(ref error); if(error == true) return;
        string username = packet.GetString(ref error); if(error == true) return; 
        string msg = packet.GetString(ref error); if(error == true) return;
        string md5 = packet.GetString(ref error); if(error == true) return; //whats the md5 signature of the file
        int idofmessage = packet.GetInt(ref error); if(error == true) return;  
        string datetime = packet.GetString(ref error); if(error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return;   
        bool initializationmessage = packet.GetBool(ref error); if(error == true) return;
        bool searched = packet.GetBool(ref error); if(error == true) return;  
        bool notmine = packet.GetBool(ref error); if(error == true) return;                     
        packet.CheckFinalizers(ref error); if (error == true) return;
        string exportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + md5 + ".mp3");
        if (Methods.foundchannel(channel) == false)
        {
            return;
        }
        if (initializationmessage == false)
            MainWindow.PlaySound(NotificationSound.Message);
        if (id == NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == false ||
           id != NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == true)
        {

            Message msgobj = new Message(id, username, "This message doesnt belong to you...", idofmessage, true, false, datetime);
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);

            if (initializationmessage)
            {
                verifyfetch(channel, searched, false);
            }
            ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched);
            return;
        }
        if (ChatManager.CurrentSavedServer != null || ChatManager.CurTempRoom != null || ChatManager.CurrentChatWithUser != null)
        {
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
            SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
            if (ss != null)
            {
                if (ss.channels[channel] != null && (ss.channels[channel] as SavedChannelsForTempRoom) != null)
                {

                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channel] as SavedChannelsForTempRoom);
                    AudioMessage msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == idofmessage) as AudioMessage;
                    if (msgobj != null)
                    {

                        if (searched == false)
                        {
                            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
                            {
                                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
                            }
                           (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
                            loadedchannel.messages.Add(msgobj);

                        }
                        else
                        {
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(msgobj);
                        }
                        if (initializationmessage)
                        {
                            verifyfetch(channel, searched, false);
                        }
                        ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched); //add to chat UI


                        return;
                    }
                }
            }
        }

        //for this group, we will create a temporary regular message placeholder, until this message loads, in which the new message will replace this one. 
        Message temp_msgobj = new Message(id, username, "Message decryption pending...", idofmessage, false, true, datetime);

        if (searched == false)
        {
            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
            {
                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
            }
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);

            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

            if (ChatManager.GetSavedServerByID(serverid) != null)
            {
                (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);
            }
        }
        else
        {
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(temp_msgobj);
        }

        ESMA.MainWindow.instance.ChatRefreshList(0, temp_msgobj, searched); //add to chat UI


        if (initializationmessage)
        {
            verifyfetch(channel, searched, false);
        }
        if (Methods.foundchannel(channel) == false)
        {
            GC.Collect();
            return;
        }
        (searched ? ESMA.BackgroundWorker.Searchactions : ESMA.BackgroundWorker.Download_Actions).Add((Action)(() =>
        {
            string dc = null;
            if (ChatManager.CurrentChatWithUser == null)
            {
                dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
            }
            else
            {
                dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
            }


            Message msgobj = new AudioMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, exportfile, datetime);
            if (File.Exists(exportfile) == true && ESMA.Methods.GetFileMD5Signature(exportfile) == md5)
            {
                ((AudioMessage)msgobj).FileExists = true;
            }

            ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);

        }));
        GC.Collect();
    }
    public static void SendBackPictureMessageToAllInRoom(Packet packet, ref bool error)
    {

        int id = packet.GetInt(ref error); if(error == true) return;
        string username = packet.GetString(ref error); if(error == true) return; 
        string msg = packet.GetString(ref error); if(error == true) return;
        string md5 = packet.GetString(ref error); if(error == true) return; //whats the md5 signature of the full quality picture
        string t_md5 = packet.GetString(ref error); if(error == true) return;//whats the md5 signature of the low res(thumbnail) picture
        int idofmessage = packet.GetInt(ref error); if(error == true) return;   
        string datetime = packet.GetString(ref error); if(error == true) return;
        int channel = packet.GetInt(ref error); if(error == true) return; 
        bool initializationmessage = packet.GetBool(ref error); if(error == true) return;
        bool searched = packet.GetBool(ref error); if(error == true) return;   
        bool notmine = packet.GetBool(ref error); if(error == true) return;
        packet.CheckFinalizers(ref error); if (error == true) return;
  
        string exportfile = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + md5);
        string exportfile_t = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + t_md5);
        if (initializationmessage == false)
            MainWindow.PlaySound(NotificationSound.Message);
        if (Methods.foundchannel(channel) == false)
        {
            return;
        }
        if (id == NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == false ||
            id != NetworkManager.MyAccountID && ChatManager.CurrentChatWithUser != null && notmine == true)
        {

            Message msgobj = new Message(id, username, "This message doesnt belong to you...", idofmessage, true, false, datetime);
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
            if (initializationmessage)
            {
                verifyfetch(channel, searched, false);
            }
            ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched);
            return;
        }
        if (ChatManager.CurrentSavedServer != null || ChatManager.CurTempRoom != null || ChatManager.CurrentChatWithUser != null)
        {
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
            SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
            if (ss != null)
            {
                if (ss.channels[channel] != null && (ss.channels[channel] as SavedChannelsForTempRoom) != null)
                {

                    SavedChannelsForTempRoom loadedchannel = (ss.channels[channel] as SavedChannelsForTempRoom);
                    ImageMessage msgobj = loadedchannel.savedmessages.Find(x => x.idofmessage == idofmessage) as ImageMessage;
                    if (msgobj != null)
                    {

                        if (searched == false)
                        {
                            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
                            {
                                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
                            }
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(msgobj);
                            loadedchannel.messages.Add(msgobj);

                        }
                        else
                        {
                            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(msgobj);
                        }
                        if (initializationmessage)
                        {
                            verifyfetch(channel, searched, false);
                        }
                        ESMA.MainWindow.instance.ChatRefreshList(0, msgobj, searched);

                        return;
                    }
                }
            }
        }

        //for this group, we will create a temporary regular message placeholder, until this message loads, in which the new message will replace this one. 
        Message temp_msgobj = new Message(id, username, "Message decryption pending...", idofmessage, false, true, datetime);

        if (searched == false)
        {
            if (ChatManager.CurrentChannel != channel && (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Count >= 30)
            {
                (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.RemoveAt(0);
            }
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);


            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

            if (ChatManager.GetSavedServerByID(serverid) != null)
            {
                (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages.Add(temp_msgobj);
            }
        }
        else
        {
            (ESMA.ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.Add(temp_msgobj);
        }
        ESMA.MainWindow.instance.ChatRefreshList(0, temp_msgobj, searched);
        if (initializationmessage)
        {
            verifyfetch(channel, searched, false);
        }
        if (Methods.foundchannel(channel) == false)
        {
            GC.Collect();
            return;
        }
        string link = ESMA.DatabaseCalls.host + "/" + Methods.GetServerDir();

        (searched ? ESMA.BackgroundWorker.Searchactions : ESMA.BackgroundWorker.Download_Actions).Add((Action)(() =>
        { 
            string encryptedString = "";
            try
            {
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    encryptedString = webclient.DownloadString(link + t_md5);
                }
                if (String.IsNullOrEmpty(encryptedString) == true)
                {
                    throw new Exception();
                }
            }
            catch
            {

                //fail to retrieve

                string dc = null;
                if (ChatManager.CurrentChatWithUser == null) 
                {
                    dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                }
                else
                {
                    dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                }

                Message msgobj = new ImageMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, "", t_md5, "", datetime);
                ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);

                return;
            }



            byte[] decrypted_bytes;
            string Decrypt = ESMA.Methods.DecryptString(encryptedString, ESMA.ChatManager.selectedroompassasbytes);
            if (Decrypt != null)
            {
                decrypted_bytes = Convert.FromBase64String(Decrypt);
            }
            else
            {
                string dc = null;
                if (ChatManager.CurrentChatWithUser == null)
                {
                    dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                }
                else
                {
                    dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                }
                Message msgobj = new ImageMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, "", t_md5, "", datetime);
                ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
                return;
            }
            if (File.Exists(exportfile_t) == false || File.Exists(exportfile_t) == true && ESMA.Methods.GetFileMD5Signature(exportfile_t) != t_md5)
            {
                try
                {
                    if (File.Exists(exportfile_t))
                    {
                        File.Delete(exportfile_t);
                    }
                    File.WriteAllBytes(exportfile_t, decrypted_bytes);

                }
                catch
                {

                    string dc = null;
                    if (ChatManager.CurrentChatWithUser == null)
                    {
                        dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                    }
                    else
                    {
                        dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                    }
                    Message msgobj = new ImageMessage(id, username, dc ?? "Error loading message content.", idofmessage, md5, "", t_md5, "", datetime);
                    ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
                    return;
                }
            }
            {
                string dc = null;
                if (ChatManager.CurrentChatWithUser == null)
                {
                    dc = ESMA.Methods.DecryptString(msg, ESMA.ChatManager.selectedroompassasbytes);
                }
                else
                {
                    dc = ESMA.Methods.AsymetricalDecryption(msg, ChatManager.MyUserPrivateKey);
                }
                Message msgobj = new ImageMessage(id, username, dc ?? "Unable to decrypt.", idofmessage, md5, exportfile, t_md5, exportfile_t, datetime);
                ESMA.MainWindow.instance.ChatRefreshList(2, msgobj, temp_msgobj, channel, searched);
            }
        }));
        GC.Collect();
    }
}
