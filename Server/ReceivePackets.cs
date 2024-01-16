
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Mysqlx.Notice;
using Mysqlx.Session;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Bcpg;
using System.Configuration;
using System.Data;
using System.Diagnostics.Metrics;
using System.Net;
using System.Threading.Channels;
using System.Xml.Linq;
using static ChatAppServer.User;


namespace ChatAppServer
{
    public class ReceivePackets
    {
        public static void GoodConnection(int _public_id, Packet packet, ref bool error)
        {
            try
            {
                int account_ID = packet.GetInt(ref error); if(error == true) return;
                int rec_id = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;

                if (NetworkManager.GetClientById(_public_id) != null)
                {
                    if (NetworkManager.GetClientByAccountId(account_ID) != null)
                    {
                        if( NetworkManager.GetClientByAccountId(account_ID).tcp_socket != null && NetworkManager.GetClientByAccountId(account_ID).tcp_socket.Client != null && NetworkManager.GetClientByAccountId(account_ID).tcp_socket.Client.RemoteEndPoint != null)
                            SendPackets.SendMessage(_public_id, "This account is currently logged in on at IPv4 " + NetworkManager.GetClientByAccountId(account_ID).tcp_socket.Client.RemoteEndPoint.ToString());
                        else
                            SendPackets.SendMessage(_public_id, "This account is currently logged in. IPv4 is undefined.");
                        SendPackets.SendCloseLoading(_public_id, "Connecting to master server...", 4);
                        User u = NetworkManager.GetClientById(_public_id);
                        if(u.tcp_socket != null && u.tcp_socket.Connected)
                            u.tcp_socket.Close();
                        NetworkManager.ClientsToTrash.Add(u);
                        return;
                    }
                    NetworkManager.GetClientById(_public_id).SuccessfulTCPConnect = true;
                }
                SendPackets.SendCloseLoading(_public_id, "Connecting to master server...", 0);
                SendPackets.SendTCPReady(_public_id);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": GoodConnection Error");
            }
        }
        public static void ServerHeartbeat(int _public_id, Packet packet, ref bool error)
        {
            try
            {
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientById(_public_id) != null)
                {
                    NetworkManager.GetClientById(_public_id).Ping_Timer = 0;
                    SendPackets.TCPCheck(_public_id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": heartbeat Error");
            }
        }


