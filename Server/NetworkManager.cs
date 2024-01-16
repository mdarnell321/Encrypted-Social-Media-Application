

using System.Net;
using System.Net.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Engines;
using Mysqlx.Crud;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Bcpg;
using System.IO;

namespace ChatAppServer
{
    public class NetworkManager
    {
        public static List<User> clients = new List<User>();
        private static TcpListener tcp_listener;
        public static bool goodtostart;
        public static List<TemporaryRooms> temporaryRooms = new List<TemporaryRooms>();
        public static List<ServerMessageCount> ServerMessages = new List<ServerMessageCount>();
        public static List<PMMessageCount> PMMessages = new List<PMMessageCount>();
        public static List<MuteParams> mutes = new List<MuteParams>();
        public static List<MuteParams> kicks = new List<MuteParams>();
        public static List<RoomWithThreads> serverthreads = new List<RoomWithThreads>();
        public static List<RoomWithThreads> pmthreads = new List<RoomWithThreads>();
        public static List<RoomWithThreads> temproomthreads = new List<RoomWithThreads>();
        public static List<OnFlyChannelManager> ChannelManagers = new List<OnFlyChannelManager>();
        public static List<int[]> dms = new List<int[]>(); 
        public static List<User> ClientsToTrash = new List<User>();
        public static int Port;

        public static List<Action> ActionsOnPrimaryActionThread = new List<Action>();
        public static List<Action> Copied_ActionsOnPrimaryActionThread = new List<Action>();
        private static bool Action_To_Execute = false;

        public static List<Action> ActionsOnHandlerThread = new List<Action>();
        public static List<Action> Copied_ActionsOnHandlerThread = new List<Action>();
        private static bool Action_To_Execute_Handler = false;

        public static List<Action> ActionsOnMSGThread = new List<Action>();
        public static List<Action> Copied_ActionsOnMSGThread = new List<Action>();
        private static bool Action_To_Execute_MSG = false;

