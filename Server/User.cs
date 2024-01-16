using System.Net.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Mysqlx.Crud;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Bcpg;
using Google.Protobuf.WellKnownTypes;
using MySqlX.XDevAPI.Common;
using Mysqlx.Session;
using Org.BouncyCastle.Crypto.Paddings;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatAppServer
{
    public class User
    {
        public static int TCPBUFFSIZE = 64000;
        public string username;
        public int id;
        public int accountid;
        public bool existingconnection;
        public bool startdis;
        public int CurrentVoiceChannelID = -1;
        public bool SuccessfulTCPConnect;
        public int? CurrentSavedServerID = null;
        public int? UserTalkingTo = null;
        public int TimeLeftInPendingCall;
        public int? CurrentCaller = null;
        public int? PotentialCaller = null;
        public int Ping_Timer = 0;
        public OnFlyChannelManager CurrentUDPForCall;
        //tcp
        public TcpClient tcp_socket;
        public NetworkStream stream;
        public Thread tcp_receivingthread;
        public User(int _public_id)
        {
            id = _public_id;
        }

        public void ClientInit(string name, int account_ID)
        {
            username = name;
            accountid = account_ID;

            for (int i = 0; i < NetworkManager.clients.Count; ++i)
            {
                //Send to this client to all currently on this server
                if (NetworkManager.clients[i].id != id /*<-- dont send yourself to yourself , you already got that */)
                {
                    if (NetworkManager.clients[i] != null && NetworkManager.clients[i].tcp_socket != null && NetworkManager.clients[i].tcp_socket.Connected == true)
                    {
                        SendPackets.CreateUser(id, NetworkManager.clients[i]);
                    }
                }
                //Send to all this client
                SendPackets.CreateUser(NetworkManager.clients[i].id, this);
            }
        }

        public void TCPReceive()
        {
            List<byte> packet_buffer = new List<byte>();
            while (stream != null)
            {
                int recv_bytes = 0;
                byte[] recv_buff = new byte[User.TCPBUFFSIZE];
                try
                {
                    recv_bytes = stream.Read(recv_buff, 0, User.TCPBUFFSIZE);
                }
                catch
                {

                }

                if (recv_bytes <= 0)
                {
                    if (NetworkManager.GetClientById(id) != null && NetworkManager.GetClientById(id).startdis == false)
                    {
                        int myid = id;
                        NetworkManager.TaskTo_ReceiveThread((Action)(() =>
                        {
                            NetworkManager.GetClientById(myid).FullDisconnect();
                        }));
                    }
                    break;
                }
                else
                {
                    packet_buffer.AddRange(recv_buff.Take(recv_bytes).ToArray());
                    if (packet_buffer[packet_buffer.Count - 1] == 230 && packet_buffer[packet_buffer.Count - 2] == 231 && packet_buffer[packet_buffer.Count - 3] == 232 && packet_buffer[packet_buffer.Count - 4] == 233)
                    {
                        byte[] toarr = packet_buffer.ToArray();
                        NetworkManager.TaskTo_ReceiveThread((Action)(() =>
                        {
                            using (Packet packet = new Packet(toarr))
                            {
                                bool error = false;
                                while (packet._seeker < (packet.Received_Bytes.Length - 1))
                                {
                                    int task_id = packet.GetInt(ref error); if (error == true) return;
                                    switch (task_id) //Find packet by id
                                    {
                                        case 0:
                                            ReceivePackets.GoodConnection(id, packet, ref error);
                                            break;
                                        case 1:
                                            ReceivePackets.ClientRequestMakeRoom(id, packet, ref error);
                                            break;
                                        case 2:
                                            ReceivePackets.SendMessage(id, packet, ref error);
                                            break;
                                        case 3:
                                            ReceivePackets.SendPictureMessage(id, packet, ref error);
                                            break;
                                        case 4:
                                            ReceivePackets.ClientRequestJoinRoom(id, packet, ref error);
                                            break;
                                        case 5:
                                            ReceivePackets.SendVideoMessage(id, packet, ref error);
                                            break;
                                        case 6:
                                            ReceivePackets.JoinRoomKeySuccess(id, packet, ref error);
                                            break;
                                        case 7:
                                            ReceivePackets.RequestMoreMessages(id, packet, ref error);
                                            break;
                                        case 8:
                                            ReceivePackets.SendSuccessInGettingNewMessageAmount(id, packet, ref error);
                                            break;
                                        case 9:
                                            ReceivePackets.SendAudioMessage(id, packet, ref error);
                                            break;
                                        case 10:
                                            ReceivePackets.SendFileMessage(id, packet, ref error);
                                            break;
                                        case 12:
                                            ReceivePackets.SendNewVoiceChannel(id, packet, ref error);
                                            break;
                                        case 13:
                                            ReceivePackets.RequestUserVoiceChannels(id, packet, ref error);
                                            break;
                                        case 14:
                                            ReceivePackets.RequestMessagesAfterJoin(id, packet, ref error);
                                            break;
                                        case 15:
                                            ReceivePackets.TCPReady(id, packet, ref error);
                                            break;
                                        case 16:
                                            ReceivePackets.SendNewUserProfilePicture(id, packet, ref error);
                                            break;
                                        case 17:
                                            ReceivePackets.CreateSavedRoom(id, packet, ref error);
                                            break;
                                        case 18:
                                            ReceivePackets.SeeIfRoomExistsBeforeJoin(id, packet, ref error);
                                            break;
                                        case 19:
                                            ReceivePackets.InitiateMessageSearch(id, packet, ref error);
                                            break;
                                        case 20:
                                            ReceivePackets.LoadSelectedMessageFromSearch(id, packet, ref error);
                                            break;
                                        case 21:
                                            ReceivePackets.ReceivedLoadSelectedMessageFromSearch(id, packet, ref error);
                                            break;
                                        case 22:
                                            ReceivePackets.CreateChannel(id, packet, ref error);
                                            break;
                                        case 23:
                                            ReceivePackets.RequestFriends(id, packet, ref error);
                                            break;
                                        case 24:
                                            ReceivePackets.AddOrRemoveFriend(id, packet, ref error);
                                            break;
                                        case 25:
                                            ReceivePackets.ClientRequestLeaveRoom(id, packet, ref error);
                                            break;
                                        case 26:
                                            ReceivePackets.NotifyServerOfPMStart(id, packet, ref error);
                                            break;
                                        case 27:
                                            ReceivePackets.RequestMessagesAfterPMVerify(id, packet, ref error);
                                            break;
                                        case 28:
                                            ReceivePackets.CallSomeone(id, packet, ref error);
                                            break;
                                        case 29:
                                            ReceivePackets.CallResponse(id, packet, ref error);
                                            break;
                                        case 31:
                                            ReceivePackets.JustCheckPassword(id, packet, ref error);
                                            break;
                                        case 32:
                                            ReceivePackets.ReadyForPMDatabaseWrite(id, packet, ref error);
                                            break;
                                        case 33:
                                            ReceivePackets.FriendRequest(id, packet, ref error);
                                            break;
                                        case 34:
                                            ReceivePackets.RequestPendingFriends(id, packet, ref error);
                                            break;
                                        case 35:
                                            ReceivePackets.UpdateRoles(id, packet, ref error);
                                            break;
                                        case 36:
                                            ReceivePackets.RemoveOrAddPLRole(id, packet, ref error);
                                            break;
                                        case 37:
                                            ReceivePackets.RemoveRole(id, packet, ref error);
                                            break;
                                        case 38:
                                            ReceivePackets.RemoveOrAddChannelRole(id, packet, ref error);
                                            break;
                                        case 39:
                                            ReceivePackets.DeleteChannel(id, packet, ref error);
                                            break;
                                        case 40:
                                            ReceivePackets.UpdateChannelSettings(id, packet, ref error);
                                            break;
                                        case 41:
                                            ReceivePackets.PunishUser(id, packet, ref error);
                                            break;
                                        case 42:
                                            ReceivePackets.RequestOpenProfile(id, packet, ref error);
                                            break;
                                        case 43:
                                            ReceivePackets.RequestSaveProfile(id, packet, ref error);
                                            break;
                                        case 44:
                                            ReceivePackets.GetProfilePicture(id, packet, ref error);
                                            break;
                                        case 45:
                                            ReceivePackets.ChangeRolePrecedence(id, packet, ref error);
                                            break;
                                        case 46:
                                            ReceivePackets.UserLeaveServer(id, packet, ref error);
                                            break;
                                        case 47:
                                            ReceivePackets.UserLeaveDM(id, packet, ref error);
                                            break;
                                        case 48:
                                            ReceivePackets.ServerHeartbeat(id, packet, ref error);
                                            break;
                                        default:
                                            return;
                                    }

                                    if (error == true) return;
                                }
                            }
                        }));
                        packet_buffer.Clear();
                    }
                }
            }
            stream = null;
            tcp_socket = null;
        }
        public void FullDisconnect()
        {

            if (startdis == false)
            {
                startdis = true;


                try
                {
                    Console.WriteLine(string.Format("{0} has disconnected from server.", username));
                }
                catch (Exception ex) { }
                if(tcp_socket != null && tcp_socket.Connected)
                    tcp_socket.Close();

                if (NetworkManager.clients.Contains(this))
                {
                    OnFlyChannelManager cm = NetworkManager.ChannelManagers.Find(x => x.clients.Contains(this));

                    if (CurrentCaller != null)
                    {
                        User c = NetworkManager.GetClientByAccountId((int)CurrentCaller);
                        if (c != null)
                        {
                            if (cm != null)
                            {
                                if (cm.clients.Contains(c))
                                {
                                    cm.clients.Remove(c);
                                }
                            }
                            c.CurrentCaller = null;
                            ReceivePackets.pass_data_toserv(c, 1, -1);
                            c.PotentialCaller = null;
                            c.TimeLeftInPendingCall = 0;
                            SendPackets.SendCallResponse(c.id, null, null, -1);
                        }
                    }
                    if (cm != null)
                    {

                        cm.clients.Remove(this);
                        if (cm.clients.Count == 0)
                        {

                            cm.Stop();
                            NetworkManager.ChannelManagers.Remove(cm);
                        }
                    }
                    NetworkManager.ClientsToTrash.Add(this);



                    if (CurrentSavedServerID != null)
                    {
                        SendPackets.AddOrRemoveUserFromList(2, this, "", (int)CurrentSavedServerID, true);
                    }
                    if (UserTalkingTo != null && NetworkManager.GetClientByAccountId((int)UserTalkingTo) != null)
                    {
                        SendPackets.AddOrRemoveUserFromList(NetworkManager.GetClientByAccountId((int)UserTalkingTo).id, 2, this, "");
                    }
                    for (int i = 0; i < NetworkManager.temporaryRooms.Count; ++i)
                    {
                        if (NetworkManager.temporaryRooms[i].clientsinroom.Contains(this))
                        {
                            NetworkManager.temporaryRooms[i].clientsinroom.Remove(this);
                            if (NetworkManager.temporaryRooms[i].clientsinroom.Count > 0)
                            {
                                SendPackets.AddOrRemoveUserFromList(2, this, "", NetworkManager.temporaryRooms[i].id, false);
                            }
                        }
                        if (NetworkManager.temporaryRooms[i].clientsinroom.Count == 0)
                        {
                            Console.WriteLine(string.Format("Room with id of {0} has been destroyed.", NetworkManager.temporaryRooms[i].id));

                            for (int ii = 0; ii < NetworkManager.ChannelManagers.Count; ++ii)
                            {
                                if (NetworkManager.ChannelManagers[ii].roomtype == 0 && NetworkManager.ChannelManagers[ii].ServerID == NetworkManager.temporaryRooms[i].id)
                                {
                                    NetworkManager.ChannelManagers[ii].Stop();
                                    NetworkManager.ChannelManagers.RemoveAt(ii);
                                    break;
                                }
                            }
                            RoomWithThreads st = NetworkManager.temproomthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == NetworkManager.temporaryRooms[i].id);
                            if (st != null)
                            {
                                st.running = false;
                                NetworkManager.temproomthreads.Remove(st);
                            }
                            NetworkManager.temporaryRooms.RemoveAt(i);
                            // GC.Collect(); keep in mind still might be important
                        }
                    }

                }

                try
                {
                    SendPackets.ClientDisconnected(id);
                }
                catch (Exception ex)
                {
                }
            }
        }
 
    }
}
