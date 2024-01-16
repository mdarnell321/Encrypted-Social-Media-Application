using ESMA;
using ESMA.Properties;
using NAudio.CoreAudioApi;
using NAudio.Mixer;
using NAudio.Wave;
using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ESMA.MainWindow;

namespace ESMA
{
    public class ChatManager
    {
        public static List<IWaveIn> wave_in_list = new List<IWaveIn>();
        public static List<WaveOutEvent> wave_out_list = new List<WaveOutEvent>();
        public static List<BufferedWaveProvider> provider_list = new List<BufferedWaveProvider>();
        public static Hashtable channels = new Hashtable();
        public static List<UserList> UserList_FULL = null;
        public static List<UserList> UserList = new List<UserList>();
        public static List<UserDataCache> UserCache = new List<UserDataCache>();
        public static string selectedencryptionseed;
        public static string selectedroompass;
        public static byte[] selectedroompassasbytesprejoin;
        public static byte[] selectedroompassasbytes;
        public static byte[] selectedroompassasbytes2;
        public static bool StillFetchingMessages;
        public static bool StillLoadingMessages;
        public static bool StillGettingVerificationOfJoinRoom;
        public static int CurrentChannel;
        public static UserList my_room_user;
        public static int? CurrentSavedServer = null;
        public static int? CurTempRoom = null;
        public static int? CurrentChatWithUser = null;
        public static string CurrentChatWithUserKey = null;
        public static string MyUserPrivateKey = null;
        public static string MyUserPublicKey = null;
        public static List<ServerDirectoryElements> MyServers = new List<ServerDirectoryElements>();
        public static List<SavedServersOnMemory> SavedServers = new List<SavedServersOnMemory>();
        public static UserList UserInCallWith = null;
        public static UserList MyTempuserWhileinCall = null;
        public static int? PotentialUserInCallWith = null;
        public static List<RolesList> RoleList = new List<RolesList>();

        public static UserList GetChatUserByID(int account_ID) // find online room user by account id
        {
            for (int i = 0; i < UserList.Count; ++i)
            {
                if (UserList[i].AccountID == account_ID)
                {
                    return UserList[i];
                }
            }
            return null;
        }
        public static UserList GetAnyChatUserByID(int account_ID) // find online/offline room user by account id
        {
            for (int i = 0; i < UserList_FULL.Count; ++i)
            {
                if (UserList_FULL[i].AccountID == account_ID)
                {
                    return UserList_FULL[i];
                }
            }
            return null;
        }
        public static UserDataCache GetUserCacheByID(int account_ID)  // find users cache element by account id
        {
            for (int i = 0; i < UserCache.Count; ++i)
            {
                if (UserCache[i].account_ID == account_ID)
                {
                    return UserCache[i];
                }
            }
            return null;
        }
        public static SavedServersOnMemory GetSavedServerByID(int id) //find saved server by id
        {
            for (int i = 0; i < SavedServers.Count; ++i)
            {
                if (SavedServers[i].ID == id)
                {
                    return SavedServers[i];
                }
            }
            return null;
        }
    }


}
public class SavedServersOnMemory
{
    public int ID;
    public Hashtable channels = new Hashtable();
    public SavedServersOnMemory(int i)
    {
        ID = i;
    }
}

public class UserDataCache
{
    public int account_ID;
    public string profilemd5;
    public ImageSource profilepic = null;
    public UserDataCache(int a, string p)
    {
        account_ID = a;
        profilemd5 = p;
        SaveProfilePicToCacheInFiles(0);
    }
    public UserDataCache()//for temp
    {
        account_ID = -1;
        profilemd5 = "";
    }
    public void SaveProfilePicToCacheInFiles(int tries)
    {
        if (tries >= 5)
        {
            profilepic = MainWindow.DefaultProfilePicture;
            return;
        }
        string webString = "";
        try
        {
            using (System.Net.WebClient webclient = new System.Net.WebClient())
            {
                webString = webclient.DownloadString(ESMA.DatabaseCalls.host + "/prof/" + profilemd5);
            }
            if (String.IsNullOrEmpty(webString) == true)
            {
                throw new Exception();
            }
        }
        catch //fail to retrieve
        {
            SaveProfilePicToCacheInFiles(tries + 1);
            return;
        }
        string exportfile_t = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + profilemd5);
        if (File.Exists(exportfile_t) == false || File.Exists(exportfile_t) == true && ESMA.Methods.GetFileMD5Signature(exportfile_t) != profilemd5) //Save file in cache
        {
            try
            {
                if (File.Exists(exportfile_t))
                {
                    File.Delete(exportfile_t);
                }
                File.WriteAllBytes(exportfile_t, Convert.FromBase64String(webString));
            }
            catch //failed writing file to cache
            {
                SaveProfilePicToCacheInFiles(tries + 1);
                return;
            }
        }
        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
        {
            profilepic = new BitmapImage(new Uri(exportfile_t));
        });
      
        if (ChatManager.GetChatUserByID(account_ID) != null)
        {
            for (int i = 0; i < ChatManager.GetChatUserByID(account_ID).ProfileImages.Count; i++)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                { 
                    ChatManager.GetChatUserByID(account_ID).ProfileImages[i].Source = profilepic;
                });
            }
        }
        
       
        if (account_ID == NetworkManager.MyAccountID)
        { 
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
            {  
                MainWindow.instance.Lobby_ProfilePic.Source = profilepic;
                MainWindow.instance.EditProfilePImage.Source = profilepic;
            });
        } 
    }
}
[System.Serializable]
public class Message
{
    
