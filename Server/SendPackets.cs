

using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Utilities;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static Mysqlx.Expect.Open.Types.Condition.Types;

namespace ChatAppServer
{
    public class SendPackets
    {

        private static void SendTCP(int publicid, Packet packet)
        {
            packet._Finalize();
            try
            {
                User c = NetworkManager.GetClientById(publicid);
                if (c != null && c.startdis == false && c.tcp_socket != null && c.tcp_socket.Connected == true)
                {
                    byte[] bytes = packet.Bytes_To_Send.ToArray();
                    c.stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Failed to send TCP Data");
            }
        }
        private static void SendTCPToAll( Packet packet)
        {
            packet._Finalize();
            try
            {
                for (int i = 0; i < NetworkManager.clients.Count; i++)
                {
                    User c = NetworkManager.clients[i];
                    if (c != null && c.startdis == false && c.tcp_socket != null && c.tcp_socket.Connected == true)
                    {
                        byte[] bytes = packet.Bytes_To_Send.ToArray();
                        c.stream.Write(bytes, 0, bytes.Length);
                    }
                }
               
            }
            catch (Exception ex)
            { 
                Console.WriteLine("Failed to send TCP Data to all");
            }


        }
        private static void SendTCPToAllInRoom(int roomid, Packet packet)
        {
            packet._Finalize();
            try
            {
                User[] c = NetworkManager.temporaryRooms.First(x => x.id == roomid).clientsinroom.ToArray();
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] != null && c[i].startdis == false && c[i].tcp_socket != null && c[i].tcp_socket.Connected == true)
                    {
                        byte[] bytes = packet.Bytes_To_Send.ToArray();
                        c[i].stream.Write(bytes, 0, bytes.Length);
                    }
                }

            }
            catch (Exception ex)
            { 
                Console.WriteLine("Failed to send TCP Data to all in room");
            }
        }

    
        private static void SendTCPToAllInSavedRoom(int roomid, Packet packet)
        {
            packet._Finalize();
            try
            {
                for (int i = 0; i < NetworkManager.clients.Count; i++)
                {
                    User c = NetworkManager.clients[i];
                    if (c != null && c.startdis == false && c.CurrentSavedServerID != null && c.CurrentSavedServerID == roomid && c.tcp_socket != null && c.tcp_socket.Connected == true)
                    {
                        byte[] bytes = packet.Bytes_To_Send.ToArray();
                        c.stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine("Failed to send TCP Data to all in saved room");
            }
        }
      

        public static void SendTCPReady(int publicid)
        {
            using (Packet packet = new Packet(16))
            {
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendConnected(int publicid)
        {
            using (Packet packet = new Packet(0))
            {
                packet.Write(publicid);
                SendPackets.SendTCP(publicid, packet);
            }
        }

        public static void SendBackMessageSpanForSearch(int publicid, int span, int msgid, int roomid, int channel, bool saved)
        {
            using (Packet packet = new Packet(24))
            {
                packet.Write(span);
                packet.Write(msgid);
                packet.Write(roomid);
                packet.Write(channel);
                packet.Write(saved);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SavedRoomCreationSuccess(int publicid, byte[] channels, int thechannel, int channelid, byte[] roles)
        {
            using (Packet packet = new Packet(18))
            {
                OnFlyChannelManager cm = NetworkManager.GetUserRoomManager(NetworkManager.GetClientById(publicid));
                packet.Write(channels);
                packet.Write(thechannel);
                packet.Write(channelid); 
                packet.Write(roles);
                packet.Write(cm.port);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void ReceiveNewProfilePicture(int account_ID, string md5)
        {
            using (Packet packet = new Packet(17))
            {
                packet.Write(account_ID);
                packet.Write(md5);
                SendPackets.SendTCPToAll(packet);
            }
        }
        public static void SendbackRoles(int serverid, byte[] roles, int removal)
        {
            using (Packet packet = new Packet(35))
            {
                packet.Write(roles);
                packet.Write(removal);
                SendPackets.SendTCPToAllInSavedRoom(serverid, packet);
            }
        }
        public static void SendBackProfilePicture(int _public_id, int acc, string pic)
        {
            using (Packet packet = new Packet(41))
            {
                packet.Write(acc);
                packet.Write(pic);
                SendPackets.SendTCP(_public_id, packet);
            }
        }
        public static void OpenUserProfile(int fromuser, int acc, string bio, string media, string name, string creation, string friends, string roles, string reqroles)
        {
            using (Packet packet = new Packet(39))
            {
                packet.Write(acc);
                packet.Write(bio);
                packet.Write(media);
                packet.Write(name);
                packet.Write(creation);
                packet.Write(friends);
                packet.Write(roles);
                packet.Write(reqroles);
                SendPackets.SendTCP(fromuser, packet);
            }
        }
        public static void SendSuccessInProfileSave(int fromuser)
        {
            using (Packet packet = new Packet(40))
            {
                SendPackets.SendTCP(fromuser, packet);
            }
        }
        public static void SendBackRoomLeftSuccess(int publicid)
        {
            using (Packet packet = new Packet(27))
            {
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void PMReady(int publicid, int msgcount, int account_ID, string name, string key)
        {
            using (Packet packet = new Packet(28))
            {
                packet.Write(msgcount); // total messages
                packet.Write(msgcount > 30 ? 30 : msgcount); // messages to send back at a time
                packet.Write(account_ID);
                packet.Write(name);
                packet.Write(key);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendBackPMHash(int publicid, string hash)
        {
            using (Packet packet = new Packet(33))
            {
                packet.Write(hash); 
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendbackRoomCreationSuccess(int publicid, int roomid, byte[] channels, int thechannel)
        {
            using (Packet packet = new Packet(4))
            {
                OnFlyChannelManager cm = NetworkManager.GetUserRoomManager(NetworkManager.GetClientById(publicid));
                packet.Write(roomid);
                packet.Write(channels);
                packet.Write(thechannel);
                packet.Write(cm.port);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendBackMoreMessagesToClient(int publicid, int howmanynow, TemporaryRooms? room, int passed_amountcurrently, int passed_indexcap, int channelkey, string messagearraypeek)
        {
            using (Packet packet = new Packet(10))
            {
                
                packet.Write(howmanynow);
                packet.Write(passed_amountcurrently);
                packet.Write(passed_indexcap);
                packet.Write(channelkey);
                packet.Write(messagearraypeek);

                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void AddOrRemoveUserFromList(int publicid, int type, User c, string roles) 
        {
            if(c.startdis == true && type == 1)
            {
                return;
            }
            using (Packet packet = new Packet(11))
            {
                packet.Write(type);
                packet.Write(c.username);
                packet.Write(c.id);
                packet.Write(c.accountid);
                packet.Write(roles);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void RoomExists(int publicid, int roomid) 
        {
            using (Packet packet = new Packet(19))
            {
                packet.Write(roomid);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendMessage(int publicid, string message) 
        {
            using (Packet packet = new Packet(21))
            {
                packet.Write(message);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendCloseLoading(int publicid, string alert, int task) 
        {
            using (Packet packet = new Packet(22))
            {
                packet.Write(alert);
                packet.Write(task);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendToUserTheirRooms(int publicid, byte[] rooms) 
        {
            using (Packet packet = new Packet(20))
            {
                packet.Write(rooms);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendToUserTheirMSGSearch(int publicid, byte[] msgs) 
        {
            using (Packet packet = new Packet(23))
            {
                packet.Write(msgs);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendToSavedServerNewUsersWRole(int serverid, byte[] users) 
        {
            using (Packet packet = new Packet(36))
            {
                packet.Write(users);
                SendPackets.SendTCPToAllInSavedRoom(serverid, packet);
            }
        }
        public static void AddOrRemoveUserFromList(int type, User c, string roles, int roomid, bool saved) 
        {
            if (c.startdis == true && type == 1)
            {
                return;
            }
            using (Packet packet = new Packet(11))
            {
                packet.Write(type);
                packet.Write(c.username);
                packet.Write(c.id);
                packet.Write(c.accountid);
                packet.Write(roles);
                if (saved)
                {
                    SendPackets.SendTCPToAllInSavedRoom(roomid, packet);
                }
                else
                {
                    SendPackets.SendTCPToAllInRoom(roomid, packet);
                }
            }
        }
        public static void SendBackSavedServerChannels(int roomid, byte[] channels) 
        {

            using (Packet packet = new Packet(25))
            {
                packet.Write(channels);
                SendPackets.SendTCPToAllInSavedRoom(roomid, packet);
            }
        }
        public static void SendBackUserInVoiceChannel(int _public_id, int roomid, int channel, int? touser, bool saved ) 
        {
           
            using (Packet packet = new Packet(15))
            {
                packet.Write(_public_id);
                packet.Write(channel);
                if(touser == null)
                {
                    if(saved == false)
                    {
                        SendPackets.SendTCPToAllInRoom(roomid, packet);
                    }
                     else
                    {
                        SendPackets.SendTCPToAllInSavedRoom(roomid, packet);
                    }
                }
                else
                {
                    SendPackets.SendTCP((int)touser, packet);
                }
            }
        }
        public static void SendBackFriends(int publicid, byte[] friends, int populatewhere)
        {
            using (Packet packet = new Packet(26))
            {
                packet.Write(friends);
                packet.Write(populatewhere);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void CleanUpOnExternalKick(int publicid, int serverid, string servername)
        {
            using (Packet packet = new Packet(38))
            {
                packet.Write(serverid);
                packet.Write(servername);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void CleanUpOnDMErase(int publicid, int other)
        {
            using (Packet packet = new Packet(42))
            {
                packet.Write(other);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendBackPendingFriends(int publicid, byte[] friends)
        {
            using (Packet packet = new Packet(34))
            {
                packet.Write(friends);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendBackCall(int publicid, int fromacc)
        {
            using (Packet packet = new Packet(29))
            {   
                packet.Write(fromacc);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendCallResponse(int publicid, int? userincallwith, string? name, int callport)
        {
            using (Packet packet = new Packet(31))
            {
                packet.Write(userincallwith ?? -1);
                packet.Write(name ?? "");
                packet.Write(callport);
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void LeaveServerNotify(int acc, int server)
        {
            using (Packet packet = new Packet(37))
            {
                packet.Write(acc);
                SendPackets.SendTCPToAllInSavedRoom(server, packet);
            }
        }
        public static void CloseCallTimesUp(int publicid)
        {
            using (Packet packet = new Packet(30))
            {
                SendPackets.SendTCP(publicid, packet);
            }
        }
        public static void SendbackRoomJoinSuccess(int publicid, int roomid, bool issaved)
        {
           
            using (Packet packet = new Packet(7))
            {
                ChannelsForTempRoom textchannel;
                int textchannelkey;
                string builtchannelstring = "";
              
                try
                {
                    if(issaved == false)
                    {
                        OnFlyChannelManager cm = NetworkManager.GetUserRoomManager(NetworkManager.GetClientById(publicid));
                        textchannel = NetworkManager.GetFirstTextChannel(NetworkManager.GetRoomById(roomid))[0] as ChannelsForTempRoom;
                        textchannelkey = (int)NetworkManager.GetFirstTextChannel(NetworkManager.GetRoomById(roomid))[1];
                        if (textchannel == null)
                        {
                            return;
                        }

                        builtchannelstring = "";
                        var originalkeys = NetworkManager.GetRoomById(roomid).channels.Keys;
                        int[] originalmykeys = new int[NetworkManager.GetRoomById(roomid).channels.Keys.Count];
                        originalkeys.CopyTo(originalmykeys, 0);
                        for(int i = originalmykeys.Length - 1; i >= 0; --i)
                        {
                            if (NetworkManager.GetRoomById(roomid).channels[originalmykeys[i]].GetType() == typeof(ChannelsForTempRoom))
                            {
                                ChannelsForTempRoom cftr = ((ChannelsForTempRoom)NetworkManager.GetRoomById(roomid).channels[originalmykeys[i]]);
                                builtchannelstring += originalmykeys[i] + "❶" + cftr.ChannelName + "❶0❶" + cftr.messages.Count + "❶" + (cftr.messages.Count > 30 ? 30 : cftr.messages.Count) + "❶0/❶0❷";
                            }
                            if (NetworkManager.GetRoomById(roomid).channels[originalmykeys[i]].GetType() == typeof(VoiceChannelsForTempRoom))
                            {
                                builtchannelstring += originalmykeys[i] + "❶" + ((VoiceChannelsForTempRoom)NetworkManager.GetRoomById(roomid).channels[originalmykeys[i]]).ChannelName + "❶0/❶0❷";
                            }
                        }
                        packet.Write(roomid);
                        packet.Write(issaved);
                        packet.Write(textchannelkey);
                        packet.Write(textchannel.messages.Count); // total messages
                        packet.Write(textchannel.messages.Count > 30 ? 30 : textchannel.messages.Count); // messages to send back at a time
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(builtchannelstring);
                        packet.Write(bytes);
                        packet.Write(" ");
                        packet.Write(" ");
                        byte[] rbytes = System.Text.Encoding.UTF8.GetBytes("0❶Member❶0❶0❶0❶0❶0❶0❷");
                        packet.Write(rbytes);
                        packet.Write(cm.port);
                        SendPackets.SendTCP(publicid, packet);

                    }
                    else
                    {
                        OnFlyChannelManager cm = NetworkManager.GetUserRoomManager(NetworkManager.GetClientById(publicid));
                        if(cm == null)
                        {
                            SendPackets.SendMessage(publicid, "Unable to make connection with server UDP");
                            return;
                        }
                        int account_ID = NetworkManager.GetClientById(publicid).accountid; 
                        object[]? ServerJoinData = DatabaseCalls.GetServerJoinStuff(new int[2] { publicid, account_ID }, roomid);
                        
                        if (ServerJoinData == null)
                        {
                         
                            SendPackets.SendMessage(publicid, "Unable to fetch room data.");
                            return;
                        }
                       
                      
                        string builtstring = " ";
                        string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), roomid.ToString());
                        string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), roomid.ToString() + "\\" + (int)ServerJoinData[2] + ".txt");
                        if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                        {
                            SendPackets.SendMessage(publicid, "This channel no longer exists.");
                            return;
                        }
                        int ReadAttempts = 0;
                        string[] messages = new string[] { };
                    RestartRead:;
                        try
                        {
                            messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {
                                SendPackets.SendMessage(publicid, "Unable to read messages from channel.");
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }
                        int textchannellins = messages.Length;

                       
                        for (int i = textchannellins > 30 ? textchannellins - 30 : 0; i < textchannellins; ++i)
                        {
                            builtstring += messages[i].Split('❶')[4] + "|";
                        }
                       
                     
                        packet.Write((int)ServerJoinData[0]);
                        packet.Write((bool)ServerJoinData[1]);
                        packet.Write((int)ServerJoinData[2]);
                        packet.Write((int)ServerJoinData[3]); // total messages
                        packet.Write((int)ServerJoinData[4]); // messages to send back at a time
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes((string)ServerJoinData[5]);
                        packet.Write(bytes);
                        packet.Write(builtstring);
                        byte[] bytes2 = (byte[])ServerJoinData[6];
                        packet.Write(bytes2); // all users in this room
                        byte[] bytes3 = (byte[])ServerJoinData[7];
                        packet.Write(bytes3); // all users in this room
                        packet.Write(cm.port);
                        SendPackets.SendTCP(publicid, packet);
                    }
                 
                }
                catch {} 
            }
        }

        public static void SendbackRoomRoot(int publicid, int roomid, bool issaved, string key, bool acheck)
        {
            using (Packet packet = new Packet(9))
            {
                packet.Write(roomid);
                packet.Write(issaved);
                packet.Write(key);
                packet.Write(acheck);
                SendPackets.SendTCP(publicid, packet);
            }
        }


        public static void SendBackMessageToAllInRoom(MessagesForTempRoom message, TemporaryRooms? room, int? toclient, bool initial, int channeltogoto, int? savedserverid, bool searched)
        {
            using (Packet packet = new Packet(5))
            {

                packet.Write(message.accountidofsender);
                packet.Write(message.sendername);
                packet.Write(message.message);
                packet.Write(message.idofmessage);
                packet.Write(message.dateposted);
                packet.Write(channeltogoto);
                packet.Write(initial);
                packet.Write(searched);
                packet.Write(message.ownedbysender);
                if (toclient == null)
                {
                    if (savedserverid == null)
                    {
                        SendPackets.SendTCPToAllInRoom(room.id, packet);
                    }
                    else
                    {
                        SendPackets.SendTCPToAllInSavedRoom((int)savedserverid, packet);
                    }
                }
                else
                {
                    SendPackets.SendTCP((int)toclient, packet);
                }
            }
        }
        public static void SendBackPictureMessageToAllInRoom(PictureForTempRoom message, TemporaryRooms room, int? toclient, bool initial, int channeltogoto, int? savedserverid, bool searched)
        {
            using (Packet packet = new Packet(6))
            {
                
                packet.Write(message.accountidofsender);
                packet.Write(message.sendername);
                packet.Write(message.message);
                packet.Write(message.md5hash);
                packet.Write(message.md5hash_t);
                packet.Write(message.idofmessage);
                packet.Write(message.dateposted);
                packet.Write(channeltogoto);
                packet.Write(initial); 
                packet.Write(searched);
                packet.Write(message.ownedbysender);
                if (toclient == null)
                {
                    if (savedserverid == null)
                    {
                        SendPackets.SendTCPToAllInRoom(room.id, packet);
                    }
                    else
                    {
                        SendPackets.SendTCPToAllInSavedRoom((int)savedserverid, packet);
                    }
                }
                else
                {
                    SendPackets.SendTCP((int)toclient, packet);
                }
            }
        }
        public static void SendBackVideoMessageToAllInRoom(VideoForTempRoom message, TemporaryRooms room, int? toclient, bool initial, int channeltogoto, int? savedserverid, bool searched)
        {
            using (Packet packet = new Packet(8))
            {

                packet.Write(message.accountidofsender);
                packet.Write(message.sendername);
                packet.Write(message.message);
                packet.Write(message.md5hash);
                packet.Write(message.md5hash_t);
                packet.Write(message.idofmessage);
                packet.Write(message.dateposted);
                packet.Write(channeltogoto);
                packet.Write(initial);
                packet.Write(searched);
                packet.Write(message.ownedbysender);

                if (toclient == null)
                {
                    if (savedserverid == null)
                    {
                        SendPackets.SendTCPToAllInRoom(room.id, packet);
                    }
                    else
                    {
                        SendPackets.SendTCPToAllInSavedRoom((int)savedserverid, packet);
                    }
                }
                else
                {
                    SendPackets.SendTCP((int)toclient, packet);
                }
            }
        }
        public static void SendBackAudioMessageToAllInRoom(AudioForTempRoom message, TemporaryRooms room, int? toclient, bool initial, int channeltogoto, int? savedserverid, bool searched)
        {
            using (Packet packet = new Packet(12))
            {

                packet.Write(message.accountidofsender);
                packet.Write(message.sendername);
                packet.Write(message.message);
                packet.Write(message.md5hash);
                packet.Write(message.idofmessage);
                packet.Write(message.dateposted);
                packet.Write(channeltogoto);
                packet.Write(initial);
                packet.Write(searched);
                packet.Write(message.ownedbysender);
                if (toclient == null)
                {
                    if (savedserverid == null)
                    {
                        SendPackets.SendTCPToAllInRoom(room.id, packet);
                    }
                    else
                    {
                        SendPackets.SendTCPToAllInSavedRoom((int)savedserverid, packet);
                    }
                }
                else
                {
                    SendPackets.SendTCP((int)toclient, packet);
                }
            }
        }
        public static void SendBackFileMessageToAllInRoom(FileForTempRoom message, TemporaryRooms room, int? toclient, bool initial, int channeltogoto, int? savedserverid, bool searched)
        {
            using (Packet packet = new Packet(13))
            {

                packet.Write(message.accountidofsender);
                packet.Write(message.sendername);
                packet.Write(message.message);
                packet.Write(message.md5hash);
                packet.Write(message.idofmessage);
                packet.Write(message.dateposted);
                packet.Write(message.extension);
                packet.Write(message.Size);
                packet.Write(channeltogoto);
                packet.Write(initial);
                packet.Write(searched);
                packet.Write(message.ownedbysender);
                if (toclient == null)
                {
                    if (savedserverid == null)
                    {
                        SendPackets.SendTCPToAllInRoom(room.id, packet);
                    }
                    else
                    {
                        SendPackets.SendTCPToAllInSavedRoom((int)savedserverid, packet);
                    }
                }
                else
                {
                    SendPackets.SendTCP((int)toclient, packet);
                }
            }
        }
        public static void TCPCheck(int publicid)
        {
            using (Packet packet = new Packet(3))
            {
                SendPackets.SendTCP(publicid, packet);
            }
        }
   
        public static void CreateUser(int publicid, User c)
        {
            using (Packet packet = new Packet(1))
            {
                packet.Write(c.id);
                packet.Write(c.username);
                packet.Write(c.accountid);
               
                SendPackets.SendTCP(publicid, packet);
            }
        }
     
        public static void ClientDisconnected(int publicid)
        {
            using (Packet packet = new Packet(2))
            {
                packet.Write(publicid);
                SendPackets.SendTCPToAll(packet);
            }
        }
    }
}