        public static void StartIt(int port)
        {
            NetworkManager.Port = port;
            NetworkManager.tcp_listener = new TcpListener(IPAddress.Any, NetworkManager.Port);
            NetworkManager.tcp_listener.Start();
            NetworkManager.tcp_listener.BeginAcceptTcpClient(new AsyncCallback(NetworkManager.TCPConnectionCallback), null);
            Console.WriteLine("Server started.");
        }
        public static string? CheckMute(int account_ID, int serverid)
        {
            if(GetClientByAccountId(account_ID) != null)
            {
                MuteParams m = mutes.Find(x => x.account_ID == account_ID && x.serverid == serverid);
                if(m != null)
                {
                    TimeSpan left =  m.datemuted - DateTime.Now;
                    return String.Format("Mute expires in {0} Hour(s) {1} Minute(s) {2} Seconds(s)", left.Hours, left.Minutes, left.Seconds);
                }
            }
            return null;
        }
        public static string? CheckKick(int account_ID, int serverid)
        {
            if (GetClientByAccountId(account_ID) != null)
            {
                MuteParams m = kicks.Find(x => x.account_ID == account_ID && x.serverid == serverid);
                if (m != null)
                {
                    TimeSpan left = m.datemuted - DateTime.Now;
                    return String.Format("Ban expires in {0} Hour(s) {1} Minute(s) {2} Seconds(s)", left.Hours, left.Minutes, left.Seconds);
                }
            }
            return null;
        }
        public static User GetClientById(int id)
        {
            for (int i = 0; i < clients.Count; ++i)
            {
                if (clients[i].id == id)
                {
                    return clients[i];
                }
            }
            return null;
        }
        public static OnFlyChannelManager GetUserRoomManager(User c)
        {
            if (c.CurrentSavedServerID != null)
            {
                for (int ii = 0; ii < ChannelManagers.Count; ++ii)
                {
                    if (ChannelManagers[ii].roomtype == 1 && ChannelManagers[ii].ServerID == c.CurrentSavedServerID)
                    {
                        return ChannelManagers[ii];
                    }
                }
            }
            for (int i = 0; i < temporaryRooms.Count; ++i)
            {
                for (int ii = 0; ii < ChannelManagers.Count; ++ii)
                {
                    if (ChannelManagers[ii].roomtype == 0 && ChannelManagers[ii].ServerID == temporaryRooms[i].id)
                    {
                        return ChannelManagers[ii];
                    }
                }
            }
            return null;
        }
        public static OnFlyChannelManager GetUserCallManager(User c)
        {
            for (int ii = 0; ii < ChannelManagers.Count; ++ii)
            {
                if (ChannelManagers[ii].roomtype == 2 && ChannelManagers[ii].clients.Contains(c))
                {
                    return ChannelManagers[ii];
                }
            }
            return null;
        }
        public static bool AddServerMessages(int serverid, int channelid, string serverfolder, ref int? msgid)
        {
            try
            {
                for (int i = 0; i < ServerMessages.Count; ++i)
                {
                    if (ServerMessages[i].serverid == serverid)
                    {
                        ServerMessages[i].msgcountbychannel[channelid] = (int)ServerMessages[i].msgcountbychannel[channelid] + 1;
                        msgid = (int)ServerMessages[i].msgcountbychannel[channelid];
                        return true;
                    }
                }
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(serverfolder);
                List<string> files = new List<string>();
                foreach (FileInfo di in dir.GetFiles())
                {
                    files.Add(di.Name);
                }
                Hashtable newtemp = new Hashtable();
                for (int i = 0; i < files.Count; ++i) // iterate through channels
                {
                    string directory = System.IO.Path.Combine(serverfolder + "\\" + files[i]);
                    int msgcount = File.ReadAllLines(directory).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray().Length;
                    if ((int.Parse(files[i].Replace(".txt", "")) == channelid))
                    {
                        msgid = msgcount +1;
                    }
                 
                    newtemp.Add(int.Parse(files[i].Replace(".txt", "")), msgcount + 1);
                }
                ServerMessages.Add(new ServerMessageCount(serverid, newtemp));
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static PMMessageCount GetPMMessageCounter(int a, int b)
        {
            for (int i = 0; i < PMMessages.Count; ++i)
            {
                if (PMMessages[i].UserA == a && PMMessages[i].UserB == b || PMMessages[i].UserA == b && PMMessages[i].UserB == a)
                {
                    return PMMessages[i];
                }
            }
            return null;
        }
        public static ServerMessageCount GetServerMessageCounter(int s)
        {
            for (int i = 0; i < ServerMessages.Count; ++i)
            {
                if (ServerMessages[i].serverid == s)
                {
                    return ServerMessages[i];
                }
            }
            return null;
        }
        public static bool AddPMMessages(int a, int b, ref int? msgid)
        {
            try
            {
                for (int i = 0; i < PMMessages.Count; ++i)
                {
                    if (PMMessages[i].UserA == a && PMMessages[i].UserB == b || PMMessages[i].UserA == b && PMMessages[i].UserB == a)
                    {
                        PMMessages[i].msgcount++;
                        msgid = PMMessages[i].msgcount;
                        return true;
                    }
                }
                int msgcount = File.ReadAllLines(ReceivePackets.TextFileName(a, b)).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray().Length;
                msgid = msgcount + 1;
                PMMessages.Add(new PMMessageCount(a, b, msgcount + 1));
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static User GetClientByAccountId(int id)
        {
            for (int i = 0; i < clients.Count; ++i)
            {
                if (clients[i].accountid == id)
                {
                    return clients[i];
                }
            }
            return null;
        }
        public static TemporaryRooms GetRoomById(int id)
        {
            for (int i = 0; i < temporaryRooms.Count; ++i)
            {
                if (temporaryRooms[i].id == id)
                {
                    return temporaryRooms[i];
                }
            }
            return null;
        }
        public static TemporaryRooms GetRoomByName(string name)
        {
            for (int i = 0; i < temporaryRooms.Count; ++i)
            {
                if (temporaryRooms[i].name == name)
                {
                    return temporaryRooms[i];
                }
            }
            return null;
        }
        private static void TCPConnectionCallback(IAsyncResult result)
        {
            TcpClient socket = NetworkManager.tcp_listener.EndAcceptTcpClient(result);
            socket.ReceiveBufferSize = User.TCPBUFFSIZE;
            socket.SendBufferSize = User.TCPBUFFSIZE;
            NetworkManager.tcp_listener.BeginAcceptTcpClient(new AsyncCallback(NetworkManager.TCPConnectionCallback), null);
            Console.WriteLine("User connecting on " + socket.Client.RemoteEndPoint + ".");

            User c;
            if (clients.Count > 0)
            {
                c = new User(clients[clients.Count - 1].id + 1);
                NetworkManager.clients.Add(c);
            }
            else
            {
                c = new User(1);
                NetworkManager.clients.Add(c);
            }
            c.tcp_socket = socket;
            c.stream = socket.GetStream();
            c.tcp_receivingthread = new Thread(new ThreadStart(c.TCPReceive));
            c.tcp_receivingthread.Start();
            SendPackets.SendConnected(c.id);
        }

        public static void Stop()
        {
            NetworkManager.tcp_listener.Stop();
        }

        public static object[] GetFirstTextChannel(TemporaryRooms room)
        {
            List<int> keylist = new List<int>();
            foreach(int i in room.channels.Keys)
            {
                if (room.channels[i].GetType() == typeof(ChannelsForTempRoom))
                {
                    keylist.Add(i);
                }
            }
            if(keylist.Count > 0)
            {
                keylist = keylist.OrderBy(x => x).ToList();
                return (new object[] { room.channels[keylist[0]] as ChannelsForTempRoom, keylist[0] });
            }
            else
            {
                return null;
            }

        }

        public static void TaskTo_MSGThread(Action a)
        {
            if (a != null)
            {
                lock (ActionsOnMSGThread)
                {
                    ActionsOnMSGThread.Add(a);
                    Action_To_Execute_MSG = true;
                }
            }

        }
        public static void TaskTo_PrimaryActionThread(Action a)
        {
            if (a != null)
            {
                lock (ActionsOnPrimaryActionThread)
                {
                    ActionsOnPrimaryActionThread.Add(a);
                    Action_To_Execute = true;
                }
            }

        }
        public static void TaskTo_ReceiveThread(Action a)
        {
            if (a != null)
            {
                lock (ActionsOnHandlerThread)
                {
                    ActionsOnHandlerThread.Add(a);
                    Action_To_Execute_Handler = true;
                }
            }

        }

        public static int serverupdateintens = 0;
        public static void UpdateReceiveThread()
        {
            while (Program.active)
            {
                if (Action_To_Execute_Handler)
                {
                    Copied_ActionsOnHandlerThread.Clear();
                    lock (ActionsOnHandlerThread)
                    {
                        try
                        {
                            Copied_ActionsOnHandlerThread.AddRange((IEnumerable<Action>)ActionsOnHandlerThread);
                            ActionsOnHandlerThread.Clear();
                            Action_To_Execute_Handler = false;
                        }
                        catch { }
                    }
                    foreach (Action _a in Copied_ActionsOnHandlerThread)
                    {
                        if (_a != null)
                        {
                            _a();
                        }
                    }
                }
                else
                {
                    Copied_ActionsOnHandlerThread.Clear();
                }
                Thread.Sleep(5);
            }
        }
        private static int clienttrashtimer = 0;
        public static void Update()
        {
            while (Program.active)
            {
                if (NetworkManager.ClientsToTrash.Count > 0)
                {
                    clienttrashtimer++;
                }
                else
                {
                    clienttrashtimer = 0;
                }
                if (clienttrashtimer >= 20)
                {
                    clienttrashtimer = 0;

                    TaskTo_ReceiveThread((Action)(() =>
                    {
                        if (NetworkManager.ClientsToTrash.Count > 0)
                        {
                            NetworkManager.clients.Remove(NetworkManager.ClientsToTrash[0]);
                            NetworkManager.ClientsToTrash.RemoveAt(0);
                        }
                    }));
                }
                for (int i = 0; i < NetworkManager.clients.Count; ++i)
                {
                    User client = NetworkManager.clients[i];
                    if ((client ?? new User(-1)).startdis == true)
                    {
                        continue;
                    }
                    if (client == null)
                        return;
                    try
                    {
                        if (client.TimeLeftInPendingCall > 0 && client.TimeLeftInPendingCall <= 33)
                        {
                            if (client.CurrentCaller == null && client.PotentialCaller != null)
                            {
                                if (NetworkManager.GetClientByAccountId((int)client.PotentialCaller) != null)
                                {
                                    SendPackets.SendCloseLoading(NetworkManager.GetClientByAccountId((int)client.PotentialCaller).id, "Calling user...", 3);
                                    NetworkManager.GetClientByAccountId((int)client.PotentialCaller).CurrentCaller = null;
                                    ReceivePackets.pass_data_toserv(NetworkManager.GetClientByAccountId((int)client.PotentialCaller), 1, -1);
                                    NetworkManager.GetClientByAccountId((int)client.PotentialCaller).PotentialCaller = null;
                                }
                                SendPackets.CloseCallTimesUp(client.id);
                                client.CurrentCaller = null;
                                ReceivePackets.pass_data_toserv(client, 1, -1);
                                client.PotentialCaller = null;
                            }
                        }
                        if (NetworkManager.clients[i].TimeLeftInPendingCall > 0)
                        {
                            NetworkManager.clients[i].TimeLeftInPendingCall -= 33;
                        }
                    }
                    catch { }
                }
                serverupdateintens++;
                if (serverupdateintens >= 10)
                {

                    serverupdateintens = 0;
                }
                Thread.Sleep(5);
            }
        }
        public static void SlowThread()
        {
            int iteration = 0;
            while (Program.active)
            {
                iteration++;
                if (iteration >= 5)
                {
                    TaskTo_ReceiveThread((Action)(() =>
                    {
                        for (int i = 0; i < NetworkManager.mutes.Count; ++i)
                        {
                            if (NetworkManager.mutes[i].datemuted - DateTime.Now <= TimeSpan.FromSeconds(0))
                            {
                                NetworkManager.mutes.RemoveAt(i);
                            }
                        }
                        for (int i = 0; i < NetworkManager.kicks.Count; ++i)
                        {
                            if (NetworkManager.kicks[i].datemuted - DateTime.Now <= TimeSpan.FromSeconds(0))
                            {
                                NetworkManager.kicks.RemoveAt(i);
                            }
                        }
                    }));
                    iteration = 0;
                }
                TaskTo_ReceiveThread((Action)(() =>
                {
                    for (int i = 0; i < NetworkManager.clients.Count; ++i)
                    {
                        User u = NetworkManager.clients[i];
                        u.Ping_Timer++;
                        if (u.Ping_Timer > 6)
                        {
                            //lost connection, handle disconnnect
                            Console.WriteLine(u.username + " was idle for too long. They have been disconnected.");
                            u.FullDisconnect();
                        }
                    }
                }));
                Thread.Sleep(1000);
            }
        }
        public static void UpdatePrimaryActionThread()
        {
            while (Program.active)
            {
                if (Action_To_Execute)
                {
                    Copied_ActionsOnPrimaryActionThread.Clear();
                    lock (ActionsOnPrimaryActionThread)
                    {
                        try
                        {
                            Copied_ActionsOnPrimaryActionThread.AddRange((IEnumerable<Action>)ActionsOnPrimaryActionThread);
                            ActionsOnPrimaryActionThread.Clear();
                            Action_To_Execute = false;
                        }
                        catch { }
                    }
                    foreach (Action _a in Copied_ActionsOnPrimaryActionThread)
                    {
                        try
                        {
                            if (_a != null)
                            {
                                _a();

                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    Copied_ActionsOnPrimaryActionThread.Clear();
                }
                Thread.Sleep(5);
            }
        }
        public static void UpdateMessagesThread()
        {
            while (Program.active)
            {
                if (Action_To_Execute_MSG)
                {
                    Copied_ActionsOnMSGThread.Clear();
                    lock (ActionsOnMSGThread)
                    {
                        try
                        {
                            Copied_ActionsOnMSGThread.AddRange((IEnumerable<Action>)ActionsOnMSGThread);
                            ActionsOnMSGThread.Clear();
                            Action_To_Execute_MSG = false;
                        }
                        catch { }
                    }
                    foreach (Action _a in Copied_ActionsOnMSGThread)
                    {
                        try
                        {
                            if (_a != null)
                            {
                                _a();
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    Copied_ActionsOnMSGThread.Clear();
                }
                Thread.Sleep(5);
            }
        }
    }
    public struct passed_udp_data
    {
        public bool read;
        public bool written;
        public int[] data;
        public int access_attempts;
    }
    [System.Serializable]
    public class OnFlyChannelManager
    {
        public UdpClient udp_socket;
        public int port = 0;
        public bool initialized = false;
        public bool closed = false;
        public int roomtype;
        public int ServerID;
        public List<User> clients = new List<User>();
        public List<ENDPOINTS> client_endpoints = new List<ENDPOINTS>();
        //Secondary
        public List<Action> ActionsOnSecondActionThread = new List<Action>();
        public List<Action> Copied_ActionsOnSecondActionThread = new List<Action>();
        private bool Action_To_Execute_Second = false;
        public Thread t2;
        public Thread receivingthread;
        public passed_udp_data[] passed_data = new passed_udp_data[20];
        //UDP thread

        public OnFlyChannelManager(int rt, int serverid)
        {
           
            ServerID = serverid;
            roomtype = rt;
            port = NetworkManager.Port;
            t2 = new Thread(new ThreadStart(ThreadManagerSecond));
            t2.Start();
            for (int i = 0; i < NetworkManager.ChannelManagers.Count; ++i)
            {
                if (NetworkManager.ChannelManagers[i].port > this.port)
                {
                    port = NetworkManager.ChannelManagers[i].port;
                }
            }
            port++;
            StartIt();
           
        }
        public void StartIt()
        {
            udp_socket = new UdpClient();
            udp_socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp_socket.Client.Bind(new IPEndPoint(IPAddress.Any, port));

            receivingthread = new Thread(new ThreadStart(UDPReceive));
            receivingthread.Start();
            initialized = true;
            closed = false;
        }
        public void Stop()
        {
            closed = true;
            udp_socket.Close();
        }

        public ENDPOINTS FindUDPEndPointElement(IPEndPoint endpoint)
        {
            for(int i = 0; i < client_endpoints.Count; ++i)
            {
                if (client_endpoints[i].ep.ToString() == endpoint.ToString())
                {
                    return client_endpoints[i];
                }
            }
            ENDPOINTS e_p = new ENDPOINTS();
            e_p.ep = endpoint;
            client_endpoints.Add(e_p);

            return e_p;
        }
        public ENDPOINTS FindUDPEndPointElement_Nullable(IPEndPoint endpoint)
        {
            for (int i = 0; i < client_endpoints.Count; ++i)
            {
                if (client_endpoints[i].ep.ToString() == endpoint.ToString())
                {
                    return client_endpoints[i];
                }
            }
            
            return null;
        }
        private void AssignEndpoint(ENDPOINTS e, int public_id, int account_ID)
        {
            bool foundepublicid = false;
            for (int i = 0; i < this.client_endpoints.Count; ++i)
            {
                if (this.client_endpoints[i].account_id == account_ID)
                    foundepublicid = true;
            }
            if (foundepublicid == false && e.account_id == -1)
            {
                e.public_id = public_id;
                e.account_id = account_ID;
            }
        }

        public void UDPReceive()
        {
            while (closed == false)
            {
                
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = null;
                try
                {
                    data = udp_socket.Receive(ref endpoint);
                }
                catch
                {

                }
                if (data == null || data.Length == 0)
                {
                    continue;
                }
                ENDPOINTS e = FindUDPEndPointElement(endpoint);
                e.data.AddRange(data);
                e.timestamp = DateTime.Now;
                for (int i = 0; i < 20; ++i) //safe method of passing information to this thread from other threads
                {
                    if (passed_data[i].written == true)
                    {
                        // we can collect the data
                        //0 task
                        //1 account id
                        //2 cur channel

                        for (int ii = 0; ii < client_endpoints.Count; ++ii)
                        {
                            if (client_endpoints[ii].account_id == passed_data[i].data[1])
                            {
                                if (passed_data[i].data[0] == 0) // assign channel
                                    client_endpoints[ii].cur_channel = passed_data[i].data[2];
                                else // assign caller 
                                    client_endpoints[ii].cur_talk = passed_data[i].data[2];
                                passed_data[i].written = false;
                                passed_data[i].access_attempts = 0;
                            }
                        }
                        if(passed_data[i].access_attempts++ > 50)
                        {
                            passed_data[i].access_attempts = 0;
                            passed_data[i].written = false;
                        }
                    }
                }
                if (e.data[e.data.Count - 1] == 230 && e.data[e.data.Count - 2] == 231 && e.data[e.data.Count - 3] == 232 && e.data[e.data.Count - 4] == 233)
                {
                    try
                    {
                        if (e.data.Count > 64000)
                        {
                            e.data.Clear();
                            continue;
                        }
                        byte[] toarr = e.data.ToArray();
                        e.data.Clear();
                        TaskTo_SecondActionThread(() =>
                        {
                            using (Packet packet = new Packet(toarr))
                            {
                                bool error = false;
                                while (packet._seeker < (packet.Received_Bytes.Length - 1))
                                {
                                    int public_id = packet.GetInt(ref error); if (error == true) return;
                                    int theaccount_ID = packet.GetInt(ref error); if (error == true) return;
                                    int task_id = packet.GetInt(ref error); if (error == true) return;
                                    switch (task_id)
                                    {
                                        case 11:
                                            {
                                                byte[] message = packet.GetBytes(ref error); if (error == true) return;
                                                int channelkey = packet.GetInt(ref error); if (error == true) return;
                                                int room = packet.GetInt(ref error); if (error == true) return;
                                                bool saved = packet.GetBool(ref error); if (error == true) return;
                                                packet.CheckFinalizers(ref error); if (error == true) return;
                                                if (e.account_id == -1)
                                                    AssignEndpoint(e, public_id, theaccount_ID);
                                                if (e.public_id != public_id || e.account_id != theaccount_ID)
                                                    break;
                                                using (Packet packet_send = new Packet(14))
                                                {
                                                    packet_send.Write(theaccount_ID);
                                                    packet_send.Write(message);
                                                    packet_send.Write(channelkey);
                                                    packet_send._Finalize();

                                                    byte[] bytes = packet_send.Bytes_To_Send.ToArray();
                                                    for (int i = 0; i < client_endpoints.Count; i++)
                                                    {
                                                        if (client_endpoints[i].cur_channel == e.cur_channel && client_endpoints[i] != e)
                                                            udp_socket.Send(bytes, bytes.Length, client_endpoints[i].ep);
                                                    }
                                                }
                                            }
                                            break;
                                        case 30:
                                            {
                                                byte[] message = packet.GetBytes(ref error); if (error == true) return;
                                                packet.CheckFinalizers(ref error); if (error == true) return;
                                                if (e.account_id == -1)
                                                    AssignEndpoint(e, public_id, theaccount_ID);
                                                if (e.public_id != public_id || e.account_id != theaccount_ID)
                                                    break;
                                                using (Packet packet_send = new Packet(32))
                                                {
                                                    packet_send.Write(message);
                                                    packet_send._Finalize();
                                                  
                                                    byte[] bytes = packet_send.Bytes_To_Send.ToArray();
                                                    for (int i = 0; i < client_endpoints.Count; i++)
                                                    {
                                                        if (client_endpoints[i].account_id == e.cur_talk)
                                                        {
                                                            udp_socket.Send(bytes, bytes.Length, client_endpoints[i].ep);
                                                            break;
                                                        }
      
                                                    }
                                                }
                                            }
                                            break;
                                        case 49:
                                            {
                                                packet.CheckFinalizers(ref error); if (error == true) return;
                                                if (e.account_id == -1)
                                                    AssignEndpoint(e, public_id, theaccount_ID);
                                                if (e.public_id != public_id || e.account_id != theaccount_ID)
                                                    break;
                                                using (Packet packet_send = new Packet(43))
                                                {
                                                    packet_send._Finalize();
                                                    byte[] bytes = packet_send.Bytes_To_Send.ToArray();
                                                    udp_socket.Send(bytes, bytes.Length, e.ep);
                                                }
                                            }
                                            break;
                                        case 50:
                                            {
                                                packet.CheckFinalizers(ref error); if (error == true) return;
                                                if (e.account_id == -1)
                                                    AssignEndpoint(e, public_id, theaccount_ID);
                                                if (e.public_id != public_id || e.account_id != theaccount_ID)
                                                    break;
                                                using (Packet packet_send = new Packet(44))
                                                {
                                                    packet_send._Finalize();
                                                    byte[] bytes = packet_send.Bytes_To_Send.ToArray();
                                                    udp_socket.Send(bytes, bytes.Length, e.ep);
                                                }
                                            }
                                            break;
                                        default:
                                            return;
                                    }
                                    if (error == true) return;
                                }
                            }

                        });
                    }
                    catch { }
                }
                for (int ii = 0; ii < client_endpoints.Count; ++ii)
                {
                    DateTime cur = DateTime.Now;
                    int diff = (int)(cur.Subtract(client_endpoints[ii].timestamp)).TotalSeconds;
                    if (diff > 5)
                    {
                        client_endpoints.RemoveAt(ii--);
                        continue;
                    }
                }
            }
        }
        private void ThreadManagerSecond()
        {
            while (closed == false)
            {
                UpdateSecond();
                Thread.Sleep(33);
            }
        }

        public void UpdateSecond()
        {
            if (Action_To_Execute_Second)
            {
                Copied_ActionsOnSecondActionThread.Clear();
                lock (ActionsOnSecondActionThread)
                {
                    try
                    {
                        Copied_ActionsOnSecondActionThread.AddRange((IEnumerable<Action>)ActionsOnSecondActionThread);
                        ActionsOnSecondActionThread.Clear();
                        Action_To_Execute_Second = false;
                    }
                    catch { }
                }
                foreach (Action _a in Copied_ActionsOnSecondActionThread)
                {
                    try
                    {
                        if (_a != null)
                        {
                            _a();
                        }
                    }
                    catch { }
                }
            }
            else
            {
                Copied_ActionsOnSecondActionThread.Clear();
            }
        }
        public void TaskTo_SecondActionThread(Action a)
        {
            if (a == null)
            {

            }
            else
            {
                lock (ActionsOnSecondActionThread)
                {
                    ActionsOnSecondActionThread.Add(a);
                    Action_To_Execute_Second = true;
                }
            }
        }
    }

    [System.Serializable]
    public class TemporaryRooms
    {
        public string name;
        public string KeySentence;
        public int id;
        public List<ChatAppServer.User> clientsinroom = new List<ChatAppServer.User>();
        public Hashtable channels = new Hashtable();

        public TemporaryRooms(int identifier, string key, ChatAppServer.User master, string n)
        {
            if(master == null)
            {
                return;
            }
            id = identifier;
            KeySentence = key;
            clientsinroom.Add(master);
            name = n;

            if (NetworkManager.temproomthreads.Any(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == id) == false)
            {
                NetworkManager.temproomthreads.Add(new ServerWithThreads((int)id, false));
            }
            if (NetworkManager.ChannelManagers.Any(x => x.GetType() == typeof(OnFlyChannelManager) && (x as OnFlyChannelManager).ServerID == id && (x as OnFlyChannelManager).roomtype == 0) == false)
            {
                NetworkManager.ChannelManagers.Add(new OnFlyChannelManager(0, (int)id));
            }
        }
    }
  
    [System.Serializable]
    public class ChannelsForTempRoom
    {
        public ChannelsForTempRoom(string name)
        {
            ChannelName = name;
        }
        public string ChannelName;
        public List<MessagesForTempRoom> messages = new List<MessagesForTempRoom>();
    }
    [System.Serializable]
    public class VoiceChannelsForTempRoom
    {
        public VoiceChannelsForTempRoom(string name)
        {
            ChannelName = name;
        }
        public string ChannelName;
       
    }
    [System.Serializable]
    public class ServerMessageCount
    {
        public ServerMessageCount(int si, Hashtable temp)
        {
            serverid = si;
            msgcountbychannel = temp;

        }
        public int serverid;
        public Hashtable msgcountbychannel = new Hashtable();
    }
    [System.Serializable]
    public class PMMessageCount
    {
        public PMMessageCount(int a, int b, int mc)
        {
            UserA = a;
            UserB = b;
            msgcount = mc;
        }
        public int UserA;
        public int UserB;
        public int msgcount;
    }
    [System.Serializable]
    public class MessagesForTempRoom
    {
        public string sendername;
        public int idofmessage;
        public int publicidofsender;
        public int accountidofsender;
        public string message;
        public bool ownedbysender;
        public string dateposted;
        public MessagesForTempRoom(string sname, int account_ID, string msg, int idofmes, int publicid, bool obs, string datetime)
        {
            idofmessage = idofmes;
            accountidofsender = account_ID;
            message = msg;
            publicidofsender = publicid;
            sendername = sname;
            ownedbysender = obs;
            dateposted = datetime;
        }
    }
    [System.Serializable]
    public class PictureForTempRoom : MessagesForTempRoom
    {
        public string md5hash;
        public string md5hash_t;
        public PictureForTempRoom(string sname, int account_ID, string msg, int idofmes, int publicid, string hash, string thash, bool obs, string datetime) : base(sname, account_ID, msg, idofmes, publicid, obs, datetime)
        {
            md5hash = hash;
            md5hash_t = thash;
        }
    }
    [System.Serializable]
    public class VideoForTempRoom : MessagesForTempRoom
    {
        public string md5hash;
        public string md5hash_t;
        public VideoForTempRoom(string sname, int account_ID, string msg, int idofmes, int publicid, string hash, string thash, bool obs, string datetime) : base(sname, account_ID, msg, idofmes, publicid, obs, datetime)
        {
            md5hash = hash;
            md5hash_t = thash;
        }
    }
    [System.Serializable]
    public class AudioForTempRoom : MessagesForTempRoom
    {
        public string md5hash;
        public AudioForTempRoom(string sname, int account_ID, string msg, int idofmes, int publicid, string hash, bool obs, string datetime) : base(sname, account_ID, msg, idofmes, publicid, obs, datetime)
        {
            md5hash = hash;
        }
    }
    [System.Serializable]
    public class FileForTempRoom : MessagesForTempRoom
    {
        public string md5hash;
        public string extension;
        public long Size;
        public FileForTempRoom(string sname, int account_ID, string msg, int idofmes, int publicid, string hash, string ext, long size, bool obs, string datetime) : base(sname, account_ID, msg, idofmes, publicid, obs, datetime)
        {
            md5hash = hash;
            extension = ext;
            Size = size;
        }
    }
    [System.Serializable]
    public class MuteParams
    {
        public int serverid;
        public int account_ID;
        public DateTime datemuted;
        public MuteParams(int a, int b, DateTime c)
        {
            serverid = a;
            account_ID = b;
            datemuted = c;
        }
    }
    [System.Serializable]
    public class ENDPOINTS
    {
        public IPEndPoint ep = null;
        public List<byte> data = new List<byte>();
        public DateTime timestamp;
        public int public_id = -1;
        public int account_id = -1;
        public int cur_channel = -1;
        public int cur_talk = -1;
    }
    [System.Serializable]
    public class RoomWithThreads
    {
        public List<Action> ActionsOnActionThread = new List<Action>();
        public List<Action> Copied_ActionsOnActionThread = new List<Action>();
        private bool Action_To_Execute = false;

        //
        public bool running;
        public Thread t;

        public int localiteration = 0;
        public RoomWithThreads()
        {
            t = new Thread(new ThreadStart(ThreadManager));

            running = true;
            t.Start();

        }
        public void DestroyThread()
        {
            running = false;
            if (this as ServerWithThreads != null)
            {
                if ((this as ServerWithThreads).saved == true)
                    NetworkManager.serverthreads.Remove(this);
            }
            if (this as PMWithThreads != null)
            {
                NetworkManager.pmthreads.Remove(this);
            }
        }
        private void ThreadManager()
        {
            while (running)
            {
                UpdateMain();
                Thread.Sleep(5);
            }
        }

        public void UpdateMain()
        {
            localiteration++;

            if (localiteration >= 30)
            {
                try
                {
                    if (this as ServerWithThreads != null)
                    {
                        if (NetworkManager.clients.Any(x => x.CurrentSavedServerID == (this as ServerWithThreads).serverid) == false && (this as ServerWithThreads).saved == true/* ||
                        Server.temporaryRooms.Any(x=> x.id == (this as ServerWithThreads).serverid) == false && (this as ServerWithThreads).saved == false*/)
                        {
                            for (int i = 0; i < NetworkManager.ChannelManagers.Count; i++)
                            {
                                if (NetworkManager.ChannelManagers[i].roomtype == 1 && NetworkManager.ChannelManagers[i].ServerID == (this as ServerWithThreads).serverid)
                                {

                                    NetworkManager.ChannelManagers[i].Stop();
                                    NetworkManager.ChannelManagers.RemoveAt(i);
                                    break;
                                }
                            }
                            DestroyThread();
                        }
                    }

                    if (this as PMWithThreads != null)
                    {
                        if (NetworkManager.clients.Any(x => x.UserTalkingTo == (this as PMWithThreads).usera && x.accountid == (this as PMWithThreads).userb ||
                        x.UserTalkingTo == (this as PMWithThreads).userb && x.accountid == (this as PMWithThreads).usera) == false)
                        {
                            DestroyThread();
                        }
                    }
                }
                catch { }
                localiteration = 0;

            }

            if (Action_To_Execute)
            {
                Copied_ActionsOnActionThread.Clear();
                lock (ActionsOnActionThread)
                {
                    try
                    {
                        Copied_ActionsOnActionThread.AddRange((IEnumerable<Action>)ActionsOnActionThread);
                        ActionsOnActionThread.Clear();
                        Action_To_Execute = false;
                    }
                    catch { }
                }
                foreach (Action _a in Copied_ActionsOnActionThread)
                {
                    try
                    {
                        if (_a != null)
                        {
                            _a();
                        }
                    }
                    catch { }
                }
            }
            else
            {
                Copied_ActionsOnActionThread.Clear();
            }
        }
        public void TaskTo_ActionThread(Action a)
        {
            if (a == null)
                return;
            lock (ActionsOnActionThread)
            {
                ActionsOnActionThread.Add(a);
                Action_To_Execute = true;
            }
        }

    }
    [System.Serializable]
    public class ServerWithThreads : RoomWithThreads
    {
        public int serverid;
        public bool saved;
        public ServerWithThreads(int a, bool s)
        {
            serverid = a;
            saved = s;
        }
    }
    [System.Serializable]
    public class PMWithThreads : RoomWithThreads
    {
        public int usera;
        public int userb;
        public PMWithThreads(int a, int b)
        {
            int[] result = ReceivePackets.GetOrderedPMIDs(a, b);
            usera = result[0];
            userb = result[1];
        }
    }
}