    public bool initializers;
    public string sender { get; set; }
    public string message { get; set; }
    public int accountid { get; set; }
    public int idofmessage { get; set; }
    public bool loaded;
    public string profilepicmd5 { get; set; }
    public string date { get; set; }
    public Visibility Visible { get; set; }
    public Message(int i, string s, string m, int idofmsg, bool _loaded, bool _visible, string datetime)
    {
        accountid = i;
        sender = s;
        message = m;
        idofmessage = idofmsg;
        loaded = _loaded;
        Visible = _visible == true ? Visibility.Visible : Visibility.Collapsed;
        string[] thesplit = datetime.Split('/');
        date = String.Format("{0}/{1}/{2} at {3}:{4} {5}", thesplit[0], thesplit[1], thesplit[2].Substring(thesplit[2].Length-2,2), (int.Parse(thesplit[3]) > 12 ? int.Parse(thesplit[3]) - 12: int.Parse(thesplit[3])), int.Parse(thesplit[4]).ToString("00"), int.Parse(thesplit[3]) > 12 ? "PM" : "AM");
    }
}
[System.Serializable]
public class ImageMessage : Message
{
    public string MyImage { get; set; }

    public string MyImage_Thumbnail { get; set; }
    public string ImageCachePath_Thumbnail { get; set; }
    public ImageMessage(int i, string s, string m, int idofmsg, string md5, string cache, string md5_t, string cache_t, string datetime) : base(i, s, m, idofmsg, true, true, datetime)
    {
        MyImage = md5;
        
        MyImage_Thumbnail = md5_t;
        ImageCachePath_Thumbnail = cache_t;
    }
}
[System.Serializable]

public class VideoMessage : Message
{
    public string MyVideo { get; set; }

    public string MyImage_Thumbnail { get; set; }
    public string ImageCachePath_Thumbnail { get; set; }
    public VideoMessage(int i, string s, string m, int idofmsg, string md5, string cache, string md5_t, string cache_t, string datetime) : base(i, s, m, idofmsg, true, true, datetime)
    {
        MyVideo = md5;
    
        MyImage_Thumbnail = md5_t;
        ImageCachePath_Thumbnail = cache_t;
    }
}
[System.Serializable]
public class AudioMessage : Message
{
    public string MyAudio { get; set; }
    public string AudioCachePath { get; set; }
    public bool FileExists { get; set; }

    public Slider TimeSlider;
    public MediaElement MediaSource;
    public bool isMediaPaused;
    public bool isseeking;
    public Slider VolumeSlider;
    public AudioMessage(int i, string s, string m, int idofmsg, string md5, string cache, string datetime) : base(i, s, m, idofmsg, true, true, datetime)
    {
        MyAudio = md5;
        AudioCachePath = cache;
    }
}
[System.Serializable]
public class FileMessage : Message
{
    public string MyFile { get; set; }
    public string Extension { get; set; }
    public long Size { get; set; }

    public string SizeStringDisplay { get; set; }
public FileMessage(int i, string s, string m, int idofmsg, string md5, string extension, long size, string datetime) : base(i, s, m, idofmsg, true, true, datetime)
    {
        MyFile = md5;
        Extension = extension;
        Size = size;
        SizeStringDisplay = String.Format("Download File.{0} ({1} bytes)",extension,size);
    }
}
[System.Serializable]
public class ChannelRoot
{
    public int key;
    public ChannelRoot(int k)
    {
        key = k;
    }
}

    [System.Serializable]
