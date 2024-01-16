
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using MySql;
using MySql.Data.MySqlClient;
using Mysqlx;
using Org.BouncyCastle.Asn1.Ocsp;


namespace ChatAppServer
{
    public class DatabaseCalls
    {
        public static readonly string _host = "127.0.0.1";
        public static readonly int _port = 3306;
        public static readonly string _name = "";
        public static readonly string _pass = "";
        public static readonly string _database = "chatdb";
        public static readonly string _charset = "utf8";
        public static void CloseSqL(MySqlConnection sqlConnection)
        {
            if (sqlConnection == null)
            {
                return;
            }
            if (sqlConnection.State != System.Data.ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        public static void OpenSql(MySqlConnection sqlConnection)
        {
            if(sqlConnection == null)
            {
                return;
            }
            if (sqlConnection.State != System.Data.ConnectionState.Open)
            {
                sqlConnection.Open();
            }
        }
        public static bool UserIsInSavedRoom(int serverid, int accountid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            string? users = null;
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    users = reader["users"].ToString();
                }
            }
            CloseSqL(sqlconnection);
            if (users == null)
            {
                goto End;
            }

            string[] usersplit = users.Split('❷');
            for (int i = 0; i < usersplit.Length; ++i)
            {

                if (String.IsNullOrWhiteSpace(usersplit[i]) == false && usersplit[i].Contains("❶"))
                {
                    if (int.Parse(usersplit[i].Split('❶')[0]) == accountid)
                    {
                        return true;
                    }
                }
            }
        End:;
            return false;
        }
        public static bool UserIsBannedFromSavedRoom(int serverid, int accountid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            string? banned = null;
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    banned = reader["prohibited"].ToString();
                }
            }
            CloseSqL(sqlconnection);
            if (banned == null)
            {
                goto End;
            }

