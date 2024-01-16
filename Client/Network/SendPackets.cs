
using System;
using System.Collections.Generic;
using System.Collections;
using ESMA;
using Org.BouncyCastle.Bcpg;
using System.Runtime.Remoting.Channels;
using System.Windows;
using Org.BouncyCastle.Utilities;
using Microsoft.WindowsAPICodePack.Net;

public class SendPackets
{
    private static void SendTCP(Packet packet)
    {
        packet._Finalize();
        if (NetworkManager.instance.tcp != null)
            NetworkManager.instance.tcp.Send(packet);
    }

    private static void SendUDP(Packet packet)
    {
        packet._Finalize();
        if (NetworkManager.instance.udp != null)
            NetworkManager.instance.udp.Send(packet);
    }
    private static void SendCallUDP(Packet packet)
    {
        packet._Finalize();
        if (NetworkManager.instance.call_udp != null)
            NetworkManager.instance.call_udp.Send(packet);

    }
    public static void GoodConnected()
    {
        if (NetworkManager.MyAccountID == null)
            return;
        using (Packet packet = new Packet(0))
        {
            packet.Write((int)NetworkManager.MyAccountID);
            packet.Write(NetworkManager.instance.public_id);
            SendTCP(packet);
        }
    
    }
    public static void SeeIfRoomExistsBeforeJoin(string roomname)
    {
        using (Packet packet = new Packet(18))
        {
            packet.Write(roomname);
            SendTCP(packet);
        }
    }
    public static void TCPReady()
    {
        using (Packet packet = new Packet(15))
        {
            packet.Write((string)NetworkManager.myusername);
            packet.Write((int)NetworkManager.MyAccountID);
            SendTCP(packet);
        }

    }
    public static void CallSomeone(int calledaccount_ID)
    {
        using (Packet packet = new Packet(28))
        {
            packet.Write(calledaccount_ID);
            SendTCP(packet);
        }
    }
    public static void CallResponse(bool resp)
    {
        using (Packet packet = new Packet(29))
        {
            packet.Write(resp);
            SendTCP(packet);
        }
    }
    public static void ClientRequestMakeRoom(string key, string roomname)
    {
        BackgroundWorker.Download_Actions.Clear();
        BackgroundWorker.Searchactions.Clear();
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            ESMA.MainWindow.UserList.ItemsSource = null;
            ChatManager.UserList.Clear();
        });
        if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel != null) // get out of a possible current voice channel before joining another room
        {
            ChatManager.my_room_user.StopTransmission();
            SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
        }
        MainWindow.instance.EnableorDisableLoading("Creating room...", true);
        using (Packet packet = new Packet(1))
        {

            packet.Write(key);
            packet.Write(roomname);
            SendTCP(packet);
        }

    }
    public static void RequestMoreMessages(int CurrentMessagesToFetch, int indexcap, int channel)
    {
        ESMA.MainWindow.instance.EnableorDisableLoading("Retrieving messages...", true);
        using (Packet packet = new Packet(7))
        {
           
            packet.Write(CurrentMessagesToFetch);
            packet.Write(indexcap);
            packet.Write(channel);
            SendTCP(packet);
        }

    }
    public static void SendSuccessInGettingNewMessageAmount(int passed_amountcurrently, int passed_indexcap, int channel)
    {


        using (Packet packet = new Packet(8))
        {
            packet.Write(passed_amountcurrently);
            packet.Write(passed_indexcap);
            packet.Write(channel);
            SendTCP(packet);
        }

    }
    public static void InitiateMessageSearch(string msg, int room, int channel, bool saved)
    {
        using (Packet packet = new Packet(19))
        {
            packet.Write(msg);
            packet.Write(room);
            packet.Write(channel);
            packet.Write(saved);
            SendTCP(packet);
        }
    }
    public static void InitiateMessageSearch(string msg)
    {

        using (Packet packet = new Packet(19))
        {
            packet.Write(msg);
            packet.Write(-1);
            packet.Write(0);
            packet.Write(false);
            SendTCP(packet);
        }

    }
    public static void CreateChannel(int serverid, string channelname, int channeltype)
    {
        using (Packet packet = new Packet(22))
        {
            packet.Write(serverid);
            packet.Write(channelname);
            packet.Write(channeltype);
            SendTCP(packet);
        }
    }
    public static void ServerHeartbeat()
    {
        using (Packet packet = new Packet(48))
        {
            SendTCP(packet);
        }
    }
    public static void DeleteChannel(int serverid, int channelid)
    {
        using (Packet packet = new Packet(39))
        {
            packet.Write(serverid);
            packet.Write(channelid);
            SendTCP(packet);
        }
    }
    public static void LeaveServer(int serverid)
    {
        using (Packet packet = new Packet(46))
        {
            packet.Write(serverid);
            SendTCP(packet);
        }
    }
    public static void LeaveDMS(int other)
    {
        using (Packet packet = new Packet(47))
        {
            packet.Write(other);
            SendTCP(packet);
        }
    }
    public static void RemoveOrAddPLRole(int serverid, int userid, int role, bool add)
    {
        using (Packet packet = new Packet(36))
        {
            packet.Write(serverid);
            packet.Write(userid);
            packet.Write(role);
            packet.Write(add);
            SendTCP(packet);
        }
    }
    public static void RemoveOrAddChannelRole(int serverid, int channelid, int role, bool add)
    {
        using (Packet packet = new Packet(38))
        {
            packet.Write(serverid);
            packet.Write(channelid);
            packet.Write(role);
            packet.Write(add);
            SendTCP(packet);
        }
    }
    public static void LoadSelectedMessageFromSearch(int msgid, int room, int channel, bool saved)
    {

        using (Packet packet = new Packet(20))
        {
            packet.Write(msgid);
            packet.Write(room);
            packet.Write(channel);
            packet.Write(saved);
            SendTCP(packet);
        }

    }
    public static void FriendRequest(string toadd)
    {

        using (Packet packet = new Packet(33))
        {
            packet.Write(toadd);

            SendTCP(packet);
        }

    }
    public static void ReceivedLoadSelectedMessageFromSearch(int msgid, int room, int channel, bool saved)
    {
        using (Packet packet = new Packet(21))
        {
            packet.Write(msgid);
            packet.Write(room);
            packet.Write(channel);
            packet.Write(saved);
            SendTCP(packet);
        }
    }
    public static void ClientRequestJoinRoom( string roomtojoin, bool isserver)
    {
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            BackgroundWorker.Download_Actions.Clear();
            BackgroundWorker.Searchactions.Clear();
            ESMA.MainWindow.UserList.ItemsSource = null;
            ChatManager.UserList.Clear();
        });

        if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel != null)
        {
            ChatManager.my_room_user.StopTransmission();
            SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
        }
        ChatManager.StillGettingVerificationOfJoinRoom = true;
        using (Packet packet = new Packet(4))
        {   
            packet.Write(roomtojoin);
            packet.Write(isserver);
            SendTCP(packet);
        }
    }
    public static void JustCheckPassword(string roomtojoin)
    {
        using (Packet packet = new Packet(31))
        {
            packet.Write(roomtojoin);
            SendTCP(packet);
        }
    }
    public static void JoinRoomKeySuccess(int roomtojoinbyid, bool issaved)
    {
        using (Packet packet = new Packet(6))
        {

            packet.Write(roomtojoinbyid);
            packet.Write(issaved);
            SendTCP(packet);
        }

    }
    public static void ChangeRolePrecedence(int serverid, int role, bool up)
    {
        using (Packet packet = new Packet(45))
        {
            packet.Write(serverid);
            packet.Write(role);  
            packet.Write(up);
            SendTCP(packet);
        }
    }
    public static void UpdateRoles(int serverid, int roleid, string rolename, int kick, int mute, int ban, int dm, int mc, int mr, string hexcode)
    {
        using (Packet packet = new Packet(35))
        {
            packet.Write(serverid);
            packet.Write(roleid);
            packet.Write(rolename);
            packet.Write(kick);
            packet.Write(mute);
            packet.Write(ban);
            packet.Write(dm);
            packet.Write(mc);
            packet.Write(mr); 
            packet.Write(hexcode);
            SendTCP(packet);
        }
    }

    public static void UpdateChannelSettings(int serverid, int channelid, int ro, int incuser)
    {
        using (Packet packet = new Packet(40))
        {
            packet.Write(serverid);
            packet.Write(channelid);
            packet.Write(ro);
            packet.Write(incuser);
            SendTCP(packet);
        }
    }
    public static void PunishUser(int serverid, int punsiher, int punsihed, int punishid)
    {
        using (Packet packet = new Packet(41))
        {
            packet.Write(serverid);
            packet.Write(punsiher);
            packet.Write(punsihed);
            packet.Write(punishid);
            SendTCP(packet);
        }
    }
    public static void RemoveRole(int serverid, int roleid)
    {
        using (Packet packet = new Packet(37))
        {
            packet.Write(serverid);
            packet.Write(roleid);
            SendTCP(packet);
        }
    }
    public static void SendMessage(string encryptedmessage, int channel, int? CurrentServerID, bool? for_me)
    {

        using (Packet packet = new Packet(2))
        {
            packet.Write(CurrentServerID == null ? -1 : (int)CurrentServerID);
            if (for_me == null)
            {
                packet.Write(ESMA.Methods.EncryptString(encryptedmessage, ESMA.ChatManager.selectedroompassasbytes) ?? "Unable to encrypt.");
            }
            else
            {
                if (for_me == false)
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.CurrentChatWithUserKey ?? "Unable to encrypt."));
                }
                else
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.MyUserPublicKey ?? "Unable to encrypt."));
                }
            }
            packet.Write(channel);
            packet.Write(for_me ?? false);
            SendTCP(packet);
            
        }

    }
    public static void SendNewChannel(int channel, int curroom)
    {
       
        using (Packet packet = new Packet(12))
        {

            packet.Write(channel);
            packet.Write(curroom);
            SendTCP(packet);
        }
    }
    public static void RequestMessagesAfterPMVerify()
    {
        using (Packet packet = new Packet(27))
        {
            SendTCP(packet);
        }
    }
    public static void RequestMessagesAfterJoin(int channel, bool saved)
    {
        using (Packet packet = new Packet(14))
        {
            packet.Write(channel);
            packet.Write(saved);
            SendTCP(packet);
        }
    }

    public static void SendPictureMessage(string encryptedmessage, string md5hash, string T_md5hash, int channel, int? CurrentServerID, bool? for_me)
    {
        using (Packet packet = new Packet(3))
        {
            packet.Write(CurrentServerID == null ? -1 : (int)CurrentServerID);
            if (for_me == null)
            {
                packet.Write(ESMA.Methods.EncryptString(encryptedmessage, ESMA.ChatManager.selectedroompassasbytes) ?? "Unable to encrypt.");
            }
            else
            {
                if (for_me == false)
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.CurrentChatWithUserKey ?? "Unable to encrypt."));
                }
                else
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.MyUserPublicKey ?? "Unable to encrypt."));
                }
            }
            packet.Write(md5hash);
            packet.Write(T_md5hash);
            packet.Write(channel);
            packet.Write(for_me == true ? true : false);
            SendTCP(packet);
        }

    }
    public static void SendVideoMessage(string encryptedmessage, string md5hash, string T_md5hash, int channel, int? CurrentServerID, bool? for_me)
    {
        using (Packet packet = new Packet(5))
        {
            packet.Write(CurrentServerID == null ? -1 : (int)CurrentServerID);
            if (for_me == null)
            {
                packet.Write(ESMA.Methods.EncryptString(encryptedmessage, ESMA.ChatManager.selectedroompassasbytes) ?? "Unable to encrypt.");
            }
            else
            {
                if (for_me == false)
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.CurrentChatWithUserKey ?? "Unable to encrypt."));
                }
                else
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.MyUserPublicKey ?? "Unable to encrypt."));
                }
            }
            packet.Write(md5hash);
            packet.Write(T_md5hash);
            packet.Write(channel);
            packet.Write(for_me == true ? true : false);
            SendTCP(packet);
        }

    }
    public static void SendNewUserProfilePicture(string profilemd5, int account_ID)
    {
        using (Packet packet = new Packet(16))
        {
            packet.Write(account_ID);
            packet.Write(profilemd5);
            SendTCP(packet);
        }
    }
    public static void RequestOpenProfile(int? serverid, int account_ID)
    {
        using (Packet packet = new Packet(42))
        {
            packet.Write(account_ID);
            packet.Write(serverid ?? -1);
            SendTCP(packet);
        }
    }
    public static void GetProfilePicture(int account_ID)
    {
        using (Packet packet = new Packet(44))
        {
            packet.Write(account_ID);
            SendTCP(packet);
        }
    }
    public static void RequestSaveProfile(string sites)
    {
        using (Packet packet = new Packet(43))
        {
            packet.Write(sites);
            SendTCP(packet);
        }
    }
    public static void NotifyServerOfPMStart(int otheraccount_ID)
    {
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            BackgroundWorker.Download_Actions.Clear();
            BackgroundWorker.Searchactions.Clear();
            ESMA.MainWindow.UserList.ItemsSource = null;
            ChatManager.UserList.Clear();
        });

        if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel != null)
        {
            ChatManager.my_room_user.StopTransmission();
            SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
        }
        using (Packet packet = new Packet(26))
        {
            packet.Write(otheraccount_ID);
            SendTCP(packet);
        }
    }
    public static void ReadyForPMDatabaseWrite(int otheraccount_ID, string myencryptedkey, string otherusersencryptedkey)
    {
        using (Packet packet = new Packet(32))
        {
            packet.Write((int)NetworkManager.MyAccountID);
            packet.Write(otheraccount_ID);
            packet.Write(myencryptedkey);
            packet.Write(otherusersencryptedkey);
            SendTCP(packet);
        }
    }
    public static void RequestFriends(int populatewhere)
    {
        using (Packet packet = new Packet(23))
        {
            packet.Write(populatewhere);
            SendTCP(packet);
        }
    }
    public static void UDPKeepAlive_Call()
    {
        using (Packet packet = new Packet(50))
        {
            SendCallUDP(packet);
        }
    }
    public static void UDPKeepAlive()
    {
      
        using (Packet packet = new Packet(49))
        {
            SendUDP(packet);
        }
    }
    public static void RequestPendingFriends()
    {
        using (Packet packet = new Packet(34))
        {
            SendTCP(packet);
        }
    }
    public static void LeaveAllServers()
    {
        BackgroundWorker.SFTP_Actions.Clear();
        if (SFTPCalls.uploader.IsConnected == true)
            SFTPCalls.cur_sftp_action = false;

        NetworkManager.instance.udp.Disconnect();
       
        using (Packet packet = new Packet(25))
        {
            SendTCP(packet);
        }
    }
    public static void AddOrRemoveFriend(int type, int otheraccount_ID)
    {
        using (Packet packet = new Packet(24))
        {
            packet.Write(type);
            packet.Write(otheraccount_ID);
            SendTCP(packet);
        }
    }
    public static void CreateSavedRoom(string name, string seed, string md5)
    {
        BackgroundWorker.Download_Actions.Clear();
        BackgroundWorker.Searchactions.Clear();
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            ESMA.MainWindow.UserList.ItemsSource = null;
            ChatManager.UserList.Clear();
        });
        if (md5 == "")
        {
            MainWindow.instance.MessageBoxShow("Server icon could not be uploaded.");
            return;
        }
        if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel != null)
        {
            ChatManager.my_room_user.StopTransmission();
            SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
        }
        MainWindow.instance.EnableorDisableLoading("Creating room...", true);
        using (Packet packet = new Packet(17))
        {
            packet.Write(name);
            packet.Write(seed);
            packet.Write(md5);
            SendTCP(packet);
        }

    }

    public static void SendAudioMessage(string encryptedmessage, string md5hash, int channel, int? CurrentServerID, bool? for_me)
    {
        using (Packet packet = new Packet(9))
        {
            packet.Write(CurrentServerID == null ? -1 : (int)CurrentServerID);
            if (for_me == null)
            {
                packet.Write(ESMA.Methods.EncryptString(encryptedmessage, ESMA.ChatManager.selectedroompassasbytes) ?? "Unable to encrypt.");
            }
            else
            {
                if (for_me == false)
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.CurrentChatWithUserKey ?? "Unable to encrypt."));
                }
                else
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.MyUserPublicKey ?? "Unable to encrypt."));
                }
            }
            packet.Write(md5hash);
            packet.Write(channel);
            packet.Write(for_me == true ? true : false);
            SendTCP(packet);
        }

    }
 
    public static void SendFileMessage(string encryptedmessage, string md5hash, string extension, long size, int channel, int? CurrentServerID, bool? for_me)
    {
        using (Packet packet = new Packet(10))
        {
            packet.Write(CurrentServerID == null ? -1 : (int)CurrentServerID);
            if (for_me == null)
            {
                packet.Write(ESMA.Methods.EncryptString(encryptedmessage, ESMA.ChatManager.selectedroompassasbytes) ?? "Unable to encrypt.");
            }
            else
            {
                if (for_me == false)
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.CurrentChatWithUserKey ?? "Unable to encrypt."));
                }
                else
                {
                    packet.Write(ESMA.Methods.AsymetricalEncryption(encryptedmessage, ChatManager.MyUserPublicKey ?? "Unable to encrypt."));
                }
            }
            packet.Write(md5hash);
            packet.Write(extension);
            packet.Write(size);
            packet.Write(channel);
            packet.Write(for_me == true ? true : false);

            SendTCP(packet);
        }

    }
    public static void SendVoiceBytes(List<byte>bytes, int channelkey, int savedroom)
    {
        if(ChatManager.CurTempRoom == null && ChatManager.CurrentSavedServer == null)
        {
            return;
        }
       
        using (Packet packet = new Packet(11))
        {
            packet.Write(bytes);
            packet.Write(channelkey);
            packet.Write(ChatManager.CurrentSavedServer == null ? (int)ChatManager.CurTempRoom : (int)ChatManager.CurrentSavedServer);
            packet.Write(ChatManager.CurrentSavedServer != null);
            SendUDP(packet);
        }
    }
    public static void SendVoiceBytesInCall(List<byte> bytes)
    {
        if (ChatManager.UserInCallWith == null )
        {
            return;
        }
     
        using (Packet packet = new Packet(30))
        {
            packet.Write(bytes);
            SendCallUDP(packet);
        }
    }
    public static void RequestUserVoiceChannels(int cursavedserver)
    {
        using (Packet packet = new Packet(13))
        {
            packet.Write(cursavedserver);
            SendTCP(packet);
        }
    }
}