public class ChannelsForTempRoom : ChannelRoot
{
    public ChannelsForTempRoom(string name, int k) : base(k)
    {
        ChannelName = name;
        initialized = false;
        MessagesFetched = 0;
        MessagesToFetched = 0;
        MessagesToFetchTotal = 0;
        MessagesToFetchedSearch = 0;
        MessagesFetchedSearch = 0;
        messages = new List<Message>();
        searchedmessages = new List<Message>();
    }
    public bool initialized;
    public bool read_only;
    public bool incoming_users;
    public List<int> Roles = new List<int>();
    public int MessagesFetched;
    public int MessagesToFetched;
    public int MessagesToFetchTotal;
    public int HighestMessageIdOnInitialization;
    //search method
    public int MessagesToFetchedSearch;
    public int MessagesFetchedSearch;
    public string ChannelName { get; set; }
    public List<Message> messages;
    public List<Message> searchedmessages;


    
}
[System.Serializable]
public class SavedChannelsForTempRoom : ChannelsForTempRoom
{
    public SavedChannelsForTempRoom(string name, int k) : base (name,k)
    {
        savedmessages = new List<Message>();
    }
    public List<Message> savedmessages;
}
[System.Serializable]
public class VoiceChannelsForTempRoom : ChannelRoot
{
    public VoiceChannelsForTempRoom(string name, int k) : base(k)
    {
        ChannelName = name;
        UsersInRoom = new List<UserList>();
    }
    public List<int> Roles = new List<int>();
    public string ChannelName { get; set; }
    public bool read_only;
    public List<UserList> UsersInRoom { get; set; }
    public ListBox ListBoxItem;
}


[System.Serializable]
public class UserList
{
    public UserList(string name, int account_ID)
    {
        Username = name;
        AccountID = account_ID;
        ActivityColor = System.Windows.Media.Brushes.Black ;
        TextColor = System.Windows.Media.Brushes.White;
    }
    public UserList() // only used for temps
    {
        Username = "";
        AccountID = -1;
        ActivityColor = System.Windows.Media.Brushes.Black;
        TextColor = System.Windows.Media.Brushes.White;
    }
    ~UserList()
    {
        StopTransmission();
    }
    public string Username { get; set; }
    public int PublicID;
    public int AccountID;
    public bool me { get; set; }
    public List<int> _Roles = new List<int>();
    public List<Image> ProfileImages = new List<Image>();
    public List<TextBlock> ProfileNames = new List<TextBlock>();
    //voice chat
    //in
    public IWaveIn wave_in;
    public List<byte> buffered = new List<byte>();

    //out
    public WaveOutEvent wave_out;
    public BufferedWaveProvider provider;