            string[] usersplit = banned.Split('❷');
            for (int i = 0; i < usersplit.Length; ++i)
            {
                if (String.IsNullOrWhiteSpace(usersplit[i]) == false)
                {
                    if (int.Parse(usersplit[i]) == accountid)
                    {
                        return true;
                    }
                }
            }
        End:;
            return false;
        }
        public static bool SavedRoomExists(int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    CloseSqL(sqlconnection);
                    return true;
                }
            }
            CloseSqL(sqlconnection);
            return false;
        }
        public static int? SavedRoomExistsByName(string server)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE name = '{0}'", server);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    int roomid = int.Parse(reader["id"].ToString());
                    CloseSqL(sqlconnection);
                    return roomid;
                }
            }
            CloseSqL(sqlconnection);
            return null;
        }
        public static bool ChannelExists(int serverid, int channelid, int type)
        {
            if (channelid == -1)
            {
                return true;
            }
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            string channels = "";
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    channels = reader["channels"].ToString();

                }
            }
            if (channels == "")
            {
                CloseSqL(sqlconnection);
                return false;
            }
            string[] channelarray = channels.Split('❷');
            for (int i = 0; i < channelarray[i].Length; ++i)
            {
                if (channelarray[i].Contains("❶"))
                {
                    string[] internalarray = channelarray[i].Split('❶');
                    if (int.Parse(internalarray[0]) == channelid && int.Parse(internalarray[1]) == type)
                    {
                        CloseSqL(sqlconnection);
                        return true;
                    }
                }


            }
            CloseSqL(sqlconnection);
            return false;
        }
        public static int[] LogChannels(int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            string channels = "";
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    channels = reader["channels"].ToString();
                }
            }
            CloseSqL(sqlconnection);
            if (channels == "")
            {
                return null;
            }
            List<int> list = new List<int>();
            string[] channelarray = channels.Split('❷');
            for (int i = 0; i < channelarray.Length; ++i)
            {
                if (channelarray[i].Contains("❶"))
                {
                    string[] internalarray = channelarray[i].Split('❶');
                    if (int.Parse(internalarray[1]) == 0&&int.Parse(internalarray[5]) == 1)
                    {
                        list.Add(int.Parse(internalarray[0]));
                    }
                }
            }
            return list.ToArray();
        }
        public static string? SavedRoomSeed(int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            string? seed = null;

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {

                    seed = reader["encryptionseed"].ToString();
                }
            }
            CloseSqL(sqlconnection);
            return seed;
        }
        public static string GetUserProfileForLogin(string name)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string? md5 = null;
            string selection = String.Format("SELECT * FROM users WHERE name = '{0}'", name);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    md5 = reader["profilepic"].ToString();
                    CloseSqL(sqlconnection);
                    return md5;
                }
            }
            CloseSqL(sqlconnection);
            return "";
        }
        public static string GetUserProfilePic(int account_ID)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string? md5 = null;
            string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    md5 = reader["profilepic"].ToString();
                    CloseSqL(sqlconnection);
                    return md5;
                }
            }
            CloseSqL(sqlconnection);
            return " ";
        }
        public static string? UpdateRoles(string[] roleadd, int serverid) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string? roles = null;
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    roles = reader["roles"].ToString();
                    if (roles == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                    if (roleadd == null)
                    {
                        return roles;
                    }
                }
            }
            if (roles == null)
            {
                CloseSqL(sqlconnection);
                return null;
            }

            string newdatachunk = "";
            string newroles = "";
            bool contains = false;
            {
                string[] rolesplit = roles.Split('❷');
                for (int i = 0; i < rolesplit.Length; ++i)
                {
                    if (rolesplit[i].Contains("❶"))
                    {
                        string[] rolesplitinternal = rolesplit[i].Split('❶');
                        if (int.Parse(rolesplitinternal[0]) == int.Parse(roleadd[0]))
                        {
                            contains = true;
                        }
                    }
                }
            }
            if (contains == true)
            {
                string[] rolesplit = roles.Split('❷');

                for (int i = 0; i < rolesplit.Length; ++i)
                {
                    if (rolesplit[i].Contains("❶"))
                    {
                        string[] rolesplitinternal = rolesplit[i].Split('❶');
                        if (int.Parse(rolesplitinternal[0]) != int.Parse(roleadd[0]))
                        {
                            newroles += rolesplit[i] + "❷";
                        }
                        else
                        {
                            string nc = (roleadd[0]) + "❶" + (roleadd[1]) + "❶" + (roleadd[2]) + "❶" + (roleadd[3]) + "❶" + (roleadd[4]) + "❶" + (roleadd[5]) + "❶" + (roleadd[6]) + "❶" + (roleadd[7]) + "❶" + (roleadd[8]) + "❶" + (rolesplitinternal[9]) + "❷";
                            newroles += nc;
                        }
                    }
                }
            }
            else
            {
                if (roles.Contains("❶" + roleadd[1] + "❶"))
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
                newdatachunk = (roleadd[1]) + "❶" + (roleadd[2]) + "❶" + (roleadd[3]) + "❶" + (roleadd[4]) + "❶" + (roleadd[5]) + "❶" + (roleadd[6]) + "❶" + (roleadd[7]) + "❶" + (roleadd[8]) + "❶" + /*precedence*/ (int.Parse((String.IsNullOrWhiteSpace(roles) == false ? roles.Trim().Remove(roles.Length - 1, 1) : roles).Split('❷').ToList().OrderByDescending(z => int.Parse(z.Split('❶')[9])).ToList()[0].Split('❶')[9]) + 1);
                newroles = roles + /*the id*/ (int.Parse(roles.Split('❷').ToList().FindAll(x => x.Contains("❶")).Last().Split('❶')[0]) + 1) + "❶" + newdatachunk + "❷";
            }
            string query = String.Format("UPDATE servers SET roles=@roles WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@roles", newroles);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
            CloseSqL(sqlconnection);
            return newroles;

        }
        public static string? UpdateChannel(int serverid, int channelid, int ro, int incuser)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string? channels = null;
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    channels = reader["channels"].ToString();
                    if (channels == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                }
            }
            if (channels == null)
            {
                CloseSqL(sqlconnection);
                return null;
            }
            string newdatachunk = "";
            string[] csplit = channels.Split('❷');

            for (int i = 0; i < csplit.Length; ++i)
            {
                if (csplit[i].Contains("❶"))
                {
                    string[] csplitinternal = csplit[i].Split('❶');
                    if (int.Parse(csplitinternal[0]) != channelid) // if this isnt the one getting updated
                    {
                        newdatachunk += csplit[i] + "❷";
                    }
                    else
                    {
                        string nc = (csplitinternal[0]) + "❶" + (csplitinternal[1]) + "❶" + (csplitinternal[2]) + "❶" + (csplitinternal[3]) + "❶" + ro + "❶" + incuser + "❷";
                        newdatachunk += nc;
                    }
                }
            }
            string query = String.Format("UPDATE servers SET channels=@channels WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@channels", newdatachunk);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
            CloseSqL(sqlconnection);
            return newdatachunk;

        }
        public static string? RemoveRoles(int roleid, int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string? roles = null;
            string? users = null;
            string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
            MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    roles = reader["roles"].ToString();
                    users = reader["users"].ToString();
                }
            }

            if (roles == null)
            {
                CloseSqL(sqlconnection);
                return null;
            }
            List<string> list = roles.Split('❷').ToList();
            string newroles = "";
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Contains("❶") && int.Parse(list[i].Split('❶')[0]) != roleid)
                {
                    newroles += (list[i] + "❷");
                }
            }

            // remove roles for all pls
            {
                users = users.Replace("❶" + roleid + "/", "❶").Replace("/" + roleid + "/", "/");
            }

      
            {
                string query = String.Format("UPDATE servers SET roles=@roles WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@roles", newroles);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
            }
            {
                string query = String.Format("UPDATE servers SET users=@users WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@users", users);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
            }
            CloseSqL(sqlconnection);
            return newroles;
        }
        public static string? GetUserRooms(int account_ID)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string? databaserooms = null;
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        databaserooms = reader["servers"].ToString();
                    }
                }
                if (databaserooms == null)
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
            }
            string newformat = "";
            string[] split = databaserooms.Split('❷');
            for (int i = 0; i < split.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(split[i]) == false)
                {
                    string? servername = null;
                    string? serverpic = null;
                    string internalselection = String.Format("SELECT * FROM servers WHERE id = '{0}'", int.Parse(split[i]));
                    MySqlCommand internalfetch = new MySqlCommand(internalselection, sqlconnection);
                    using (MySqlDataReader reader = internalfetch.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            servername = reader["name"].ToString();
                            serverpic = reader["pic"].ToString();
                        }
                    }
                    if (servername == null || serverpic == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                    newformat += split[i] + "❶" + servername + "❶" + serverpic + "❷";
                }
            }

            CloseSqL(sqlconnection);
            return newformat;
        }
        public static string[]? AddOrRemoveFriend(int account_ID, bool add, int? otheraccount_ID)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            //add to user account who sent this
            string accstring = "";
            {
                string? friendssplit = null;
                {
                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            friendssplit = reader["friends"].ToString();
                        }
                    }
                    if (friendssplit == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                }

                string newfriendstring = "";
                string[] split = friendssplit.Split('❷');

                bool founduser = false;
                for (int i = 0; i < split.Length; i++)
                {
                    if (String.IsNullOrWhiteSpace(split[i]) == false)
                    {
                        if (add == false)
                        {
                            if (int.Parse(split[i]) == otheraccount_ID)
                            {
                                founduser = true;
                            }
                            else if (add == false && otheraccount_ID != null)
                            {
                                newfriendstring += split[i] + "❷";
                                //get this user's name by account id
                                {
                                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                                    string? username = null;
                                    using (MySqlDataReader reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            username = reader["name"].ToString();
                                        }
                                    }
                                    if (username == null)
                                    {
                                        CloseSqL(sqlconnection);
                                        return null;
                                    }
                                    accstring += split[i] + "❶" + (string)username + "❷";
                                }
                            }
                            if (add == true || otheraccount_ID == null)
                            {
                                //get this users name by account id
                                {
                                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                                    string? username = null;
                                    using (MySqlDataReader reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            username = reader["name"].ToString();
                                        }
                                    }
                                    if (username == null)
                                    {
                                        CloseSqL(sqlconnection);
                                        return null;
                                    }
                                    accstring += split[i] + "❶" + (string)username + "❷";
                                }
                            }
                        }
                    }
                }
                if (otheraccount_ID == null)
                {
                    CloseSqL(sqlconnection);
                    return new string[1] { accstring };
                }
                if (founduser == false && add == true)
                {
                    newfriendstring = friendssplit + otheraccount_ID + "❷";
                    //get this user's name by account id
                    {
                        string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", otheraccount_ID);
                        MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                        string? username = null;
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                username = reader["name"].ToString();
                            }
                        }
                        if (username == null)
                        {
                            CloseSqL(sqlconnection);
                            return null;
                        }
                        accstring += otheraccount_ID + "❶" + (string)username + "❷";
                    }

                }
                string query = String.Format("UPDATE users SET friends=@friends WHERE id = '{0}'", account_ID);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@friends", newfriendstring);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return null;
                }

            }
            //add to other user account
            string otheraccstring = "";
            {
                string? friendssplit = null;
                {
                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", otheraccount_ID);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            friendssplit = reader["friends"].ToString();
                        }
                    }
                    if (friendssplit == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                }

                string newfriendstring = "";
                string[] split = friendssplit.Split('❷');

                bool founduser = false;
                for (int i = 0; i < split.Length; i++)
                {
                    if (String.IsNullOrWhiteSpace(split[i]) == false)
                    {
                        if (add == false)
                        {
                            if (int.Parse(split[i]) == account_ID)
                            {
                                founduser = true;
                            }
                            else if (add == false)
                            {
                                newfriendstring += split[i] + "❷";
                                //get this user's name by account id
                                {
                                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                                    string? username = null;
                                    using (MySqlDataReader reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            username = reader["name"].ToString();
                                        }
                                    }
                                    if (username == null)
                                    {
                                        CloseSqL(sqlconnection);
                                        return null;
                                    }
                                    otheraccstring += split[i] + "❶" + (string)username + "❷";
                                }
                            }
                            if (add == true)
                            {
                                //get this user's name by account id
                                {
                                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                                    string? username = null;
                                    using (MySqlDataReader reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            username = reader["name"].ToString();
                                        }
                                    }
                                    if (username == null)
                                    {
                                        CloseSqL(sqlconnection);
                                        return null;
                                    }
                                    otheraccstring += split[i] + "❶" + (string)username + "❷";
                                }
                            }
                        }
                    }
                }
                if (founduser == false && add == true)
                {
                    newfriendstring = friendssplit + account_ID + "❷";
                    //get this user's name by account id
                    {
                        string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
                        MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                        string? username = null;
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                username = reader["name"].ToString();
                            }
                        }
                        if (username == null)
                        {
                            CloseSqL(sqlconnection);
                            return null;
                        }
                        otheraccstring += account_ID + "❶" + (string)username + "❷";
                    }
                }
                string query = String.Format("UPDATE users SET friends=@friends WHERE id = '{0}'", otheraccount_ID);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@friends", newfriendstring);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
                otheraccstring = newfriendstring;
            }

            CloseSqL(sqlconnection);
            return new string[2] { accstring, otheraccstring };
        }
        public static string? GetUserFriends(int account_ID)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string? friendssplit = null;
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        friendssplit = reader["friends"].ToString();
                    }
                }
                if (friendssplit == null)
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
            }
            string newformat = "";
            string[] split = friendssplit.Split('❷');
            for (int i = 0; i < split.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(split[i]) == false)
                {
                    string? username = null;
                    string internalselection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                    MySqlCommand internalfetch = new MySqlCommand(internalselection, sqlconnection);
                    using (MySqlDataReader reader = internalfetch.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            username = reader["name"].ToString();
                        }
                    }
                    if (username == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                    newformat += split[i] + "❶" + username + "❷";
                }
            }
            if (newformat == "")
            {
                newformat = " ";
            }
            CloseSqL(sqlconnection);
            return newformat;
        }
        public static string? GetUserPendingFriends(int[] hostclient)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string? pendingfriendssplit = null;
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        pendingfriendssplit = reader["pendingfriends"].ToString();
                    }
                }
                if (pendingfriendssplit == null)
                {
                    CloseSqL(sqlconnection);
         
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                     
                                SendPackets.SendMessage(hostclient[0], "Could not load pending friend.");
                                SendPackets.SendCloseLoading(hostclient[0], "Loading pending friends...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                 
                    return null;
                }
            }
            string newformat = "";
            string[] split = pendingfriendssplit.Split('❷');
            for (int i = 0; i < split.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(split[i]) == false)
                {
                    string? username = null;
                    string internalselection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                    MySqlCommand internalfetch = new MySqlCommand(internalselection, sqlconnection);
                    using (MySqlDataReader reader = internalfetch.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            username = reader["name"].ToString();
                        }
                    }
                    if (username == null)
                    {
                        CloseSqL(sqlconnection);
                  
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                   
                                    SendPackets.SendMessage(hostclient[0], "Could not load pending friend.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Loading pending friends...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                       
                        return null;
                    }
                    newformat += split[i] + "❶" + username + "❷";
                }
            }
            if (newformat == "")
            {
                newformat = " ";
            }
            CloseSqL(sqlconnection);
            return newformat;
        }
        public static string? GetUserDMS(int[] hostclient)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string? pendingdmsssplit = null;
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        pendingdmsssplit = reader["chats"].ToString();
                    }
                }
                if (pendingdmsssplit == null)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Could not load dms.");
                                SendPackets.SendCloseLoading(hostclient[0], "Loading private messages...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return null;
                }
            }
            string newformat = "";
            string[] split = pendingdmsssplit.Split('❷');
            for (int i = 0; i < split.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(split[i]) == false)
                {
                    string? username = null;
                    string internalselection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(split[i]));
                    MySqlCommand internalfetch = new MySqlCommand(internalselection, sqlconnection);
                    using (MySqlDataReader reader = internalfetch.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            username = reader["name"].ToString();
                        }
                    }
                    if (username == null)
                    {
                        CloseSqL(sqlconnection);
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    
                                    SendPackets.SendMessage(hostclient[0], "Could not load dms.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Loading private messages...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                        return null;
                    }
                    newformat += split[i] + "❶" + username + "❷";
                }
            }
            if (newformat == "")
            {
                newformat = " ";
            }
            CloseSqL(sqlconnection);
            return newformat;
        }
        public static bool CanPunish(int[] hostclient, int serverid, int punishmentid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            int publicid = hostclient[0];
            List<string>? userroles = null;
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userroles = reader["users"].ToString().Split('❷').ToList().Find(x => int.Parse(x.Split('❶')[0]) == hostclient[1]).Split('❶')[1].Split('/').ToList();
                    }
                }
            }
            List<string>? roles = null;
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        roles = reader["roles"].ToString().Split('❷').ToList();
                    }
                }
            }
            CloseSqL(sqlconnection);
            if (userroles == null || roles == null)
            {
                return false;
            }
            if (userroles.Any(x => roles.Any(xx => xx.Contains("❶") && xx.Split('❶')[0] == x && xx.Split('❶')[punishmentid + 2] == "1")))
            {
                return true;
            }
            //2 -kick
            //3- mute
            //4- ban
            return false;
        }
        public static object[]? OpenUserProfile(int accrequester, int account_ID, int serverid)
        {
            string bio = "";
            string media = "";
            string name = "";
            string creation = "";
            string friends = "";
            string? roles = "";
            string? reqroles = "";
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        bio = reader["bio"].ToString();
                        media = reader["media"].ToString();
                        name = reader["name"].ToString();
                        creation = reader["Creation"].ToString();
                        friends = reader["friends"].ToString();
                    }
                }
            }
            if (serverid != -1)
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string u = reader["users"].ToString();
                        roles = u.Split('❷').ToList().Find(x => int.Parse(x.Split('❶')[0]) == account_ID).Split('❶')[1];
                        reqroles = u.Split('❷').ToList().Find(x => int.Parse(x.Split('❶')[0]) == accrequester).Split('❶')[1];
                    }
                }
            }
            else
            {
                roles = "-1/"; reqroles = "-1/";
            }
            CloseSqL(sqlconnection);
            if (roles == null) { return null; }

            return new object[] { bio, media, name, creation, friends, roles, reqroles };
        }
        public static string ValidateMediaSite(string rawsite) 
        {

            string[] splita = rawsite.Split('/');
            if (splita.Length >= 3 && splita[2].Contains('.'))
            {
                string WebsiteName = splita[2].Split('.')[1].ToUpper();
                switch (WebsiteName)
                {
                    case "YOUTUBE":
                        {
                            if (splita.Length == 5 && splita[0] == "https:" && splita[2].ToLower() == "www.youtube.com" && splita[3] == "channel" && rawsite.Contains('=') == false ||
                                splita.Length == 4 && splita[0] == "https:" && splita[2].ToLower() == "www.youtube.com" && splita[3][0] == '@' && rawsite.Contains('=') == false)
                            {
                                return "good";
                            }
                            else
                            {
                                if (splita.Length > 0 && splita[0] == "http:")
                                {
                                    return "HTTP is not allowed. Please use HTTPS";
                                }
                                else
                                {
                                    return "This is an invalid youtube channel url format";
                                }
                            }
                        }

                    case "BITCHUTE":
                        {
                            if (splita.Length == 5 && splita[0] == "https:" && splita[2].ToLower() == "www.bitchute.com" && splita[3] == "channel" && rawsite.Contains('=') == false)
                            {
                                return "good";
                            }
                            else
                            {
                                if (splita.Length > 0 && splita[0] == "http:")
                                {
                                    return "HTTP is not allowed. Please use HTTPS";
                                }
                                else
                                {
                                    return "This is an invalid bitchute channel url format";
                                }
                            }
                        }
                    case "GITHUB":
                        {
                            if (splita.Length == 4 && splita[0] == "https:" && splita[2].ToLower() == "www.github.com" && splita[3] != "login" && splita[3] != "signup" && splita[3] != "pricing" && rawsite.Contains('=') == false)
                            {
                                return "good";
                            }
                            else
                            {
                                if (splita.Length > 0 && splita[0] == "http:")
                                {
                                    return "HTTP is not allowed. Please use HTTPS";
                                }
                                else
                                {
                                    return "This is an invalid twitter account url format";
                                }
                            }
                        }
                    case "FACEBOOK":
                        {
                            if (splita.Length == 4 && splita[0] == "https:" && splita[2].ToLower() == "www.facebook.com" && splita[3] != "login" && splita[3] != "reg" && splita[3] != "lite" && splita[3] != "watch"
                                 && splita[3] != "places" && splita[3] != "games" && splita[3] != "fundraisers" && splita[3] != "marketplace" && splita[3] != "votinginformationcenter"
                                && rawsite.Contains('=') == false)
                            {
                                return "good";
                            }
                            else
                            {
                                if (splita.Length > 0 && splita[0] == "http:")
                                {
                                    return "HTTP is not allowed. Please use HTTPS";
                                }
                                else
                                {
                                    return "This is an invalid facebook account url format";
                                }
                            }
                        }
                    case "TWITTER":
                        {
                            if (splita.Length == 4 && splita[0] == "https:" && splita[2].ToLower() == "www.twitter.com" && rawsite.Contains('=') == false)
                            {
                                return "good";
                            }
                            else
                            {
                                if (splita.Length > 0 && splita[0] == "http:")
                                {
                                    return "HTTP is not allowed. Please use HTTPS";
                                }
                                else
                                {
                                    return "This is an invalid twitter account url format";
                                }
                            }
                        }

                    default:

                        return String.Format("Unable to use '{0}' for your media. Media platforms are currently restricted to only twitter,github,facebook,youtube,and bitchute", rawsite);

                }
            }
            else
            {
                return "This is an invalid url format. Please use format 'https://www.website.com' ";
            }
        }
        public static bool SaveUserMedia(int[] hostclient, string sites)
        {
            string[] listofsites = sites.Split('\n');
            string builtstring = " ";
            for (int i = 0; i < listofsites.Length; ++i)
            {
                string NewTrimmedSite = listofsites[i].Trim();
                if (String.IsNullOrWhiteSpace(NewTrimmedSite) == true)
                {
                    continue;
                }
                string s = ValidateMediaSite(NewTrimmedSite);
                if (s == "good")
                {
                    string webData = "";
                    try
                    {
                        using (System.Net.WebClient webclient = new System.Net.WebClient())
                        {
                            webData = webclient.DownloadString("http://"+ _host +"/FetchMediaDetails.php?wp=" + NewTrimmedSite);
                        }
                        if (String.IsNullOrEmpty(webData) == true)
                        {
                            throw new Exception();
                        }
                    }
                    catch //fail to retrieve
                    {
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    
                                    SendPackets.SendMessage(hostclient[0], String.Format("Failed to retrieve '{0}' info", NewTrimmedSite));
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                        continue;
                    }
                    builtstring += NewTrimmedSite + "t2h2i2s2i2s2a2s2p2l2i2t2" + webData + "t1h1i1s1i1s1a1t1a1p1l1i1t1";
                }
                else
                {   
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], s);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
            }
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string query = String.Format("UPDATE users SET media=@media WHERE id = '{0}'", hostclient[1]);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@media", builtstring);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            
                            SendPackets.SendMessage(hostclient[0], "Failed to write to database");
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return false;
            }
            CloseSqL(sqlconnection);
            return true;
        }
        public static string GetUsernameByID(int account_ID) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
            MySqlCommand fetch = new MySqlCommand(selection, sqlconnection);
            using (MySqlDataReader reader = fetch.ExecuteReader())
            {
                if (reader.Read())
                {
                    string name = reader["name"].ToString();
                    CloseSqL(sqlconnection);
                    return name;
                }
            }
            CloseSqL(sqlconnection);
            return "";
        }
        public static string? GetUserPublicKeyByID(int account_ID) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", account_ID);
            MySqlCommand fetch = new MySqlCommand(selection, sqlconnection);
            string? retrievedkey = null;
            using (MySqlDataReader reader = fetch.ExecuteReader())
            {
                if (reader.Read())
                {
                    string rk = reader["publickey"].ToString();
                    retrievedkey = rk == "" ? null : rk;
                    CloseSqL(sqlconnection);
                    return retrievedkey;
                }
            }
            CloseSqL(sqlconnection);
            return null;
        }
        public static string? GetPMHashKey(int passed_acca, int passed_accb)
        {
            int acca, accb;
            if (passed_acca > passed_accb)
            {
                acca = passed_acca;
                accb = passed_accb;
            }
            else
            {
                acca = passed_accb;
                accb = passed_acca;
            }
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            OpenSql(sqlconnection);
            string selection = String.Format("SELECT * FROM pms WHERE a = '{0}' and b = '{1}'", acca, accb);
            MySqlCommand fetch = new MySqlCommand(selection, sqlconnection);

            using (MySqlDataReader reader = fetch.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (int.Parse(reader["a"].ToString()) == passed_acca)
                    {
                        string returnvalue = reader["codea"].ToString();
                        CloseSqL(sqlconnection);
                        return returnvalue;
                    }
                    if (int.Parse(reader["b"].ToString()) == passed_acca)
                    {
                        string returnvalue = reader["codeb"].ToString();
                        CloseSqL(sqlconnection);
                        return returnvalue;
                    }
                }
            }
            CloseSqL(sqlconnection);
            return null;
        }
        public static bool AddPMToDatabase(int passed_acca, int passed_accb, string passed_codea, string passed_codeb) 
        {
            int acca, accb;
            string codea, codeb;
            if (passed_acca > passed_accb)
            {
                acca = passed_acca;
                accb = passed_accb;
                codea = passed_codea;
                codeb = passed_codeb;
            }
            else
            {
                acca = passed_accb;
                accb = passed_acca;
                codea = passed_codeb;
                codeb = passed_codea;
            }
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            //update users servers section
            {
                string selection = String.Format("SELECT * FROM pms WHERE a = '{0}' and b = '{1}'", acca, accb);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CloseSqL(sqlconnection);
                        return true;
                    }
                }
            }
            string query = "INSERT INTO pms (a, b, codea, codeb) values(@a ,@b ,@codea,@codeb)";
            MySqlCommand cmd2 = new MySqlCommand();
            cmd2.CommandText = query;
            cmd2.Parameters.AddWithValue("@a", acca);
            cmd2.Parameters.AddWithValue("@b", accb);
            cmd2.Parameters.AddWithValue("@codea", codea);
            cmd2.Parameters.AddWithValue("@codeb", codeb);
            cmd2.Connection = sqlconnection;
            try
            {
                cmd2.ExecuteNonQuery();
            }
            catch
            {

                CloseSqL(sqlconnection);
                return false;
            };
            CloseSqL(sqlconnection);
            return true;
        }
        public static bool AddPendingFriend(string receivername, int[] hostclient /*requester*/)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            //update users pending friends
            {
                string? pendingfriends = null;
                string? mypendingfriends = null;
                int receiverid = 0;
                //for receiver
                {
                    string selection = String.Format("SELECT * FROM users WHERE name = '{0}'", receivername);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (int.Parse(reader["id"].ToString()) == hostclient[1])
                            {

                                CloseSqL(sqlconnection);
                                
                                {
                                    Action a = (Action)(() =>
                                    {
                                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                        {
                                            
                                            SendPackets.SendMessage(hostclient[0], "You cant add yourself.");
                                            SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                        }
                                    });
                                    ReceivePackets.RunOnPrimaryActionThread(a);
                                }
                                
                                return false;
                            }
                            receiverid = int.Parse(reader["id"].ToString());
                            pendingfriends = reader["pendingfriends"].ToString();
                        }
                    }
                }
                //for requester

                {
                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mypendingfriends = reader["pendingfriends"].ToString();

                        }
                    }
                }
                if (pendingfriends == null)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Player not found.");
                                SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (mypendingfriends == null)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Error.");
                                SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }

                if (mypendingfriends.Split('❷').ToList().Contains(receiverid.ToString()) == true)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "The person you tried to add already sent you a friend request.");
                                SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (pendingfriends.Split('❷').ToList().Contains(hostclient[1].ToString()) == false)
                {
                    string query = String.Format("UPDATE users SET pendingfriends=@pendingfriends WHERE name = '{0}'", receivername);
                    MySqlCommand curcommand = new MySqlCommand();
                    curcommand.CommandText = query;
                    curcommand.Parameters.AddWithValue("@pendingfriends", pendingfriends + hostclient[1] + "❷");
                    curcommand.Connection = sqlconnection;
                    try
                    {
                        curcommand.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    
                                    SendPackets.SendMessage(hostclient[0], "Could not add friend.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                        return false;
                    }
                }
                else
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "You already sent a friend request.");
                                SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
            }
            CloseSqL(sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        SendPackets.SendMessage(hostclient[0], "Friend request sent.");
                        SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            
            return true;
        }
        public static bool AddFriendFromPending(int[] hostclient, int otheraccount, bool justremove) 
        {

            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            //update account with pending users pending friends
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? pendingfriends = null;
                string? friends = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        pendingfriends = reader["pendingfriends"].ToString();
                        friends = reader["friends"].ToString();
                    }
                }
                if (pendingfriends == null || friends == null)
                {
              
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                if (justremove == true)
                                {
                                    SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                }
                                else
                                {
                                    SendPackets.SendMessage(hostclient[0], "Could not add friend.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                }
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (pendingfriends.Split('❷').ToList().Contains(otheraccount.ToString()) == true && friends.Split('❷').ToList().Contains(otheraccount.ToString()) == false) // verify if this user is in the pending
                {
                    //remove pending user from pending list
                    {
                        string query = String.Format("UPDATE users SET pendingfriends=@pendingfriends WHERE id = '{0}'", hostclient[1]);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@pendingfriends", pendingfriends.Replace(otheraccount + "❷", ""));
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                            CloseSqL(sqlconnection);
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        
                                        if (justremove == true)
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                            SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                        }
                                        else
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not add friend.");
                                            SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                        }
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            
                            return false;
                        }
                    }
                    if (justremove == false)
                    {//add pending user to friends list now
                        string query = String.Format("UPDATE users SET friends=@friends WHERE id = '{0}'", hostclient[1]);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@friends", friends + otheraccount + "❷");
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                            CloseSqL(sqlconnection);
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        
                                        if (justremove == true)
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                            SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                        }
                                        else
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not add friend.");
                                            SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                        }
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            
                            return false;
                        }
                    }
                }
                else
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Critical error.");
                                SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
            }
            //update other pending friends
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", otheraccount);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? pendingfriends = null;
                string? friends = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        pendingfriends = reader["pendingfriends"].ToString();
                        friends = reader["friends"].ToString();
                    }
                }
                if (pendingfriends == null || friends == null)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                if (justremove == true)
                                {
                                    SendPackets.SendMessage(hostclient[0], "Could not remove friend (B).");
                                    SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                }
                                else
                                {
                                    SendPackets.SendMessage(hostclient[0], "Could not add friend (B).");
                                    SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                }
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (friends.Split('❷').ToList().Contains(hostclient[1].ToString()) == false) // verify if this user is in the pending
                {
                    //remove pending user from pending list
                    if (pendingfriends.Split('❷').ToList().Contains(hostclient[1].ToString()) == true)
                    {
                        string query = String.Format("UPDATE users SET pendingfriends=@pendingfriends WHERE id = '{0}'", otheraccount);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@pendingfriends", pendingfriends.Replace(hostclient[1] + "❷", ""));
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                           
                            CloseSqL(sqlconnection);
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        
                                        if (justremove == true)
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not remove friend (B).");
                                            SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                        }
                                        else
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not add friend (B).");
                                            SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                        }
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            

                            return false;
                        }
                    }
                    if (justremove == false)
                    {//add pending user to friends list now
                        string query = String.Format("UPDATE users SET friends=@friends WHERE id = '{0}'", otheraccount);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@friends", friends + hostclient[1] + "❷");
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                         
                            CloseSqL(sqlconnection);
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        
                                        if (justremove == true)
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not remove friend (B).");
                                            SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                        }
                                        else
                                        {
                                            SendPackets.SendMessage(hostclient[0], "Could not add friend (B).");
                                            SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                                        }
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            

                            return false;
                        }
                    }
                }
                else
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Critical error (B).");
                                SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                                SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    

                    return false;
                }
            }

            CloseSqL(sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        SendPackets.SendMessage(hostclient[0], "Success.");
                        SendPackets.SendCloseLoading(hostclient[0], "Adding friend...", 0);
                        SendPackets.SendCloseLoading(hostclient[0], "Rejecting friend...", 0);
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            
            return true;
        }
        public static bool AddDMSBothSides(int[] hostclient, int other, bool onesided)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
       
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? chats = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chats = reader["chats"].ToString();
                    }
                }
                if (chats == null)
                {

                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Could not start DM.");
                                SendPackets.SendCloseLoading(hostclient[0], "Starting DM...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (chats.Split('❷').ToList().Contains(other.ToString()) == false)
                {
                    string query = String.Format("UPDATE users SET chats=@chats WHERE id = '{0}'", hostclient[1]);
                    MySqlCommand curcommand = new MySqlCommand();
                    curcommand.CommandText = query;
                    curcommand.Parameters.AddWithValue("@chats", chats + other + "❷");
                    curcommand.Connection = sqlconnection;
                    try
                    {
                        curcommand.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    
                                    SendPackets.SendMessage(hostclient[0], "Could not start DM.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Starting DM...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                        return false;
                    }
                }
            }
            if(onesided == false)
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", other);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? chats = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chats = reader["chats"].ToString();
                    }
                }
                if (chats == null)
                {

                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Could not start DM.");
                                SendPackets.SendCloseLoading(hostclient[0], "Starting DM...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (chats.Split('❷').ToList().Contains(hostclient[1].ToString()) == false)
                {
                    string query = String.Format("UPDATE users SET chats=@chats WHERE id = '{0}'", other);
                    MySqlCommand curcommand = new MySqlCommand();
                    curcommand.CommandText = query;
                    curcommand.Parameters.AddWithValue("@chats", chats + hostclient[1] + "❷");
                    curcommand.Connection = sqlconnection;
                    try
                    {
                        curcommand.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    
                                    SendPackets.SendMessage(hostclient[0], "Could not start DM.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Starting DM...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                        return false;
                    }
                }
            }
            CloseSqL(sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        SendPackets.SendCloseLoading(hostclient[0], "Starting DM...", 0);
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            
            
            return true;
        }
        public static bool RemoveDMS(int[] hostclient, int other)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);

            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? chats = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chats = reader["chats"].ToString();
                    }
                }
                if (chats == null)
                {
                    return false;
                }

                {
                    string newchats = "";
                    string[] arrayofchats = chats.Split('❷');
                    for (int i = 0; i < arrayofchats.Length; i++)
                    {
                        if (String.IsNullOrWhiteSpace(arrayofchats[i]) == false && int.Parse(arrayofchats[i]) != other)
                        {
                            newchats += arrayofchats[i] + "❷";
                        }
                    }
                    string query = String.Format("UPDATE users SET chats=@chats WHERE id = '{0}'", hostclient[1]);
                    MySqlCommand curcommand = new MySqlCommand();
                    curcommand.CommandText = query;
                    curcommand.Parameters.AddWithValue("@chats", newchats);
                    curcommand.Connection = sqlconnection;
                    try
                    {
                        curcommand.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);
                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    
                                    SendPackets.SendMessage(hostclient[0], "Could not remove DM.");
                                    SendPackets.SendCloseLoading(hostclient[0], "Removing DM...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                        return false;
                    }
                }
            }

            //check for perm del
            {
                
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", other);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? chats = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chats = reader["chats"].ToString();
                    }
                }
                if (chats == null)
                {
                    return false;
                }
                bool otherstillhaschat = false;
                {
                    string[] arrayofchats = chats.Split('❷');
                    for (int i = 0; i < arrayofchats.Length; i++)
                    {
                        if (String.IsNullOrWhiteSpace(arrayofchats[i]) == false && int.Parse(arrayofchats[i]) == hostclient[1])
                        {
                            otherstillhaschat = true;
                        }
                    }
                }
                if (otherstillhaschat == false)
                {
                    //delete dm in entirety
                    int passed_acca = hostclient[1];
                    int passed_accb = other;
                    int acca, accb;

                    if (passed_acca > passed_accb)
                    {
                        acca = passed_acca;
                        accb = passed_accb;
                    }
                    else
                    {
                        acca = passed_accb;
                        accb = passed_acca;
                    }
                    if (GetPMHashKey(acca, accb) == null)
                    {
                        return false;
                    }

                    string seltodel = String.Format("DELETE  FROM pms WHERE a = '{0}' and b = '{1}'", acca, accb);
                    MySqlCommand cmd2 = new MySqlCommand();
                    cmd2.CommandText = seltodel;
                    cmd2.Connection = sqlconnection;
                    try
                    {
                        cmd2.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);

                        
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                SendPackets.SendMessage(hostclient[0], "Unable to permanently remove conversataion.");
                                SendPackets.SendCloseLoading(hostclient[0], "Removing DM...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                        

                        return false;
                    };
                    string filename = ReceivePackets.TextFileName(acca, accb);
                    if (File.Exists(filename) == true)
                    {
                        try
                        {
                            File.Delete(filename);
                        }
                        catch
                        {

                        }
                    }
                }
            }

            CloseSqL(sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        SendPackets.SendCloseLoading(hostclient[0], "Removing DM...", 0);
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            

            return true;
        }
        public static bool RemoveFriend(int[] hostclient, int accb)
        {

            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? friends = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        friends = reader["friends"].ToString();
                    }
                }
                if (friends == null)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                SendPackets.SendCloseLoading(hostclient[0], "Removing friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (friends.Split('❷').ToList().Contains(accb.ToString()) == true) // verify if this user is friend with
                {
                    {//remove friend
                        string query = String.Format("UPDATE users SET friends=@friends WHERE id = '{0}'", hostclient[1]);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@friends", friends.Replace(accb + "❷", ""));
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                            CloseSqL(sqlconnection);                   
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        
                                        SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                        SendPackets.SendCloseLoading(hostclient[0], "Removing friend...", 0);
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            
                            return false;
                        }
                    }
                }
                else
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Critical error.");
                                SendPackets.SendCloseLoading(hostclient[0], "Removing friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
            }
      
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", accb);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? friends = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        friends = reader["friends"].ToString();
                    }
                }
                if (friends == null)
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                SendPackets.SendCloseLoading(hostclient[0], "Removing friend...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return false;
                }
                if (friends.Split('❷').ToList().Contains(hostclient[1].ToString()) == true) // verify if this user is friend with
                {
                    {//remove friend
                        string query = String.Format("UPDATE users SET friends=@friends WHERE id = '{0}'", accb);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@friends", friends.Replace(hostclient[1] + "❷", ""));
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                            CloseSqL(sqlconnection);
                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        
                                        SendPackets.SendMessage(hostclient[0], "Could not remove friend.");
                                        SendPackets.SendCloseLoading(hostclient[0], "Removing friend...", 0);
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            
                            return false;
                        }
                    }
                }
            }

            CloseSqL(sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        SendPackets.SendMessage(hostclient[0], "Success.");
                        SendPackets.SendCloseLoading(hostclient[0], "Removing friend...", 0);
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            
            return true;
        }
        public static bool[] AddAUserToSavedRoom(int serverid, int accountid) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            //update users servers section
            {
                string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", accountid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                string? servers = null;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        servers = reader["servers"].ToString();
                    }
                }
                if (servers == null)
                {
                    CloseSqL(sqlconnection);
                    return new bool[2] { false, false };
                }
                if (servers.Split('❷').ToList().Contains(serverid.ToString()) == false)
                {
                    string query = String.Format("UPDATE users SET servers=@servers WHERE id = '{0}'", accountid);
                    MySqlCommand curcommand = new MySqlCommand();
                    curcommand.CommandText = query;
                    curcommand.Parameters.AddWithValue("@servers", servers + serverid + "❷");
                    curcommand.Connection = sqlconnection;
                    try
                    {
                        curcommand.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);
                        return new bool[2] { false,false};
                    }
                }
            }
            
            if (UserIsInSavedRoom(serverid, accountid))
            {
                CloseSqL(sqlconnection);
                return new bool[2] { true, false };
            }
            string? users = null;

            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        users = reader["users"].ToString();
                    }
                }

                if (users == null)
                {
                    CloseSqL(sqlconnection);
                    return new bool[2] { false, false };
                }
            }
            users += (accountid + "❶" + "0/" + "❷");
            {
                string query = String.Format("UPDATE servers SET users=@users WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@users", users);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return new bool[2] { false, false };
                }
            }
            CloseSqL(sqlconnection);
            return new bool[2] { true, true };
        }
        public static string? GetUserServerRoles(int account_ID, int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? users = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        users = reader["users"].ToString();
                    }
                }
            }
            if (users == null)
            {
                return null;
            }
            string[] usersplit = users.Split('❷');
            for (int i = 0; i < usersplit.Length; ++i)
            {
                if (usersplit[i].Contains("❶"))
                {
                    string[] usersplitinternal = usersplit[i].Split('❶');
                    if (int.Parse(usersplitinternal[0]) == account_ID)
                    {
                        return usersplitinternal[1];
                    }
                }
            }
            CloseSqL (sqlconnection);
            return null;
        }
        public static string? AddUserRole(int account_ID, int serverid, int roleid) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? users = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        users = reader["users"].ToString();
                    }
                }
            }
            if (users == null)
            {
                return null;
            }
            string newuserdatachunk = "";
            string[] usersplit = users.Split('❷');
            for (int i = 0; i < usersplit.Length; ++i)
            {
                if (usersplit[i].Contains("❶"))
                {
                    string[] usersplitinternal = usersplit[i].Split('❶');
                    if (int.Parse(usersplitinternal[0]) == account_ID) // if this is the user
                    {
                        string roledatachunk = "";
                        string[] rolesplit = usersplitinternal[1].Split('/');
                        for (int ii = 0; ii < rolesplit.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                            {
                                if (int.Parse(rolesplit[ii]) != roleid)
                                {
                                    roledatachunk += rolesplit[ii] + "/";
                                }
                            }
                        }
                        roledatachunk += roleid + "/";
                        newuserdatachunk += usersplitinternal[0] + "❶" + roledatachunk + "❷";

                    }
                    else
                    {
                        newuserdatachunk += usersplit[i] + "❷";
                    }
                }
            }
           
            string query = String.Format("UPDATE servers SET users=@users WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@users", newuserdatachunk);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
            
            CloseSqL(sqlconnection);
            return newuserdatachunk;
        }
        public static string? TransferServerOwnership(int originalowner, int newowner, int serverid) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? users = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        users = reader["users"].ToString();
                    }
                }
            }
            if (users == null || originalowner == newowner)
            {
                return null;
            }
            int roleid = 1;
            string newuserdatachunk = "";
            string[] usersplit = users.Split('❷');

            bool originalisowner = false;
            for (int i = 0; i < usersplit.Length; ++i) // check that this user is an owner
            {
                if (usersplit[i].Contains("❶"))
                {
                    string[] usersplitinternal = usersplit[i].Split('❶');
                    if (int.Parse(usersplitinternal[0]) == originalowner) // if this is the user
                    {
                        string roledatachunk = "";
                        string[] rolesplit = usersplitinternal[1].Split('/');
                        for (int ii = 0; ii < rolesplit.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                            {
                                if (int.Parse(rolesplit[ii]) == roleid)
                                {
                                    originalisowner = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (originalisowner == false)
            {
                return null;
            }
            for (int i = 0; i < usersplit.Length; ++i)
            {
                if (usersplit[i].Contains("❶"))
                {
                    string[] usersplitinternal = usersplit[i].Split('❶');
                    if (int.Parse(usersplitinternal[0]) == newowner || int.Parse(usersplitinternal[0]) == originalowner) // if this is the user
                    {
                        string roledatachunk = "";
                        string[] rolesplit = usersplitinternal[1].Split('/');
                        for (int ii = 0; ii < rolesplit.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                            {
                                if (int.Parse(rolesplit[ii]) != roleid)
                                {
                                    roledatachunk += rolesplit[ii] + "/"; // re assemble whats there already
                                }
                            }
                        }
                        if(int.Parse(usersplitinternal[0]) == newowner)
                        {
                            roledatachunk += roleid + "/"; // add new role
                        }
                        newuserdatachunk += usersplitinternal[0] + "❶" + roledatachunk + "❷";

                    }
                    else
                    {
                        newuserdatachunk += usersplit[i] + "❷";
                    }
                }
            }
           
            string query = String.Format("UPDATE servers SET users=@users WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@users", newuserdatachunk);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
            
            CloseSqL(sqlconnection);
            return newuserdatachunk;
        }
        public static string? AddChannelRole(int channelid, int serverid, int roleid) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? channels = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        channels = reader["channels"].ToString();
                    }
                }
            }
            if (channels == null)
            {
                return null;
            }
            string newchanneldatachunk = "";
            string[] channelssplit = channels.Split('❷');
            for (int i = 0; i < channelssplit.Length; ++i)
            {
                if (channelssplit[i].Contains("❶"))
                {
                    string[] channelsplitinternal = channelssplit[i].Split('❶');
                    if (int.Parse(channelsplitinternal[0]) == channelid) // if this is the user
                    {
                        string roledatachunk = "";
                        string[] rolesplit = channelsplitinternal[3].Split('/');
                        for (int ii = 0; ii < rolesplit.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                            {
                                if (int.Parse(rolesplit[ii]) != roleid)
                                {
                                    roledatachunk += rolesplit[ii] + "/";
                                }
                            }
                        }
                        roledatachunk += roleid + "/";
                        if (channelsplitinternal.Length > 5)
                            newchanneldatachunk += channelsplitinternal[0] + "❶" + channelsplitinternal[1] + "❶" + channelsplitinternal[2] + "❶" + roledatachunk + "❶" + channelsplitinternal[4] + "❶" + channelsplitinternal[5] + "❷";
                        else
                            newchanneldatachunk += channelsplitinternal[0] + "❶" + channelsplitinternal[1] + "❶" + channelsplitinternal[2] + "❶" + roledatachunk + "❶" + channelsplitinternal[4] + "❷";

                    }
                    else
                    {
                        newchanneldatachunk += channelssplit[i] + "❷";
                    }
                }
            }
        
            string query = String.Format("UPDATE servers SET channels=@channels WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@channels", newchanneldatachunk);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
             
            CloseSqL(sqlconnection);
            return newchanneldatachunk;
        }
        public static string? RemoveChannelRole(int channelid, int serverid, int roleid, int[] hostclient)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? channels = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        channels = reader["channels"].ToString();
                    }
                }
            }
            if (channels == null)
            {
                return null;
            }

            string newchanneldatachunk = "";
            string[] channelssplit = channels.Split('❷');
            int channelsforall = 0;
            for (int i = 0; i < channelssplit.Length; ++i)
            {
                if (channelssplit[i].Contains("❶"))
                {
                    string[] channelsplitinternal = channelssplit[i].Split('❶');
                    if (int.Parse(channelsplitinternal[0]) == channelid) // if this is the user
                    {
                        string roledatachunk = "";
                        string[] rolesplit = channelsplitinternal[3].Split('/');
                        for (int ii = 0; ii < rolesplit.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                            {
                                if (int.Parse(rolesplit[ii]) != roleid)
                                {
                                    roledatachunk += rolesplit[ii] + "/";
                                }
                            }
                        }
                    
                        if (channelsplitinternal.Length > 5)
                            newchanneldatachunk += channelsplitinternal[0] + "❶" + channelsplitinternal[1] + "❶" + channelsplitinternal[2] + "❶" + roledatachunk + "❶" + channelsplitinternal[4] + "❶" + channelsplitinternal[5] + "❷";
                        else
                            newchanneldatachunk += channelsplitinternal[0] + "❶" + channelsplitinternal[1] + "❶" + channelsplitinternal[2] + "❶" + roledatachunk + "❶" + channelsplitinternal[4] + "❷";
                    

                    }
                    else
                    {
                        newchanneldatachunk += channelssplit[i] + "❷";
                    }
                }
            }
            List<string> csplit = newchanneldatachunk.Split('❷').ToList();
            for (int i = 0; i < csplit.Count; ++i)
            {
                if (csplit[i].Contains("❶"))
                {
                    string[] internalsplit = csplit[i].Split('❶');
                    if (int.Parse(internalsplit[0]) != channelid)
                    {
                        if (internalsplit[3].Contains("/") && internalsplit[3].Split('/').ToList().Contains("0") && internalsplit[1] == "0")
                        {
                            channelsforall++;
                        }
                    }
                }
            }
            if (channelsforall == 0)
            {
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            SendPackets.SendMessage(hostclient[0], "Please leave at least one text channel for all.");
                            SendPackets.SendCloseLoading(hostclient[0], "Deleting channel...", 0);
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return null ;
            }
     
            string query = String.Format("UPDATE servers SET channels=@channels WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@channels", newchanneldatachunk);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
            CloseSqL(sqlconnection) ;
            return newchanneldatachunk;
        }
        public static string? ChangeRolePrecedence(int serverid, int roleid, bool up)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? roles = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        roles = reader["roles"].ToString();
                    }
                }
            }
            if (roles == null)
            {
                CloseSqL(sqlconnection);
                return null;
            }
            string[] rolesplit = roles.Split('❷');
            List<string> rolesplitmod = (String.IsNullOrWhiteSpace(roles) == false ? roles.Trim().Remove(roles.Length - 1, 1) : roles).Split('❷').ToList().OrderBy(x => int.Parse(x.Split('❶')[9])).ToList();
            for (int i = 0; i < rolesplitmod.Count; i++)
            {
                if (rolesplitmod[i].Contains("❶") && int.Parse(rolesplitmod[i].Split('❶')[0]) == roleid)
                {
                    if (up == false && i == rolesplitmod.Count - 1 || up == true && i == 0)
                    {

                    }
                    else
                    {
                        if (up == false)// going down increases index number
                        {
                            string temp = rolesplitmod[i + 1];
                            rolesplitmod[i + 1] = rolesplitmod[i];
                            rolesplitmod[i] = temp;
                        }
                        else
                        {
                            string temp = rolesplitmod[i - 1];
                            rolesplitmod[i - 1] = rolesplitmod[i];
                            rolesplitmod[i] = temp;
                        }
                    }
                    break;
                }
            }

            for (int i = 0; i < rolesplitmod.Count; i++)
            {
                if (rolesplitmod[i].Contains("❶") == false)
                {
                    continue;
                }
                string[] rolesplitinternal = rolesplitmod[i].Split('❶');
                rolesplitmod[i] = rolesplitinternal[0] + "❶" + rolesplitinternal[1] + "❶" + rolesplitinternal[2] + "❶" + rolesplitinternal[3] + "❶" + rolesplitinternal[4] + "❶" + rolesplitinternal[5] + "❶" + rolesplitinternal[6] + "❶" +
                                       rolesplitinternal[7] + "❶" + rolesplitinternal[8] + "❶" + i;
            }
            //change values
            string newdatachunk = "";
            for (int i = 0; i < rolesplit.Length; i++)
            {
                if (rolesplit[i].Contains("❶") == false)
                {
                    continue;
                }
                string[] rolesplitinternal = rolesplit[i].Split('❶');
                string tempdatachunk = rolesplitinternal[0] + "❶" + rolesplitinternal[1] + "❶" + rolesplitinternal[2] + "❶" + rolesplitinternal[3] + "❶" + rolesplitinternal[4] + "❶" + rolesplitinternal[5] + "❶" + rolesplitinternal[6] + "❶" +
                                       rolesplitinternal[7] + "❶" + rolesplitinternal[8] + "❶" + rolesplitmod.Find(x => x.Contains("❶") && x.Split('❶')[0] == rolesplitinternal[0]).Split('❶')[9];
                newdatachunk += tempdatachunk + "❷";
            }
            {
               
                string query = String.Format("UPDATE servers SET roles=@roles WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@roles", newdatachunk);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
             
            }
            CloseSqL(sqlconnection);
            return newdatachunk;
        }
        public static string? BanUser(int serverid, int account_ID)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? prohibited = null;
            string? sname = null;
            {

                OpenSql(sqlconnection);
                {
                    string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            prohibited = reader["prohibited"].ToString();
                            sname = reader["name"].ToString();
                        }
                    }
                }
                if (prohibited == null)
                {
                    return null;
                }
                bool contained = false;
                string[] bansplit = prohibited.Split('❷');
                for (int i = 0; i < bansplit.Length; ++i)
                {
                    if (String.IsNullOrWhiteSpace(bansplit[i]) == false && int.Parse(bansplit[i]) == account_ID)
                    {
                        return sname;
                    }
                }
                prohibited += account_ID + "❷";
            }
            {
                string query = String.Format("UPDATE servers SET prohibited=@prohibited WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@prohibited", (string)prohibited);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return null;
                }
                 
            }

            CloseSqL(sqlconnection);
            return sname;
        }
        public static string? GetServerNameByID(int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string sname = reader["name"].ToString();
                        CloseSqL(sqlconnection);
                        return sname;
                    }
                }
            }
            CloseSqL(sqlconnection);
            return null;
        }
        public static int LeaveServer(int serverid, int acc) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string newuserstring = "";
            bool destroy = false;
            {
                string? users = null;
                OpenSql(sqlconnection);
                {
                    string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            users = reader["users"].ToString();
                        }
                    }
                }
                if (users == null)
                {
                    return 0;
                }
               
                string[] usersplit = users.Split('❷');
                for (int i = 0; i < usersplit.Length; ++i)
                {
                    if (usersplit[i].Contains("❶") == true)
                    {
                        if (int.Parse(usersplit[i].Split('❶')[0]) != acc)
                        {
                            newuserstring += usersplit[i] + "❷";
                        }
                        else
                        {
                            if (usersplit[i].Split('❶')[1].Split('/').ToList().Contains("1") == true)
                            {
                                if (usersplit.Length <= 2)
                                {
                                    //delete entire server
                                    destroy = true;   
                                }
                                else
                                {
                                    {
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(acc) != null)
                                            {
                                                SendPackets.SendMessage(NetworkManager.GetClientByAccountId(acc).id, "You must transfer ownership before leaving a server with other users in it.");
                                            }
                                        });
                                        ReceivePackets.RunOnPrimaryActionThread(a);
                                    }
                                    return 0;
                                }
                            }
                        }
                    }
                }
            }
         
            string newserverstring = "";
            {
                string? servers = null;
                {
                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", acc);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            servers = reader["servers"].ToString();
                        }
                    }
                }
                if (servers == null)
                {
                    return 0;
                }
                string[] serversplit = servers.Split('❷');
                for (int i = 0; i < serversplit.Length; ++i)
                {
                    if (String.IsNullOrWhiteSpace(serversplit[i]) == false && int.Parse(serversplit[i]) != serverid)
                    {
                        newserverstring += serversplit[i] + "❷";
                    }
                }
            }

            //update server user list
            {
         
                string query = String.Format("UPDATE servers SET users=@users WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@users", newuserstring);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return 0;
                }
                 
            }
            //update user servers list
           
            {
              
                string query = String.Format("UPDATE users SET servers=@servers WHERE id = '{0}'", acc);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@servers", newserverstring);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    return 0;
                }
                 
            }
            CloseSqL(sqlconnection);
            return destroy ? 2 : 1;
        }
        public static bool DestroyServer(int serverid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            {
                string? users = null;
                OpenSql(sqlconnection);
                {
                    string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            users = reader["users"].ToString();
                        }
                    }
                }
                if (users == null)
                {
                    return false;
                }
                if (String.IsNullOrWhiteSpace(users))
                {
                    string serverfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), serverid.ToString());
                    try
                    {
                        if (Directory.Exists(serverfolder))
                        {
                            Directory.Delete(serverfolder, true);
                        }
                    }
                    catch
                    {
                        return false;
                    }
                    string seltodel = String.Format("DELETE  FROM servers WHERE id = '{0}'", serverid);
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.CommandText = seltodel;
                    cmd.Connection = sqlconnection;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);
                        return false;
                    };
                }
                CloseSqL(sqlconnection);
                return true;
            }
        }
        public static string? RemoveUserRole(int acc, int serverid, int roleid)
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? users = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        users = reader["users"].ToString();
                    }
                }
            }
            if (users == null)
            {
                return null;
            }
            string newuserdatachunk = "";
            string[] usersplit = users.Split('❷');
            for (int i = 0; i < usersplit.Length; ++i)
            {
                if (usersplit[i].Contains("❶"))
                {
                    string[] usersplitinternal = usersplit[i].Split('❶');
                    if (int.Parse(usersplitinternal[0]) == acc) // if this is the user
                    {
                        string roledatachunk = "";
                        string[] rolesplit = usersplitinternal[1].Split('/');
                        for (int ii = 0; ii < rolesplit.Length; ++ii)
                        {
                            if (String.IsNullOrWhiteSpace(rolesplit[ii]) == false)
                            {
                                if (int.Parse(rolesplit[ii]) != roleid)
                                {
                                    roledatachunk += rolesplit[ii] + "/";
                                }
                            }
                        }
                        newuserdatachunk += usersplitinternal[0] + "❶" + roledatachunk + "❷";

                    }
                    else
                    {
                        newuserdatachunk += usersplit[i] + "❷";
                    }
                }
            }
     
            string query = String.Format("UPDATE servers SET users=@users WHERE id = '{0}'", serverid);
            MySqlCommand curcommand = new MySqlCommand();
            curcommand.CommandText = query;
            curcommand.Parameters.AddWithValue("@users", newuserdatachunk);
            curcommand.Connection = sqlconnection;
            try
            {
                curcommand.ExecuteNonQuery();
            }
            catch
            {
                CloseSqL(sqlconnection);
                return null;
            }
            CloseSqL (sqlconnection);
             
            return newuserdatachunk;
        }
        public static object[]? GetServerJoinStuff(int[] hostclient, int roomid) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            string? channels = null;
            string? users = null;
            string? roles = null;
            OpenSql(sqlconnection);
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", roomid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        channels = reader["channels"].ToString();
                        users = reader["users"].ToString();
                        roles = reader["roles"].ToString();
                    }
                }

            }

            string channelstringtosend = "";
            string[] channelssplit = channels.Split('❷');
            string[] userssplit = users.Split('❷');
            int? primarytextchannel = null;

            int messages = 0;
            int messagesatatime = 0;

            string userstring = "";
            if (users == null)
            {
                CloseSqL(sqlconnection);
                return null;
            }
            for (int i = 0; i < userssplit.Length; ++i)
            {
                if (String.IsNullOrEmpty(userssplit[i]) == false)
                //get this users name by account id
                {
                    string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", int.Parse(userssplit[i].Split('❶')[0]));
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                    string? username = null;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            username = reader["name"].ToString();
                        }
                    }
                    if (username == null)
                    {
                        CloseSqL(sqlconnection);
                        return null;
                    }
                    userstring += userssplit[i] + "❶" + (string)username + "❷";
                }
            }
            CloseSqL(sqlconnection);
            if (channels != null && users != null)
            {
                string path = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), roomid.ToString());

                for (int i = 0; i < channelssplit.Length; i++)
                {
                    if (channelssplit[i].Contains("❶") == true)
                    {
                        string textpath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), roomid.ToString() + "\\" + channelssplit[i][0] + ".txt");
                        string[] channelssplitinternal = channelssplit[i].Split('❶');
                        if (channelssplitinternal[1] == "0")
                        {
                        restartthis:;
                            int tries = 0;
                            if (System.IO.File.Exists(textpath) == false)
                            {
                                tries++;

                                File.Create(textpath);
                                goto restartthis;
                            }
                            int channelmessagecountfull = File.ReadAllLines(textpath).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray().Length;
                            if (primarytextchannel == null)
                            {
                                primarytextchannel = int.Parse(channelssplitinternal[0]);
                                messages = channelmessagecountfull;
                                messagesatatime = (channelmessagecountfull > 30 ? 30 : channelmessagecountfull);
                            }
                            channelstringtosend += channelssplitinternal[0] + "❶" + channelssplitinternal[2] + "❶" + channelssplitinternal[1] + "❶" + channelmessagecountfull + "❶" + (channelmessagecountfull > 30 ? 30 : channelmessagecountfull) + "❶" + channelssplitinternal[3] + "❶" + channelssplitinternal[4] + "❶" + channelssplitinternal[5] + "❷";
                        }
                        else
                        {
                            channelstringtosend += channelssplitinternal[0] + "❶" + channelssplitinternal[2] + "❶" + channelssplitinternal[3] + "❶" + channelssplitinternal[4] + "❷";
                        }
                    }
                }
            }
            string? roomstring = GetUserRooms(hostclient[1]);
            
            if (roomstring != null)
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        if (String.IsNullOrWhiteSpace(roomstring) == false)
                        {
                            SendPackets.SendToUserTheirRooms(hostclient[0], System.Text.Encoding.UTF8.GetBytes(roomstring));
                        }
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            
            if (channels != null && primarytextchannel != null)
            {
                return new object[] { roomid, true, (int)primarytextchannel, messages, messagesatatime, channelstringtosend, System.Text.Encoding.UTF8.GetBytes(userstring), System.Text.Encoding.UTF8.GetBytes(roles) };
            }
            return null;
        }
        public static void CreateChannel(int serverid, string channelname, bool text, int[] hostclient, int channeltype) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string? channels = null;
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        channels = reader["channels"].ToString();
                    }
                }
            }

            if (channels == null)
            {
                CloseSqL(sqlconnection);
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            
                            SendPackets.SendMessage(hostclient[0], "This server does not exist.");
                            SendPackets.SendCloseLoading(hostclient[0], "Creating channel...", 0);
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return;
            }
            int channelid = channels.Contains("❷") ? int.Parse(channels.Split('❷')[channels.Count(x => x == '❷') - 1].Split('❶')[0]) + 1 : 0;
            string textpath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), serverid.ToString() + "\\" + channelid + ".txt");
            try
            {
                if (text == true)
                {
                    if (File.Exists(textpath) == true)
                    {
                        File.Delete(textpath);
                    }
                    using (FileStream fs = File.Create(textpath)) { }
                }
            }
            catch
            {
                CloseSqL(sqlconnection);
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            
                            SendPackets.SendMessage(hostclient[0], "Could not create channel (2).");
                            SendPackets.SendCloseLoading(hostclient[0], "Creating channel...", 0);
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return;
            }
            string addedstring = channelid + "❶" + channeltype + "❶" + channelname + "❶0/❶0❶0❷";
            {

                string query = String.Format("UPDATE servers SET channels=@channels WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@channels", channels + addedstring);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                
                                SendPackets.SendMessage(hostclient[0], "Unable to insert channel into database.");
                                SendPackets.SendCloseLoading(hostclient[0], "Creating channel...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return;
                }
            }
            CloseSqL (sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        
                        SendPackets.SendCloseLoading(hostclient[0], "Creating channel...", 0);
                        SendPackets.SendBackSavedServerChannels(serverid, System.Text.Encoding.UTF8.GetBytes(channels + addedstring));
                        for (int i = 0; i < NetworkManager.ServerMessages.Count; ++i)
                        {
                            if (NetworkManager.ServerMessages[i].serverid == serverid)
                            {
                                NetworkManager.ServerMessages[i].msgcountbychannel[channelid] = 0;
                            }
                        }
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            
        }
        public static void DeleteChannel(int serverid, int channelid, int[] hostclient) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));
            OpenSql(sqlconnection);
            string? channels = null;
            {
                string selection = String.Format("SELECT * FROM servers WHERE id = '{0}'", serverid);
                MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        channels = reader["channels"].ToString();
                    }
                }
            }
            if (channels == null)
            {
                CloseSqL(sqlconnection);
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            SendPackets.SendMessage(hostclient[0], "This server does not exist.");
                            SendPackets.SendCloseLoading(hostclient[0], "Deleting channel...", 0);
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return;
            }
            string newchannelstring = "";
            string[] csplit = channels.Split('❷');
            int channelsforall = 0;
            for (int i = 0; i < csplit.Length; ++i)
            {
                if (csplit[i].Contains("❶"))
                {
                    string[] internalsplit = csplit[i].Split('❶');
                    if (int.Parse(internalsplit[0]) != channelid)
                    {
                        if (internalsplit[3].Contains("/") && internalsplit[3].Split('/').ToList().Contains("0") && internalsplit[1] == "0")
                        {
                            channelsforall++;
                        }
                        newchannelstring += csplit[i] + "❷";
                    }
                }
            }
            if(channelsforall == 0)
            {
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            SendPackets.SendMessage(hostclient[0], "Please leave at least one text channel for all.");
                            SendPackets.SendCloseLoading(hostclient[0], "Deleting channel...", 0);
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return;
            }
            string textpath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), serverid.ToString() + "\\" + channelid + ".txt");
            try
            {
                if (File.Exists(textpath) == true)
                {
                    File.Delete(textpath);
                }
            }
            catch
            {   
                CloseSqL(sqlconnection);
                
                {
                    Action a = (Action)(() =>
                    {
                        if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                        {
                            SendPackets.SendMessage(hostclient[0], "Could not delete channel.");
                            SendPackets.SendCloseLoading(hostclient[0], "Deleting channel...", 0);
                        }
                    });
                    ReceivePackets.RunOnPrimaryActionThread(a);
                }
                
                return;
            }
        
            {

                string query = String.Format("UPDATE servers SET channels=@channels WHERE id = '{0}'", serverid);
                MySqlCommand curcommand = new MySqlCommand();
                curcommand.CommandText = query;
                curcommand.Parameters.AddWithValue("@channels", newchannelstring);
                curcommand.Connection = sqlconnection;
                try
                {
                    curcommand.ExecuteNonQuery();
                }
                catch
                {
                    CloseSqL(sqlconnection);
                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                SendPackets.SendMessage(hostclient[0], "Unable to remove channel from database.");
                                SendPackets.SendCloseLoading(hostclient[0], "Deleting channel...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    
                    return;
                }
            }
            CloseSqL(sqlconnection);
            
            {
                Action a = (Action)(() =>
                {
                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                    {
                        SendPackets.SendCloseLoading(hostclient[0], "Deleting channel...", 0);
                        SendPackets.SendBackSavedServerChannels(serverid, System.Text.Encoding.UTF8.GetBytes(newchannelstring));
                        for (int i = 0; i < NetworkManager.ServerMessages.Count; ++i)
                        {
                            if (NetworkManager.ServerMessages[i].serverid == serverid)
                            {
                                NetworkManager.ServerMessages[i].msgcountbychannel[channelid] = 0;
                            }
                        }
                    }
                });
                ReceivePackets.RunOnPrimaryActionThread(a);
            }
            

        }
        public static void CreateRoom(string name, string seed, int[] hostclient /* 0 is public | 1 is acc*/, string md5) 
        {
            MySqlConnection sqlconnection = new MySqlConnection(String.Format("SERVER={0}; port={1}; user id={2}; PASSWORD={3}; DATABASE={4};CharSet={5}", _host, _port, _name, _pass, _database, _charset));

            new Thread(delegate ()
            {
                OpenSql(sqlconnection);
                {
                    string selection = String.Format("SELECT * FROM servers WHERE name = '{0}'", name);
                    MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CloseSqL(sqlconnection);

                            
                            {
                                Action a = (Action)(() =>
                                {
                                    if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                    {
                                        SendPackets.SendMessage(hostclient[0], "Unable to create room (1).");
                                        SendPackets.SendCloseLoading(hostclient[0], "Creating room...", 0);
                                    }
                                });
                                ReceivePackets.RunOnPrimaryActionThread(a);
                            }
                            

                            return;
                        }
                    }
                }
                {
                    string query = "INSERT INTO servers (name, encryptionseed, channels, users, pic,roles) values(@name ,@encryptionseed ,@channels,@users,@pic,@roles)";
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.CommandText = query;
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@encryptionseed", seed);
                    cmd.Parameters.AddWithValue("@channels", "0❶0❶Text A❶0/❶0❶0❷1❶0❶Text B❶0/❶0❶0❷2❶1❶Voice❶0/❶0❷");
                    cmd.Parameters.AddWithValue("@users", hostclient[1] + "❶" + "0/1/" + "❷");
                    cmd.Parameters.AddWithValue("@pic", md5);
                    cmd.Parameters.AddWithValue("@roles", "0❶Member❶0❶0❶0❶0❶0❶0❶#FFFFFF❶0❷1❶Owner❶1❶1❶1❶1❶1❶1❶#FFFFFF❶1❷");


                    cmd.Connection = sqlconnection;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        CloseSqL(sqlconnection);

                        
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                SendPackets.SendMessage(hostclient[0], "Unable to create room (2).");
                                SendPackets.SendCloseLoading(hostclient[0], "Creating room...", 0);
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                        

                        return;
                    };
                }
                {
                    string? channels = null;
                    string? roles = null;
                    int? serverid = null;
                    {
                        string selection = String.Format("SELECT * FROM servers WHERE name = '{0}'", name);
                        MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                roles = reader["roles"].ToString();
                                channels = reader["channels"].ToString();
                                serverid = int.Parse(reader["id"].ToString());
                            }
                        }
                    }

                    
                    {
                        Action a = (Action)(() =>
                        {
                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                            {
                                NetworkManager.GetClientByAccountId(hostclient[1]).CurrentSavedServerID = serverid;
                            }
                        });
                        ReceivePackets.RunOnPrimaryActionThread(a);
                    }
                    

                    if (NetworkManager.serverthreads.Any(x => x.GetType() == typeof(ServerWithThreads) && (x as ServerWithThreads).serverid == serverid) == false)
                    {
                        NetworkManager.serverthreads.Add(new ServerWithThreads((int)serverid, true));
                    }
                    if (NetworkManager.ChannelManagers.Any(x => x.GetType() == typeof(OnFlyChannelManager) && (x as OnFlyChannelManager).ServerID == serverid && (x as OnFlyChannelManager).roomtype == 1) == false)
                    {
                        NetworkManager.ChannelManagers.Add(new OnFlyChannelManager(1, (int)serverid));
                    }
                    //update users servers section
                    {
                        string selection = String.Format("SELECT * FROM users WHERE id = '{0}'", hostclient[1]);
                        MySqlCommand cmd = new MySqlCommand(selection, sqlconnection);
                        string? servers = null;
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                servers = reader["servers"].ToString();
                            }
                        }
                        if (servers == null)
                        {
                            CloseSqL(sqlconnection);
                            
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    SendPackets.SendMessage(hostclient[0], "Unable to create room (4).");
                                    SendPackets.SendCloseLoading(hostclient[0], "Creating room...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                            
                            return;
                        }
                        string query = String.Format("UPDATE users SET servers=@servers WHERE id = '{0}'", hostclient[1]);
                        MySqlCommand curcommand = new MySqlCommand();
                        curcommand.CommandText = query;
                        curcommand.Parameters.AddWithValue("@servers", (servers + serverid + "❷"));
                        curcommand.Connection = sqlconnection;
                        try
                        {
                            curcommand.ExecuteNonQuery();
                        }
                        catch (System.Exception ex)
                        {
                            CloseSqL(sqlconnection);
                            
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    SendPackets.SendMessage(hostclient[0], "Unable to create room (5).");
                                    SendPackets.SendCloseLoading(hostclient[0], "Creating room...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                            
                            return;
                        }
                    }
                    CloseSqL(sqlconnection);
                     

                    string channelstringtosend = "";
                    string[] channelssplit = channels.Split('❷');
                    int? primarytextchannel = null;
                    if (serverid != null && channels != null)
                    {
                        string path = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), serverid.ToString());

                        System.IO.Directory.CreateDirectory(path);
                        for (int i = 0; i < channelssplit.Length; i++)
                        {
                            if (channelssplit[i].Contains("❶") == true)
                            {
                                string textpath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), serverid.ToString() + "\\" + channelssplit[i][0] + ".txt");
                                try
                                {
                                    if (channelssplit[i][2] == '0')
                                    {
                                        if (primarytextchannel == null)
                                        {
                                            primarytextchannel = int.Parse(channelssplit[i][0].ToString());
                                        }
                                        using (FileStream fs = File.Create(textpath)) { }
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Error creating channel text file");
                                }
                                string[] channelssplitinternal = channelssplit[i].Split('❶');
                                if (channelssplit[i][2] == '0')
                                {
                                    if (System.IO.File.Exists(textpath) == false)
                                    {
                                        
                                        Action a = (Action)(() =>
                                        {
                                            if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                            {
                                                SendPackets.SendMessage(hostclient[0], "Unable to create room (6).");
                                                SendPackets.SendCloseLoading(hostclient[0], "Creating room...", 0);
                                            }
                                        });
                                        ReceivePackets.RunOnPrimaryActionThread(a);
                                        
                                        return;
                                    }


                                    int channelmessagecountfull = File.ReadAllLines(textpath).Where(x => String.IsNullOrWhiteSpace(x) == false && x != "\n").ToArray().Length;
                                    channelstringtosend += channelssplitinternal[0] + "❶" + channelssplitinternal[2] + "❶" + channelssplitinternal[1] + "❶" + channelmessagecountfull + "❶" + (channelmessagecountfull > 30 ? 30 : channelmessagecountfull) + "❶" + channelssplitinternal[4] + "❶" + channelssplitinternal[5] + "❷";
                                }
                                else
                                {
                                    channelstringtosend += channelssplitinternal[0] + "❶" + channelssplitinternal[2] + "❶" + channelssplitinternal[4] + "❷";

                                }
                            }

                        }

                    }
                    if (channels != null && primarytextchannel != null)
                    {
                        string? roomstring = GetUserRooms(hostclient[1]);
                        
                        if (roomstring != null)
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    if (String.IsNullOrWhiteSpace(roomstring) == false)
                                    {
                                        SendPackets.SendToUserTheirRooms(hostclient[0], System.Text.Encoding.UTF8.GetBytes(roomstring));
                                    }
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        

                        
                        {
                            Action a = (Action)(() =>
                            {
                                if (NetworkManager.GetClientByAccountId(hostclient[1]) != null && NetworkManager.GetClientByAccountId(hostclient[1]).id == hostclient[0])
                                {
                                    SendPackets.AddOrRemoveUserFromList(1, NetworkManager.GetClientByAccountId(hostclient[1]), "1", (int)serverid, true);
                                    SendPackets.SavedRoomCreationSuccess(hostclient[0], System.Text.Encoding.UTF8.GetBytes(channelstringtosend), (int)primarytextchannel, (int)serverid, System.Text.Encoding.UTF8.GetBytes(roles));
                                    SendPackets.SendCloseLoading(hostclient[0], "Creating room...", 0);
                                }
                            });
                            ReceivePackets.RunOnPrimaryActionThread(a);
                        }
                        
                    }
                }
            }).Start();
        }
    }
}
