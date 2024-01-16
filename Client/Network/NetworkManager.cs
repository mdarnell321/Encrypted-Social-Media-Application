using ESMA;
using NAudio.Mixer;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Paddings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

public class NetworkManager
{
    public static NetworkManager instance;
    public static List<GlobalUser> global_users = new List<GlobalUser>();
    public static GlobalUser me = null;
    public static int? MyAccountID = null;
    public static int? MyPublicID;
    public static string myusername;
    public static List<UserList> Friends = new List<UserList>();
    public static List<UserList> DMS = new List<UserList>();
    private static string ip = "127.0.0.1";
    private readonly int port = 5000;
    public int public_id = 0;
    public TCP tcp;
    public UDP udp;
    public UDP call_udp;
    private bool connected = false;
    public bool MadeFullConnection = false;
    public static int Ping_Timer = 0;
    public const int BUFFSIZE = 4096;

    public static List<Action> ActionsOnPrimaryActionThread = new List<Action>();
    public static List<Action> Copied_ActionsOnPrimaryActionThread = new List<Action>();
    public static bool Action_To_Execute = false;
    public static int Time_By_MIL = 0;
    public static bool alive = false;

    public static GlobalUser GetGlobalUser(int id)
    {
        foreach (GlobalUser pl in global_users)
        {
            if (pl.id == id)
            {
                return pl;
            }
        }
        return null;
    }
    public static void LogOut()
    {

    }
    public static void CreateGlobalUser(int id, string username, int account_ID)
    {
        GlobalUser gu = new GlobalUser();
        gu.id = id;
        gu.username = username;
        gu.account_ID = account_ID;

        global_users.Add(gu); // Adds user to global UserList
        if (id == MyPublicID)
        {
            me = gu;
        }
        if (ChatManager.UserList_FULL != null && ChatManager.UserList_FULL.Any(x => x.AccountID == account_ID))
        {
            ReceivePackets.RefreshUserList();
        }
    }