    //channel voice stuff
    public TextBlock TransmissionStatus = null;
    public int TransmissionIconTimer = 0;
    public int? CurrentVoiceChannel = null;
    private bool samplesgood = false;
    private int lasttimeofvoice = 0;
    //misc
    public System.Windows.Media.SolidColorBrush ActivityColor { get; set; }
    public System.Windows.Media.SolidColorBrush TextColor { get; set; }
    public void StartReceivingVoice()
    {
        wave_out = new WaveOutEvent();
        NetworkManager.TaskTo_PrimaryActionThread(() =>
        {
            ChatManager.wave_out_list.Add(wave_out);
        });
        provider = new BufferedWaveProvider(new WaveFormat());
        NetworkManager.TaskTo_PrimaryActionThread(() =>
        {
            ChatManager.provider_list.Add(provider);
        });
        wave_out.Init(provider);
    }
    public void StartTransmittingVoice()
    {
        if (NAudio.Wave.WaveInEvent.DeviceCount > 0)
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            wave_in = new NAudio.Wave.WaveInEvent{DeviceNumber = 0, WaveFormat = new WaveFormat(), BufferMilliseconds = 10};
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                ChatManager.wave_in_list.Add(wave_in);
            });
            wave_in.DataAvailable += new EventHandler<WaveInEventArgs>(this.ReceivedInputData);
            if (AccountID == NetworkManager.MyAccountID)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    Button sender = MainWindow.instance.MicOptionsButton;
                    if (MainWindow.Mic_Status == MicStatus.Available)
                    {
                        MainWindow.Mic_Status = MicStatus.Active;
                        sender.Content = "🔊";
                    }
                });
            }
        }
    }


    public void StopTransmission()
    {
     
        if (wave_in != null)
        {
            wave_in.StopRecording();
            wave_in.Dispose();
            if (AccountID == NetworkManager.MyAccountID)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    Button sender = MainWindow.instance.MicOptionsButton;
                    if (MainWindow.Mic_Status == MicStatus.Active)
                    {
                        MainWindow.Mic_Status = MicStatus.Available;
                        sender.Content = "🔈";
                    }
                });
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                ChatManager.wave_in_list.Remove(wave_in);
            });
            wave_in = null;
         
        }
        if (wave_out != null)
        {
            wave_out.Stop();
            wave_out.Dispose();
         
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                ChatManager.wave_out_list.Remove(wave_out);
            });
            wave_out = null;
        }
        if (provider != null)
        {
            provider.ClearBuffer();
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                ChatManager.provider_list.Remove(provider);
            });
            provider = null;
        }
    
  
        
    }
    DateTime last_valid_sound = DateTime.Now;
    private void ReceivedInputData(object sender, WaveInEventArgs e)
    {
        if(MainWindow.Mic_Status == MicStatus.Active)
        {
            try
            {
                if (e.BytesRecorded > 0)
                {
                    byte[] encrypted_data = Methods.EncryptOrDecrypt(e.Buffer, ChatManager.selectedroompassasbytes2);
                    if (encrypted_data == null)
                        return;
                    buffered.AddRange(encrypted_data);
                }
                if (buffered.Count > 512) //once the buffer reaches a length of 2048 send it to the server
                {
                    if (ChatManager.UserInCallWith != null)
                    {
                        SendPackets.SendVoiceBytesInCall(buffered);
                    }
                    else if (ChatManager.CurTempRoom != null || ChatManager.CurrentSavedServer != null)
                    {
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                TransmissionIconTimer = 15;
                                if (TransmissionStatus != null)
                                {
                                    TransmissionStatus.Text = "🔊";
                                }
                            });
                            SendPackets.SendVoiceBytes(buffered, (int)CurrentVoiceChannel, ChatManager.CurrentSavedServer ?? -1);
                    }
                    buffered.Clear();
                }
            }
            catch { }
        }
    }
}
public class RolesList
{
    public int id;
    public string rolename { get; set; }
    public int[] powers;
    public string Hex;
    public int precedence;

    public RolesList(int id, string rolename, int[] powers /*group of 6*/, string hex, int _precedence)
    {
        this.id = id;
        this.rolename = rolename;
        this.powers = powers;
        this.Hex = hex;
        this.precedence = _precedence;
    }
}


public class ServerDirectoryElements
{
    public int elementtype;
    public string name { get; set; }
    public string md5 { get; set; }
    public ImageSource image { get; set; }
    public ServerDirectoryElements(int et, string n, string p)
    {
        elementtype = et;
        name = n;
        md5 = p;
        Start(0);
    }
    public void Start(int tries)
    {
        if(md5 == "")
        {
            if(elementtype == -1)
            {
                image = new BitmapImage(new Uri("pack://application:,,,/Resources/add.png")) as ImageSource;
            }
            else if (elementtype == -2)
            {
                image = new BitmapImage(new Uri("pack://application:,,,/Resources/friends.png")) as ImageSource;
            }
            else
            {
                image = MainWindow.UndefinedSource as ImageSource;
            }
         
            return;
        }
        string exportfile_t = Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + md5);
        if (File.Exists(exportfile_t) == true && ESMA.Methods.GetFileMD5Signature(exportfile_t) == md5) //Save file in cache
        {
            image = new BitmapImage(new Uri(exportfile_t));
            return;
        }
        if (tries >= 5)
        {
            image = MainWindow.UndefinedSource as ImageSource;
            return;
        }
        string webString = "";
        try
        {
            using (System.Net.WebClient webclient = new System.Net.WebClient())
            {
                webString = webclient.DownloadString(ESMA.DatabaseCalls.host + "/serverpics/" + md5);
            }
            if (String.IsNullOrEmpty(webString) == true)
            {
                throw new Exception();
            }
        }
        catch //fail to retrieve
        {
            Start(tries + 1);
            return;
        }
      
        if (File.Exists(exportfile_t) == false || File.Exists(exportfile_t) == true && ESMA.Methods.GetFileMD5Signature(exportfile_t) != md5) //Save file in cache
        {
            try
            {
                if (File.Exists(exportfile_t))
                {
                    File.Delete(exportfile_t);
                }
                File.WriteAllBytes(exportfile_t, Convert.FromBase64String(webString));
            }
            catch //failed writing file to cache
            {
                Start(tries + 1);
                return;
            }
        }
        image = new BitmapImage(new Uri(exportfile_t));
    }
}