        public static void CreateChannel(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                string channelname = packet.GetString(ref error); if(error == true) return;
                int channeltype = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    DatabaseCalls.CreateChannel(serverid, channelname,true, new int[2] { _public_id, account_ID }, channeltype);
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to create new channel.");
                SendPackets.SendCloseLoading(_public_id, "Creating channel...", 0);
                Console.WriteLine(ex.Message + ": CreateChannel Error");
            }
        }
        public static void DeleteChannel(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int channelid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                Thread _thread =
                new Thread(delegate ()
                {
                    DatabaseCalls.DeleteChannel(serverid, channelid, new int[2] {_public_id, account_ID });
                });
                _thread.Start();
               
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to delete channel.");
                SendPackets.SendCloseLoading(_public_id, "Deleting channel...", 0);
                Console.WriteLine(ex.Message + ": DeleteChannel Error");
            }
        }
        public static void FriendRequest(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                string toadd = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    DatabaseCalls.AddPendingFriend(toadd, new int[2] { _public_id, account_ID });
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to add friend.");
                SendPackets.SendCloseLoading(_public_id, "Adding friend...", 0);
                Console.WriteLine(ex.Message + ": FriendRequest Error");
            }
        }
        public static void RemoveRole(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int roleid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    string? roles = DatabaseCalls.RemoveRoles(roleid, serverid);
        
                    if (roles != null)
                    {
                 
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendbackRoles(serverid, System.Text.Encoding.UTF8.GetBytes(roles), roleid);
                                    SendPackets.SendCloseLoading(_public_id, "Fetching roles...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                    
                        return;
                    }
                    else
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to fetch roles.");
                                    SendPackets.SendCloseLoading(_public_id, "Fetching roles...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to remove role.");
                SendPackets.SendCloseLoading(_public_id, "Removing Role...", 0);
                Console.WriteLine(ex.Message + ": RemoveRole Error");
            }
        }
        public static void ChangeRolePrecedence(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int roleid = packet.GetInt(ref error); if(error == true) return;
                bool up = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    string? roles = DatabaseCalls.ChangeRolePrecedence(serverid, roleid,up);
                    if (roles != null)
                    { 
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendbackRoles(serverid, System.Text.Encoding.UTF8.GetBytes(roles), -1);
                                    SendPackets.SendCloseLoading(_public_id, "Changing role precedence...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                     
                        return;
                    }
                    else
                    { 
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to change role precedence.");
                                    SendPackets.SendCloseLoading(_public_id, "Changing role precedence...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to change role precedence (2).");
                SendPackets.SendCloseLoading(_public_id, "Changing role precedence...", 0);
                Console.WriteLine(ex.Message + ": ChangeRolePrecedence Error");
            }
        }
        public static void GetProfilePicture(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int theiracc = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    string picture = DatabaseCalls.GetUserProfilePic(theiracc);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                SendPackets.SendBackProfilePicture(_public_id, theiracc, picture);
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                   
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to fetch profile picture.");
                Console.WriteLine(ex.Message + ": GetProfilePicture Error");
            }
        }
        public static void UserLeaveDM(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int other = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    if (DatabaseCalls.RemoveDMS(new int[2] { _public_id,account_ID}, other) == true)
                    {
                       
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                NetworkManager.dms.RemoveAll(x => x[1] == other && x[0] == account_ID);
                                SendPackets.CleanUpOnDMErase(_public_id, other);
                                SendPackets.SendCloseLoading(_public_id, "Removing DM...", 0);
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "Unable to remove DM from your list (0).");
                        SendPackets.SendCloseLoading(_public_id, "Removing DM...", 0);
                    }
                   
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to remove DM from your list (" + ex.Message + ").");
                SendPackets.SendCloseLoading(_public_id, "Removing DM...", 0);
                Console.WriteLine(ex.Message + ": LeaveDMS Error");
            }
        }
        public static void UserLeaveServer(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    if (DatabaseCalls.UserIsInSavedRoom(serverid, account_ID) == false)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "You are not a member of this server.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                        return;
                    }
                    int result = DatabaseCalls.LeaveServer(serverid, account_ID);
                    if (result > 0)
                    {
                        string? sname = DatabaseCalls.GetServerNameByID(serverid);
                        if (sname == null)
                        {
                            
                            Action aa = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to leave server.");
                                    SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(aa);
                           
                            return;
                        }
                        if( result == 2)
                        {
                            bool delres = DatabaseCalls.DestroyServer(serverid);
                            if(delres == false)
                            {
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "Failure in permanently deleting server.");
                                        SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                                
                                return;
                            }
                        }
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                User leaver = NetworkManager.GetClientByAccountId(account_ID);
                                int? publicid = null;
                                if (leaver != null)
                                {
                                    publicid = leaver.id;
                                }
                                SendPackets.CleanUpOnExternalKick((int)publicid, serverid, sname);     
                                SendPackets.LeaveServerNotify(account_ID, serverid);
                                SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                    else
                    {
                        Action aa = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                SendPackets.SendMessage(_public_id, "Unable to leave server.");
                                SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                            }
                        });
                        RunOnPrimaryActionThread(aa);
                    }
                   
                }).Start();
            }
            catch(Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to leave server.");
                SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                Console.WriteLine(ex.Message + ": LeaveServer Error");
            }
        }
                
        public static void PunishUser(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int punsiher = packet.GetInt(ref error); if(error == true) return;
                int punsihed = packet.GetInt(ref error); if(error == true) return;
                int punishid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {

                    if (DatabaseCalls.CanPunish(new int[2] { _public_id, punsiher }, serverid, punishid) == false)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "You do not have the ability to do this.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                        return;
                    }
                    if (DatabaseCalls.UserIsInSavedRoom( serverid, punsihed) == false)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "This user is not a member of this server.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                        return;
                    }

                    if (punishid == 1)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    NetworkManager.mutes.Add(new MuteParams(serverid, punsihed, DateTime.Now.AddDays(1)));
                                    User punishedc = NetworkManager.GetClientByAccountId(punsihed);
                                    int? punishedpublicid = null;
                                    if (punishedc != null)
                                    {
                                        punishedpublicid = punishedc.id;
                                    }
                                    if (NetworkManager.GetClientByAccountId(punsihed) != null && punishedpublicid != null && NetworkManager.GetClientByAccountId(punsihed).id == punishedpublicid)
                                    {
                                        SendPackets.SendMessage((int)punishedpublicid, "You have been muted by " + NetworkManager.GetClientById(_public_id).username + ".");
                                        SendPackets.SendMessage(_public_id, "You have muted " + punishedc.username + ".");
                                    }
                                    else
                                    {
                                        SendPackets.SendMessage(_public_id, "User muted.");
                                    }
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                    if (punishid == 0)
                    {
                        
                        int result = DatabaseCalls.LeaveServer(serverid, punsihed);
                        if (result > 0)
                        {
                            string? sname = DatabaseCalls.GetServerNameByID(serverid);
                            if (sname == null)
                            {
                                return;
                            }
                            if (result == 2)
                            {
                                bool delres = DatabaseCalls.DestroyServer(serverid);
                                if (delres == false)
                                {
                                    Action aa = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Failure in permanently deleting server.");
                                            SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(aa);
                                    
                                    return;
                                }
                            }
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    NetworkManager.kicks.Add(new MuteParams(serverid, punsihed, DateTime.Now.AddDays(1)));
                                    User punishedc = NetworkManager.GetClientByAccountId(punsihed);
                                    int? punishedpublicid = null;
                                    if (punishedc != null)
                                    {
                                        punishedpublicid = punishedc.id;
                                    }
                                    if (NetworkManager.GetClientByAccountId(punsihed) != null && punishedpublicid != null && NetworkManager.GetClientByAccountId(punsihed).id == punishedpublicid)
                                    {
                                        SendPackets.SendMessage(punishedc.id, "You have been kicked from the server by " + NetworkManager.GetClientById(_public_id).username + ".");
                                        SendPackets.SendMessage(_public_id, "You have kicked " + punishedc.username + ".");
                                        SendPackets.CleanUpOnExternalKick(punishedc.id, serverid, sname);
                                    }
                                    else
                                    {
                                        SendPackets.SendMessage(_public_id, "User kicked.");
                                    }
                                    SendPackets.LeaveServerNotify(punsihed, serverid);
                                    SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       

                    }
                    if (punishid == 2)
                    {
                        
                        int result = DatabaseCalls.LeaveServer(serverid, punsihed);
                        if (result > 0)
                        {
                            string? sname = DatabaseCalls.BanUser(serverid, punsihed);
                            if (sname == null)
                            {
                                return;
                            }
                            if (result == 2)
                            {
                                bool delres = DatabaseCalls.DestroyServer(serverid);
                                if (delres == false)
                                {
                                    Action aa = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Failure in permanently deleting server.");
                                            SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(aa);
                                    return;
                                }
                            }
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    User punishedc = NetworkManager.GetClientByAccountId(punsihed);
                                    int? punishedpublicid = null;
                                    if (punishedc != null)
                                    {
                                        punishedpublicid = punishedc.id;
                                    }
                                    if (NetworkManager.GetClientByAccountId(punsihed) != null && punishedpublicid != null && NetworkManager.GetClientByAccountId(punsihed).id == punishedpublicid)
                                    {
                                        SendPackets.SendMessage(punishedc.id, "You have been banned from the server by " + NetworkManager.GetClientById(_public_id).username + ".");
                                        SendPackets.SendMessage(_public_id, "You have banned " + punishedc.username + ".");
                                        SendPackets.CleanUpOnExternalKick(punishedc.id, serverid, sname);
                                    }
                                    else
                                    {
                                        SendPackets.SendMessage(_public_id, "User banned.");
                                    }
                                    SendPackets.LeaveServerNotify(punsihed, serverid);
                                    SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                    return;
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to apply punishment.");
                SendPackets.SendCloseLoading(_public_id, "Please wait a moment...", 0);
                Console.WriteLine(ex.Message + ": PunishUser Error");
            }
        }
        public static void UpdateRoles(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int roleid = packet.GetInt(ref error); if(error == true) return;
                string rolename = packet.GetString(ref error); if(error == true) return;
                int rolekick = packet.GetInt(ref error); if(error == true) return;
                int rolemute = packet.GetInt(ref error); if(error == true) return;
                int roleban = packet.GetInt(ref error); if(error == true) return;
                int roledm = packet.GetInt(ref error); if(error == true) return;
                int rolemc = packet.GetInt(ref error); if(error == true) return;
                int rolemr = packet.GetInt(ref error); if(error == true) return;
                string hex = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    string[] datachunk = new string[] { roleid.ToString(), rolename, rolekick.ToString(), rolemute.ToString(), roleban.ToString(), roledm.ToString(), rolemc.ToString(), rolemr.ToString(),hex };
                    string? roles = DatabaseCalls.UpdateRoles(datachunk, serverid);
                    if(roles != null)
                    {  
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendbackRoles(serverid, System.Text.Encoding.UTF8.GetBytes(roles), -1);
                                    SendPackets.SendCloseLoading(_public_id, "Fetching roles...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                 
                        return;
                    }
                    else
                    {  
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to fetch roles.");
                                    SendPackets.SendCloseLoading(_public_id, "Fetching roles...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                  
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to fetch roles.");
                SendPackets.SendCloseLoading(_public_id, "Fetching roles...", 0);
                Console.WriteLine(ex.Message + ": UpdateRoles Error");
            }
        }
        public static void UpdateChannelSettings(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int channelid = packet.GetInt(ref error); if(error == true) return;
                int ro = packet.GetInt(ref error); if(error == true) return;
                int incuser = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;

                new Thread(delegate ()
                {
                    string? channels = DatabaseCalls.UpdateChannel(serverid, channelid, ro,incuser);
                    if (channels != null)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendBackSavedServerChannels(serverid, System.Text.Encoding.UTF8.GetBytes(channels));
                                    SendPackets.SendCloseLoading(_public_id, "Updating channel...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                      
                        return;
                    }
                    else
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to update channel settings.");
                                    SendPackets.SendCloseLoading(_public_id, "Updating channel...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to update channel settings.");
                SendPackets.SendCloseLoading(_public_id, "Updating channel...", 0);
                Console.WriteLine(ex.Message + ": UpdateChannelSettings Error");
            }
        }
        public static void CallResponse(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            User thec = NetworkManager.GetClientByAccountId(account_ID);
            try
            {
                bool response = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (response == true)
                {
                    if (thec != null)
                    {
                        thec.TimeLeftInPendingCall = 0;
                        if (thec.CurrentCaller == null && thec.PotentialCaller != null) // for the user getting called
                        {
                            if (NetworkManager.GetClientByAccountId((int)thec.PotentialCaller) != null && NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).startdis == false)
                            {
                                thec.CurrentCaller = thec.PotentialCaller; // assign current caller to user who called
                              
                                int max = 0;
                                bool found = false;
                                int port = -1;
                                for (int i = 0; i < NetworkManager.ChannelManagers.Count; ++i)
                                {
                                    if(NetworkManager.ChannelManagers[i].roomtype == 2)
                                    {
                                        if (NetworkManager.ChannelManagers[i].ServerID > max)
                                        {
                                            max = NetworkManager.ChannelManagers[i].ServerID;
                                        }
                                        if (NetworkManager.ChannelManagers[i].clients.Count <= 34)
                                        {
                                            found = true;
                                            port = NetworkManager.ChannelManagers[i].port;
                                            NetworkManager.ChannelManagers[i].clients.Add(thec);
                                            NetworkManager.ChannelManagers[i].clients.Add(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller));
                                        }
                                    }

                                }
                                max++;
                             
                                if (found == false)
                                {
                                    if (NetworkManager.ChannelManagers.Any(x => x.GetType() == typeof(OnFlyChannelManager) && (x as OnFlyChannelManager).ServerID == max && (x as OnFlyChannelManager).roomtype == 2) == false)
                                    {
                                        OnFlyChannelManager ofcm = new OnFlyChannelManager(2, max);
                                        port = ofcm.port;
                                        NetworkManager.ChannelManagers.Add(ofcm);
                                        ofcm.clients.Add(thec);
                                        ofcm.clients.Add(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller));
                                    }
                                }
                                if(port == -1)
                                {
                                    SendPackets.SendMessage(_public_id, "Failed to initialize UDP Handler on server side.");
                                    SendPackets.SendCloseLoading(_public_id, "Calling user...", 0);
                                    return;
                                }
                                NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).CurrentCaller = account_ID; // assign called to callers currentcall
                                pass_data_toserv(thec, 1, (int)thec.PotentialCaller);
                                pass_data_toserv(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller), 1, account_ID);
                                NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).PotentialCaller = null;
                                thec.PotentialCaller = null;
                                SendPackets.SendCallResponse(NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).id, account_ID, thec.username, port);
                                SendPackets.SendCallResponse(thec.id, thec.CurrentCaller, NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).username, port);
                            }
                            else
                            {
                                SendPackets.SendMessage(_public_id, "User is no longer online.");
                                SendPackets.SendCloseLoading(_public_id, "Calling user...", 0);
                            }
                        }
                    }
                    else
                    {
                        if(thec.CurrentCaller != null && NetworkManager.GetClientByAccountId((int)thec.CurrentCaller) != null)
                        {
                            SendPackets.SendCallResponse(NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).id, null, null, -1);
                            NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).CurrentCaller = null;
                            pass_data_toserv(NetworkManager.GetClientByAccountId((int)thec.CurrentCaller), 1, -1);
                            NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).PotentialCaller = null;
                        }
                        else if(thec.PotentialCaller != null && NetworkManager.GetClientByAccountId((int)thec.PotentialCaller) != null)
                        {
                            SendPackets.SendCallResponse(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).id, null, null, -1);
                            NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).CurrentCaller = null;
                            pass_data_toserv(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller), 1, -1);
                            NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).PotentialCaller = null;
                        }
                        SendPackets.SendCallResponse(thec.id, null, null, -1);
                        thec.TimeLeftInPendingCall = 0;
                     

                        if(NetworkManager.ChannelManagers.Any(x=>x.clients.Contains(thec)))
                        {
                            OnFlyChannelManager cm = NetworkManager.ChannelManagers.Find(x => x.clients.Contains(thec));
                            cm.clients.Remove(thec);
                            User other = NetworkManager.GetClientByAccountId((int)thec.CurrentCaller);
                            if (other != null)
                            {
                                if(cm.clients.Contains(other))
                                {
                                    cm.clients.Remove(other);
                                }
                            }
                            if(cm.clients.Count == 0)
                            {
                                cm.Stop();
                                NetworkManager.ChannelManagers.Remove(cm);
                            }
                        }
                        thec.CurrentCaller = null;
                        pass_data_toserv(thec, 1, -1);
                        thec.PotentialCaller = null;
                    }
                }
                else
                {
                    if (thec.CurrentCaller != null && NetworkManager.GetClientByAccountId((int)thec.CurrentCaller) != null)
                    {
                        SendPackets.SendCallResponse(NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).id, null, null, -1);
                        NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).CurrentCaller = null;
                        pass_data_toserv(NetworkManager.GetClientByAccountId((int)thec.CurrentCaller), 1, -1);
                        NetworkManager.GetClientByAccountId((int)thec.CurrentCaller).PotentialCaller = null;
                    }
                    else if (thec.PotentialCaller != null && NetworkManager.GetClientByAccountId((int)thec.PotentialCaller) != null)
                    {
                        SendPackets.SendCallResponse(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).id, null, null, -1);
                        NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).CurrentCaller = null;
                        pass_data_toserv(NetworkManager.GetClientByAccountId((int)thec.PotentialCaller), 1, -1);
                        NetworkManager.GetClientByAccountId((int)thec.PotentialCaller).PotentialCaller = null;
                    }
                    SendPackets.SendCallResponse(thec.id, null, null,-1);
                    thec.TimeLeftInPendingCall = 0;
                  

                    if (NetworkManager.ChannelManagers.Any(x => x.clients.Contains(thec)))
                    {
                        OnFlyChannelManager cm = NetworkManager.ChannelManagers.Find(x => x.clients.Contains(thec));
                        cm.clients.Remove(thec);
                        User other = NetworkManager.GetClientByAccountId((int)thec.CurrentCaller);
                        if (other != null)
                        {
                            if (cm.clients.Contains(other))
                            {
                                cm.clients.Remove(other);
                            }
                        }
                        if (cm.clients.Count == 0)
                        {
                            cm.Stop();

                            NetworkManager.ChannelManagers.Remove(cm);
                        }
                    }
                    thec.CurrentCaller = null;
                    pass_data_toserv(thec, 1, -1);
                    thec.PotentialCaller = null;
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to call this user.");
                SendPackets.SendCloseLoading(_public_id, "Calling user...", 0);
                Console.WriteLine(ex.Message + ": CallResponse Error");
            }
        }
        public static void CallSomeone(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int toaccountid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientByAccountId(toaccountid) == null || NetworkManager.GetClientByAccountId(toaccountid) != null && NetworkManager.GetClientByAccountId(toaccountid).startdis == true)
                {
                    SendPackets.SendMessage(_public_id, "Unable to call this user. This user is not online. ");
                    SendPackets.SendCloseLoading(_public_id, "Calling user...", 3);
                    return;
                }
                if(NetworkManager.GetClientByAccountId(account_ID).CurrentCaller == null && NetworkManager.GetClientByAccountId(account_ID).PotentialCaller == null)
                {
                    if (NetworkManager.GetClientByAccountId(toaccountid).CurrentCaller == null && NetworkManager.GetClientByAccountId(toaccountid).PotentialCaller == null)
                    {
                        NetworkManager.GetClientByAccountId(account_ID).PotentialCaller = toaccountid;
                        NetworkManager.GetClientByAccountId(toaccountid).PotentialCaller = account_ID;
                        NetworkManager.GetClientByAccountId(toaccountid).TimeLeftInPendingCall = 20000;
                        SendPackets.SendBackCall(NetworkManager.GetClientByAccountId(toaccountid).id, account_ID);
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "Unable to call this user. This user is already being called/in a call. ");
                        SendPackets.SendCloseLoading(_public_id, "Calling user...", 0);
                    }
                }
                else
                {
                    SendPackets.SendMessage(_public_id, "Unable to call this user. You are already in a call. ");
                    SendPackets.SendCloseLoading(_public_id, "Calling user...", 0);
                }
              
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to initiate call.");
                SendPackets.SendCloseLoading(_public_id, "Calling user...", 0);
                Console.WriteLine(ex.Message + ": CallSomeone Error");
            }
        }
        public static string TextFileName(int account_ID, int otheraccount_ID)
        {
            if(account_ID > otheraccount_ID)
            {
                return System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), account_ID + "~" + otheraccount_ID + ".txt");
            }
            else
            {
                return System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), otheraccount_ID + "~" + account_ID + ".txt");
            }
        }
        public static int[] GetOrderedPMIDs(int account_ID, int otheraccount_ID)
        {
            if (account_ID > otheraccount_ID)
            {
                return new int[] { account_ID, otheraccount_ID};
            }
            else
            {
                return new int[] { otheraccount_ID, account_ID };
            }
        }
        public static void NotifyServerOfPMStart(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int acca = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    
                    string filename = TextFileName(account_ID, acca);
                    int messagecount;
                    if(File.Exists(filename) == false)
                    {
                        if(DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, account_ID }, acca, false) == false)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendCloseLoading(_public_id, "Loading private messages...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }
                        Action aa = (Action)(() =>
                        {
                            if (NetworkManager.dms.Any(x => x[0] == acca && x[1] == account_ID) == false)
                            {
                                NetworkManager.dms.Add(new int[2] { acca, account_ID });
                            }
                            if (NetworkManager.dms.Any(x => x[1] == acca && x[0] == account_ID) == false)
                            {
                                NetworkManager.dms.Add(new int[2] { account_ID, acca });
                            }
                        });
                        RunOnPrimaryActionThread(aa);
                       

                        try
                        {
                            using (FileStream fs = File.Create(filename)) { }
                        }
                        catch (System.Exception ex) {
                            Console.WriteLine(ex.Message);
                            return;
                        }
                    }
                    else
                    {
                        if (DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, account_ID }, acca, true) == false)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendCloseLoading(_public_id, "Loading private messages...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }
                        Action aa = (Action)(() =>
                        {
                            if (NetworkManager.dms.Any(x => x[1] == acca && x[0] == account_ID) == false)
                            {
                                NetworkManager.dms.Add(new int[2] { account_ID, acca });
                            }
                        });
                        RunOnPrimaryActionThread(aa);
                       
                    }
                    int ReadAttempts = 0;
                    List<string> messages = new List<string>();
                RestartRead:;
                    try
                    {
                        messages = File.ReadAllLines(filename, System.Text.Encoding.UTF8).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList().FindAll(x=> (int.Parse(x.Split('❶').Last()) == 1 && int.Parse(x.Split('❶')[2]) == account_ID) || (int.Parse(x.Split('❶').Last()) == 0 && int.Parse(x.Split('❶')[2]) != account_ID)); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                    }
                    catch // This will most likely occur if another thread is reading/writing from/to this file
                    {
                        Thread.Sleep(200);
                        if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                        {
                            Action aa = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to read messages from conversation.");
                                }
                            });
                            RunOnPrimaryActionThread(aa);
                          
                            return;
                        }
                        ReadAttempts++;
                        goto RestartRead; // Attempt to read again.
                    }
                    
                    int textchannellins = messages.Count;
                    string? otheraccpublickey = DatabaseCalls.GetUserPublicKeyByID(acca);
                    if(otheraccpublickey == null)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Unable to retrieve user public key.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                        return;
                    }
                    RemovePLFromAllRooms(new int[2] { _public_id, account_ID },true);
                    {
                        Action a = (Action)(() => //execute on one of the primary threads. Wait until this has executed
                        {
                            NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo = acca;
                            if (NetworkManager.pmthreads.Any(x => x as PMWithThreads != null && (x as PMWithThreads).usera == acca && (x as PMWithThreads).userb == account_ID ||
                            x as PMWithThreads != null && (x as PMWithThreads).usera == account_ID && (x as PMWithThreads).userb == acca))
                            {

                            }
                            else
                            {

                                NetworkManager.pmthreads.Add(new PMWithThreads(acca, account_ID));
                            }
                        
                            SendPackets.AddOrRemoveUserFromList(NetworkManager.GetClientByAccountId(account_ID).id, 1, NetworkManager.GetClientByAccountId(account_ID), " ");
                            if (NetworkManager.GetClientByAccountId(acca) != null && NetworkManager.GetClientByAccountId(acca).UserTalkingTo == account_ID && NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo == acca)
                            {
                                SendPackets.AddOrRemoveUserFromList(NetworkManager.GetClientByAccountId(account_ID).id, 1, NetworkManager.GetClientByAccountId(acca), " ");
                                SendPackets.AddOrRemoveUserFromList(NetworkManager.GetClientByAccountId(acca).id, 1, NetworkManager.GetClientByAccountId(account_ID), " ");
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                    
                    string accaname = DatabaseCalls.GetUsernameByID(acca);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                SendPackets.PMReady(_public_id, textchannellins, acca, accaname, otheraccpublickey);
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                   
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to start PM.");
                SendPackets.SendCloseLoading(_public_id, "Loading private messages...", 0);
                Console.WriteLine(ex.Message + ": NotifyServerOfPMStart Error");
            }
        }
        public static void ReadyForPMDatabaseWrite(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int acca = packet.GetInt(ref error); if(error == true) return;
                int accb = packet.GetInt(ref error); if(error == true) return;
                string codea = packet.GetString(ref error); if(error == true) return; // this is the senders public key
                string codeb = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                 {
                     if (DatabaseCalls.AddPMToDatabase(acca, accb, codea, codeb) == false) // put this pm into the database
                     { 
                         
                         {
                             Action a = (Action)(() =>
                             {
                                 if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                 {
                                     SendPackets.SendMessage(_public_id, "Unable to start conversation.");
                                     SendPackets.SendCloseLoading(_public_id, "Loading private messages...", 0);
                                 }
                             });
                             RunOnPrimaryActionThread(a);
                         }
                        
                         return;
                     }

                     string? hashkey = DatabaseCalls.GetPMHashKey(acca, accb);
                     if (hashkey == null)
                     {                     
                         
                         {
                             Action a = (Action)(() =>
                             {
                                 if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                 {
                                     SendPackets.SendMessage(_public_id, "Unable to fetch encryption key.");
                                     SendPackets.SendCloseLoading(_public_id, "Loading private messages...", 0);
                                 }
                             });
                             RunOnPrimaryActionThread(a);
                         }
                        
                         return;
                     }
                     
                     {
                         Action a = (Action)(() =>
                         {
                             if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                             {
                                 SendPackets.SendBackPMHash(_public_id, (string)hashkey);
                             }
                         });
                         RunOnPrimaryActionThread(a);
                     }
                    
                  
                 }).Start();
            }
            catch(System.Exception ex)
            {
                Console.WriteLine(ex.Message + ": ReadyForPMDatabaseWrite Error");
            }
        }
        public static void RequestOpenProfile(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            try
            {
                int openedacc = packet.GetInt(ref error); if(error == true) return;
                int serverid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                int account_ID = NetworkManager.GetClientById(_public_id).accountid;
                new Thread(delegate ()
                {
                    object[]? returneddata = DatabaseCalls.OpenUserProfile(account_ID, openedacc, serverid);
                    if (returneddata != null)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.OpenUserProfile(_public_id, openedacc, (string)returneddata[0], (string)returneddata[1], (string)returneddata[2], (string)returneddata[3], (string)returneddata[4], (string)returneddata[5], (string)returneddata[6]);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not open profile.");
                Console.WriteLine(ex.Message + ": RequestOpenProfile Error");
            }
        }
        public static void RequestSaveProfile(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            try
            {
                string sites = packet.GetString(ref error); if (error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                int account_ID = NetworkManager.GetClientById(_public_id).accountid;
                new Thread(delegate ()
                {
                    bool returned = DatabaseCalls.SaveUserMedia(new int[2] { _public_id, account_ID }, sites);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                SendPackets.SendSuccessInProfileSave(_public_id);
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                   
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not save profile.");
                Console.WriteLine(ex.Message + ": RequestSaveProfile Error");
            }
        }
        public static void LoadSelectedMessageFromSearch(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;

            try
            {

                int msgid = packet.GetInt(ref error); if(error == true) return;
                int roomid = packet.GetInt(ref error); if(error == true) return;
                int channel = packet.GetInt(ref error); if(error == true) return;
                bool saved = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;

                if (NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo != null)
                {
                    new Thread(delegate () //making a new thread is required, or else it will stop up this current thread.
                    {
                        //does this channel exist, specifically as a readable and writable .txt
                        string serverchanneltextfile = TextFileName(account_ID, (int)NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo);

                        if (File.Exists(serverchanneltextfile) == false)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }
                        int ReadAttempts = 0;
                        string[] messages = new string[] { };
                    RestartRead:;
                        try
                        {
                            messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {  
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                            SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                               
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }
                        int themsgindex = -1;
                        for (int i = 0; i < messages.Length; ++i)
                        {
                            if (messages[i].Contains("❶"))
                            {
                                string[] thesplit = messages[i].Split('❶');
                                if (int.Parse(thesplit[4]) == msgid)
                                {
                                    themsgindex = i;
                                }
                            }
                        }
                        if (themsgindex != -1)
                        { 
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendBackMessageSpanForSearch(_public_id, Methods.Clamp(themsgindex + 30, 0, messages.Length) - Methods.Clamp(themsgindex - 30, 0, messages.Length), msgid, -1, 0, false); // Send to player how many messages are in this chunk
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                    }).Start();
                }
                else if (saved == false)
                {
                    if (NetworkManager.GetRoomById(roomid) != null)
                    {
                        if (NetworkManager.GetRoomById(roomid).clientsinroom.Contains(NetworkManager.GetClientByAccountId(account_ID)) == true)
                        {
                            if ((NetworkManager.GetRoomById(roomid).channels[channel] as ChannelsForTempRoom) != null)
                            {
                                ChannelsForTempRoom c = (NetworkManager.GetRoomById(roomid).channels[channel] as ChannelsForTempRoom);
                                int themsgindex = -1;
                                for (int i = 0; i < c.messages.Count; ++i)
                                {
                                    if (c.messages[i].idofmessage == msgid)
                                    {
                                        themsgindex = i;
                                    }
                                }
                                if (themsgindex != -1)
                                {
                                    SendPackets.SendBackMessageSpanForSearch(_public_id, Methods.Clamp(themsgindex + 30, 0, c.messages.Count) - Methods.Clamp(themsgindex - 30, 0, c.messages.Count), msgid, roomid, channel, saved); // Send to player how many messages are in this chunk
                                }
                            }
                            else
                            {
                                SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                            }
                        }
                        else
                        {
                            SendPackets.SendMessage(_public_id, "You are not a member of this room.");
                            SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                        }
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                        SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                    }
                }
                else
                {
                    new Thread(delegate () //making a new thread is required, or else it will stop up this current thread.
                    {
                        if (DatabaseCalls.UserIsInSavedRoom((int)NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID, NetworkManager.GetClientByAccountId(account_ID).accountid) == true) // Is this user even in this room?
                        {
                            //does this channel exist, specifically as a readable and writable .txt
                            string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID.ToString());
                            string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID.ToString() + "\\" + channel + ".txt");

                            if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                            SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            int ReadAttempts = 0;
                            string[] messages = new string[] { };
                        RestartRead:;
                            try
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                {
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                                SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }
                            int themsgindex = -1;
                            for (int i = 0; i < messages.Length; ++i)
                            {
                                if (messages[i].Contains("❶"))
                                {
                                    string[] thesplit = messages[i].Split('❶');
                                    if (int.Parse(thesplit[4]) == msgid)
                                    {
                                        themsgindex = i;
                                    }
                                }
                            }
                            if (themsgindex != -1)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackMessageSpanForSearch(_public_id, Methods.Clamp(themsgindex + 30, 0, messages.Length) - Methods.Clamp(themsgindex - 30, 0, messages.Length), msgid, roomid, channel, saved); // Send to player how many messages are in this chunk
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to load search messages.");
                SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                Console.WriteLine(ex.Message + ": LoadSelectedMessageFromSearch Error");
            }
        }
        public static void ReceivedLoadSelectedMessageFromSearch(int _public_id, Packet packet, ref bool error) 
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;

            try
            {
                int msgid = packet.GetInt(ref error); if(error == true) return;
                int roomid = packet.GetInt(ref error); if(error == true) return;
                int channel = packet.GetInt(ref error); if(error == true) return;
                bool saved = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo != null)
                {
                    int usertalkingto = (int)NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo;
                    new Thread(delegate ()
                    {
                        string serverchanneltextfile = TextFileName(account_ID, usertalkingto);

                        if (File.Exists(serverchanneltextfile) == false)
                        {
                           
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }

                        int ReadAttempts = 0;
                        string[] messages = new string[] { };
                    RestartRead:;
                        try
                        {
                            messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                        SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                             
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }
                        int themsgindex = -1;
                        for (int i = 0; i < messages.Length; ++i)
                        {
                            if (messages[i].Contains("❶"))
                            {
                                string[] thesplit = messages[i].Split('❶');
                                if (int.Parse(thesplit[4]) == msgid)
                                {
                                    themsgindex = i;
                                }
                            }
                        }

                        if (themsgindex != -1)
                        {
                            for (int i = Methods.Clamp(themsgindex - 30, 0, messages.Length); i < Methods.Clamp(themsgindex + 30, 0, messages.Length); ++i)
                            {
                                Thread.Sleep(20);
                                if (messages[i].Contains("❶") == false)
                                {
                                    continue;
                                }
                                string[] MessageStuff = messages[i].Split('❶');
                                if (MessageStuff[0] == "0")
                                {
                                    MessagesForTempRoom msgobj = new MessagesForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, int.Parse(MessageStuff[6]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, true, channel, roomid, true);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "1")
                                {
                                    MessagesForTempRoom msgobj = new PictureForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackPictureMessageToAllInRoom(msgobj as PictureForTempRoom, null, _public_id, true, channel, roomid, true);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "2")
                                {
                                    MessagesForTempRoom msgobj = new VideoForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackVideoMessageToAllInRoom(msgobj as VideoForTempRoom, null, _public_id, true, channel, roomid, true);

                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "3")
                                {
                                    MessagesForTempRoom msgobj = new AudioForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], int.Parse(MessageStuff[7]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackAudioMessageToAllInRoom(msgobj as AudioForTempRoom, null, _public_id, true, channel, roomid, true);

                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "4")
                                {
                                    MessagesForTempRoom msgobj = new FileForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]), int.Parse(MessageStuff[9]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackFileMessageToAllInRoom(msgobj as FileForTempRoom, null, _public_id, true, channel, roomid, true);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                            }
                        }
                    }).Start();
                }
                else if (saved == false)
                {
                    if (NetworkManager.GetRoomById(roomid) != null)
                    {
                        if (NetworkManager.GetRoomById(roomid).clientsinroom.Contains(NetworkManager.GetClientByAccountId(account_ID)) == true)
                        {
                            if ((NetworkManager.GetRoomById(roomid).channels[channel] as ChannelsForTempRoom) != null)
                            {
                                ChannelsForTempRoom c = (NetworkManager.GetRoomById(roomid).channels[channel] as ChannelsForTempRoom);
                                int themsgindex = -1;
                                for (int i = 0; i < c.messages.Count; ++i)
                                {
                                    if (c.messages[i].idofmessage == msgid)
                                    {
                                        themsgindex = i;
                                    }
                                }
                                if (themsgindex != -1)
                                {
                                    for (int i = Methods.Clamp(themsgindex - 30, 0, c.messages.Count); i < Methods.Clamp(themsgindex + 30, 0, c.messages.Count); ++i)
                                    {
                                        Thread.Sleep(20);
                                        if (c.messages[i].GetType() == typeof(MessagesForTempRoom))
                                        {
                                            SendPackets.SendBackMessageToAllInRoom(c.messages[i], null, _public_id, true, channel, roomid, true);
                                        }
                                        if (c.messages[i].GetType() == typeof(PictureForTempRoom))
                                        {
                                            SendPackets.SendBackPictureMessageToAllInRoom(c.messages[i] as PictureForTempRoom, null, _public_id, true, channel, roomid, true);
                                        }
                                        if (c.messages[i].GetType() == typeof(VideoForTempRoom))
                                        {
                                            SendPackets.SendBackVideoMessageToAllInRoom(c.messages[i] as VideoForTempRoom, null, _public_id, true, channel, roomid, true);
                                        }
                                        if (c.messages[i].GetType() == typeof(AudioForTempRoom))
                                        {
                                            SendPackets.SendBackAudioMessageToAllInRoom(c.messages[i] as AudioForTempRoom, null, _public_id, true, channel, roomid, true);
                                        }
                                        if (c.messages[i].GetType() == typeof(FileForTempRoom))
                                        {
                                            SendPackets.SendBackFileMessageToAllInRoom(c.messages[i] as FileForTempRoom, null, _public_id, true, channel, roomid, true);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                            }
                        }
                        else
                        {
                            SendPackets.SendMessage(_public_id, "You are not a member of this room.");
                            SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                        }
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                        SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                    }
                }
                else
                {
                    if(NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID == null)
                        {
                            return;
                        }
                    int sserver = (int)NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID;
                    new Thread(delegate ()
                    {
                       
                        if (DatabaseCalls.UserIsInSavedRoom(sserver, account_ID) == true)
                        {
                            string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), sserver.ToString());
                            string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), sserver.ToString() + "\\" + channel + ".txt");

                            if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                            {
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                               
                                return;
                            }

                            int ReadAttempts = 0;
                            string[] messages = new string[] { };
                        RestartRead:;
                            try
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                {
                                    Action aa = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                            SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(aa);
                                  
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }
                            int themsgindex = -1;
                            for (int i = 0; i < messages.Length; ++i)
                            {
                                if (messages[i].Contains("❶"))
                                {
                                    string[] thesplit = messages[i].Split('❶');
                                    if (int.Parse(thesplit[4]) == msgid)
                                    {
                                        themsgindex = i;
                                    }
                                }
                            }

                            if (themsgindex != -1)
                            {
                                for (int i = Methods.Clamp(themsgindex - 30, 0, messages.Length); i < Methods.Clamp(themsgindex + 30, 0, messages.Length); ++i)
                                {
                                    Thread.Sleep(20);
                                    if (messages[i].Contains("❶") == false)
                                    {
                                        continue;
                                    }
                                    string[] MessageStuff = messages[i].Split('❶');
                                    if (MessageStuff[0] == "0")
                                    {
                                        MessagesForTempRoom msgobj = new MessagesForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, int.Parse(MessageStuff[6]) == 1 ? true : false, MessageStuff[5]);
                                        
                                        {
                                            Action a = (Action)(() =>
                                            {
                                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                                {
                                                    SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, true, channel, roomid, true);
                                                }
                                            });
                                            RunOnPrimaryActionThread(a);
                                        }
                                       
                                    }
                                    if (MessageStuff[0] == "1")
                                    {
                                        MessagesForTempRoom msgobj = new PictureForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                        
                                        {
                                            Action a = (Action)(() =>
                                            {
                                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                                {
                                                    SendPackets.SendBackPictureMessageToAllInRoom(msgobj as PictureForTempRoom, null, _public_id, true, channel, roomid, true);
                                                }
                                            });
                                            RunOnPrimaryActionThread(a);
                                        }
                                       
                                    }
                                    if (MessageStuff[0] == "2")
                                    {
                                        MessagesForTempRoom msgobj = new VideoForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                        
                                        {
                                            Action a = (Action)(() =>
                                            {
                                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                                {
                                                    SendPackets.SendBackVideoMessageToAllInRoom(msgobj as VideoForTempRoom, null, _public_id, true, channel, roomid, true);
                                                }
                                            });
                                            RunOnPrimaryActionThread(a);
                                        }
                                       
                                    }
                                    if (MessageStuff[0] == "3")
                                    {
                                        MessagesForTempRoom msgobj = new AudioForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], int.Parse(MessageStuff[7]) == 1 ? true : false, MessageStuff[5]);
                                        
                                        {
                                            Action a = (Action)(() =>
                                            {
                                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                                {
                                                    SendPackets.SendBackAudioMessageToAllInRoom(msgobj as AudioForTempRoom, null, _public_id, true, channel, roomid, true);
                                                }
                                            });
                                            RunOnPrimaryActionThread(a);
                                        }
                                       
                                    }
                                    if (MessageStuff[0] == "4")
                                    {
                                        MessagesForTempRoom msgobj = new FileForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]), int.Parse(MessageStuff[9]) == 1 ? true : false, MessageStuff[5]);
                                        
                                        {
                                            Action a = (Action)(() =>
                                            {
                                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                                {
                                                    SendPackets.SendBackFileMessageToAllInRoom(msgobj as FileForTempRoom, null, _public_id, true, channel, roomid, true);
                                                }
                                            });
                                            RunOnPrimaryActionThread(a);
                                        }
                                       
                                    }
                                }
                            }
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to load search messages.");
                SendPackets.SendCloseLoading(_public_id, "Retrieving messages...", 0);
                Console.WriteLine(ex.Message + ": ReceivedLoadSelectedMessageFromSearch Error");
            }
        }
        public static void InitiateMessageSearch(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;

            try
            {

                string msg = packet.GetString(ref error); if(error == true) return;
                int roomid = packet.GetInt(ref error); if(error == true) return;
                int channel = packet.GetInt(ref error); if(error == true) return;
                bool saved = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo != null)
                {
                    int talkingto = (int)NetworkManager.GetClientByAccountId(account_ID).UserTalkingTo;
                    new Thread(delegate ()
                    {
                        string serverchanneltextfile = TextFileName(account_ID, talkingto);

                        if (File.Exists(serverchanneltextfile) == false)
                        { 
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                           
                            return;
                        }

                        int ReadAttempts = 0;
                        string[] messages = new string[] { };
                    RestartRead:;
                        try
                        {
                            messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {  
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                            SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }

                        string builtstring = " ";
                        for (int i = 0; i < messages.Length; ++i)
                        {
                            if (messages[i].Contains("❶"))
                            {
                                string[] thesplit = messages[i].Split('❶');
                                if (thesplit[3].Contains(msg))
                                {
                                    builtstring += messages[i] + "❷";
                                }
                            }
                        } 
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                    SendPackets.SendToUserTheirMSGSearch(_public_id, System.Text.Encoding.UTF8.GetBytes(builtstring)); //Populate the results on user client side
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }).Start();
                }
                else if (saved == false)
                {

                    if (NetworkManager.GetRoomById(roomid) != null)
                    {
                        if (NetworkManager.GetRoomById(roomid).clientsinroom.Contains(NetworkManager.GetClientByAccountId(account_ID)) == true)
                        {
                            if ((NetworkManager.GetRoomById(roomid).channels[channel] as ChannelsForTempRoom) != null)
                            {
                                ChannelsForTempRoom c = (NetworkManager.GetRoomById(roomid).channels[channel] as ChannelsForTempRoom);
                                string builtstring = " ";
                                for (int i = 0; i < c.messages.Count; ++i)
                                {
                                    if (c.messages[i].message.Contains(msg))
                                    {
                                        builtstring += (c.messages[i].GetType() == typeof(MessagesForTempRoom) ? 0 : (c.messages[i].GetType() == typeof(PictureForTempRoom) ? 1 : (c.messages[i].GetType() == typeof(VideoForTempRoom) ? 2 : (c.messages[i].GetType() == typeof(AudioForTempRoom) ? 3 : 4)))) + "❶" +
                                        c.messages[i].sendername + "❶" + c.messages[i].accountidofsender + "❶" + c.messages[i].message + "❶" + c.messages[i].idofmessage + "❶" + c.messages[i].dateposted + "❷";
                                    }
                                }
                                SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                SendPackets.SendToUserTheirMSGSearch(_public_id, System.Text.Encoding.UTF8.GetBytes(builtstring)); //Populate the results on user client side
                            }
                        }
                        else
                        {
                            SendPackets.SendMessage(_public_id, "You are not a member of this room.");
                            SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                        }
                    }
                }
                else
                {
                    if(NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID == null)
                    {
                        return;
                    }
                    int sserver = (int)NetworkManager.GetClientByAccountId(account_ID).CurrentSavedServerID;
                    new Thread(delegate ()
                    {
                        if (DatabaseCalls.UserIsInSavedRoom(sserver, account_ID) == true)
                        {
                            string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), sserver.ToString());
                            string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), sserver.ToString() + "\\" + channel + ".txt");

                            if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                            { 
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                            SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                              
                                return;
                            }

                            int ReadAttempts = 0;
                            string[] messages = new string[] { };
                        RestartRead:;
                            try
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                { 
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                            {
                                                SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                                SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                  
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }

                            string builtstring = " ";
                            for (int i = 0; i < messages.Length; ++i)
                            {
                                if (messages[i].Contains("❶"))
                                {
                                    string[] thesplit = messages[i].Split('❶');
                                    if (thesplit[3].Contains(msg))
                                    {
                                        builtstring += messages[i] + "❷";
                                    }
                                }
                            }
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                        SendPackets.SendToUserTheirMSGSearch(_public_id, System.Text.Encoding.UTF8.GetBytes(builtstring)); //Populate the results on user client side
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                          
                        }
                        else
                        { 
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "You are not a member of this room.");
                                        SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                          
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to search for messages.");
                SendPackets.SendCloseLoading(_public_id, "Searching...", 0);
                Console.WriteLine(ex.Message + ": InitiateMessageSearch Error");
            }
        }
        public static void ClientRequestLeaveRoom(int _public_id, Packet packet, ref bool error)
        {
          
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientByAccountId(account_ID) != null)
                {
                    RemovePLFromAllRooms(new int[2] { _public_id, account_ID }, false);
                    SendPackets.SendCloseLoading(_public_id, "Leaving current room...", 0);
                    SendPackets.SendBackRoomLeftSuccess(_public_id);
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to leave current room.");
                SendPackets.SendCloseLoading(_public_id, "Leaving current room...", 0);
                Console.WriteLine(ex.Message + ": ClientRequestLeaveRoom Error");
            }
        }
        public static void ClientRequestMakeRoom(int _public_id, Packet packet, ref bool error) //Make temp room
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                string key = packet.GetString(ref error); if(error == true) return;
                string _name = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientByAccountId(account_ID) != null)
                {
                    RemovePLFromAllRooms(new int[2] { _public_id, account_ID }, false);
                    if (NetworkManager.temporaryRooms.Any(r => r.name == _name) == true)
                    {
                        SendPackets.SendMessage(_public_id, "Unable to create room. Room already exists with this name.");
                        SendPackets.SendCloseLoading(_public_id, "Creating room...", 1);
                        return;
                    }
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(account_ID))) == false)
                    {
                        TemporaryRooms room = new TemporaryRooms(NetworkManager.temporaryRooms.Count > 0 ? NetworkManager.temporaryRooms.Last().id + 1 : 1, key, NetworkManager.GetClientByAccountId(account_ID), _name);
                        NetworkManager.temporaryRooms.Add(room);
                        room.channels.Add(1, new ChannelsForTempRoom("Main"));
                        room.channels.Add(2, new ChannelsForTempRoom("Test"));
                        room.channels.Add(3, new VoiceChannelsForTempRoom("Voice A"));

                        SendPackets.AddOrRemoveUserFromList(1, NetworkManager.GetClientByAccountId(account_ID), " ", room.id, false);
                        Console.WriteLine(String.Format( "Private temp room with id of {0} has been created", room.id));
                        string builtchannelstring = "";
                        foreach(int i in room.channels.Keys)
                        {
                            if (room.channels[i].GetType() == typeof(ChannelsForTempRoom))
                            {
                                ChannelsForTempRoom cftr = (ChannelsForTempRoom)room.channels[i];
                                builtchannelstring += i + "❶" + cftr.ChannelName + "❶0❶" + cftr.messages.Count + "❶" + (cftr.messages.Count > 30 ? 30 : cftr.messages.Count) + "❶0/❶0❷";
                            }
                            if (room.channels[i].GetType() == typeof(VoiceChannelsForTempRoom))
                            {
                                VoiceChannelsForTempRoom vcftr = (VoiceChannelsForTempRoom)room.channels[i];
                                builtchannelstring += i + "❶" + vcftr.ChannelName + "❶0/❶0❷";
                            }
                        }
                        SendPackets.SendbackRoomCreationSuccess(_public_id, room.id, System.Text.Encoding.UTF8.GetBytes(builtchannelstring), 1);
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "Unable to create room. Please manually leave your current room.");
                        SendPackets.SendCloseLoading(_public_id, "Creating room...", 1);
                    }
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Unable to create room.");
                SendPackets.SendCloseLoading(_public_id, "Creating room...", 1);
                Console.WriteLine(ex.Message + ": ClientRequestMakeRoom Error");
            }

        }
        public static void ClientRequestJoinRoom(int _public_id, Packet packet, ref bool error)
        {
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                string roomname = packet.GetString(ref error); if(error == true) return;
                bool issaved = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (issaved == false)
                {
                    if (NetworkManager.GetRoomByName(roomname) == null) // if room trying to join even exists
                    {
                        SendPackets.SendMessage(_public_id, "This room does not exist.");
                        SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                    }
                    RemovePLFromAllRooms(new int[2] { _public_id, account_ID }, false);
                    if (NetworkManager.GetRoomByName(roomname) != null) // if room trying to join even exists
                    {
                        SendPackets.SendbackRoomRoot(_public_id, NetworkManager.GetRoomByName(roomname).id, false, NetworkManager.GetRoomByName(roomname).KeySentence, false);
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "This room does not exist.");
                        SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                    }
                }
                else
                {
                    new Thread(delegate ()
                    {
                        int? roomid = DatabaseCalls.SavedRoomExistsByName(roomname);
                        if (roomid != null) // if room trying to join even exists
                        {
                            string? iskicked = null;
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        string? iskicked = NetworkManager.CheckKick(account_ID, (int)roomid);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            if (iskicked != null)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, iskicked);
                                            SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            if (DatabaseCalls.UserIsBannedFromSavedRoom((int)roomid, account_ID) == true)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "You are banned from this server");
                                            SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            RemovePLFromAllRooms(new int[2] { _public_id, account_ID },true);
                            string? seed = DatabaseCalls.SavedRoomSeed((int)roomid);
                            if (seed == null)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Error getting encryption info.");
                                            SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendbackRoomRoot(_public_id, (int)roomid, true, (string)seed, false);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                        else
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This room does not exist.");
                                        SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not join room.");
                SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                Console.WriteLine(ex.Message + ": ClientRequestJoinRoom Error");
            }
        }
        public static void JustCheckPassword(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                string roomname = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    int? roomid = DatabaseCalls.SavedRoomExistsByName(roomname);
                    if (roomid != null) // if room trying to join even exists
                    {
                        string? seed = DatabaseCalls.SavedRoomSeed((int)roomid);
                        if (seed == null)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "Error getting encryption info.");
                                        SendPackets.SendCloseLoading(_public_id, "Attempting to join - iteration 1.", 1);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                                
                            return;
                        }
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendbackRoomRoot(_public_id, (int)roomid, true, (string)seed, true);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                         
                       
                    }
                    else
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "This room does not exist.");
                                    SendPackets.SendCloseLoading(_public_id, "Attempting to join - iteration 1.", 1);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not join room.");
                SendPackets.SendCloseLoading(_public_id, "Attempting to join - iteration 1.", 1);
                Console.WriteLine(ex.Message + ": ClientRequestJoinRoom Error");
            }
        }
        public static void RemoveOrAddPLRole(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int useraccount_ID = packet.GetInt(ref error); if(error == true) return;
                int role = packet.GetInt(ref error); if(error == true) return;
                bool add = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    if (DatabaseCalls.SavedRoomExists(serverid) == false)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendCloseLoading(_public_id, "Changing user's role...", 0);
                                    SendPackets.SendMessage(_public_id, "This room does not exist.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                         
                      
                       
                        return;
                    }
                    if (DatabaseCalls.UserIsInSavedRoom(serverid, useraccount_ID) == false)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendCloseLoading(_public_id, "Changing user's role...", 0);
                                    SendPackets.SendMessage(_public_id, "This user is not in this room.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                         
                       
                        return;
                    }
                    string? newrolereturn = null;
                    if(role != 1)
                    {
                        newrolereturn = add ? DatabaseCalls.AddUserRole(useraccount_ID, serverid, role) : DatabaseCalls.RemoveUserRole(useraccount_ID, serverid, role);
                    }
                    else
                    {
                        newrolereturn = DatabaseCalls.TransferServerOwnership(account_ID, useraccount_ID, serverid);
                    }
                    if (newrolereturn == null)
                    {   
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendCloseLoading(_public_id, "Changing user's role...", 0);
                                    SendPackets.SendMessage(_public_id, "Could not change user roles.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                         
                        return;
                    }
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                SendPackets.SendCloseLoading(_public_id, "Changing user's role...", 5);
                                if(role == 1)
                                {
                                    SendPackets.SendMessage(_public_id, "Ownership has been transfered.");
                                }
                                SendPackets.SendToSavedServerNewUsersWRole(serverid, System.Text.Encoding.UTF8.GetBytes(newrolereturn));
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                     

                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not change user roles.");
                Console.WriteLine(ex.Message + ": RemoveOrAddPLRole Error");
            }
        }
        public static void RemoveOrAddChannelRole(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int account_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int serverid = packet.GetInt(ref error); if(error == true) return;
                int channelid = packet.GetInt(ref error); if(error == true) return;
                int role = packet.GetInt(ref error); if(error == true) return;
                bool add = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    if (DatabaseCalls.SavedRoomExists(serverid) == false)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendCloseLoading(_public_id, "Changing channel access...", 0);
                                    SendPackets.SendMessage(_public_id, "This room does not exist.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                         

                        return;
                    }
                    string? newrolereturn = add ? DatabaseCalls.AddChannelRole(channelid, serverid, role) : DatabaseCalls.RemoveChannelRole(channelid, serverid, role, new int[2] { _public_id,account_ID});
                    if (newrolereturn == null)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                {
                                    SendPackets.SendCloseLoading(_public_id, "Changing channel access...", 0);
                                    SendPackets.SendMessage(_public_id, "Could not change channel roles.");
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                         
                        return;
                    }
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                            {
                                SendPackets.SendCloseLoading(_public_id, "Changing channel access...", 0);
                                SendPackets.SendBackSavedServerChannels(serverid, System.Text.Encoding.UTF8.GetBytes(newrolereturn));
                            }
                        });
                        RunOnPrimaryActionThread(a);
                    }
                     
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not change access.");
                Console.WriteLine(ex.Message + ": RemoveOrAddChannelRole Error");
            }
        }
        public static void pass_data_toserv(User c, int task_id, int val)
        {
            int account_ID = c.accountid;
            OnFlyChannelManager ofcm = task_id == 0 ? NetworkManager.GetUserRoomManager(c) : NetworkManager.GetUserCallManager(c);
            if (ofcm == null)
                return;
            for (int i = 0; i < 10; ++i)
            {
                if (ofcm.passed_data[i].written == false)
                {
                    if (ofcm.passed_data[i].data == null)
                        ofcm.passed_data[i].data = new int[3];
                    ofcm.passed_data[i].data[0] = task_id;
                    ofcm.passed_data[i].data[1] = account_ID;
                    ofcm.passed_data[i].data[2] = val;
                    ofcm.passed_data[i].written = true;
                    break;
                }
            }
        }
        public static void SendNewVoiceChannel(int _public_id, Packet packet, ref bool error)
        {
            User c = NetworkManager.GetClientById(_public_id);
            if (c == null)
            {
                return;
            }
            int account_ID = c.accountid;
            try
            {
                int channel = packet.GetInt(ref error); if(error == true) return;
                int roomid = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientByAccountId(account_ID).CurrentCaller == null)
                {
                    if (roomid == -1)
                    {
                        TemporaryRooms tr = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(account_ID)));
                        if (tr !=null)
                        {
                            pass_data_toserv(c, 0,channel);

                            NetworkManager.GetClientByAccountId(account_ID).CurrentVoiceChannelID = channel;
                            SendPackets.SendBackUserInVoiceChannel(_public_id, tr.id, channel, null, false);
                        }
                    }
                    else //saved
                    {
                       
                        new Thread(delegate ()
                        {
                            if (DatabaseCalls.UserIsInSavedRoom(roomid, account_ID) && DatabaseCalls.ChannelExists(roomid, channel, 1))
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                        {
                                            pass_data_toserv(c, 0, channel);
                                            NetworkManager.GetClientByAccountId(account_ID).CurrentVoiceChannelID = channel;
                                            SendPackets.SendBackUserInVoiceChannel(_public_id, roomid, channel, null, true);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                        }).Start();
                    }
                }
                else
                {
                    SendPackets.SendMessage(_public_id, "Please leave your current call.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendNewVoiceChannel Error");
            }

        }
        public static void TCPReady(int _public_id, Packet packet, ref bool error)
        {
            try
            {
                string name = packet.GetString(ref error); if(error == true) return;
                int account_ID = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (NetworkManager.GetClientById(_public_id) != null)
                {
                    new Thread(delegate ()
                    {
                        string? roomstring = DatabaseCalls.GetUserRooms(account_ID);
                        if (roomstring != null)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(account_ID) != null && NetworkManager.GetClientByAccountId(account_ID).id == _public_id)
                                    {
                                        if (String.IsNullOrWhiteSpace(roomstring) == false)
                                        {
                                            SendPackets.SendToUserTheirRooms(_public_id, System.Text.Encoding.UTF8.GetBytes(roomstring));
                                        }
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                    }).Start();
                    NetworkManager.GetClientById(_public_id).ClientInit(name, account_ID);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": TCPReady Error");
            }
        }
        public static void SendNewUserProfilePicture(int _public_id, Packet packet, ref bool error)
        {
            try
            {
                int account_ID = packet.GetInt(ref error); if(error == true) return;
                string md5 = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                SendPackets.ReceiveNewProfilePicture(account_ID, md5);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendNewUserProfilePicture Error");
            }

        }
        public static void RemovePLFromAllRooms(int[] hostclient, bool inthread)
        {
            Action a = (Action)(() =>
            {
                User theclient = NetworkManager.GetClientByAccountId(hostclient[1]);
                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                {
                    //remove
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(theclient)) == true)
                    {
                        TemporaryRooms room = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(theclient));
                       
                        room.clientsinroom.Remove(theclient);
                        if (room.clientsinroom.Count > 0)
                        {
                            SendPackets.AddOrRemoveUserFromList(2, theclient, " ", room.id, false);
                        }
                        else
                        {
                            RoomWithThreads st = NetworkManager.temproomthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == room.id);
                            if(st!= null)
                            {
                                st.running = false;
                                NetworkManager.temproomthreads.Remove(st);
                            }
                            for (int ii = 0; ii < NetworkManager.ChannelManagers.Count; ++ii)
                            {
                                if (NetworkManager.ChannelManagers[ii].roomtype == 0 && NetworkManager.ChannelManagers[ii].ServerID == room.id)
                                {
                                    NetworkManager.ChannelManagers[ii].Stop();
                                    NetworkManager.ChannelManagers.RemoveAt(ii);
                                    break;
                                }
                            }
                            NetworkManager.temporaryRooms.Remove(room);
                        }
                        pass_data_toserv(theclient, 0, -1);
                        theclient.CurrentVoiceChannelID = -1;
                    }
                    if (theclient.CurrentSavedServerID != null)
                    {
                        SendPackets.AddOrRemoveUserFromList(2, theclient, " ", (int)theclient.CurrentSavedServerID, true);
                        pass_data_toserv(theclient, 0, -1);
                        theclient.CurrentVoiceChannelID = -1;
                        theclient.CurrentSavedServerID = null;
                    }
                    //remove from pm
                    if (theclient.UserTalkingTo != null)
                    {
                        int otheraccount_ID = (int)theclient.UserTalkingTo;
                        SendPackets.AddOrRemoveUserFromList(theclient.id, 2, theclient, "");
                        if (NetworkManager.GetClientByAccountId(otheraccount_ID) != null && NetworkManager.GetClientByAccountId(otheraccount_ID).UserTalkingTo == hostclient[1] && theclient.UserTalkingTo == otheraccount_ID)
                        {
                            SendPackets.AddOrRemoveUserFromList(NetworkManager.GetClientByAccountId(otheraccount_ID).id, 2, theclient, " "); 
                        }
                        theclient.UserTalkingTo = null;
                    }

                    
                }
            });
            if (inthread == false)
            {
                a();
                return;
            }
            RunOnPrimaryActionThread(a);
        }
        public static void RunOnPrimaryActionThread(Action a)// should only be used for local new threads
        {
            NetworkManager.TaskTo_ReceiveThread(a);
            int attempts = 0;
            while (NetworkManager.ActionsOnHandlerThread.Contains(a) == true || NetworkManager.Copied_ActionsOnHandlerThread.Contains(a) == true)
            {
                if (attempts++ == 40) 
                {
                    return;
                }
                Thread.Sleep(15);
            }
        }
        public static void CreateSavedRoom(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                string name = packet.GetString(ref error); if(error == true) return;
                string seed = packet.GetString(ref error); if(error == true) return;
                string md5 = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                RemovePLFromAllRooms(new int[2] { _public_id, theaccount_ID }, false);
                DatabaseCalls.CreateRoom(name, seed, new int[2] { _public_id, theaccount_ID }, md5);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": CreateSavedRoom Error");
            }

        }
        public static void RequestFriends(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int populatewhere = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    if (populatewhere > 0)
                    {
                        string[]? friends = DatabaseCalls.AddOrRemoveFriend(theaccount_ID, false, null);
                        if (friends != null)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendBackFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(friends[0]), populatewhere);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        string? dms = DatabaseCalls.GetUserDMS(new int[2] { _public_id, theaccount_ID });
                        if (dms != null)
                        { 
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendBackFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(dms), 0);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                        else
                        {
                        }
                    }

                }).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": RequestFriends Error");
            }

        }
        public static void RequestPendingFriends(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    string? friends = DatabaseCalls.GetUserPendingFriends(new int[2] { _public_id, theaccount_ID });
                    if (friends != null)
                    { 
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                {
                                    SendPackets.SendBackPendingFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(friends));
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                    else
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Could not load pending friend.");
                                    SendPackets.SendCloseLoading(_public_id, "Loading pending friends...", 0);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Could not load pending friend.");
                SendPackets.SendCloseLoading(_public_id, "Loading pending friends...", 0);
                Console.WriteLine(ex.Message + ": RequestPendingFriends Error");
            }

        }

        public static void AddOrRemoveFriend(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int type = packet.GetInt(ref error); if(error == true) return;
                int otheraccount_ID = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    if (type == 0) //add from pending
                    {
                        if (DatabaseCalls.AddFriendFromPending(new int[2] { _public_id, theaccount_ID }, otheraccount_ID, false) == true)
                        {
                            string? friends = DatabaseCalls.GetUserFriends(theaccount_ID);
                            string? pfriends = DatabaseCalls.GetUserPendingFriends(new int[2] { _public_id, theaccount_ID });
                            if (friends != null)
                            { 
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(friends), 2);
                                            SendPackets.SendBackPendingFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(pfriends));
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            else
                            {

                            }
                        }
                    }
                    if (type == 1) // reject
                    {

                        if (DatabaseCalls.AddFriendFromPending(new int[2] { _public_id, theaccount_ID }, otheraccount_ID, true) == true)
                        {
                            string? pfriends = DatabaseCalls.GetUserPendingFriends(new int[2] { _public_id, theaccount_ID });
                            if (pfriends != null)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackPendingFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(pfriends));
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            else
                            {

                            }
                        }

                    }
                    if (type == 2) //remove
                    {
                        if (DatabaseCalls.RemoveFriend(new int[2] { _public_id, theaccount_ID }, otheraccount_ID) == true)
                        {
                            string? friends = DatabaseCalls.GetUserFriends(theaccount_ID);
                            if (friends != null)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackFriends(_public_id, System.Text.Encoding.UTF8.GetBytes(friends), 2);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            else
                            {

                            }
                        }

                    }
                }).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": RequestFriends Error");
            }

        }
        public static void RequestMessagesAfterPMVerify(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
         
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                packet.CheckFinalizers(ref error); if (error == true) return;
                int? talkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;
                new Thread(delegate ()
                {
                    if (talkingto != null)
                    {
                        string pmtextfile = TextFileName(theaccount_ID, (int)talkingto);
                        if (File.Exists(pmtextfile) == false)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }

                        int ReadAttempts = 0;
                        List<string> messages = new List<string>();
                    RestartRead:;
                        try
                        {
                            messages = File.ReadAllLines(pmtextfile, System.Text.Encoding.UTF8).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList().FindAll(x => (int.Parse(x.Split('❶').Last()) == 1 && int.Parse(x.Split('❶')[2]) == theaccount_ID) || (int.Parse(x.Split('❶').Last()) == 0 && int.Parse(x.Split('❶')[2]) != theaccount_ID)); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Unable to read messages from conversation.");
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }

                        int textchannellins = messages.Count;

                        if (textchannellins == 0)
                        {
                            return;
                        }
                        int initiali = textchannellins > 30 ? textchannellins - 30 : 0;
                        for (int i = initiali; i < textchannellins; ++i)
                        {
                            Thread.Sleep(20);
                            if (messages[i].Contains("❶") == false)
                            {
                                continue;
                            }
                            string[] MessageStuff = messages[i].Split('❶');

                            if (MessageStuff[0] == "0")
                            {
                                MessagesForTempRoom msgobj = new MessagesForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, int.Parse(MessageStuff[6]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, true, 0, null, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "1")
                            {
                                MessagesForTempRoom msgobj = new PictureForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackPictureMessageToAllInRoom(msgobj as PictureForTempRoom, null, _public_id, true, 0, null, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "2")
                            {
                                MessagesForTempRoom msgobj = new VideoForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackVideoMessageToAllInRoom(msgobj as VideoForTempRoom, null, _public_id, true, 0, null, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "3")
                            {
                                MessagesForTempRoom msgobj = new AudioForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], int.Parse(MessageStuff[7]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackAudioMessageToAllInRoom(msgobj as AudioForTempRoom, null, _public_id, true, 0, null, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "4")
                            {
                                MessagesForTempRoom msgobj = new FileForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]), int.Parse(MessageStuff[9]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackFileMessageToAllInRoom(msgobj as FileForTempRoom, null, _public_id, true, 0, null, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                        }
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": RequestMessagesAfterJoin Error");
            }
        }
        public static void RequestMessagesAfterJoin(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int keythatclientreceived = packet.GetInt(ref error); if(error == true) return; //channel
                bool saved = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (saved == false)
                {
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                    {
                        TemporaryRooms? room = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                        if (Methods.foundchannel(room.id, keythatclientreceived) == false)
                        {
                            SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                            return;
                        }
                        ChannelsForTempRoom textchannel = room.channels[keythatclientreceived] as ChannelsForTempRoom;
                        for (int i = textchannel.messages.Count > 30 ? textchannel.messages.Count - 30 : 0; i < textchannel.messages.Count; ++i)
                        {
                            Thread.Sleep(20);
                            if (textchannel.messages[i].GetType() == typeof(PictureForTempRoom))
                            {
                                SendPackets.SendBackPictureMessageToAllInRoom(textchannel.messages[i] as PictureForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                            }
                            if (textchannel.messages[i].GetType() == typeof(VideoForTempRoom))
                            {
                                SendPackets.SendBackVideoMessageToAllInRoom(textchannel.messages[i] as VideoForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                            }
                            if (textchannel.messages[i].GetType() == typeof(AudioForTempRoom))
                            {
                                SendPackets.SendBackAudioMessageToAllInRoom(textchannel.messages[i] as AudioForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                            }
                            if (textchannel.messages[i].GetType() == typeof(MessagesForTempRoom))
                            {
                                SendPackets.SendBackMessageToAllInRoom(textchannel.messages[i], room, _public_id, true, keythatclientreceived, null, false);
                            }
                            if (textchannel.messages[i].GetType() == typeof(FileForTempRoom))
                            {
                                SendPackets.SendBackFileMessageToAllInRoom(textchannel.messages[i] as FileForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                            }
                        }
                    }
                }
                else
                {
                    if (NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID == null)
                    {
                        return;
                    }
                    int curserver = (int)NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID;
                    new Thread(delegate ()
                    {
                        if (DatabaseCalls.UserIsInSavedRoom(curserver, theaccount_ID) == true)
                        {
                            string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), curserver.ToString());
                            string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), curserver.ToString() + "\\" + keythatclientreceived + ".txt");
                            if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }

                            int ReadAttempts = 0;
                            string[] messages = new string[] { };
                        RestartRead:;
                            try
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                {
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }

                            int textchannellins = messages.Length;
                           
                            if (textchannellins == 0)
                            {
                                return;
                            }
                            for (int i = textchannellins > 30 ? textchannellins - 30 : 0; i < textchannellins; ++i)
                            {
                                Thread.Sleep(20);
                                if (messages[i].Contains("❶") == false)
                                {
                                    continue;
                                }
                                string[] MessageStuff = messages[i].Split('❶');
                                if (MessageStuff[0] == "0")
                                {
                                    MessagesForTempRoom msgobj = new MessagesForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, int.Parse(MessageStuff[6]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "1")
                                {
                                    MessagesForTempRoom msgobj = new PictureForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackPictureMessageToAllInRoom(msgobj as PictureForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "2")
                                {
                                    MessagesForTempRoom msgobj = new VideoForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackVideoMessageToAllInRoom(msgobj as VideoForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "3")
                                {
                                    MessagesForTempRoom msgobj = new AudioForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], int.Parse(MessageStuff[7]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackAudioMessageToAllInRoom(msgobj as AudioForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "4")
                                {
                                    MessagesForTempRoom msgobj = new FileForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]), int.Parse(MessageStuff[9]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackFileMessageToAllInRoom(msgobj as FileForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                            }
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": RequestMessagesAfterJoin Error");
            }
        }
        public static void SendSuccessInGettingNewMessageAmount(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int amountcurrently = packet.GetInt(ref error); if(error == true) return;
                int indexcap = packet.GetInt(ref error); if(error == true) return;
                int keythatclientreceived = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                bool plinsavedroom = NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID != null;
                if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                {
                    int account_ID = NetworkManager.GetClientByAccountId(theaccount_ID).accountid;
                    int? talkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;
                    if (talkingto == null)
                    {
                        return;
                    }
                    new Thread(delegate ()
                    {

                        string serverchanneltextfile = TextFileName(theaccount_ID, (int)talkingto);

                        if (File.Exists(serverchanneltextfile) == false)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }

                        int ReadAttempts = 0;
                        List<string> messages = new List<string>();
                    RestartRead:;
                        try
                        {
                            if (talkingto == null)
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 

                            }
                            else
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList().FindAll(x => (int.Parse(x.Split('❶').Last()) == 1 && int.Parse(x.Split('❶')[2]) == theaccount_ID) || (int.Parse(x.Split('❶').Last()) == 0 && int.Parse(x.Split('❶')[2]) != theaccount_ID)); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }

                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }
                        int textchannellins = messages.Count;
                        if (textchannellins == 0)
                        {
                            return;
                        }
                        for (int i = (indexcap - amountcurrently) > 30 ? (indexcap - amountcurrently) - 30 : 0; i < (indexcap - amountcurrently); ++i)
                        {
                            Thread.Sleep(20);
                            if (messages[i].Contains("❶") == false)
                            {
                                continue;
                            }
                            string[] MessageStuff = messages[i].Split('❶');
                            if (MessageStuff[0] == "0")
                            {
                                MessagesForTempRoom msgobj = new MessagesForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, int.Parse(MessageStuff[6]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, true, keythatclientreceived, NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "1")
                            {
                                MessagesForTempRoom msgobj = new PictureForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackPictureMessageToAllInRoom(msgobj as PictureForTempRoom, null, _public_id, true, keythatclientreceived, NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "2")
                            {
                                MessagesForTempRoom msgobj = new VideoForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackVideoMessageToAllInRoom(msgobj as VideoForTempRoom, null, _public_id, true, keythatclientreceived, NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "3")
                            {
                                MessagesForTempRoom msgobj = new AudioForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], int.Parse(MessageStuff[7]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackAudioMessageToAllInRoom(msgobj as AudioForTempRoom, null, _public_id, true, keythatclientreceived, NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                            if (MessageStuff[0] == "4")
                            {
                                MessagesForTempRoom msgobj = new FileForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]), int.Parse(MessageStuff[9]) == 1 ? true : false, MessageStuff[5]);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendBackFileMessageToAllInRoom(msgobj as FileForTempRoom, null, _public_id, true, keythatclientreceived, NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID, false);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                        }
                    }).Start();
                }
                else if (plinsavedroom == false)
                {
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                    {
                        TemporaryRooms? room = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));

                        if (room != null)
                        {
                            if (Methods.foundchannel(room.id, keythatclientreceived) == false)
                            {
                                SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                //Retrieving messages...,
                                return;
                            }
                            ChannelsForTempRoom textchannel = (room.channels[keythatclientreceived] as ChannelsForTempRoom);
                            //send back another 30 or however many left if less than 30

                            for (int i = (indexcap - amountcurrently) > 30 ? (indexcap - amountcurrently) - 30 : 0; i < (indexcap - amountcurrently); ++i)
                            {
                                Thread.Sleep(20);
                                if (textchannel.messages[i].GetType() == typeof(MessagesForTempRoom))
                                {
                                    SendPackets.SendBackMessageToAllInRoom(textchannel.messages[i], room, _public_id, true, keythatclientreceived, null, false);
                                }
                                if (textchannel.messages[i].GetType() == typeof(PictureForTempRoom))
                                {
                                    SendPackets.SendBackPictureMessageToAllInRoom(textchannel.messages[i] as PictureForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                                }
                                if (textchannel.messages[i].GetType() == typeof(VideoForTempRoom))
                                {
                                    SendPackets.SendBackVideoMessageToAllInRoom(textchannel.messages[i] as VideoForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                                }
                                if (textchannel.messages[i].GetType() == typeof(AudioForTempRoom))
                                {
                                    SendPackets.SendBackAudioMessageToAllInRoom(textchannel.messages[i] as AudioForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                                }
                                if (textchannel.messages[i].GetType() == typeof(FileForTempRoom))
                                {
                                    SendPackets.SendBackFileMessageToAllInRoom(textchannel.messages[i] as FileForTempRoom, room, _public_id, true, keythatclientreceived, null, false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    int account_ID = NetworkManager.GetClientByAccountId(theaccount_ID).accountid;
                    int? curserver = NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID;
                    if (curserver == null)
                    {
                        return;
                    }
                    new Thread(delegate ()
                    {
                        if (DatabaseCalls.UserIsInSavedRoom((int)curserver, NetworkManager.GetClientByAccountId(theaccount_ID).accountid) == true)
                        {
                            string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), curserver.ToString());
                            string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), curserver.ToString() + "\\" + keythatclientreceived + ".txt");

                            if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }

                            int ReadAttempts = 0;
                            List<string> messages = new List<string>();
                        RestartRead:;
                            try
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                {
                                    messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 

                                }
                                else
                                {
                                    messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList().FindAll(x => (int.Parse(x.Split('❶').Last()) == 1 && int.Parse(x.Split('❶')[2]) == theaccount_ID) || (int.Parse(x.Split('❶').Last()) == 0 && int.Parse(x.Split('❶')[2]) != theaccount_ID)); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                                }

                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                {
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }
                            int textchannellins = messages.Count;
                            if (textchannellins == 0)
                            {
                                return;
                            }
                            for (int i = (indexcap - amountcurrently) > 30 ? (indexcap - amountcurrently) - 30 : 0; i < (indexcap - amountcurrently); ++i)
                            {
                                Thread.Sleep(20);
                                if (messages[i].Contains("❶") == false)
                                {
                                    continue;
                                }
                                string[] MessageStuff = messages[i].Split('❶');
                                if (MessageStuff[0] == "0")
                                {
                                    MessagesForTempRoom msgobj = new MessagesForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, int.Parse(MessageStuff[6]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "1")
                                {
                                    MessagesForTempRoom msgobj = new PictureForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackPictureMessageToAllInRoom(msgobj as PictureForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "2")
                                {
                                    MessagesForTempRoom msgobj = new VideoForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackVideoMessageToAllInRoom(msgobj as VideoForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "3")
                                {
                                    MessagesForTempRoom msgobj = new AudioForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], int.Parse(MessageStuff[7]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackAudioMessageToAllInRoom(msgobj as AudioForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                                if (MessageStuff[0] == "4")
                                {
                                    MessagesForTempRoom msgobj = new FileForTempRoom(MessageStuff[1], int.Parse(MessageStuff[2]), MessageStuff[3], int.Parse(MessageStuff[4]), _public_id, MessageStuff[6], MessageStuff[7], int.Parse(MessageStuff[8]), int.Parse(MessageStuff[9]) == 1 ? true : false, MessageStuff[5]);
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackFileMessageToAllInRoom(msgobj as FileForTempRoom, null, _public_id, true, keythatclientreceived, curserver, false);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                            }
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendSuccessInGettingNewMessageAmount Error");
            }

        }
        public static void RequestMoreMessages(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int amountcurrently = packet.GetInt(ref error); if(error == true) return;
                int indexcap = packet.GetInt(ref error); if(error == true) return;
                int channelkey = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                bool plinsavedroom = NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID != null;
                int? talkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;
                if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                {
                    new Thread(delegate ()
                    {
                        string builtstring = " ";
                        
                        string serverchanneltextfile = TextFileName(theaccount_ID, (int)talkingto);
                        if (File.Exists(serverchanneltextfile) == false)
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                            return;
                        }
                        int ReadAttempts = 0;
                        List<string> messages = new List<string>();
                    RestartRead:;
                        try
                        {
                            if (talkingto == null)
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }
                            else
                            {
                                messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList().FindAll(x => (int.Parse(x.Split('❶').Last()) == 1 && int.Parse(x.Split('❶')[2]) == theaccount_ID) || (int.Parse(x.Split('❶').Last()) == 0 && int.Parse(x.Split('❶')[2]) != theaccount_ID)); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                            }
                        }
                        catch // This will most likely occur if another thread is reading/writing from/to this file
                        {
                            Thread.Sleep(200);
                            if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            ReadAttempts++;
                            goto RestartRead; // Attempt to read again.
                        }
                        int textchannellins = messages.Count;
                        if (textchannellins == 0)
                        {
                            return;
                        }
                        for (int i = (indexcap - amountcurrently) > 30 ? (indexcap - amountcurrently) - 30 : 0; i < (indexcap - amountcurrently); ++i)
                        {
                            builtstring += messages[i].Split('❶')[4] + "|";
                        }
                       
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                {
                                    SendPackets.SendBackMoreMessagesToClient(_public_id, (indexcap - amountcurrently) > 30 ? amountcurrently + 30 : indexcap, null, amountcurrently, indexcap, channelkey, builtstring);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                    }).Start();
                }
                else if (plinsavedroom == false)
                {
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                    {
                        TemporaryRooms? room = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                        if (room != null)
                        {
                            SendPackets.SendBackMoreMessagesToClient(_public_id, (indexcap - amountcurrently) > 30 ? amountcurrently + 30 : indexcap, room, amountcurrently, indexcap, channelkey, " ");
                        }
                    }
                }
                else
                {
                    int? curserver = NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID;
                    if (curserver == null)
                    {
                        return;
                    }
                    new Thread(delegate ()
                    {
                        if (DatabaseCalls.UserIsInSavedRoom((int)curserver, NetworkManager.GetClientByAccountId(theaccount_ID).accountid) == true)
                        {
                            string builtstring = " ";
                         
                            string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), curserver.ToString());
                            string serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), curserver.ToString() + "\\" + channelkey + ".txt");
                            if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "This channel no longer exists.");
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                                return;
                            }
                            int ReadAttempts = 0;
                            List<string> messages = new List<string>();
                        RestartRead:;
                            try
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                {
                                    messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 

                                }
                                else
                                {
                                    messages = File.ReadAllLines(serverchanneltextfile, System.Text.Encoding.UTF8).Where(x=>String.IsNullOrWhiteSpace(x) == false && x != "\n").ToList().FindAll(x => (int.Parse(x.Split('❶').Last()) == 1 && int.Parse(x.Split('❶')[2]) == theaccount_ID) || (int.Parse(x.Split('❶').Last()) == 0 && int.Parse(x.Split('❶')[2]) != theaccount_ID)); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                                }

                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                {
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendMessage(_public_id, "Unable to read messages from channel.");
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }
                            int textchannellins = messages.Count;
                            if (textchannellins == 0)
                            {
                                return;
                            }
                            for (int i = (indexcap - amountcurrently) > 30 ? (indexcap - amountcurrently) - 30 : 0; i < (indexcap - amountcurrently); ++i)
                            {
                                builtstring += messages[i].Split('❶')[4] + "|";
                            }
                           
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendBackMoreMessagesToClient(_public_id, (indexcap - amountcurrently) > 30 ? amountcurrently + 30 : indexcap, null, amountcurrently, indexcap, channelkey, builtstring);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                           
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": RequestMoreMessages Error");
            }
        }

        public static void JoinRoomKeySuccess(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int roomid = packet.GetInt(ref error); if(error == true) return;
                bool issaved = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (issaved == false)
                {
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == false)
                    {
                        if (NetworkManager.GetRoomById(roomid) != null)
                        {
                            SendPackets.AddOrRemoveUserFromList(1, NetworkManager.GetClientByAccountId(theaccount_ID), " ", roomid, issaved); // send to all in room this user joining
                            NetworkManager.GetRoomById(roomid).clientsinroom.Add(NetworkManager.GetClientByAccountId(theaccount_ID));
                            for (int i = 0; i < NetworkManager.GetRoomById(roomid).clientsinroom.Count; ++i)
                            {
                                SendPackets.AddOrRemoveUserFromList(_public_id, 1, NetworkManager.GetRoomById(roomid).clientsinroom[i], " ");
                            }
                            SendPackets.SendbackRoomJoinSuccess(_public_id, roomid, false);
                        }
                        else
                        {
                            SendPackets.SendMessage(_public_id, "Room does not exist.");
                            SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                        }
                    }
                    else
                    {
                        SendPackets.SendMessage(_public_id, "You are already in a room. Please manually leave current room.");
                        SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                    }
                }
                else
                {
                    new Thread(delegate ()
                    {
                        if (DatabaseCalls.SavedRoomExists(roomid) == true)
                        {
                            bool[] w = DatabaseCalls.AddAUserToSavedRoom(roomid, theaccount_ID);
                            if (w[0] == true)
                            {
                                {
                                    int[]? chan = DatabaseCalls.LogChannels(roomid);
                                    if (chan == null)
                                    {
                                        return;
                                    }
                                    Action a = (Action)(() => //run on one of the primary threads. Wait until this has executed
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            string theroles = DatabaseCalls.GetUserServerRoles(theaccount_ID, (int)roomid) ?? " ";
                                            SendPackets.AddOrRemoveUserFromList(1, NetworkManager.GetClientByAccountId(theaccount_ID), theroles, (int)roomid, true);
                                            NetworkManager.GetClientByAccountId(theaccount_ID).CurrentSavedServerID = roomid;

                                            if (NetworkManager.serverthreads.Any(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == roomid) == false)
                                            {
                                                NetworkManager.serverthreads.Add(new ServerWithThreads(roomid, true));
                                            }
                                            if (NetworkManager.ChannelManagers.Any(x => x.GetType() == typeof(OnFlyChannelManager) && (x as OnFlyChannelManager).ServerID == roomid) == false)
                                            {
                                                NetworkManager.ChannelManagers.Add(new OnFlyChannelManager(1, (int)roomid));
                                            }
                                            if (w[1] == true)
                                            {
                                                LogBotMessage(roomid, chan, NetworkManager.GetClientByAccountId(theaccount_ID).username + " has joined the server.");
                                            }
                                            for (int i = 0; i < NetworkManager.clients.Count; ++i)
                                            {
                                                if (NetworkManager.clients[i].CurrentSavedServerID == roomid)
                                                {
                                                    string therolesb = DatabaseCalls.GetUserServerRoles(NetworkManager.clients[i].accountid, (int)roomid) ?? " ";
                                                    SendPackets.AddOrRemoveUserFromList(_public_id, 1, NetworkManager.clients[i], therolesb);
                                                }
                                            }
                                        }
                                        SendPackets.SendbackRoomJoinSuccess(_public_id, roomid, true);
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                              
                            }
                            else
                            {
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                        {
                                            SendPackets.SendMessage(_public_id, "Could not add you to this room.");
                                            SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                        }
                                    });
                                    RunOnPrimaryActionThread(a);
                                }
                               
                            }
                        }
                        else
                        {
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {
                                        SendPackets.SendMessage(_public_id, "This room no longer exists.");
                                        SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                    }
                                });
                                RunOnPrimaryActionThread(a);
                            }
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": JoinRoomKeySuccess Error");
            }
        }
        public static void SeeIfRoomExistsBeforeJoin(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                string roomname = packet.GetString(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                new Thread(delegate ()
                {
                    int? roomid = DatabaseCalls.SavedRoomExistsByName(roomname);
                    if (roomid != null)
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                {
                                    SendPackets.RoomExists(_public_id, (int)roomid);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       

                    }
                    else
                    {  
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                {
                                    SendPackets.SendMessage(_public_id, "Room does not exist.");
                                    SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                                }
                            });
                            RunOnPrimaryActionThread(a);
                        }
                       
                      
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                SendPackets.SendMessage(_public_id, "Error checking if room exists.");
                SendPackets.SendCloseLoading(_public_id, "Joining room...", 1);
                Console.WriteLine(ex.Message + ": SeeIfRoomExistsBeforeJoin Error");
            }
        }
        public static void RequestUserVoiceChannels(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int savedserver = packet.GetInt(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (savedserver == -1) //if == -1 then its a temp room
                {
                    if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                    {
                        TemporaryRooms room = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                        for (int i = 0; i < room.clientsinroom.Count; ++i)
                        {
                            SendPackets.SendBackUserInVoiceChannel(room.clientsinroom[i].id, room.id, room.clientsinroom[i].CurrentVoiceChannelID, _public_id, false);
                        }
                    }
                }
                else
                {
                    new Thread(delegate ()
                    {
                        if (DatabaseCalls.UserIsInSavedRoom(savedserver, theaccount_ID))
                        {
                            for (int i = 0; i < NetworkManager.clients.Count; ++i)
                            {
                                if (NetworkManager.clients[i] != null && NetworkManager.clients[i].CurrentSavedServerID == savedserver)
                                {
                                    
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                SendPackets.SendBackUserInVoiceChannel(NetworkManager.clients[i].id, savedserver, NetworkManager.clients[i].CurrentVoiceChannelID, _public_id, true);
                                            }
                                        });
                                        RunOnPrimaryActionThread(a);
                                    }
                                   
                                }
                            }
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": RequestPLVoices Error");
            }
        }
        public static void SendMessage(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int savedserver = packet.GetInt(ref error); if(error == true) return;
                string message = packet.GetString(ref error); if(error == true) return;
                int channelkey = packet.GetInt(ref error); if(error == true) return;
                bool mine = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (savedserver == -1 && NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                {
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                        {
                            TemporaryRooms _roomobj = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                            if (Methods.foundchannel(_roomobj.id, channelkey) == false)
                            {
                                return;
                            }
                            ChannelsForTempRoom textchannel = _roomobj.channels[channelkey] as ChannelsForTempRoom;
                            DateTime date = DateTime.Now;
                            MessagesForTempRoom msgobj = new MessagesForTempRoom(NetworkManager.GetClientByAccountId(theaccount_ID).username, NetworkManager.GetClientByAccountId(theaccount_ID).accountid, message, textchannel.messages.Count > 0 ? textchannel.messages.Last().idofmessage + 1 : 1, _public_id, textchannel.messages.Count > 0 ? textchannel.messages.Last().ownedbysender : false, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                            textchannel.messages.Add(msgobj);
                            SendPackets.SendBackMessageToAllInRoom(msgobj, _roomobj, null, false, channelkey, null, false);
                        }
                    }
                }
                else
                {
                    bool pming = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null;
                    RoomWithThreads st = NetworkManager.serverthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == savedserver);
                    RoomWithThreads pmt = NetworkManager.pmthreads.Find(x => x.GetType() == typeof(PMWithThreads) &&
                    (x as PMWithThreads).usera == theaccount_ID && (x as PMWithThreads).userb == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1) ||
                    x.GetType() == typeof(PMWithThreads) &&
                    (x as PMWithThreads).userb == theaccount_ID && (x as PMWithThreads).usera == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1));
                    if (pming == false)
                    {
                        if ((pming ? pmt : st) == null)
                        {
                            return;
                        }
                    }

                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        string username = NetworkManager.GetClientByAccountId(theaccount_ID).username;
                        int? usertalkingto = null;
                        string? muted = NetworkManager.CheckMute(theaccount_ID, savedserver);
                        if (muted != null)
                        {
                            SendPackets.SendMessage(NetworkManager.GetClientByAccountId(theaccount_ID).id, muted);
                            return;
                        }
                        usertalkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;

                        (pming ? pmt : st).TaskTo_ActionThread(() =>
                        {
                            if (DatabaseCalls.UserIsInSavedRoom(savedserver, theaccount_ID) == true || usertalkingto != null)
                            {
                                int? msgid = null;
                                string serverfolder = "";
                                string serverchanneltextfile = "";
                                bool dmbothside = false;
                                bool stop = false;
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {

                                        if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                        {
                                            serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString());
                                            serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString() + "\\" + channelkey + ".txt");

                                            if (NetworkManager.AddServerMessages(savedserver, channelkey, serverfolder, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            serverchanneltextfile = TextFileName(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo);
                                            if (NetworkManager.AddPMMessages(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                if (NetworkManager.dms.Any(x => x[0] == (int)usertalkingto && x[1] == theaccount_ID) == false)
                                                {
                                                    NetworkManager.dms.Add(new int[2] { (int)usertalkingto, theaccount_ID });
                                                    dmbothside = true;
                                                }
                                            }
                                        }
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                                if (stop == true)
                                    return;
                                if (dmbothside == true && usertalkingto != null)
                                    DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, theaccount_ID }, (int)usertalkingto, false);
                                if (Directory.Exists(serverfolder) == false && usertalkingto == null || File.Exists(serverchanneltextfile) == false)
                                {
                                    return;
                                }

                                if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                {
                                    return;
                                }
                                DateTime date = DateTime.Now;
                                MessagesForTempRoom msgobj = new MessagesForTempRoom(username, theaccount_ID, message, (int)msgid, _public_id, mine, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                                string rawmsgobj = "0❶" + username + "❶" + theaccount_ID + "❶" + message + "❶" + msgid + "❶" + msgobj.dateposted + "❶" + (mine == true ? 1 : 0);

                                //write to file
                                {
                                    int ReadAttempts = 0;
                                RestartRead:;
                                    try
                                    {
                                        using (StreamWriter file = new StreamWriter(serverchanneltextfile, true))
                                        {
                                            file.WriteLine(rawmsgobj);
                                        }
                                    }
                                    catch // This will most likely occur if another thread is reading/writing from/to this file
                                    {
                                        Thread.Sleep(200);
                                        if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                        {
                                            return;
                                        }
                                        ReadAttempts++;
                                        goto RestartRead; // Attempt to read again.
                                    }
                                }
                                
                                Action aaa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                    {
                                        return;
                                    }
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                                    {
                                        if (mine == true)
                                        {
                                            SendPackets.SendBackMessageToAllInRoom(msgobj, null, _public_id, false, 0, null, false); // send back to sender
                                        }
                                        else
                                        {
                                            if (NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo) != null && NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).UserTalkingTo == theaccount_ID)
                                            {
                                                SendPackets.SendBackMessageToAllInRoom(msgobj, null, NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).id, false, 0, null, false);
                                            }
                                        }

                                    }
                                    else
                                    {
                                        SendPackets.SendBackMessageToAllInRoom(msgobj, null, null, false, channelkey, savedserver, false);
                                    }
                                });
                                RunOnPrimaryActionThread(aaa);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendMessage Error");
            }
        }
        public static void LogBotMessage(int savedserver, int[] channelkey, string message)
        {
            try
            {

                RoomWithThreads st = NetworkManager.serverthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == savedserver);
                st.TaskTo_ActionThread(() =>
                {
                    for (int i = 0; i < channelkey.Length; ++i)
                    {
                        int? msgid = null;
                        string serverfolder = "";
                        string serverchanneltextfile = "";
                        serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString());
                        serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString() + "\\" + channelkey[i] + ".txt");

                        if (NetworkManager.AddServerMessages(savedserver, channelkey[i], serverfolder, ref msgid) == false)
                        {
                            return;
                        }
                        if (Directory.Exists(serverfolder) == false || File.Exists(serverchanneltextfile) == false)
                        {
                            return;
                        }

                        DateTime date = DateTime.Now;
                        MessagesForTempRoom msgobj = new MessagesForTempRoom("Bot", -2, message, (int)msgid, -2, false, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                        string rawmsgobj = "0❶" + "Bot" + "❶" + -2 + "❶" + message + "❶" + msgid + "❶" + msgobj.dateposted + "❶" + 0;

                        //write to file
                        {
                            int ReadAttempts = 0;
                        RestartRead:;
                            try
                            {
                                using (StreamWriter file = new StreamWriter(serverchanneltextfile, true))
                                {
                                    file.WriteLine(rawmsgobj);
                                }
                            }
                            catch // This will most likely occur if another thread is reading/writing from/to this file
                            {
                                Thread.Sleep(200);
                                if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                {
                                    return;
                                }
                                ReadAttempts++;
                                goto RestartRead; // Attempt to read again.
                            }
                        }
                        
                        SendPackets.SendBackMessageToAllInRoom(msgobj, null, null, false, channelkey[i], savedserver, false);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": Botmessage Error");
            }
        }
        public static void SendPictureMessage(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int savedserver = packet.GetInt(ref error); if(error == true) return;
                string message = packet.GetString(ref error); if(error == true) return;
                string md5hash = packet.GetString(ref error); if(error == true) return;
                string T_md5hash = packet.GetString(ref error); if(error == true) return;
                int channelkey = packet.GetInt(ref error); if(error == true) return;
                bool mine = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (savedserver == -1 && NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                {
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                        {
                            TemporaryRooms _roomobj = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                            if (Methods.foundchannel(_roomobj.id, channelkey) == false)
                            {
                                return;
                            }
                            DateTime date = DateTime.Now;
                            ChannelsForTempRoom textchannel = _roomobj.channels[channelkey] as ChannelsForTempRoom;
                            MessagesForTempRoom msgobj = new PictureForTempRoom(NetworkManager.GetClientByAccountId(theaccount_ID).username, NetworkManager.GetClientByAccountId(theaccount_ID).accountid, message, textchannel.messages.Count > 0 ? textchannel.messages.Last().idofmessage + 1 : 1, _public_id, md5hash, T_md5hash, textchannel.messages.Count > 0 ? textchannel.messages.Last().ownedbysender : false, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                            textchannel.messages.Add(msgobj);
                            SendPackets.SendBackPictureMessageToAllInRoom((PictureForTempRoom)msgobj, _roomobj, null, false, channelkey, null, false);
                        }
                    }
                }
                else
                {
                    bool pming = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null;
                    RoomWithThreads st = NetworkManager.serverthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == savedserver);
                    RoomWithThreads pmt = NetworkManager.pmthreads.Find(x => x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).usera == theaccount_ID && (x as PMWithThreads).userb == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1) ||
                                      x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).userb == theaccount_ID && (x as PMWithThreads).usera == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1)); if (pming == false)
                    {
                        if ((pming ? pmt : st) == null)
                        {
                            return;
                        }
                    }
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        string username = NetworkManager.GetClientByAccountId(theaccount_ID).username;
                        int? usertalkingto = null;
                        string? muted = NetworkManager.CheckMute(theaccount_ID, savedserver);
                        if (muted != null)
                        {
                            SendPackets.SendMessage(NetworkManager.GetClientByAccountId(theaccount_ID).id, muted);
                            return;
                        }
                        usertalkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;

                        (pming ? pmt : st).TaskTo_ActionThread(() =>
                        {
                            if (DatabaseCalls.UserIsInSavedRoom(savedserver, theaccount_ID) == true || usertalkingto != null)
                            {
                                int? msgid = null;
                                string serverfolder = "";
                                string serverchanneltextfile = "";
                                bool dmbothside = false;
                                bool stop = false;
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {

                                        if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                        {
                                            serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString());
                                            serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString() + "\\" + channelkey + ".txt");

                                            if (NetworkManager.AddServerMessages(savedserver, channelkey, serverfolder, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            serverchanneltextfile = TextFileName(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo);
                                            if (NetworkManager.AddPMMessages(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                if (NetworkManager.dms.Any(x => x[0] == (int)usertalkingto && x[1] == theaccount_ID) == false)
                                                {
                                                    NetworkManager.dms.Add(new int[2] { (int)usertalkingto, theaccount_ID });
                                                    dmbothside = true;
                                                }
                                            }
                                        }
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                                if (stop == true)
                                    return;
                                if (dmbothside == true && usertalkingto != null)
                                    DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, theaccount_ID }, (int)usertalkingto, false);
                                if (Directory.Exists(serverfolder) == false && usertalkingto == null || File.Exists(serverchanneltextfile) == false)
                                {
                                    return;
                                }

                                if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                {
                                    return;
                                }
                                DateTime date = DateTime.Now;
                                MessagesForTempRoom msgobj = new PictureForTempRoom(username, theaccount_ID, message, (int)msgid, _public_id, md5hash, T_md5hash, mine, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                                string rawmsgobj = "1❶" + username + "❶" + theaccount_ID + "❶" + message + "❶" + msgid + "❶" + msgobj.dateposted + "❶" + md5hash + "❶" + T_md5hash + "❶" + (mine == true ? 1 : 0);
                                //write to file
                                {
                                    int ReadAttempts = 0;
                                RestartRead:;
                                    try
                                    {
                                        using (StreamWriter file = new StreamWriter(serverchanneltextfile, true))
                                        {
                                            file.WriteLine(rawmsgobj);
                                        }
                                    }
                                    catch // This will most likely occur if another thread is reading/writing from/to this file
                                    {
                                        Thread.Sleep(200);
                                        if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                        {
                                            return;
                                        }
                                        ReadAttempts++;
                                        goto RestartRead; // Attempt to read again.
                                    }
                                }
                                
                                Action aaa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                    {
                                        return;
                                    }
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                                    {
                                        if (mine == true)
                                        {
                                            SendPackets.SendBackPictureMessageToAllInRoom((PictureForTempRoom)msgobj, null, _public_id, false, 0, null, false);
                                        }
                                        else
                                        {
                                            if (NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo) != null && NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).UserTalkingTo == theaccount_ID)
                                            {
                                                SendPackets.SendBackPictureMessageToAllInRoom((PictureForTempRoom)msgobj, null, NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).id, false, 0, null, false);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        SendPackets.SendBackPictureMessageToAllInRoom((PictureForTempRoom)msgobj, null, null, false, channelkey, savedserver, false);
                                    }
                                });
                                RunOnPrimaryActionThread(aaa);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendPictureMessage Error");
            }
        }
        public static void SendVideoMessage(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int savedserver = packet.GetInt(ref error); if(error == true) return;
                string message = packet.GetString(ref error); if(error == true) return;
                string md5hash = packet.GetString(ref error); if(error == true) return;
                string T_md5hash = packet.GetString(ref error); if(error == true) return;
                int channelkey = packet.GetInt(ref error); if(error == true) return;
                bool mine = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (savedserver == -1 && NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                {
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                        {
                            TemporaryRooms _roomobj = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                            if (Methods.foundchannel(_roomobj.id, channelkey) == false)
                            {
                                return;
                            }
                            DateTime date = DateTime.Now;
                            ChannelsForTempRoom textchannel = _roomobj.channels[channelkey] as ChannelsForTempRoom;
                            MessagesForTempRoom msgobj = new VideoForTempRoom(NetworkManager.GetClientByAccountId(theaccount_ID).username, NetworkManager.GetClientByAccountId(theaccount_ID).accountid, message, textchannel.messages.Count > 0 ? textchannel.messages.Last().idofmessage + 1 : 1, _public_id, md5hash, T_md5hash, textchannel.messages.Count > 0 ? textchannel.messages.Last().ownedbysender : false, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                            textchannel.messages.Add(msgobj);
                            SendPackets.SendBackVideoMessageToAllInRoom((VideoForTempRoom)msgobj, _roomobj, null, false, channelkey, null, false);
                        }
                    }
                }
                else
                {
                    bool pming = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null;
                    RoomWithThreads st = NetworkManager.serverthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == savedserver);
                    RoomWithThreads pmt = NetworkManager.pmthreads.Find(x => x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).usera == theaccount_ID && (x as PMWithThreads).userb == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1) ||
                                      x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).userb == theaccount_ID && (x as PMWithThreads).usera == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1)); if (pming == false)
                    {
                        if ((pming ? pmt : st) == null)
                        {
                            return;
                        }
                    }
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        string username = NetworkManager.GetClientByAccountId(theaccount_ID).username;
                        int? usertalkingto = null;
                        string? muted = NetworkManager.CheckMute(theaccount_ID, savedserver);
                        if (muted != null)
                        {
                            SendPackets.SendMessage(NetworkManager.GetClientByAccountId(theaccount_ID).id, muted);
                            return;
                        }
                        usertalkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;

                        (pming ? pmt : st).TaskTo_ActionThread(() =>
                        {
                            if (DatabaseCalls.UserIsInSavedRoom(savedserver, theaccount_ID) == true || usertalkingto != null)
                            {
                                int? msgid = null;
                                string serverfolder = "";
                                string serverchanneltextfile = "";
                                bool dmbothside = false;
                                bool stop = false;
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {

                                        if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                        {
                                            serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString());
                                            serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString() + "\\" + channelkey + ".txt");

                                            if (NetworkManager.AddServerMessages(savedserver, channelkey, serverfolder, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            serverchanneltextfile = TextFileName(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo);
                                            if (NetworkManager.AddPMMessages(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                if (NetworkManager.dms.Any(x => x[0] == (int)usertalkingto && x[1] == theaccount_ID) == false)
                                                {
                                                    NetworkManager.dms.Add(new int[2] { (int)usertalkingto, theaccount_ID });
                                                    dmbothside = true;
                                                }
                                            }
                                        }
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                                if (stop == true)
                                    return;
                                if (dmbothside == true && usertalkingto != null)
                                    DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, theaccount_ID }, (int)usertalkingto, false);
                                if (Directory.Exists(serverfolder) == false && usertalkingto == null || File.Exists(serverchanneltextfile) == false)
                                {
                                    return;
                                }

                                if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                {
                                    return;
                                }
                                DateTime date = DateTime.Now;
                                MessagesForTempRoom msgobj = new VideoForTempRoom(username, theaccount_ID, message, (int)msgid, _public_id, md5hash, T_md5hash, mine, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                                string rawmsgobj = "2❶" + username + "❶" + theaccount_ID + "❶" + message + "❶" + msgid + "❶" + msgobj.dateposted + "❶" + md5hash + "❶" + T_md5hash + "❶" + (mine == true ? 1 : 0);
                                //write to file
                                {
                                    int ReadAttempts = 0;
                                RestartRead:;
                                    try
                                    {
                                        using (StreamWriter file = new StreamWriter(serverchanneltextfile, true))
                                        {
                                            file.WriteLine(rawmsgobj);
                                        }
                                    }
                                    catch // This will most likely occur if another thread is reading/writing from/to this file
                                    {
                                        Thread.Sleep(200);
                                        if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                        {
                                            return;
                                        }
                                        ReadAttempts++;
                                        goto RestartRead; // Attempt to read again.
                                    }
                                }
                                Action aaa = (Action)(() =>
                                {
                                    
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                    {
                                        return;
                                    }
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                                    {
                                        SendPackets.SendBackVideoMessageToAllInRoom((VideoForTempRoom)msgobj, null, _public_id, false, 0, null, false);
                                        if (NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo) != null && NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).UserTalkingTo == theaccount_ID)
                                        {
                                            SendPackets.SendBackVideoMessageToAllInRoom((VideoForTempRoom)msgobj, null, NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).id, false, 0, null, false);
                                        }
                                    }
                                    else
                                    {
                                        SendPackets.SendBackVideoMessageToAllInRoom((VideoForTempRoom)msgobj, null, null, false, channelkey, savedserver, false);
                                    }
                                });
                                RunOnPrimaryActionThread(aaa);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendVideoMessage Error");
            }
        }
        public static void SendAudioMessage(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int savedserver = packet.GetInt(ref error); if(error == true) return;
                string message = packet.GetString(ref error); if(error == true) return;
                string md5hash = packet.GetString(ref error); if(error == true) return;
                int channelkey = packet.GetInt(ref error); if(error == true) return;
                bool mine = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (savedserver == -1 && NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                {
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                        {
                            TemporaryRooms _roomobj = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                            if (Methods.foundchannel(_roomobj.id, channelkey) == false)
                            {
                                return;
                            }
                            DateTime date = DateTime.Now;
                            ChannelsForTempRoom textchannel = _roomobj.channels[channelkey] as ChannelsForTempRoom;
                            MessagesForTempRoom msgobj = new AudioForTempRoom(NetworkManager.GetClientByAccountId(theaccount_ID).username, NetworkManager.GetClientByAccountId(theaccount_ID).accountid, message, textchannel.messages.Count > 0 ? textchannel.messages.Last().idofmessage + 1 : 1, _public_id, md5hash, textchannel.messages.Count > 0 ? textchannel.messages.Last().ownedbysender : false, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                            textchannel.messages.Add(msgobj);
                            SendPackets.SendBackAudioMessageToAllInRoom((AudioForTempRoom)msgobj, _roomobj, null, false, channelkey, null, false);
                        }
                    }
                }
                else
                {
                    bool pming = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null;
                    RoomWithThreads st = NetworkManager.serverthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == savedserver);
                    RoomWithThreads pmt = NetworkManager.pmthreads.Find(x => x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).usera == theaccount_ID && (x as PMWithThreads).userb == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1) ||
                                      x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).userb == theaccount_ID && (x as PMWithThreads).usera == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1));
                    if (pming == false)
                    {
                        if ((pming ? pmt : st) == null)
                        {
                            return;
                        }
                    }
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        string username = NetworkManager.GetClientByAccountId(theaccount_ID).username;
                        int? usertalkingto = null;
                        string? muted = NetworkManager.CheckMute(theaccount_ID, savedserver);
                        if (muted != null)
                        {
                            SendPackets.SendMessage(NetworkManager.GetClientByAccountId(theaccount_ID).id, muted);
                            return;
                        }
                        usertalkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;

                        (pming ? pmt : st).TaskTo_ActionThread(() =>
                        {
                            if (DatabaseCalls.UserIsInSavedRoom(savedserver, theaccount_ID) == true || usertalkingto != null)
                            {
                                int? msgid = null;
                                string serverfolder = "";
                                string serverchanneltextfile = "";
                                bool dmbothside = false;
                                bool stop = false;
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {

                                        if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                        {
                                            serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString());
                                            serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString() + "\\" + channelkey + ".txt");

                                            if (NetworkManager.AddServerMessages(savedserver, channelkey, serverfolder, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            serverchanneltextfile = TextFileName(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo);
                                            if (NetworkManager.AddPMMessages(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                if (NetworkManager.dms.Any(x => x[0] == (int)usertalkingto && x[1] == theaccount_ID) == false)
                                                {
                                                    NetworkManager.dms.Add(new int[2] { (int)usertalkingto, theaccount_ID });
                                                    dmbothside = true;
                                                }
                                            }
                                        }
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                                if (stop == true)
                                    return;
                                if (dmbothside == true && usertalkingto != null)
                                    DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, theaccount_ID }, (int)usertalkingto, false);
                                if (Directory.Exists(serverfolder) == false && usertalkingto == null || File.Exists(serverchanneltextfile) == false)
                                {
                                    return;
                                }

                                if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                {
                                    return;
                                }
                                DateTime date = DateTime.Now;
                                MessagesForTempRoom msgobj = new AudioForTempRoom(username, theaccount_ID, message, (int)msgid, _public_id, md5hash, mine, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                                string rawmsgobj = "3❶" + username + "❶" + theaccount_ID + "❶" + message + "❶" + msgid + "❶" + msgobj.dateposted + "❶" + md5hash + "❶" + (mine == true ? 1 : 0);
                                //write to file
                                {
                                    int ReadAttempts = 0;
                                RestartRead:;
                                    try
                                    {
                                        using (StreamWriter file = new StreamWriter(serverchanneltextfile, true))
                                        {
                                            file.WriteLine(rawmsgobj);
                                        }
                                    }
                                    catch // This will most likely occur if another thread is reading/writing from/to this file
                                    {
                                        Thread.Sleep(200);
                                        if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                        {
                                            return;
                                        }
                                        ReadAttempts++;
                                        goto RestartRead; // Attempt to read again.
                                    }
                                }
                                Action aaa = (Action)(() =>
                                {
                                    
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                    {
                                        return;
                                    }
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                                    {
                                        SendPackets.SendBackAudioMessageToAllInRoom((AudioForTempRoom)msgobj, null, _public_id, false, 0, null, false);
                                        if (NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo) != null && NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).UserTalkingTo == theaccount_ID)
                                        {
                                            SendPackets.SendBackAudioMessageToAllInRoom((AudioForTempRoom)msgobj, null, NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).id, false, 0, null, false);
                                        }
                                    }
                                    else
                                    {
                                        SendPackets.SendBackAudioMessageToAllInRoom((AudioForTempRoom)msgobj, null, null, false, channelkey, savedserver, false);
                                    }
                                });
                                RunOnPrimaryActionThread(aaa);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendAudioMessage Error");
            }
        }
        public static void SendFileMessage(int _public_id, Packet packet, ref bool error)
        {
            if (NetworkManager.GetClientById(_public_id) == null)
            {
                return;
            }
            int theaccount_ID = NetworkManager.GetClientById(_public_id).accountid;
            try
            {
                int savedserver = packet.GetInt(ref error); if(error == true) return;
                string message = packet.GetString(ref error); if(error == true) return;
                string md5hash = packet.GetString(ref error); if(error == true) return;
                string ext = packet.GetString(ref error); if(error == true) return;
                long size = packet.GetLong(ref error); if (error == true) return;
                int channelkey = packet.GetInt(ref error); if(error == true) return;
                bool mine = packet.GetBool(ref error); if(error == true) return;
                packet.CheckFinalizers(ref error); if (error == true) return;
                if (savedserver == -1 && NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                {
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        if (NetworkManager.temporaryRooms.Any(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID))) == true)
                        {
                            TemporaryRooms _roomobj = NetworkManager.temporaryRooms.Find(r => r.clientsinroom.Contains(NetworkManager.GetClientByAccountId(theaccount_ID)));
                            if (Methods.foundchannel(_roomobj.id, channelkey) == false)
                            {
                                return;
                            }
                            DateTime date = DateTime.Now;
                            ChannelsForTempRoom textchannel = _roomobj.channels[channelkey] as ChannelsForTempRoom;
                            MessagesForTempRoom msgobj = new FileForTempRoom(NetworkManager.GetClientByAccountId(theaccount_ID).username, NetworkManager.GetClientByAccountId(theaccount_ID).accountid, message, textchannel.messages.Count > 0 ? textchannel.messages.Last().idofmessage + 1 : 1, _public_id, md5hash, ext, size, textchannel.messages.Count > 0 ? textchannel.messages.Last().ownedbysender : false, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                            textchannel.messages.Add(msgobj);
                            SendPackets.SendBackFileMessageToAllInRoom((FileForTempRoom)msgobj, _roomobj, null, false, channelkey, null, false);
                        }
                    }
                }
                else
                {
                    bool pming = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null;
                    RoomWithThreads st = NetworkManager.serverthreads.Find(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == savedserver);
                    RoomWithThreads pmt = NetworkManager.pmthreads.Find(x => x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).usera == theaccount_ID && (x as PMWithThreads).userb == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1) ||
                                      x.GetType() == typeof(PMWithThreads) &&
                                      (x as PMWithThreads).userb == theaccount_ID && (x as PMWithThreads).usera == (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo ?? -1)); if (pming == false)
                    {
                        if ((pming ? pmt : st) == null)
                        {
                            return;
                        }
                    }
                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null)
                    {
                        string username = NetworkManager.GetClientByAccountId(theaccount_ID).username;
                        int? usertalkingto = null;
                        string? muted = NetworkManager.CheckMute(theaccount_ID, savedserver);
                        if (muted != null)
                        {
                            SendPackets.SendMessage(NetworkManager.GetClientByAccountId(theaccount_ID).id, muted);
                            return;
                        }
                        usertalkingto = NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo;

                        (pming ? pmt : st).TaskTo_ActionThread(() =>
                        {
                            if (DatabaseCalls.UserIsInSavedRoom(savedserver, theaccount_ID) == true || usertalkingto != null)
                            {
                                int? msgid = null;
                                string serverfolder = "";
                                string serverchanneltextfile = "";
                                bool dmbothside = false;
                                bool stop = false;
                                Action aa = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                    {

                                        if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo == null)
                                        {
                                            serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString());
                                            serverchanneltextfile = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), savedserver.ToString() + "\\" + channelkey + ".txt");

                                            if (NetworkManager.AddServerMessages(savedserver, channelkey, serverfolder, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            serverchanneltextfile = TextFileName(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo);
                                            if (NetworkManager.AddPMMessages(theaccount_ID, (int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo, ref msgid) == false)
                                            {
                                                stop = true;
                                                return;
                                            }
                                            if (NetworkManager.GetClientByAccountId(theaccount_ID) != null && NetworkManager.GetClientByAccountId(theaccount_ID).id == _public_id)
                                            {
                                                if (NetworkManager.dms.Any(x => x[0] == (int)usertalkingto && x[1] == theaccount_ID) == false)
                                                {
                                                    NetworkManager.dms.Add(new int[2] { (int)usertalkingto, theaccount_ID });
                                                    dmbothside = true;
                                                }
                                            }
                                        }
                                    }
                                });
                                RunOnPrimaryActionThread(aa);
                                if (stop == true)
                                    return;
                                if (dmbothside == true && usertalkingto != null)
                                    DatabaseCalls.AddDMSBothSides(new int[2] { _public_id, theaccount_ID }, (int)usertalkingto, false);
                                if (Directory.Exists(serverfolder) == false && usertalkingto == null || File.Exists(serverchanneltextfile) == false)
                                {
                                    return;
                                }

                                if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                {
                                    return;
                                }
                                DateTime date = DateTime.Now;
                                MessagesForTempRoom msgobj = new FileForTempRoom(username, theaccount_ID, message, (int)msgid, _public_id, md5hash, ext, size, mine, String.Format("{0}/{1}/{2}/{3}/{4}", date.Month, date.Day, date.Year, date.Hour, date.Minute));
                                string rawmsgobj = "4❶" + username + "❶" + theaccount_ID + "❶" + message + "❶" + msgid + "❶" + msgobj.dateposted + "❶" + md5hash + "❶" + ext + "❶" + size + "❶" + (mine == true ? 1 : 0);
                                //write to file
                                {
                                    int ReadAttempts = 0;
                                RestartRead:;
                                    try
                                    {
                                        using (StreamWriter file = new StreamWriter(serverchanneltextfile, true))
                                        {
                                            file.WriteLine(rawmsgobj);
                                        }
                                    }
                                    catch // This will most likely occur if another thread is reading/writing from/to this file
                                    {
                                        Thread.Sleep(200);
                                        if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                                        {
                                            return;
                                        }
                                        ReadAttempts++;
                                        goto RestartRead; // Attempt to read again.
                                    }
                                }
                                Action aaa = (Action)(() =>
                                {
                                    
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID) == null)
                                    {
                                        return;
                                    }
                                    if (NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo != null)
                                    {
                                        SendPackets.SendBackFileMessageToAllInRoom((FileForTempRoom)msgobj, null, _public_id, false, 0, null, false);
                                        if (NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo) != null && NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).UserTalkingTo == theaccount_ID)
                                        {
                                            SendPackets.SendBackFileMessageToAllInRoom((FileForTempRoom)msgobj, null, NetworkManager.GetClientByAccountId((int)NetworkManager.GetClientByAccountId(theaccount_ID).UserTalkingTo).id, false, 0, null, false);
                                        }
                                    }
                                    else
                                    {
                                        SendPackets.SendBackFileMessageToAllInRoom((FileForTempRoom)msgobj, null, null, false, channelkey, savedserver, false);
                                    }
                                });
                                RunOnPrimaryActionThread(aaa);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": SendFileMessage Error");
            }
        }
    }
}