    public void NetworkInit()
    {
        instance = this;
        tcp = new TCP();
        udp = new UDP();
        call_udp = new UDP();
        tcp.Connect();
        connected = true;
    }
    public static void Start()
    {
        new Thread(new ThreadStart(PrimaryActionThread)).Start();
        new Thread(new ThreadStart(MyTimer)).Start();
        new Thread(new ThreadStart(One_Second_Update)).Start();
    }
    private static void PrimaryActionThread()
    {
        while (ESMA.MainWindow.isrunning && alive == true)
        {
            if (Action_To_Execute)
            {
                Copied_ActionsOnPrimaryActionThread.Clear();
                lock (ActionsOnPrimaryActionThread) // prevent other threads from accessing this at the same time, otherwise errors may occur.
                {
                    Copied_ActionsOnPrimaryActionThread.AddRange(ActionsOnPrimaryActionThread);
                    ActionsOnPrimaryActionThread.Clear();
                    Action_To_Execute = false;
                }
                for (int i = 0; i < Copied_ActionsOnPrimaryActionThread.Count; i++)
                {
                    Copied_ActionsOnPrimaryActionThread[i]();
                }
            }
            Thread.Sleep(15);
        }
    }
    private static void MyTimer()
    {
        while (ESMA.MainWindow.isrunning && alive == true)
        {
            Time_By_MIL += 5;
            if (Time_By_MIL > 100000)
            {
                Time_By_MIL = 0;
            }
            Thread.Sleep(5);
        }
    }
    private static void One_Second_Update()
    {
        int i = 0;
        bool lost = false;
        while (ESMA.MainWindow.isrunning && alive == true)
        {
            i++;
            if (i >= 2) // update every 2 seconds
            {
                i = 0;
                GC.Collect();
            }
            if (NetworkManager.me != null)
            {
                if (NetworkManager.instance.udp.started == true)
                    SendPackets.UDPKeepAlive();
                if (NetworkManager.instance.call_udp.started == true)
                    SendPackets.UDPKeepAlive_Call();
                TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.ServerHeartbeat();
                    NetworkManager.Ping_Timer++;
                    if (NetworkManager.Ping_Timer > 10)
                    {
                        //lost connection, handle disconnnect
                        if (lost == false)
                        {
                            ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                            {
                                MainWindow.instance.MessageBoxShow("Connection to server has been lost.");
                            }));
                        }
                        lost = true;
                    }
                });
            }
            Thread.Sleep(1000);
        }
    }
    public static void TaskTo_PrimaryActionThread(Action a)
    {
        if (a == null)
            return;
        lock (ActionsOnPrimaryActionThread)
        {
            ActionsOnPrimaryActionThread.Add(a);
            Action_To_Execute = true;
        }
    }


    public class UDP
    {
        public UdpClient socket = null;
        public IPEndPoint endpoint;
        public bool started = false;
        private Thread receivingthread;
        
        public void Start(int port)
        {
            if (started == true)
                return;
            started = true;
            endpoint = new IPEndPoint(IPAddress.Parse(ip), port); // room udp endpoint
            socket = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            socket.Connect(endpoint);
            receivingthread = new Thread(new ThreadStart(UDPReceive));
            receivingthread.Start();
        }
        public void Send(Packet packet)
        {
            try
            {
                packet.WriteFront(MyAccountID ?? 0);
                packet.WriteFront(instance.public_id);
                if (socket != null)
                {
                    byte[] bytes = packet.Bytes_To_Send.ToArray();
                    socket.Send(bytes, bytes.Length);
                }
            }
            catch { }
        }
        private void UDPReceive()
        {
            List<byte> packet_buffer = new List<byte>();
            while (started == true)
            {
                IPEndPoint captured_ep = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = null;
                try
                {
                    data = socket.Receive(ref captured_ep);
                }
                catch
                {

                }

                if (data == null || data.Length <= 0)
                {
                    break;
                }
                packet_buffer.AddRange(data);
                if (packet_buffer[packet_buffer.Count - 1] == 230 && packet_buffer[packet_buffer.Count - 2] == 231 && packet_buffer[packet_buffer.Count - 3] == 232 && packet_buffer[packet_buffer.Count - 4] == 233)
                {
                    byte[] toarr = packet_buffer.ToArray();
                    packet_buffer.Clear();
                    TaskTo_PrimaryActionThread(() =>
                    {
                        using (Packet packet = new Packet(toarr))
                        {
                            bool error = false;
                            while (packet._seeker < (packet.Received_Bytes.Length - 1))
                            {
                                int task_id = packet.GetInt(ref error); if (error == true) return;
                                switch (task_id)
                                {
                                    case 14:
                                        ReceivePackets.ReceiveVoice(packet, ref error);
                                        break;
                                    case 32:
                                        ReceivePackets.ReceiveVoiceInCall(packet, ref error);
                                        break;
                                    case 43:
                                        ReceivePackets.ReceivedUDPCheck(packet, ref error);
                                        break;
                                    case 44:
                                        ReceivePackets.ReceivedUDPCheck(packet, ref error);
                                        break;
                                    default:
                                        return;
                                }

                                if (error == true) return;
                            }
                        }
                    });
                }
            }
            TaskTo_PrimaryActionThread(() =>
            {
                Disconnect();
            });
            receivingthread = null;
        }
        public void Disconnect() // shut off room udp
        {
            started = false;
           
            endpoint = null;
            
            if (socket == null)
                return;
            socket.Close();
            socket = null;
            
        }
    }
    public class TCP
    {
        public TcpClient socket;
        public NetworkStream stream;
        private Thread receivingthread;
        public void Connect()
        {
            Action a = (Action)(() =>
            {
                if (socket != null)
                    return;
                socket = new TcpClient{ReceiveBufferSize = BUFFSIZE,SendBufferSize = BUFFSIZE};
                socket.BeginConnect(ip, instance.port, ConnectionReceived, socket);
            });

            MainWindow.instance.RunOnPrimaryActionThread(a);
        }
        private void ConnectionReceived(IAsyncResult result)
        {
            socket.EndConnect(result);

            if (socket.Connected == false)
                return;

            stream = socket.GetStream();
            receivingthread = new Thread(new ThreadStart(TCPReceive));
            receivingthread.Start();
        }
        public void Send(Packet packet)
        {
            try
            {
                if (socket != null)
                {
                    byte[] bytes = packet.Bytes_To_Send.ToArray();
                    stream.BeginWrite(bytes, 0, bytes.Length, null, null);
                }
            }
            catch { }
        }
        private void TCPReceive()
        {
            List<byte> packet_buffer = new List<byte>();
            while (stream != null) // while we have a stream to read from, keep looping
            {
                int recv_bytes = 0;
                byte[] recv_buff = new byte[BUFFSIZE];
                try
                {
                    recv_bytes = stream.Read(recv_buff, 0, BUFFSIZE); // store read bytes in recv_buff
                }
                catch { }

                if (recv_bytes <= 0)
                {
                    TaskTo_PrimaryActionThread(() =>
                    {
                        Disconnect();
                    });
                    break;
                }
                else
                {
                    packet_buffer.AddRange(recv_buff.Take(recv_bytes).ToArray()); // take filled data from received bytes and put them in buff

                    if (packet_buffer[packet_buffer.Count - 1] == 230 && packet_buffer[packet_buffer.Count - 2] == 231 && packet_buffer[packet_buffer.Count - 3] == 232 && packet_buffer[packet_buffer.Count - 4] == 233)
                    {
                        byte[] toarr = packet_buffer.ToArray(); // found valid finalizer, time to build some packets
                        TaskTo_PrimaryActionThread(() => // task this off to the primary action thread, but also a benefit of this is so we can carry back on to stream read instantly
                        {
                            using (Packet packet = new Packet(toarr))
                            {
                                bool error = false; // if any part of the packet reading process goes wrong, this will be set to true and the whole action will be terminated at a checkpoint because something in the byte array was corrupt
                                while (packet._seeker < (packet.Received_Bytes.Length - 1))
                                {
                                    int task_id = packet.GetInt(ref error); if (error == true) return; //there can be multiple tasks to carry out if this stream read was fragmented (multiple finalizers)
                                    switch (task_id) // find task to carry out based off task id
                                    {
                                        case 0:
                                            ReceivePackets.Connected(packet, ref error);
                                            break;
                                        case 1:
                                            ReceivePackets.CreateUser(packet, ref error);
                                            break;
                                        case 2:
                                            ReceivePackets.UserDisconnected(packet, ref error);
                                            break;
                                        case 3:
                                            ReceivePackets.TCPCheck(packet, ref error);
                                            break;
                                        case 4:
                                            ReceivePackets.SendbackRoomCreationSuccess(packet, ref error);
                                            break;
                                        case 5:
                                            ReceivePackets.SendBackMessageToAllInRoom(packet, ref error);
                                            break;
                                        case 6:
                                            ReceivePackets.SendBackPictureMessageToAllInRoom(packet, ref error);
                                            break;
                                        case 7:
                                            ReceivePackets.SendbackRoomJoinSuccess(packet, ref error);
                                            break;
                                        case 8:
                                            ReceivePackets.SendBackVideoMessageToAllInRoom(packet, ref error);
                                            break;
                                        case 9:
                                            ReceivePackets.SendbackRoomRoot(packet, ref error);
                                            break;
                                        case 10:
                                            ReceivePackets.SendBackMoreMessagesToClient(packet, ref error);
                                            break;
                                        case 11:
                                            ReceivePackets.AddOrRemoveUserFromList(packet, ref error);
                                            break;
                                        case 12:
                                            ReceivePackets.SendBackAudioMessageToAllInRoom(packet, ref error);
                                            break;
                                        case 13:
                                            ReceivePackets.SendBackFileMessageToAllInRoom(packet, ref error);
                                            break;
                                        case 15:
                                            ReceivePackets.SendBackUserInVoiceChannel(packet, ref error);
                                            break;
                                        case 16:
                                            ReceivePackets.SendTCPReady(packet, ref error);
                                            break;
                                        case 17:
                                            ReceivePackets.ReceiveNewProfilePicture(packet, ref error);
                                            break;
                                        case 18:
                                            ReceivePackets.SavedRoomCreationSuccess(packet, ref error);
                                            break;
                                        case 19:
                                            ReceivePackets.RoomExists(packet, ref error);
                                            break;
                                        case 20:
                                            ReceivePackets.SendToUserTheirRooms(packet, ref error);
                                            break;
                                        case 21:
                                            ReceivePackets.SendMessage(packet, ref error);
                                            break;
                                        case 22:
                                            ReceivePackets.SendCloseLoading(packet, ref error);
                                            break;
                                        case 23:
                                            ReceivePackets.SendToUserTheirMSGSearch(packet, ref error);
                                            break;
                                        case 24:
                                            ReceivePackets.SendBackMessageSpanForSearch(packet, ref error);
                                            break;
                                        case 25:
                                            ReceivePackets.SendBackSavedServerChannels(packet, ref error);
                                            break;
                                        case 26:
                                            ReceivePackets.SendBackFriends(packet, ref error);
                                            break;
                                        case 27:
                                            ReceivePackets.SendBackRoomLeftSuccess(packet, ref error);
                                            break;
                                        case 28:
                                            ReceivePackets.PMReady(packet, ref error);
                                            break;
                                        case 29:
                                            ReceivePackets.SendBackCall(packet, ref error);
                                            break;
                                        case 30:
                                            ReceivePackets.CloseCallTimesUp(packet, ref error);
                                            break;
                                        case 31:
                                            ReceivePackets.SendCallResponse(packet, ref error);
                                            break;
                                        case 33:
                                            ReceivePackets.SendBackPMHash(packet, ref error);
                                            break;
                                        case 34:
                                            ReceivePackets.SendBackPendingFriends(packet, ref error);
                                            break;
                                        case 35:
                                            ReceivePackets.SendbackRoles(packet, ref error);
                                            break;
                                        case 36:
                                            ReceivePackets.SendToSavedServerNewUsersWRole(packet, ref error);
                                            break;
                                        case 37:
                                            ReceivePackets.LeaveServerNotify(packet, ref error);
                                            break;
                                        case 38:
                                            ReceivePackets.CleanUpOnExternalKick(packet, ref error);
                                            break;
                                        case 39:
                                            ReceivePackets.OpenUserProfile(packet, ref error);
                                            break;
                                        case 40:
                                            ReceivePackets.SendSuccessInProfileSave(packet, ref error);
                                            break;
                                        case 41:
                                            ReceivePackets.SendBackProfilePicture(packet, ref error);
                                            break;
                                        case 42:
                                            ReceivePackets.CleanUpOnDMErase(packet, ref error);
                                            break;
                                        default:
                                            //task id not found, terminate entire action
                                            return;
                                    }

                                    if (error == true) return;
                                }
                            }
                        });
                        packet_buffer.Clear();
                    }
                }
            }
        }

        public void Disconnect() // this implies we have been cut off from master server tcp, so we just shut the whole network portion down
        {
            if (instance != null)
                instance.FullDisconnect();

            receivingthread = null;
            stream = null;
            socket = null;
        
        }
    }
    public void FullDisconnect() //fresh restart
    {
        if (connected)
        {
            for (int i = 0; i < ChatManager.wave_in_list.Count; ++i)
            {
                ChatManager.wave_in_list[i].StopRecording();
                ChatManager.wave_in_list[i].Dispose();
                ChatManager.wave_in_list.RemoveAt(i--);
            }
            for (int i =0; i < ChatManager.provider_list.Count; ++i)
            {
                ChatManager.provider_list[i].ClearBuffer();
                ChatManager.provider_list.RemoveAt(i--);
            }
            for (int i = 0; i < ChatManager.wave_out_list.Count; ++i)
            {
                ChatManager.wave_out_list[i].Stop();
                ChatManager.wave_out_list[i].Dispose();
                ChatManager.wave_out_list.RemoveAt(i--);
            }
            if (ChatManager.my_room_user != null)
                ChatManager.my_room_user.StopTransmission();
            for(int i = 0; i < ChatManager.UserList.Count; i++)
            {
                ChatManager.UserList[i].StopTransmission();
            }
            connected = false;
            if (tcp.socket != null)
            {
                tcp.socket.Close();
            }
            if (udp != null)
            {
                udp.Disconnect();
            }
            if (call_udp != null)
            {
                call_udp.Disconnect();
            }
            tcp.stream = null;
            tcp.socket = null;
            connected = false;
            MadeFullConnection = false;
            global_users.Clear();
            Ping_Timer = 0;
            instance.public_id = 0;
            MyPublicID = null;
           
            ActionsOnPrimaryActionThread.Clear();
            Copied_ActionsOnPrimaryActionThread.Clear();
            Action_To_Execute = false;

            MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                MainWindow.instance.Lobby_ProfilePic.Source = null;
                            });
            myusername = "";
            MyAccountID = null;
            instance = null;
       
            alive = false;
            ChatManager.channels.Clear();
            ChatManager.UserList_FULL = null;
            ChatManager.UserList.Clear();
            ChatManager.UserCache.Clear();
            ChatManager.selectedencryptionseed = "";
            ChatManager.selectedroompass = "";
            ChatManager.selectedroompassasbytesprejoin = null;
            ChatManager.selectedroompassasbytes = null;
            ChatManager.selectedroompassasbytes2 = null;
            ChatManager.StillFetchingMessages = false;
            ChatManager.StillLoadingMessages = false;
            ChatManager.StillGettingVerificationOfJoinRoom = false;
            ChatManager.CurrentChannel = 0;
          
            ChatManager.my_room_user = null;
            ChatManager.CurrentSavedServer = null;
            ChatManager.CurTempRoom = null;
            ChatManager.CurrentChatWithUser = null;
            ChatManager.CurrentChatWithUserKey = null;
            ChatManager.MyUserPrivateKey = null;
            ChatManager.MyUserPublicKey = null;
            ChatManager.MyServers.Clear();
            ChatManager.SavedServers.Clear();
            ChatManager.UserInCallWith = null;
            ChatManager.MyTempuserWhileinCall = null;
            ChatManager.PotentialUserInCallWith = null;
            ChatManager.RoleList = new List<RolesList>();
            global_users.Clear();
            MyAccountID = null;
            MyPublicID = null;
            myusername = "";
            Friends.Clear();
            DMS.Clear();
            me = null;
            public_id = 0;
            MainWindow.instance.SetFormVisibility(1);
            MainWindow.instance.Dispatcher.Invoke(() =>
            {
                MainWindow.instance.ServerDirectoryListBox.ItemsSource = ChatManager.MyServers;
                MainWindow.instance.ServerDirectoryListBox.Items.Refresh();//refresh UI
                MainWindow.instance.LoadingObj.Visibility = Visibility.Hidden;
            });
        }
    }
}
[System.Serializable]
public class GlobalUser
{
    public int id = -1;
    public int account_ID = -1;
    public string username;
}
