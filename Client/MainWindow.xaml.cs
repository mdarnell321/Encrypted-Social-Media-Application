
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls; 
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using Google.Protobuf;
using System.Security.Cryptography;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties;
using NAudio.CoreAudioApi;
using System.Windows.Interop;
using System.Text;
using System.Security.Policy;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using System.Windows.Controls.Primitives;
using System.Collections;
using System.Security.Authentication.ExtendedProtection;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data.OleDb;
using Microsoft.Win32;
using System.Windows.Markup;
using Org.BouncyCastle.Crypto;
using System.Media;
using static NetVips.Enums;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Net;
using NAudio.Gui;
using System.Drawing.Imaging;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ESMA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow instance;
        public static bool isrunning;
        public static Viewbox _LoginForm;
        public static Viewbox _RegisterForm;
        public static Viewbox _TempChatRoomForm;
        public static Viewbox _PictureViewer;
        public static Viewbox _RoomForm;
        public static Viewbox _MediaViewer;
        public static MediaElement _VideoElement;
        public static TextBox _ChatInput;
        public static Slider _MovieSlider;
        public static ProgressBar _ProgressBar;
        public static ScrollViewer ChatScroll;
        public static MicStatus Mic_Status;
        public static ListView ChatList;
        public static ListView UserList;
        public static ListView ChannelList;
        private static int LastSelectedTextChannel = 1;
        public static bool FileDialogOpened;
        private bool isshiftpressed = false;
        public static int LoadMediaResult = 0;
        private static bool isMediaPaused = false;
        private bool seeking;
        public static int ChatListBoxCount = 0;
        public static int LoadedChatListBoxCount = 0;
        public static int UnconditionalChatListBoxCount = 0;
        //User Profile stuff
        public Grid UserWindow;
        public TextBlock UserProfileName;
        public TextBlock UserProfileCreationDate;
        public ListBox UserProfileMedia;
        public static BitmapImage UndefinedSource;
        public static bool msgboxopen = false;
        public static ImageSource DefaultProfilePicture = new BitmapImage(new Uri("pack://application:,,,/Resources/Screenshot_2.png")) as ImageSource;
        public static bool CanLoadMSGSWithScroll = true;
        public static bool LoadingFriends;
        public static SoundPlayer CallSound;
        public static SoundPlayer CallPendingSound;
        public static int CurPLWindow;
        public static bool AddingPeopletoRoles = false;
        public static int LastSelectedRoleID = -1;
        public static bool AddingRoleToChannel;
        public enum MicStatus
        {
            Off = 1,
            Available,
            Active
        }

        public void MessageBoxShow(string msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                MessageBoxObj.Visibility = Visibility.Visible;
                MessageBoxText.Text = msg;
                msgboxopen = true;
            });
        }
        private void CloseMSGBox(object sender, RoutedEventArgs e)
        {
            MessageBoxObj.Visibility = Visibility.Collapsed;
            msgboxopen = false;
        }
        public static void PlaySound(NotificationSound sound)
        {
            new Thread(delegate ()
            {
                switch (sound)
                {
                    case NotificationSound.Message:
                        using (System.IO.Stream stream = ESMA.Properties.Resources.message)
                        {
                            new System.Media.SoundPlayer(stream).Play();
                        }
                        break;
                    case NotificationSound.CallConnect:
                        using (System.IO.Stream stream = ESMA.Properties.Resources.callconnect)
                        {
                            new System.Media.SoundPlayer(stream).Play();
                        }
                        break;
                    case NotificationSound.CallDisconnect:
                        using (System.IO.Stream stream = ESMA.Properties.Resources.calldisconnected)
                        {
                            new System.Media.SoundPlayer(stream).Play();
                        }
                        break;
                    case NotificationSound.Online:
                        using (System.IO.Stream stream = ESMA.Properties.Resources.online)
                        {
                            new System.Media.SoundPlayer(stream).Play();
                        }
                        break;
                }
            }).Start();
        }

        public MainWindow()
        {
            InitializeComponent();


            Mic_Status = MicStatus.Available;
            instance = this;
            UndefinedSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Untitled.png"));
            _LoginForm = LoginForm;
            _RegisterForm = RegisterForm;
            _PictureViewer = PictureViewer;
            _MediaViewer = MediaViewer;
            _ProgressBar = UploadFileProgress;
            _RoomForm = TempChatRoomForm;
            _ChatInput = ChatInput;
            _VideoElement = VideoElement;
            _MovieSlider = MovieSlider;
            ChatScroll = ChatListBoxScroll;
            ChatList = ChatListBox;
            UserList = UserListBox;
            ChannelList = ChannelListBox;
            VideoElement.LoadedBehavior = MediaState.Manual;
            isrunning = true;
       
            SetFormVisibility(1);
            this.Closed += (s, a) => { System.Diagnostics.Process.GetCurrentProcess().Kill(); };
            UserProfile_MediaTextBox.PreviewKeyDown += UserProfileMediaTextInput_KeyDown;

            pic_grids.MouseWheel += Inspect_Zoom;
            pic_grids.MouseMove += Inspect_drag;
            pic_grids.MouseUp += Inspect_Up;
            pic_grids.MouseDown += Inspec_down;
            pic_grids.MouseLeave += Inspect_Leave;

            ChatInput.PreviewKeyDown += ChatInput_KeyDown;
            ChatInput.KeyUp += (s, a) => { if (a.Key == Key.Return) { canpressenteragain = true; ChatInput.IsReadOnly = false; } };
            ChatInput.LostFocus += (s, a) => { ChatInput.IsReadOnly = false; canpressenteragain = true; };
            this.KeyUp += (s, a) =>
            {
                if (a.Key == Key.RightShift) { isshiftpressed = false; }

            };
            ChatListBoxScroll.PreviewMouseUp += (s, a) =>
            {
                CanLoadMSGSWithScroll = true;
            };
            this.PreviewMouseUp += (s, a) =>
            {
                if (seeking == true)
                {
                    if (VideoElement.Source != null)
                    {
                        seeking = false;
                        VideoElement.Play();
                        isMediaPaused = false;
                        playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;


                    }
                }

            };
            this.KeyDown += (s, a) => { if (a.Key == Key.RightShift) { isshiftpressed = true; } };
            VideoElement.MediaOpened += (s, a) => { LoadMediaResult = 1; };
            VideoElement.MediaFailed += (s, a) => { LoadMediaResult = 2; };
            VideoElement.MediaEnded += (s, a) =>
            {
                isMediaPaused = true;
                playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
            };
            BackgroundWorker.Start();
        }
        private bool canpressenteragain = true;
        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {

            if (isshiftpressed == true || canpressenteragain == false)
            {
                return;
            }
            if (e.Key == Key.Return)
            {
                canpressenteragain = false;
                ChatInput.IsReadOnly = true;
                string t = ChatInput.Text;
                if (Methods.Allowed(t) == false)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Invalid message.");
                    }));
                    return;
                }
                if (String.IsNullOrWhiteSpace(t) == false)
                {
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.CurrentChatWithUser == null)
                        {
                            SendPackets.SendMessage(t, ChatManager.CurrentChannel, ChatManager.CurrentSavedServer, null);
                        }
                        else
                        {
                            SendPackets.SendMessage(t, ChatManager.CurrentChannel, ChatManager.CurrentSavedServer, true);
                            SendPackets.SendMessage(t, ChatManager.CurrentChannel, ChatManager.CurrentSavedServer, false);
                        }
                    });
                }
                ChatInput.Text = "";

            }
        }
        private void CloseTheUserProfile(object sender, RoutedEventArgs e)
        {
            UserProfileWindow.Visibility = Visibility.Hidden;
            ServerBrowserWindow.Visibility = Visibility.Hidden;
            EditProfileWindow.Visibility = Visibility.Hidden;
            MSGSearchGrid.Visibility = Visibility.Hidden;
            CreateChannel.Visibility = Visibility.Hidden;
            ManageRoles.Visibility = Visibility.Hidden;
            ManageChannels.Visibility = Visibility.Hidden;
        }
        private void CloseTheCreateChannel(object sender, RoutedEventArgs e)
        {
            CreateChannel.Visibility = Visibility.Hidden;
        }
        private void CloseTheCreateRoleWindow(object sender, RoutedEventArgs e)
        {
            CreateNewRole.Visibility = Visibility.Hidden;
        }

        public double lastChatScrollValue = 100;
        public double savedChatScrollValue = 100;

        private void ChatScroll_Change(object sender, ScrollChangedEventArgs e)
        {

            if (ChatManager.CurrentSavedServer == null && ChatManager.CurTempRoom == null && ChatManager.CurrentChatWithUser == null || ChatManager.CurrentChannel == null)
            {
                return;
            }
            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom) != null && (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count > 0 || (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom) == null)
            {
                return;
            }
            int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if ((ChatManager.GetSavedServerByID(serverid)) != null && (ChatManager.GetSavedServerByID(serverid).channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count != (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count ||
               (ChatManager.GetSavedServerByID(serverid)) != null && (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetched != (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchTotal)
            {
                if (scrollViewer.VerticalOffset == 0 && lastChatScrollValue > 0 && scrollViewer.ScrollableHeight > 0 && CanLoadMSGSWithScroll == true)
                {

                    
                    if (ChatManager.GetSavedServerByID(serverid) != null && ChatListBoxCount == (ChatManager.GetSavedServerByID(serverid).channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count && ChatManager.StillLoadingMessages == false && (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetched < (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchTotal
                        && (ChatManager.GetSavedServerByID(serverid).channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count <= (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count ||
                        ChatManager.GetSavedServerByID(serverid) == null && ChatManager.StillLoadingMessages == false && (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetched < (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchTotal)
                    {
                        CanLoadMSGSWithScroll = false;
                        ChatManager.StillLoadingMessages = true;
                        savedChatScrollValue = scrollViewer.ScrollableHeight;
                        SendPackets.RequestMoreMessages((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetched, (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchTotal, ChatManager.CurrentChannel);
                    }
                    else if (ChatManager.GetSavedServerByID(serverid) != null && ChatListBoxCount < (ChatManager.GetSavedServerByID(serverid).channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count && ChatManager.StillLoadingMessages == false && (ChatManager.GetSavedServerByID(serverid).channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count > (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count)
                    {
                        CanLoadMSGSWithScroll = false;
                        savedChatScrollValue = scrollViewer.ScrollableHeight;
                        ChatManager.StillLoadingMessages = true;
                        MainWindow.instance.EnableorDisableLoading("Retrieving messages...", true);
                        int precount = (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count; // count of messages currently
                        ChannelsForTempRoom loadedchannel = ChatManager.GetSavedServerByID(serverid).channels[ChatManager.CurrentChannel] as ChannelsForTempRoom;
                        (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.AddRange(loadedchannel.messages.GetRange((loadedchannel.messages.Count - precount) - Methods.Clamp((loadedchannel.messages.Count - precount), 0, 30), Methods.Clamp((loadedchannel.messages.Count - precount), 0, 30) /*how many to load max of 30*/ ));
                        (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages = (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.OrderBy(x => x.idofmessage).ToList();
                        for (int i = 0; i < ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count - precount); ++i)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                ChatRefreshList(0, (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages[i], false);
                            });
                        }
                    }
                }
            }



            if (CanLoadMSGSWithScroll == false || ChatManager.StillLoadingMessages == true)
            {
                ChatListBoxScroll.ScrollToVerticalOffset(ChatListBoxScroll.ScrollableHeight - savedChatScrollValue);
            }

            lastChatScrollValue = scrollViewer.VerticalOffset;

        }

        private void UserProfileMediaTextInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (isshiftpressed == true)
            {
                return;
            }
            if (e.Key == Key.Return)
            {
                this.Dispatcher.Invoke(() =>
                {
                    UserProfile_MediaTextBox.Visibility = Visibility.Hidden;
                });
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    DatabaseCalls.SaveUserMedia();
                });
            }
        }

        public void SetFormVisibility(int num)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (num)
                {
                    case 0:
                        _LoginForm.Visibility = Visibility.Hidden;
                        _RegisterForm.Visibility = Visibility.Visible;
                        _RoomForm.Visibility = Visibility.Hidden;
                        _PictureViewer.Visibility = Visibility.Hidden;
                        _MediaViewer.Visibility = Visibility.Hidden;
                        ChatCover.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        _LoginForm.Visibility = Visibility.Visible;
                        _RegisterForm.Visibility = Visibility.Hidden;
                        _RoomForm.Visibility = Visibility.Hidden;
                        _PictureViewer.Visibility = Visibility.Hidden;
                        _MediaViewer.Visibility = Visibility.Hidden;
                        ChatCover.Visibility = Visibility.Visible;
                        ChannelCover.Visibility = Visibility.Hidden;
                        FriendHud.Visibility = Visibility.Hidden;
                        DMSListBoxScroll.Visibility = Visibility.Hidden;
                        IncomingCall.Visibility = Visibility.Hidden;
                        CurrentCallText.Text = "";
                        Call.Visibility = Visibility.Hidden;
                        HangUpCall.Visibility = Visibility.Hidden;
                        break;
                    case 2:
                        _LoginForm.Visibility = Visibility.Hidden;
                        _RegisterForm.Visibility = Visibility.Hidden;
                        _RoomForm.Visibility = Visibility.Hidden;
                        _PictureViewer.Visibility = Visibility.Hidden;
                        _MediaViewer.Visibility = Visibility.Hidden;
                        break;
                    case 3:
                        _LoginForm.Visibility = Visibility.Hidden;
                        _RegisterForm.Visibility = Visibility.Hidden;
                        _RoomForm.Visibility = Visibility.Visible;
                        _PictureViewer.Visibility = Visibility.Hidden;
                        _MediaViewer.Visibility = Visibility.Hidden;
                        break;
                    case 4:
                        _LoginForm.Visibility = Visibility.Hidden;
                        _RegisterForm.Visibility = Visibility.Hidden;
                        _RoomForm.Visibility = Visibility.Hidden;
                        _PictureViewer.Visibility = Visibility.Visible;
                        _MediaViewer.Visibility = Visibility.Hidden;
                        break;
                    case 5:
                        _LoginForm.Visibility = Visibility.Hidden;
                        _RegisterForm.Visibility = Visibility.Hidden;
                        _RoomForm.Visibility = Visibility.Hidden;
                        _PictureViewer.Visibility = Visibility.Hidden;
                        _MediaViewer.Visibility = Visibility.Visible;
                        break;
                }
            });
        }
        public void GoToLobby(string profilepic, string username, int id)
        {
            NetworkManager.MyAccountID = id;
            NetworkManager.myusername = username;
            string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + profilepic);
            if (Methods.GetFileMD5Signature(file) == System.IO.Path.GetFileName(file))
            {
                Lobby_ProfilePic.Source = new BitmapImage(new Uri(file));
            }
            else
            {
                Lobby_ProfilePic.Source = UndefinedSource;
            }


            Lobby_ProfileName.Text = NetworkManager.myusername;
            NetworkManager c = new NetworkManager();
            NetworkManager.instance = c;
            NetworkManager.alive = true;
            NetworkManager.Start();
            c.NetworkInit();
            SetFormVisibility(3);

        }
        private void LoginExecute_Click(object sender, RoutedEventArgs e)
        {
            string username = Login_Username.Text;
            string password = Login_Password.Password;

            if (NetworkManager.instance == null && NetworkManager.Copied_ActionsOnPrimaryActionThread.Count == 0)
            {
                new Thread(delegate ()
                {

                    DatabaseCalls.Login(username, password);
                }).Start();
            }
        }


        public static bool HashKeyPromptOpen;
        public static bool? checkinghash = null;
        public string ServerPasswordCacheMethod(string servername, string password)
        {
         
            EnableorDisableLoading("Attempting to join - iteration 1.", true);
            string filepath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\servercache");
            if (System.IO.File.Exists(filepath) == false)
            {
                try
                {
                    using (FileStream fs = File.Create(filepath)) { }
                }
                catch
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Failure create cache file.");
                    }));
                    EnableorDisableLoading("Attempting to join - iteration 1.", false);
                    return ""; // failure creating
                }
            }
            if (System.IO.File.Exists(filepath) == true)
            {
                int ReadAttempts = 0;
                List<string> messages = new List<string>();
            RestartRead:;
                try
                {
                    messages = File.ReadAllLines(filepath, System.Text.Encoding.UTF8).ToList(); //Load this into memory so we dont take too long reading this file. Other user requests may have to read this file as well. 
                }
                catch // This will most likely occur if another thread is reading/writing from/to this file
                {
                    Thread.Sleep(200);
                    if (ReadAttempts >= 20) // if greater than 20 failed attempts, then abort.
                    {
                        ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                        {
                            MainWindow.instance.MessageBoxShow("Failure to read from cache.");
                        }));
                        EnableorDisableLoading("Attempting to join - iteration 1.", false);
                        return ""; // failure to read
                    }
                    ReadAttempts++;
                    goto RestartRead; // Attempt to read again.
                }
                string s = messages.Find(x => x.Contains(servername + "|" + NetworkManager.MyAccountID + "|"));
                if (s != null &&  String.IsNullOrEmpty(password) == true) // we found a password that is contained for this server for this account id, but this has to be a join from a server directory interaction
                {
                    string thereturnpass = s.Split('|')[2];
                    EnableorDisableLoading("Attempting to join - iteration 1.", false);
                    using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                    {
                        byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(Methods.EncryptOrDecryptExposable(thereturnpass, "test")));
                        ChatManager.selectedroompassasbytesprejoin = keys;
                    }
                    return Methods.EncryptOrDecryptExposable(thereturnpass, "test"); // return password in cache
                }
                else
                {

                    string thepassedpass = "";
                    //We neeed to write a password. Open prompt
                    if ( String.IsNullOrEmpty(password) == false) // if password is passed via Join server window
                    {
                        thepassedpass = password;
                    }
                    else
                    {
                        EnableorDisableLoading("Attempting to join - iteration 1.", false);
                        HashKeyPromptOpen = true;
                        this.Dispatcher.Invoke(() =>
                        {
                            AskHash.Visibility = Visibility.Visible;
                        });
                        while (HashKeyPromptOpen == true && NetworkManager.MyAccountID != null)
                        {
                            Thread.Sleep(50);
                        }
                        EnableorDisableLoading("Attempting to join - iteration 1.", true);
                        this.Dispatcher.Invoke(() =>
                        {
                            thepassedpass = HashInput.Text.Replace("❶", "").Replace("❷", "");
                        });
                    }
                    checkinghash = null;
                    using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                    {
                        byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(thepassedpass));
                        ChatManager.selectedroompassasbytesprejoin = keys;
                    }
                    EnableorDisableLoading("Attempting to join - iteration 1.", false);
                    new Thread(delegate ()
                    {
                        while (checkinghash == null && NetworkManager.MyAccountID != null)
                        {
                            Thread.Sleep(50);
                        }
                        if (checkinghash == false)
                        {

                        }
                        else // correct hashkey
                        {
                            try // attempt to write to cache this server's details
                            {
                                if ( String.IsNullOrEmpty(thepassedpass) == true)
                                {
                                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                                    {
                                        MainWindow.instance.MessageBoxShow("Critical error.");
                                    }));
                                }
                                else
                                {
                                    if (s == null) //if this key wasnt in the cache to begin with
                                    {
                                        string newstr = Methods.EncryptOrDecryptExposable(thepassedpass, "test");
                                        if ( String.IsNullOrEmpty(newstr) == false)
                                        {
                                            using (StreamWriter file = new StreamWriter(filepath, true))
                                            {
                                                file.WriteLine(servername + "|" + NetworkManager.MyAccountID + "|" + newstr);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                                {
                                    MainWindow.instance.MessageBoxShow("Failure to write password to cache.");
                                }));
                            }
                        }
                    }).Start();
                    return thepassedpass;

                    // we now got a password, now lets see if it worked 
                }
            }
            ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
            {
                MainWindow.instance.MessageBoxShow("Critical error.");
            }));
            EnableorDisableLoading("Attempting to join - iteration 1.", false);
            return ""; // critical error
        }
        private void GoToRegisterButtion_Click(object sender, RoutedEventArgs e)
        {
            SetFormVisibility(0);
        }
        private int FindChatBoxInsertIndex(Message msg)
        {

            int index = 0;
            while (true && NetworkManager.MyAccountID != null)
            {
                if (index < UnconditionalChatListBoxCount)
                {
                    if (msg.idofmessage > ((Message)ChatListBox.Items[index]).idofmessage)
                    {
                        index++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            return index;
        }
        public void ChatRefreshList(int type, Message msg, bool searched) // for adding or removing
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if(ChatManager.channels.ContainsKey(ChatManager.CurrentChannel) == false)
                {
                    return;
                }
                try
                {
                    switch (type)
                    {
                        case 0:

                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Contains(msg) && ChatListBox.Items.Contains(msg) == false && searched == false && (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count == 0 ||
                            (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Contains(msg) && ChatListBox.Items.Contains(msg) == false && searched == true)
                            {
                                int index = FindChatBoxInsertIndex(msg);

                                if (msg.GetType() == typeof(ImageMessage) &&  String.IsNullOrEmpty((msg as ImageMessage).ImageCachePath_Thumbnail) == false && System.IO.File.Exists((msg as ImageMessage).ImageCachePath_Thumbnail) && Methods.GetFileMD5Signature((msg as ImageMessage).ImageCachePath_Thumbnail) == System.IO.Path.GetFileName((msg as ImageMessage).ImageCachePath_Thumbnail)
                                   ||

                                     msg.GetType() == typeof(VideoMessage) &&  String.IsNullOrEmpty((msg as VideoMessage).ImageCachePath_Thumbnail) == false && System.IO.File.Exists((msg as VideoMessage).ImageCachePath_Thumbnail) && Methods.GetFileMD5Signature((msg as VideoMessage).ImageCachePath_Thumbnail) == System.IO.Path.GetFileName((msg as VideoMessage).ImageCachePath_Thumbnail)
                                    ||

                                     msg.GetType() == typeof(AudioMessage) &&  String.IsNullOrEmpty((msg as AudioMessage).AudioCachePath) == false && System.IO.File.Exists((msg as AudioMessage).AudioCachePath) && Methods.GetFileMD5Signature((msg as AudioMessage).AudioCachePath) + ".mp3" == System.IO.Path.GetFileName((msg as AudioMessage).AudioCachePath)
                                    || msg.GetType() == typeof(Message) || msg.GetType() == typeof(FileMessage)

                                    )
                                {
                                    //good
                                }
                                else
                                {
                                    if (msg.GetType() == typeof(ImageMessage))
                                    {
                                        ((ImageMessage)msg).ImageCachePath_Thumbnail = "pack://application:,,,/Resources/Untitled.png";
                                    }
                                    if (msg.GetType() == typeof(VideoMessage))
                                    {
                                        ((VideoMessage)msg).ImageCachePath_Thumbnail = "pack://application:,,,/Resources/Untitled.png";
                                    }
                                }

                                this.Dispatcher.Invoke(() =>
                                {
                                    ChatListBox.Items.Insert(index, msg);
                                });
                                UnconditionalChatListBoxCount++; // this is just general spawns
                                if (msg.loaded == true)
                                {
                                    ChatListBoxCount++; // when it is loaded
                                }
                                //loadedchatlistboxcount is completely loaded in entirety
                            }

                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count > 0 && searched == false)
                            {
                                return;
                            }
                            if (msg.loaded == true)
                            {
                           
                                int mainccount = (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count;

                                if (ChatListBoxCount == (searched == false ? mainccount : (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count))
                                {
                                    bool ScrolltoSelect = false;
                                    if (ChatManager.StillLoadingMessages == true)
                                    {
                                        if (searched == false)
                                        {
                                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count <= 30)
                                            {
                                                this.Dispatcher.Invoke(() =>
                                                {
                                                    MainWindow.instance.ChatListBoxScroll.ScrollToBottom();
                                                });
                                            }
                                            else
                                            {
                                                this.Dispatcher.Invoke(() =>
                                                {
                                                    MainWindow.instance.ChatListBoxScroll.ScrollToVerticalOffset(MainWindow.instance.ChatListBoxScroll.ScrollableHeight - MainWindow.instance.savedChatScrollValue);
                                                });
                                            }
                                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetched == (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetched)
                                            {
                                                ChatManager.StillLoadingMessages = false;

                                                MainWindow.instance.EnableorDisableLoading("Retrieving messages...", false);
                                                MainWindow.instance.EnableorDisableLoading("Joining room...", false);
                                                MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                                            }
                                        }
                                        else
                                        {
                                            this.Dispatcher.Invoke(() =>
                                            {
                                                MainWindow.instance.ChatListBoxScroll.ScrollToBottom();
                                            });
                                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetchedSearch == (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchedSearch)
                                            {
                                                ChatManager.StillLoadingMessages = false;

                                                MainWindow.instance.EnableorDisableLoading("Retrieving messages...", false);
                                                MainWindow.instance.EnableorDisableLoading("Joining room...", false);
                                                MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {

                            }

                            break;
                        default:
                            break;
                    }


                }
                catch { }
            });

        }
        public void ChatRefreshList(int type, Message msg, Message oldmsg, int channel, bool searched) // for modifying
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                try
                {
                    switch (type)
                    {
                        case 2:
                            if (msg.GetType() == typeof(ImageMessage) &&  String.IsNullOrEmpty((msg as ImageMessage).ImageCachePath_Thumbnail) == false && System.IO.File.Exists((msg as ImageMessage).ImageCachePath_Thumbnail) && Methods.GetFileMD5Signature((msg as ImageMessage).ImageCachePath_Thumbnail) == System.IO.Path.GetFileName((msg as ImageMessage).ImageCachePath_Thumbnail)
                                    ||

                                      msg.GetType() == typeof(VideoMessage) &&  String.IsNullOrEmpty((msg as VideoMessage).ImageCachePath_Thumbnail) == false && System.IO.File.Exists((msg as VideoMessage).ImageCachePath_Thumbnail) && Methods.GetFileMD5Signature((msg as VideoMessage).ImageCachePath_Thumbnail) == System.IO.Path.GetFileName((msg as VideoMessage).ImageCachePath_Thumbnail)
                                     ||

                                      msg.GetType() == typeof(AudioMessage) &&  String.IsNullOrEmpty((msg as AudioMessage).AudioCachePath) == false && System.IO.File.Exists((msg as AudioMessage).AudioCachePath) && Methods.GetFileMD5Signature((msg as AudioMessage).AudioCachePath) + ".mp3" == System.IO.Path.GetFileName((msg as AudioMessage).AudioCachePath)
                                     || msg.GetType() == typeof(Message) || msg.GetType() == typeof(FileMessage)

                                     )
                            {
                                //good
                            }
                            else
                            {
                                if (msg.GetType() == typeof(ImageMessage))
                                {

                                    ((ImageMessage)msg).ImageCachePath_Thumbnail = "pack://application:,,,/Resources/Untitled.png";
                                }
                                if (msg.GetType() == typeof(VideoMessage))
                                {


                                    ((VideoMessage)msg).ImageCachePath_Thumbnail = "pack://application:,,,/Resources/Untitled.png";
                                }
                                if (msg.GetType() == typeof(AudioMessage))
                                {
                                    // ((AudioMessage)msg).AudioCachePath = "";
                                }
                            }
                            if (searched == false)
                            {
                                int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
                                {
                                    int index = (ChatManager.channels[channel] as ChannelsForTempRoom).messages.IndexOf(oldmsg);
                                    (ChatManager.channels[channel] as ChannelsForTempRoom).messages[index] = msg;

                                    if (ChatManager.CurrentSavedServer != null || ChatManager.CurTempRoom != null || ChatManager.CurrentChatWithUser != null)
                                    {
                                        if (ChatManager.GetSavedServerByID(serverid) != null)
                                        {
                                            int loadedindex = (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages.IndexOf(oldmsg);
                                            (ChatManager.GetSavedServerByID(serverid).channels[channel] as ChannelsForTempRoom).messages[loadedindex] = msg;
                                        }
                                    }
                                }
                                if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Contains(msg) && ChatListBox.Items.Contains(oldmsg) == true)
                                {
                                    {
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            for (int i = 0; i < ChatListBox.Items.Count; i++)
                                            {
                                                if (((Message)ChatListBox.Items[i]).idofmessage ==  oldmsg.idofmessage)
                                                {
                                                    ChatListBox.Items[i] = msg;
                                                }
                                            }
                                        });
                                    }
                                }

                            }
                            else
                            {
                                {
                                    int index = (ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages.IndexOf(oldmsg);
                                    (ChatManager.channels[channel] as ChannelsForTempRoom).searchedmessages[index] = msg;
                                }
                                if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Contains(msg) && ChatListBox.Items.Contains(oldmsg) == true)
                                {
                                    {
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            for (int i = 0; i < ChatListBox.Items.Count; i++)
                                            {
                                                if (((Message)ChatListBox.Items[i]).idofmessage == oldmsg.idofmessage)
                                                {
                                                    ChatListBox.Items[i] = msg;
                                                }
                                            }
                                        });
                                    }
                                }
                            }
                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count > 0 && searched == false)
                            {
                                return;
                            }
                            if (oldmsg.loaded == false)
                            {
                                ChatListBoxCount++;
                            }

                            break;
                        default:
                            break;
                    }
                    if (msg.loaded == true)
                    {
                        if (ChatListBoxCount == (searched == false ? (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count : (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count))
                        {
                            if (ChatManager.StillLoadingMessages == true)
                            {
                                if (searched == false)
                                {
                                    if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count <= 30)
                                    {
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            MainWindow.instance.ChatListBoxScroll.ScrollToBottom();
                                        });
                                    }
                                    else
                                    {
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            MainWindow.instance.ChatListBoxScroll.ScrollToVerticalOffset(MainWindow.instance.ChatListBoxScroll.ScrollableHeight - MainWindow.instance.savedChatScrollValue);
                                        });
                                    }
                                    if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetched == (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetched)
                                    {
                                      
                                        int curchannel = ChatManager.CurrentChannel;
                                        int curserver = (ChatManager.CurrentSavedServer ?? ChatManager.CurTempRoom) ?? (int)ChatManager.CurrentChatWithUser;
                                        new Thread(delegate ()
                                        {
                                        while (((ChatManager.CurrentSavedServer ?? ChatManager.CurTempRoom) ?? (int)ChatManager.CurrentChatWithUser) == curserver && curchannel == ChatManager.CurrentChannel &&
                                        (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.All(x => x.loaded) == false && NetworkManager.MyAccountID != null)
                                            {
                                                Thread.Sleep(10);
                                            }
                                            NetworkManager.TaskTo_PrimaryActionThread(() =>
                                            {
                                                if (((ChatManager.CurrentSavedServer ?? ChatManager.CurTempRoom) ?? (int)ChatManager.CurrentChatWithUser) != curserver || curchannel != ChatManager.CurrentChannel)
                                                {
                                                    return;
                                                }
                                                if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count <= 30)
                                                {
                                                    this.Dispatcher.Invoke(() =>
                                                    {
                                                        MainWindow.instance.ChatListBoxScroll.ScrollToBottom();
                                                    });
                                                }
                                                else
                                                {
                                                    this.Dispatcher.Invoke(() =>
                                                    {
                                                        MainWindow.instance.ChatListBoxScroll.ScrollToVerticalOffset(MainWindow.instance.ChatListBoxScroll.ScrollableHeight - MainWindow.instance.savedChatScrollValue);
                                                    });
                                                }
                                            });
                                        }).Start();

                                        ChatManager.StillLoadingMessages = false;
                                        MainWindow.instance.EnableorDisableLoading("Retrieving messages...", false);
                                        MainWindow.instance.EnableorDisableLoading("Joining room...", false);
                                        MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                                    }
                                }
                                else
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        MainWindow.instance.ChatListBoxScroll.ScrollToBottom();
                                    });
                                    if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesFetchedSearch == (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchedSearch)
                                    {
                                        ChatManager.StillLoadingMessages = false;

                                        MainWindow.instance.EnableorDisableLoading("Retrieving messages...", false);
                                        MainWindow.instance.EnableorDisableLoading("Joining room...", false);
                                        MainWindow.instance.EnableorDisableLoading("Loading private messages...", false);
                                    }
                                }
                            }

                        }
                    }
                }
                catch { }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e) // Choose Profile Picture
        {
            if (FileDialogOpened == true)
            {
                return;
            }
        
            new Thread(delegate ()
            {
                string path = Methods.Show_OpenFileDialog(new string[] { "png", "jpg", "jpeg", "tiff", "bmp", "gif" }, 0);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {
                    int height = 0;
                    int width = 0;
                    double decimatevalue = 1;
                    BitmapImage source = UndefinedSource;
                    string file = path;

                    source = new BitmapImage(new Uri(path));


                    height = source.PixelHeight;
                    width = source.PixelWidth;
                    if (width > 256 || height > 256)
                    {
                        if (width > height)
                        {
                            decimatevalue = (double)width / 256;
                        }
                        else
                        {
                            decimatevalue = (double)height / 256;
                        }
                    }
                    height = (int)((double)height / decimatevalue);
                    width = (int)((double)width / decimatevalue);

                   
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        BitmapImage NewSource = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(path)))
                        {
                            NewSource.BeginInit();
                            NewSource.StreamSource = stream;
                            NewSource.DecodePixelHeight = height;
                            NewSource.DecodePixelWidth = width;
                            NewSource.EndInit();


                            if (height != width)
                            {

                                Register_ProfilePic.Source = Methods.BitmapSourceToBitmapImage(new CroppedBitmap(NewSource as BitmapImage, new Int32Rect((int)((double)(height < width ? ((double)width - (double)height) / 2d : 0)), (int)((double)(width < height ? ((double)height - (double)width) / 2d : 0)), (height < width ? height : width), (height < width ? height : width)))) as ImageSource;
                            }
                            else
                            {
                                Register_ProfilePic.Source = NewSource as ImageSource;

                            }
                        }
                    });
                   
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not open image.");
                    }));
                }
            }).Start();
        }

        private void RegisterExecute_Click(object sender, RoutedEventArgs e)
        {
            if (Register_ProfilePic.Source == null)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please select a profile picture.");
                }));
                return;
            }
            byte[] ProfileByte = Methods.ImageSourceToBytes((BitmapImage)Register_ProfilePic.Source);
            string a = Register_Username.Text;
            string b = Register_Password.Password;
            string c = Register_PasswordConfirm.Password;
            // create keys   

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            string publickey = rsa.ToXmlString(false);
            string privatekey = rsa.ToXmlString(true);

            //write private key to local pc
            if (islocalkey.IsChecked == true)
            {
                string thisaccountkeypath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), a + ".xml");
                if (File.Exists(thisaccountkeypath))
                {
                    File.Delete(thisaccountkeypath);
                }
                using (StreamWriter file = new StreamWriter(thisaccountkeypath, true))
                {
                    file.WriteLine(privatekey);
                }
            }
            else
            {
                string constanta = "Thestandardconstant@#$!@#";
                string constantb = "C0n$1stent1yC0nst4nt";
                byte[] code;
                using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                {
                    code = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(b));
                }
                byte[] ccode;
                using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                {
                    ccode = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(constantb));
                }
                privatekey = Methods.EncryptString(Methods.EncryptString(Methods.EncryptOrDecryptExposable(privatekey, constanta), code), ccode);

            }
            bool check = (bool)islocalkey.IsChecked;
            new Thread(delegate ()
            {
                DatabaseCalls.Register(a, b, c, ProfileByte, publickey, (check ? "Local" : privatekey));
            }).Start();
        }

        private void GoToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            SetFormVisibility(1);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if ( String.IsNullOrEmpty(CreateServername.Text) == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name field required.");
                }));
                return;
            }
            string servername = CreateServername.Text;
            string pas = PasswordForRoom.Text;
            if (String.IsNullOrEmpty(pas))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server password is required.");
                }));
                return;
            }
            if (String.IsNullOrEmpty(servername))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name is required.");
                }));
                return;
            }
            if (Methods.Allowed(servername) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid server name.");
                }));
                return;
            }
            if (Methods.Allowed(pas) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid password.");
                }));
                return;
            }
            string ms = "Joining room...";
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.StillGettingVerificationOfJoinRoom == false)
                {
                    string passfetch = ServerPasswordCacheMethod(servername, pas);
                    if ( String.IsNullOrEmpty(passfetch) == true)
                    {
                        checkinghash = false;
                        return;
                    }
                    ChatManager.selectedroompass = passfetch;
                    using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                    {
                        byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(pas));
                        ChatManager.selectedroompassasbytes = keys;
                    }
                    ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(pas);
                    ChatManager.selectedencryptionseed = "";
                    if (ChatManager.CurrentSavedServer != null)
                    {
                        SavedServersOnMemory ss = ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer);
                        if (ss != null)
                        {
                            int[] keyarray = new int[ChatManager.channels.Keys.Count];
                            ss.channels.Keys.CopyTo(keyarray, 0);
                            foreach (int k in keyarray)// update all audio messages slider            
                            {
                                SavedChannelsForTempRoom scftr = (ss.channels[k] as SavedChannelsForTempRoom);
                                if (scftr == null)
                                {
                                    continue;
                                }
                                for (int i = 0; i < scftr.messages.Count; ++i)
                                {
                                    if (scftr.messages[i].loaded == true)
                                    {
                                        if (scftr.messages[i].GetType() == typeof(AudioMessage))
                                        {
                                            (scftr.messages[i] as AudioMessage).VolumeSlider = null;
                                            (scftr.messages[i] as AudioMessage).MediaSource = null; (scftr.messages[i] as AudioMessage).TimeSlider = null;
                                            (scftr.messages[i] as AudioMessage).isMediaPaused = false;
                                            (scftr.messages[i] as AudioMessage).isseeking = false;
                                        }
                                        if (scftr.savedmessages.Any(x => x.idofmessage == scftr.messages[i].idofmessage) == false)
                                        {

                                            scftr.savedmessages.Add(scftr.messages[i]);
                                        }
                                    }
                                }

                            }
                        }
                    }

                    EnableorDisableLoading(ms, true);
                    SendPackets.LeaveAllServers();
                    SendPackets.ClientRequestJoinRoom(servername, false);

                }
            });
        }

        private void CreateTemp_Click(object sender, RoutedEventArgs e)
        {
            if ( String.IsNullOrEmpty(CreateServername.Text) == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name field required.");
                }));
                return;
            }
            string root = "";
            for (int i = 0; i < 50; ++i)
            {
                root += WordVerififier.Awords[Methods.RandomRange.Next(0, WordVerififier.Awords.Length)] + " ";
            }
            string csn = CreateServername.Text;
            string t = PasswordForRoom.Text;
            if (String.IsNullOrEmpty(t))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server password is required.");
                }));
                return;
            }
            if (String.IsNullOrEmpty(csn))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name is required.");
                }));
                return;
            }
            if (Methods.Allowed(csn) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid server name.");
                }));
                return;
            }
            if (Methods.Allowed(t) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid password.");
                }));
                return;
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                ChatManager.selectedroompass = t;
                using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                {
                    byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(t));
                    ChatManager.selectedroompassasbytes = keys;
                }
                ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(t);
                root = Methods.EncryptString(root, ChatManager.selectedroompassasbytes);
                ChatManager.selectedencryptionseed = root;
                if (ChatManager.CurrentSavedServer != null)
                {
                    SavedServersOnMemory ss = ChatManager.GetSavedServerByID((int)ChatManager.CurrentSavedServer);
                    if (ss != null)
                    {
                        int[] keyarray = new int[ChatManager.channels.Keys.Count];
                        ss.channels.Keys.CopyTo(keyarray, 0);
                        foreach (int k in keyarray)// update all audio messages slider            
                        {
                            SavedChannelsForTempRoom scftr = (ss.channels[k] as SavedChannelsForTempRoom);
                            if (scftr == null)
                            {
                                continue;
                            }
                            for (int i = 0; i < scftr.messages.Count; ++i)
                            {
                                if (scftr.messages[i].loaded == true)
                                {
                                    if (scftr.messages[i].GetType() == typeof(AudioMessage))
                                    {
                                        (scftr.messages[i] as AudioMessage).VolumeSlider = null;
                                        (scftr.messages[i] as AudioMessage).MediaSource = null; (scftr.messages[i] as AudioMessage).TimeSlider = null;
                                        (scftr.messages[i] as AudioMessage).isMediaPaused = false;
                                        (scftr.messages[i] as AudioMessage).isseeking = false;
                                    }
                                    if (scftr.savedmessages.Any(x => x.idofmessage == scftr.messages[i].idofmessage) == false)
                                    {

                                        scftr.savedmessages.Add(scftr.messages[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                SendPackets.LeaveAllServers();
                SendPackets.ClientRequestMakeRoom(root, csn);
            });
        }
     
        private void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.Allowed(_ChatInput.Text) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Could not send this message due to an invalid character contained within.");
                }));
                return;
            }
            if (FileDialogOpened == true)
            {
                return;
            }
            new Thread(delegate ()
            {

                string path = Methods.Show_OpenFileDialog(new string[] { "png", "jpg", "jpeg", "tiff", "bmp", "gif" }, 0);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {

                    string t = "";
                    this.Dispatcher.Invoke(() =>
                    {
                        t = _ChatInput.Text;
                    });

                    SFTPCalls.UploadPrivateChatPic(path, t, ChatManager.CurrentChannel);
                    this.Dispatcher.Invoke(() =>
                    {
                        _ChatInput.Text = "";
                    });
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not use image.");
                    }));
                }
            }).Start();
        }
        private void SearchMSG_Click(object sender, RoutedEventArgs e)
        {
            if (ChatManager.CurrentChatWithUser == null)
            {
                MSGSearchGrid.Visibility = Visibility.Visible;
                MSGSearchInput.Clear();
                MessageHistoryListBox.ItemsSource = null;
                MessageHistoryListBox.Items.Refresh();
            }
            else
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Unable to search messages that are using asymmetrical encryption.");
                }));
            }
        }
        private void ClickSearch(object sender, RoutedEventArgs e)
        {
            string msi = MSGSearchInput.Text;
            if (Methods.Allowed(msi) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid message.");
                }));
                return;
            }
            EnableorDisableLoading("Searching...", true);
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.CurrentChatWithUser == null)
                {
                    SendPackets.InitiateMessageSearch(Methods.EncryptString(msi, ChatManager.selectedroompassasbytes), ChatManager.CurrentSavedServer ?? (int)ChatManager.CurTempRoom, ChatManager.CurrentChannel, ChatManager.CurrentSavedServer != null);
                }
                else
                {
                    SendPackets.InitiateMessageSearch(Methods.AsymetricalEncryption(msi, ChatManager.MyUserPublicKey));
                }
            });
        }
        private void UploadVideo_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.Allowed(_ChatInput.Text) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Could not send this message due to an invalid character contained within.");
                }));
                return;
            }
            if (FileDialogOpened == true)
            {
                return;
            }
            new Thread(delegate ()
            {
                string path = Methods.Show_OpenFileDialog(new string[] { "mp4" }, 1);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });

                if (path != null)
                {
                    string t = "";
                    this.Dispatcher.Invoke(() =>
                    {
                        t = _ChatInput.Text;
                    });

                    SFTPCalls.UploadPrivateChatVideo(path, t, ChatManager.CurrentChannel);
                    this.Dispatcher.Invoke(() =>
                    {
                        _ChatInput.Text = "";
                    });
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not use video.");
                    }));
                }
            }).Start();
        }
        private void UploadAudio_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.Allowed(_ChatInput.Text) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Could not send this message due to an invalid character contained within.");
                }));
                return;
            }
            if (FileDialogOpened == true)
            {
                return;
            }
            new Thread(delegate ()
            {
                string path = Methods.Show_OpenFileDialog(new string[] { "mp3" }, 1);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {
                    string t = "";
                    this.Dispatcher.Invoke(() =>
                    {
                        t = _ChatInput.Text;
                    });

                    SFTPCalls.UploadPrivateChatAudio(path, t, ChatManager.CurrentChannel);
                    this.Dispatcher.Invoke(() =>
                    {
                        _ChatInput.Text = "";
                    });
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not use audio.");
                    }));
                }
            }).Start();
        }
        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.Allowed(_ChatInput.Text) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Could not send this message due to an invalid character contained within.");
                }));
                return;
            }
            if (FileDialogOpened == true)
            {
                return;
            }

            new Thread(delegate ()
            {
                string path = Methods.Show_OpenFileDialog(new string[] { }, 2);



                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {
                    FileInfo file = new FileInfo(path);
                    long sizeinbytes = file.Length;
                    string extension = System.IO.Path.GetExtension(path).Replace(".", "");
                    string t = "";
                    this.Dispatcher.Invoke(() =>
                    {
                        t = _ChatInput.Text;
                    });

                    SFTPCalls.UploadPrivateChatFile(path, t, extension, sizeinbytes, ChatManager.CurrentChannel);
                    this.Dispatcher.Invoke(() =>
                    {
                        _ChatInput.Text = "";
                    });
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not use file.");
                    }));
                }
            }).Start();
        }
        private void LoadSearchMsg_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                bool sfm = ChatManager.StillFetchingMessages;
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    if (MessageHistoryListBox.SelectedItem == null || sfm == true)
                    {
                        return;
                    }
                    if (MessageHistoryListBox.SelectedItem.GetType() == typeof(Message))
                    {
                        int idofmes = (MessageHistoryListBox.SelectedItem as Message).idofmessage;
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            EnableorDisableLoading("Retrieving messages...", true);

                            ChatManager.StillFetchingMessages = true;
                            ChatManager.StillLoadingMessages = true;
                            if (ChatManager.CurrentChatWithUser == null)
                            {
                                SendPackets.LoadSelectedMessageFromSearch(idofmes, ChatManager.CurrentSavedServer ?? (int)ChatManager.CurTempRoom, ChatManager.CurrentChannel, ChatManager.CurrentSavedServer != null);
                            }
                            else
                            {
                                SendPackets.LoadSelectedMessageFromSearch(idofmes, -1, 0, false);
                            }
                        });
                    }
                });
            });
        }
        private void SocialMedia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserProfile_MediaListBox.SelectedItem == null)
            {
                return;
            }
            if (UserProfile_MediaListBox.SelectedItem.GetType() == typeof(MediaFormat))
            {
                string destinationurl = ((MediaFormat)UserProfile_MediaListBox.SelectedItem).Raw;
                System.Diagnostics.Process.Start("explorer.exe", destinationurl);
                
            }
        }
        private void ChangeUserRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserRoleListBox.SelectedItem == null) { return; }
            if (UserRoleListBox.SelectedItem.GetType() == typeof(UserList))
            {
                int a = (UserRoleListBox.SelectedItem as UserList).AccountID;
                int b = (RolesEditListBox.SelectedItem as RolesList).id;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (AddingPeopletoRoles == true)
                    {
                        EnableorDisableLoading("Changing user's role...", true);
                        SendPackets.RemoveOrAddPLRole((int)ChatManager.CurrentSavedServer, a,b, true);
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            Button_Click_6(null, null);
                        });
                    }
                });
            }
        }
        private void ChangeChannelRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelsRoleListBox.SelectedItem == null) { return; }
            if (ChannelsRoleListBox.SelectedItem.GetType() == typeof(RolesList))
            {
                if (AddingRoleToChannel == true)
                {
                    EnableorDisableLoading("Changing channel access...", true);
                    if ((ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) == null && (ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom) == null)
                    {
                        return;
                    }
                    SendPackets.RemoveOrAddChannelRole((int)ChatManager.CurrentSavedServer, (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) != null ? (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom).key : (ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom).key, (ChannelsRoleListBox.SelectedItem as RolesList).id, true); ;
                    ADDROLETOCHANNEL_Click(null, null);
                }
            }
        }
        public void UpdateRoleUI()
        {

            if (ChatManager.CurrentSavedServer == null)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    AddChannelbutton.Visibility = Visibility.Hidden;
                    ManageRolesButton.Visibility = Visibility.Hidden;
                });
                return;
            }
            bool[] excused = new bool[6] { false, false, false, false, false, false };
            for (int i = 0; i < ChatManager.my_room_user._Roles.Count; ++i)
            {
                RolesList temp = ChatManager.RoleList.Find(x => x.id == ChatManager.my_room_user._Roles[i]);
                if (temp == null)
                {
                    continue;
                }
                if (excused[4] == false)
                {
                    Visibility v = temp.powers[4] == 1 ? Visibility.Visible : Visibility.Hidden;
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        AddChannelbutton.Visibility = v;
                    });
                }
                if (excused[5] == false)
                {
                    Visibility v = temp.powers[5] == 1 ? Visibility.Visible : Visibility.Hidden;
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        ManageRolesButton.Visibility = v;
                    });
                }
                if (temp.powers[4] == 1)
                {
                    excused[4] = true;
                }
                if (temp.powers[5] == 1)
                {
                    excused[5] = true;
                }
            }

        }
        private void EditRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
                RolesList sr = RolesEditListBox.SelectedItem as RolesList;
            if (sr == null)
                return;
                int srid = sr.id;
                bool a = sr.id > 1;
                bool a1 = sr.id > 1;
                bool a2 = sr.id > 1;
                bool a3 = sr.id > 1;
                bool a4 = sr.id > 1;
                bool a5 = sr.id > 1;
                bool a6 = sr.id > 1;
                bool a7 = sr.id > 1;

                bool a8 = sr.powers[0] == 0 ? false : true;
                bool a9 = sr.powers[1] == 0 ? false : true;
                bool a10 = sr.powers[2] == 0 ? false : true;
                bool a11 = sr.powers[3] == 0 ? false : true;
                bool a12 = sr.powers[4] == 0 ? false : true;
                bool a13 = sr.powers[5] == 0 ? false : true;
                string hex = sr.Hex;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    List<UserList> temp = ChatManager.UserList.FindAll(x => x._Roles.Contains(srid));
                    if (sr != null)
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            editkick.IsEnabled = a;
                            editmute.IsEnabled = a1;
                            editban.IsEnabled = a2;
                            editdelete.IsEnabled = a3;
                            editmanagechannels.IsEnabled = a4;
                            editmanageroles.IsEnabled = a5;
                            deleteRole.IsEnabled = a6;
                            addusertorole.IsEnabled = a7;

                            editkick.IsChecked = a8;
                            editmute.IsChecked = a9;
                            editban.IsChecked = a10;
                            editdelete.IsChecked = a11;
                            editmanagechannels.IsChecked = a12;
                            editmanageroles.IsChecked = a13;

                            RoleColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                            UserRoleListBox.ItemsSource = temp;
                            UserRoleListBox.Items.Refresh();
                            addusertorole.Content = "Add Users To Role";
                            NetworkManager.TaskTo_PrimaryActionThread(() =>
                            {
                                AddingPeopletoRoles = false;
                                RefreshUserRoleListBox();
                            });
                        });
                    }
                });
           

        }
   
        private void EditChannels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChannelsForTempRoom tc = ChannelsEditListBox.SelectedItem as ChannelsForTempRoom;
            VoiceChannelsForTempRoom vc = ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom;

            if (tc != null)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (tc != null)
                    {
                        List<RolesList> lor = new List<RolesList>();
                        for (int i = 0; i < tc.Roles.Count; ++i)
                        {
                            RolesList r = ChatManager.RoleList.Find(x => x.id == tc.Roles[i]);
                            if (r != null)
                            {
                                lor.Add(r);
                            }
                        }
                        bool a = tc.read_only;
                        bool b = tc.incoming_users;
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            ChannelsRoleListBox.ItemsSource = lor;
                            readonlychannel.IsChecked = a;
                            incominguser.IsChecked = b;
                            readonlychannel.IsEnabled = true;
                            incominguser.IsEnabled = true;
                            ChannelsRoleListBox.Items.Refresh();
                        });
                    }
                });
            }
            if (vc != null)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (vc != null)
                    {
                        List<RolesList> lor = new List<RolesList>();
                        for (int i = 0; i < vc.Roles.Count; ++i)
                        {
                            RolesList r = ChatManager.RoleList.Find(x => x.id == vc.Roles[i]);
                            if (r != null)
                            {
                                lor.Add(r);
                            }
                        }
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            ChannelsRoleListBox.ItemsSource = lor;
                            readonlychannel.IsChecked = false;
                            incominguser.IsChecked = false;
                            readonlychannel.IsEnabled = false;
                            incominguser.IsEnabled = false;
                            ChannelsRoleListBox.Items.Refresh();
                        });
                    }
                });    
            }
           

        }
       
        public void GoTODMFunction()
        {
            if (ChatManager.my_room_user != null)
                ChatManager.my_room_user.StopTransmission();
            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    ChannelCover.Visibility = Visibility.Visible;
                    FriendHud.Visibility = Visibility.Visible;
                    DMSListBoxScroll.Visibility = Visibility.Visible;
                });
                for (int i = 0; i < NetworkManager.DMS.Count; ++i)
                {
                    UserList _u = NetworkManager.DMS[i];
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        bool found = false;
                        for (int ii = 0; ii < DMSListBox.Items.Count; ++ii)
                        {
                            if (((UserList)DMSListBox.Items[ii]).AccountID == (_u ?? new UserList("undefined", -1)).AccountID)
                            {
                                found = true;
                            }
                        }
                        if (found == false)
                        {
                            DMSListBox.Items.Add(_u ?? new UserList("undefined", -2));
                        }
                    });
                }
                
                List<UserList> copied = new List<UserList>();
                for(int i = 0; i < NetworkManager.DMS.Count; ++i)
                {
                    copied.Add(new UserList("undefined", NetworkManager.DMS[i].AccountID));
                }
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < DMSListBox.Items.Count; ++i)
                    {
                        bool found = false;
                        for (int ii = 0; ii < copied.Count; ++ii)
                        {
                            if (((UserList)DMSListBox.Items[i]).AccountID == copied[ii].AccountID)
                            {
                                found = true;
                            }
                        }
                        if (found == false)
                        {
                            DMSListBox.Items.RemoveAt(i);
                        }
                    }
                    ChatCover.Visibility = Visibility.Visible;
                    ChatListBox.Items.Clear();
                });


                EnableorDisableLoading("Leaving current room...", true);
                SendPackets.LeaveAllServers();
            
        }
        private void ServerDirectoryInteraction(object sender, SelectionChangedEventArgs e)
        {
            string ms = "Joining room...";
            
            if (ServerDirectoryListBox.SelectedItem == null)
            {
                return;
            }
            if (ServerDirectoryListBox.SelectedItem.GetType() == typeof(ServerDirectoryElements))
            {
                AddChannelbutton.Visibility = Visibility.Hidden;
                ManageRolesButton.Visibility = Visibility.Hidden;
                if (((ServerDirectoryElements)ServerDirectoryListBox.SelectedItem).elementtype == -1)
                {
                    ServerBrowserWindow.Visibility = Visibility.Visible;
                    EditServerImage.Source = null;
                }
                else if (((ServerDirectoryElements)ServerDirectoryListBox.SelectedItem).elementtype == -2)
                {
                    EnableorDisableLoading("Loading private messages...", true);

                    new Thread(delegate ()
                    {
                        LoadingFriends = true;
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            SendPackets.RequestFriends(0);
                        });
                        while (LoadingFriends == true && NetworkManager.MyAccountID != null)
                        {
                            Thread.Sleep(10);
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.CurrentSavedServer != null || ChatManager.CurrentChatWithUser != null)
                            {
                                int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
                                SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
                                if (ss != null)
                                {

                                    int[] keyarray = new int[ChatManager.channels.Keys.Count];
                                    ss.channels.Keys.CopyTo(keyarray, 0);
                                    foreach (int k in keyarray)// update all audio messages slider            
                                    {
                                        SavedChannelsForTempRoom scftr = (ss.channels[k] as SavedChannelsForTempRoom);
                                        if (scftr == null)
                                        {
                                            continue;
                                        }
                                        for (int i = 0; i < scftr.messages.Count; ++i)
                                        {
                                            if (scftr.messages[i].loaded == true)
                                            {
                                                if (scftr.messages[i].GetType() == typeof(AudioMessage))
                                                {
                                                    (scftr.messages[i] as AudioMessage).MediaSource = null;
                                                    (scftr.messages[i] as AudioMessage).TimeSlider = null;
                                                    (scftr.messages[i] as AudioMessage).VolumeSlider = null;
                                                    (scftr.messages[i] as AudioMessage).isMediaPaused = false;
                                                    (scftr.messages[i] as AudioMessage).isseeking = false;
                                                }
                                                if (scftr.savedmessages.Any(x => x.idofmessage == scftr.messages[i].idofmessage) == false)
                                                {
                                                    scftr.savedmessages.Add(scftr.messages[i]);
                                                }
                                            }
                                        }

                                    }
                                }
                            }

                            GoTODMFunction();
                        });
                    }).Start();
                }
                else
                {
                    if (ChatManager.StillGettingVerificationOfJoinRoom == false)
                    {
                        string servername = ((ServerDirectoryElements)ServerDirectoryListBox.SelectedItem).name;
                        new Thread(delegate ()
                        {
                            NetworkManager.TaskTo_PrimaryActionThread(() =>
                            {
                                string passfetch = ServerPasswordCacheMethod(servername, "");
                                if ( String.IsNullOrEmpty(passfetch) == true)
                                {
                                    checkinghash = false;
                                    return;
                                }
                                ChatManager.selectedroompass = passfetch;
                                using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                                {
                                    byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(passfetch));
                                    ChatManager.selectedroompassasbytes = keys;
                                }
                                ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(passfetch);
                                ChatManager.selectedencryptionseed = "";
                                int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
                                if (ChatManager.CurrentSavedServer != null || ChatManager.CurrentChatWithUser != null)
                                {
                                    SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
                                    if (ss != null)
                                    {

                                        int[] keyarray = new int[ChatManager.channels.Keys.Count];
                                        ss.channels.Keys.CopyTo(keyarray, 0);
                                        foreach (int k in keyarray)// update all audio messages slider            
                                        {
                                            SavedChannelsForTempRoom scftr = (ss.channels[k] as SavedChannelsForTempRoom);
                                            if (scftr == null)
                                            {
                                                continue;
                                            }
                                            for (int i = 0; i < scftr.messages.Count; ++i)
                                            {
                                                if (scftr.messages[i].loaded == true)
                                                {
                                                    if (scftr.messages[i].GetType() == typeof(AudioMessage))
                                                    {
                                                        (scftr.messages[i] as AudioMessage).VolumeSlider = null;
                                                        (scftr.messages[i] as AudioMessage).MediaSource = null; (scftr.messages[i] as AudioMessage).TimeSlider = null;
                                                        (scftr.messages[i] as AudioMessage).isMediaPaused = false;
                                                        (scftr.messages[i] as AudioMessage).isseeking = false;
                                                    }
                                                    if (scftr.savedmessages.Any(x => x.idofmessage == scftr.messages[i].idofmessage) == false)
                                                    {
                                                        scftr.savedmessages.Add(scftr.messages[i]);
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }
                              
                                EnableorDisableLoading(ms, true);
                                SendPackets.LeaveAllServers();
                                SendPackets.ClientRequestJoinRoom(servername, true);
                            });
                        }).Start();
                    }
                }
            }
            ServerDirectoryListBox.UnselectAll();
        }
        private void PendingFriendsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DMSListBox.SelectedItem == null)
            {
                return;
            }
        }
        private void AllFriendsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DMSListBox.SelectedItem == null)
            {
                return;
            }
        }

        public void StartDM(UserList acc)
        {
          
            for (int i = 0; i < DMSListBox.Items.Count; i++)
            {
                if (DMSListBox.Items[i].GetType() == typeof(UserList))
                {
                    if (((UserList)DMSListBox.Items[i]).AccountID == acc.AccountID)
                    {
                        return;
                    }
                }
            }
            DMSListBox.Items.Add(acc);
        }
        private void DMSListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DMSListBox.SelectedItem == null)
            {
                return;
            }

            if (DMSListBox.SelectedItem.GetType() == typeof(UserList))
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
                    if (ChatManager.CurrentSavedServer != null || ChatManager.CurrentChatWithUser != null)
                    {
                        SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
                        if (ss != null)
                        {
                            int[] keyarray = new int[ChatManager.channels.Keys.Count];
                            ss.channels.Keys.CopyTo(keyarray, 0);
                            foreach (int k in keyarray)// update all audio messages slider            
                            {
                                SavedChannelsForTempRoom scftr = (ss.channels[k] as SavedChannelsForTempRoom);
                                if (scftr == null)
                                {
                                    continue;
                                }
                                for (int i = 0; i < scftr.messages.Count; ++i)
                                {
                                    if (scftr.messages[i].loaded == true)
                                    {
                                        if (scftr.messages[i].GetType() == typeof(AudioMessage))
                                        {
                                            (scftr.messages[i] as AudioMessage).VolumeSlider = null;
                                            (scftr.messages[i] as AudioMessage).MediaSource = null; (scftr.messages[i] as AudioMessage).TimeSlider = null;
                                            (scftr.messages[i] as AudioMessage).isMediaPaused = false;
                                            (scftr.messages[i] as AudioMessage).isseeking = false;
                                        }
                                        if (scftr.savedmessages.Any(x => x.idofmessage == scftr.messages[i].idofmessage) == false)
                                        {
                                            scftr.savedmessages.Add(scftr.messages[i]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (ChatManager.CurTempRoom != null || ChatManager.CurrentSavedServer != null)
                    {
                        SendPackets.LeaveAllServers();
                    }
                    EnableorDisableLoading("Loading private messages...", true);
                   
                });
                int _accountid = (DMSListBox.SelectedItem as UserList).AccountID;
              
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.NotifyServerOfPMStart(_accountid);
                });

            }
            DMSListBox.UnselectAll();
        }
        private void ChatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatListBox.SelectedItem == null)
            {
                return;
            }

            if (ChatListBox.SelectedItem.GetType() == typeof(ImageMessage))
            {
                ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", true);
                ImageMessage msg = (ImageMessage)ChatListBox.SelectedItem;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    string link = ESMA.DatabaseCalls.host + "/" + Methods.GetServerDir();
                    new Thread(delegate ()
                    {
                        if ( String.IsNullOrEmpty(msg.ImageCachePath_Thumbnail) == true)
                        {
                            ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", false);
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                ChatListBox.UnselectAll();
                            });
                            return;
                        }


                        string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyImage);
                        if (File.Exists(file) == false || File.Exists(file) && Methods.GetFileMD5Signature(file) != msg.MyImage)
                        {
                            try
                            {


                                string webString = "";
                                using (System.Net.WebClient webclient = new System.Net.WebClient())
                                {
                                    webString = webclient.DownloadString(link + msg.MyImage);

                                }
                                if (String.IsNullOrEmpty(webString) == true)
                                {
                                    throw new Exception();
                                }
                                byte[] decrypted_bytes;
                                string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);

                                if (Decrypt != null)
                                {
                                    decrypted_bytes = Convert.FromBase64String(Decrypt);

                                }
                                else
                                {
                                    throw new Exception();
                                }
                                if (File.Exists(file))
                                {
                                    File.Delete(file);
                                }
                                File.WriteAllBytes(file, decrypted_bytes);
                            }
                            catch
                            {
                                ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", false);
                                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                                {
                                    MainWindow.instance.MessageBoxShow("Could not view image.");
                                }));

                            }
                        }
                        ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", false);
                        this.Dispatcher.Invoke(() =>
                        {
                            if (Methods.GetFileMD5Signature(file) == System.IO.Path.GetFileName(file))
                            {
                                openedimage.Source = new BitmapImage(new Uri(file));
                            }
                            else
                            {
                                openedimage.Source = UndefinedSource;
                            }
                        });

                        SetFormVisibility(4);
                    }).Start();
                });
            }
            if (ChatListBox.SelectedItem.GetType() == typeof(VideoMessage))
            {
                ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", true);
                VideoMessage msg = Methods.DeepClone ((VideoMessage)ChatList.SelectedItem) as VideoMessage;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                 
                    string link = ESMA.DatabaseCalls.host + "/" + Methods.GetServerDir();

                    
                    new Thread(delegate ()
                    {
                        if ( String.IsNullOrEmpty(msg.ImageCachePath_Thumbnail) == true)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                ChatListBox.UnselectAll();
                            });

                            ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", false);
                            return;
                        }

                        string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyVideo + ".mp4");
                        if (File.Exists(file) == false || File.Exists(file) && Methods.GetFileMD5Signature(file) != msg.MyVideo)
                        {
                            try
                            {
                                string webString = "";
                                using (System.Net.WebClient webclient = new System.Net.WebClient())
                                {
                                    webString = webclient.DownloadString(link + msg.MyVideo);
                                }
                                if (String.IsNullOrEmpty(webString) == true)
                                {
                                    throw new Exception();
                                }
                                byte[] decrypted_bytes;
                                string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);

                                if (Decrypt != null)
                                {
                                    decrypted_bytes = Convert.FromBase64String(Decrypt);

                                }
                                else
                                {
                                    throw new Exception();
                                }
                                if (File.Exists(file))
                                {
                                    File.Delete(file);
                                }
                                File.WriteAllBytes(file, decrypted_bytes);
                            }
                            catch
                            {
                                ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", false);
                                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                                {
                                    MainWindow.instance.MessageBoxShow("Could not view video.");
                                }));
                            }
                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            VideoElement.Source = new Uri(file);

                            VideoElement.Play();
                            isMediaPaused = false;
                            playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
                         
                        });
                        ESMA.MainWindow.instance.EnableorDisableLoading("Loading media...", false);
                        SetFormVisibility(5);
                    }).Start();
                });
            }
        End:;
            ChatListBox.UnselectAll();
        }
        public void ChangeTheSelectionOfChannelListBox(int key)
        {
            this.Dispatcher.Invoke(() =>
            {
                LastSelectedTextChannel = key;
                for (int i = 0; i < ChannelList.Items.Count; ++i)
                {
                    if (ChannelList.Items[i].GetType() == typeof(ChannelsForTempRoom))
                    {
                        if ((ChannelList.Items[i] as ChannelsForTempRoom).key == LastSelectedTextChannel)
                        {
                            ChannelListBox.SelectedItem = ChannelList.Items[i];
                        }
                    }
                }
            });
        }
        private void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserListBox.SelectedItem == null)
            {
                return;
            }
            if (UserListBox.SelectedItem.GetType() == typeof(UserList))
            {
                int aid = ((UserList)UserListBox.SelectedItem).AccountID;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {

                    SendPackets.RequestOpenProfile(ChatManager.CurrentSavedServer, aid);
                });
            }
            UserListBox.UnselectAll();
        }

        public void CreateSavedServerFromCurrent(bool temp)
        {
            try
            {
                if (ChatManager.CurTempRoom == null && ChatManager.CurrentSavedServer == null && ChatManager.CurrentChatWithUser == null)
                {
                    return;
                }
                int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);

                SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
                if (ss == null)
                {
                    SavedServersOnMemory saved = new SavedServersOnMemory(serverid);
                    ChatManager.SavedServers.Add(saved);
                    saved.channels = new Hashtable(ChatManager.channels);
                    int[] keyarray = new int[ChatManager.channels.Keys.Count];
                    ChatManager.channels.Keys.CopyTo(keyarray, 0);
                    foreach (int k in keyarray)
                    {
                        if (saved.channels[k].GetType() == typeof(ChannelsForTempRoom))
                        {
                            ChannelsForTempRoom old = (saved.channels[k] as ChannelsForTempRoom);
                            SavedChannelsForTempRoom newchannel = new SavedChannelsForTempRoom(old.ChannelName, old.key);
                            saved.channels[k] = newchannel;
                            (saved.channels[k] as SavedChannelsForTempRoom).messages = new List<Message>((ChatManager.channels[k] as ChannelsForTempRoom).messages); 
                        }
                    }
                    foreach (int k in keyarray)
                    {
                        if (ChatManager.channels[k].GetType() == typeof(ChannelsForTempRoom))
                        {
                            ChannelsForTempRoom channel = ChatManager.channels[k] as ChannelsForTempRoom;
                            if (channel.messages.Count > 30)
                            {
                                channel.messages.RemoveRange(0, channel.messages.Count - 30);
                            }
                        }
                    }
                }
                else
                {
                    if (ss.channels.ContainsKey(ChatManager.CurrentChannel) == false)
                    {
                        return;
                    }
                    if (ss.channels[ChatManager.CurrentChannel].GetType() == typeof(SavedChannelsForTempRoom))
                    {
                        if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count >= (ss.channels[ChatManager.CurrentChannel] as SavedChannelsForTempRoom).messages.Count)
                        {
                            (ss.channels[ChatManager.CurrentChannel] as SavedChannelsForTempRoom).messages = new List<Message>((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages);
                        }
                    }
                    if (ChatManager.channels[ChatManager.CurrentChannel].GetType() == typeof(ChannelsForTempRoom))
                    {
                        ChannelsForTempRoom channel = ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom;
                        if (channel.messages.Count > 30)
                        {
                            channel.messages.RemoveRange(0, channel.messages.Count - 30);
                        }
                    }
                }
            }
            catch { }
        }

        public void ChannelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelListBox.SelectedItem == null)
            {
                return;
            }


            if (ChannelListBox.SelectedItem.GetType() == typeof(ChannelsForTempRoom) && LastSelectedTextChannel != (ChannelListBox.SelectedItem as ChannelsForTempRoom).key)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (ChatManager.StillFetchingMessages == true)
                    {
                        return;
                    }
                    int[] keyarray = new int[ChatManager.channels.Keys.Count];
                    ChatManager.channels.Keys.CopyTo(keyarray, 0);
                    foreach (int k in keyarray)
                    {
                        if (ChatManager.channels[k].GetType() == typeof(ChannelsForTempRoom))
                        {
                            (ChatManager.channels[k] as ChannelsForTempRoom).searchedmessages.Clear();
                        }
                    }
                    if (ChatManager.channels.ContainsKey(ChatManager.CurrentChannel) == true)
                    {
                        
                        for (int i = 0; i < (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages.Count; ++i)
                        {
                            if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages[i].GetType() == typeof(AudioMessage))
                            {
                                AudioMessage msg = ((AudioMessage)(ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages[i]);
                               msg.VolumeSlider = null;
                                msg.MediaSource = null;
                                msg.isMediaPaused = false;
                                msg.isseeking = false;
                                msg.TimeSlider = null;
                            }
                        }
                        MainWindow.instance.CreateSavedServerFromCurrent(ChatManager.CurTempRoom != null);
                    }
                    BackgroundWorker.Searchactions.Clear();
                    this.Dispatcher.Invoke(() =>
                    {
                        ChatManager.CurrentChannel = (ChannelListBox.SelectedItem as ChannelsForTempRoom).key;
                        ChatListBox.Items.Clear();
                        if ((ChannelListBox.SelectedItem as ChannelsForTempRoom).initialized == false)
                        {

                            (ChannelListBox.SelectedItem as ChannelsForTempRoom).initialized = true;

                            NetworkManager.TaskTo_PrimaryActionThread(() =>
                            {
                                if ((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchTotal != 0)
                                {
                                    SendPackets.RequestMoreMessages((ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetched, (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).MessagesToFetchTotal, ChatManager.CurrentChannel);
                                }
                            });
                        }
                        else
                        {
                            NetworkManager.TaskTo_PrimaryActionThread(() =>
                            {
                                System.Collections.Generic.List<Message> temp = (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).messages;
                                for (int i = 0; i < temp.Count; i++)
                                {
                                    ChatRefreshList(0, temp[i], false);
                                }
                            });
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            MainWindow.LoadedChatListBoxCount = 0;
                            ChatListBoxCount = 0;
                            UnconditionalChatListBoxCount = 0;
                        });
                        this.Dispatcher.Invoke(() =>
                        {
                            if ((ChannelListBox.SelectedItem as ChannelsForTempRoom) == null)
                                return;
                            ChatInput.IsEnabled = ChatManager.my_room_user._Roles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == x) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).powers[4] == 1) ? true : !(ChannelListBox.SelectedItem as ChannelsForTempRoom).read_only;
                            UploadAudio.IsEnabled = ChatManager.my_room_user._Roles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == x) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).powers[4] == 1) ? true : !(ChannelListBox.SelectedItem as ChannelsForTempRoom).read_only;
                            UploadFile.IsEnabled = ChatManager.my_room_user._Roles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == x) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).powers[4] == 1) ? true : !(ChannelListBox.SelectedItem as ChannelsForTempRoom).read_only;
                            UploadVideo.IsEnabled = ChatManager.my_room_user._Roles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == x) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).powers[4] == 1) ? true : !(ChannelListBox.SelectedItem as ChannelsForTempRoom).read_only;
                            UploadImage.IsEnabled = ChatManager.my_room_user._Roles.Any(x => (ChatManager.RoleList.Find(xx => xx.id == x) ?? new RolesList(-1, "", new int[6], "#FFFFFF", -1)).powers[4] == 1) ? true : !(ChannelListBox.SelectedItem as ChannelsForTempRoom).read_only;
                            LastSelectedTextChannel = (ChannelListBox.SelectedItem as ChannelsForTempRoom).key;
                        });
                    });
                });
            }
            if (ChannelListBox.SelectedItem.GetType() == typeof(VoiceChannelsForTempRoom))
            {
                int k = (ChannelListBox.SelectedItem as VoiceChannelsForTempRoom).key;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.SendNewChannel(k, ChatManager.CurrentSavedServer ?? -1);
                });
                for (int i = 0; i < ChannelList.Items.Count; ++i)
                {
                    if (ChannelList.Items[i].GetType() == typeof(ChannelsForTempRoom))
                    {
                        if ((ChannelList.Items[i] as ChannelsForTempRoom).key == LastSelectedTextChannel)
                        {
                            ChannelListBox.SelectedItem = ChannelList.Items[i];
                        }

                    }
                }
            }
        End:;
        }
        private void OnChannelListLoaded(object sender, RoutedEventArgs e)
        {
            if ((sender as ListBox).DataContext.GetType() == typeof(VoiceChannelsForTempRoom))
            {
                ((sender as ListBox).DataContext as VoiceChannelsForTempRoom).ListBoxItem = sender as ListBox;
            }
        }
        private void VoiceChannelStatusLoaded(object sender, RoutedEventArgs e)
        {
            if (((sender as TextBlock).DataContext as UserList) != null)
            {
                ((sender as TextBlock).DataContext as UserList).TransmissionStatus = sender as TextBlock;
            }
        }
        private void ChatItemAdded(object sender, RoutedEventArgs e)
        {
            LoadedChatListBoxCount++;

            if (ChatManager.StillLoadingMessages == false && (MainWindow.instance.ChatListBoxScroll.ScrollableHeight - MainWindow.instance.ChatListBoxScroll.VerticalOffset) < 400)
            {
                MainWindow.instance.ChatListBoxScroll.ScrollToBottom();
            }
            else
            {

            }
        }
        private void ChatItemRemoved(object sender, RoutedEventArgs e)
        {
            //  LoadedChatListBoxCount--;
        }
        private void UserText(object sender, RoutedEventArgs e)
        {
            UserList _u = ((sender as Image).DataContext as UserList);
            if (_u != null)
            {
                //   (sender as TextBlock).Foreground = pl.textColor;
            }
        }
        void OnProfileNameLoaded(object sender, RoutedEventArgs e)
        {

            if (((sender as TextBlock).DataContext as Message) != null)
            {
                int account_ID = ((sender as TextBlock).DataContext as Message) != null ? ((sender as TextBlock).DataContext as Message).accountid : (((sender as TextBlock).DataContext as UserList) != null ? ((sender as TextBlock).DataContext as UserList).AccountID : -1);
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {     
                    if (ChatManager.GetAnyChatUserByID(account_ID) != null)
                    {
                        SolidColorBrush scb = ChatManager.GetAnyChatUserByID(account_ID).TextColor;
                        this.Dispatcher.Invoke(() =>
                        {
                            (sender as TextBlock).Foreground = scb;
                        });
                        ChatManager.GetAnyChatUserByID(account_ID).ProfileNames.Add(sender as TextBlock);
                    }
                });
            }
        }
        void OnProfileNameUnloaded(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBlock) != null)
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (ChatManager.UserList_FULL == null)
                        return;
                    TextBlock tb = sender as TextBlock;
                    for (int i = 0; i < ChatManager.UserList_FULL.Count; ++i)
                    {
                        if (ChatManager.UserList_FULL[i].ProfileNames.Contains(tb))
                        {
                            ChatManager.UserList_FULL[i].ProfileNames.Remove(tb);
                        }
                    }
                });
            }
        }
        public static void RefreshProfileNameColor()
        {
            //always will be on primary action thread

            for (int i = 0; i < ChatManager.UserList_FULL.Count; ++i)
            {
                for (int ii = 0; ii < ChatManager.UserList_FULL[i].ProfileNames.Count; ii++)
                {
                    UserList _u = ChatManager.UserList_FULL[i];
                    TextBlock pn = ChatManager.UserList_FULL[i].ProfileNames[ii];
                    MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        pn.Foreground = _u.TextColor;
                    });
                }
            }
        }
        public void RunOnPrimaryActionThread(Action a) // waits until the task has been executed
        {
            NetworkManager.TaskTo_PrimaryActionThread(a);
            int attempts = 0;
            while (NetworkManager.ActionsOnPrimaryActionThread.Contains(a) == true || NetworkManager.Copied_ActionsOnPrimaryActionThread.Contains(a) == true)
            {
                if (attempts++ == 40)
                {
                    return;
                }
                Thread.Sleep(15);
            }
        }
        void OnProfilePicLoaded(object sender, RoutedEventArgs e)
        {

            if (((sender as Image).DataContext as Message) != null)
            {
                Message msg = ((sender as Image).DataContext as Message);
                int msgaccount_ID = msg.accountid;
                if (msg.accountid == -2)
                {
                    (sender as Image).Source = DefaultProfilePicture;
                    return;
                }
                Image sai = (sender as Image);
                int saidataaccount_ID = (sai.DataContext as Message).accountid;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (ChatManager.GetUserCacheByID(msgaccount_ID) == null)
                    {
                        DatabaseCalls.AddUserCache(msgaccount_ID);
                    }
                    else
                    {
                        if (ChatManager.GetUserCacheByID(msgaccount_ID) != null && ChatManager.GetUserCacheByID(msgaccount_ID).profilepic != null)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                sai.Source = ChatManager.GetUserCacheByID(msgaccount_ID).profilepic;
                            });
                            return;
                        }
                    }
                    new Thread(delegate ()
                    {
                        int tries = 0;
                        bool check = true;
                        while (check == true)
                        {
                            Action a = (Action)(() =>
                            {
                                check = ChatManager.GetUserCacheByID(saidataaccount_ID) == null && NetworkManager.MyAccountID != null || ChatManager.GetUserCacheByID(saidataaccount_ID).profilepic == null && NetworkManager.MyAccountID != null;                 
                            });
                            RunOnPrimaryActionThread(a);
                            //
                            tries++;
                            if (tries == 10)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    sai.Source = UndefinedSource as ImageSource;
                                });
                                return;
                            }
                            Thread.Sleep(500);
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.GetUserCacheByID(saidataaccount_ID) != null && ChatManager.GetUserCacheByID(saidataaccount_ID).profilepic != null)
                            {
                                ImageSource source = ChatManager.GetUserCacheByID(saidataaccount_ID).profilepic;
                                this.Dispatcher.Invoke(() =>
                                {
                                    sai.Source = source;
                                });
                            }
                        });
                    }).Start();
                });
            }
            if ((sender as Image).DataContext.GetType() == typeof(UserList))
            {
                int account_ID = ((sender as Image).DataContext as UserList).AccountID;
                Image sai = (sender as Image);
                int saidataaccount_ID = (sai.DataContext as UserList).AccountID;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (ChatManager.GetChatUserByID(account_ID) != null)
                    {
                        ChatManager.GetChatUserByID(account_ID).ProfileImages.Add(sender as Image);
                    }
                    if (ChatManager.GetUserCacheByID(account_ID) == null)
                    {
                        DatabaseCalls.AddUserCache(account_ID);
                    }
                    else
                    {
                        if (ChatManager.GetUserCacheByID(account_ID) != null && ChatManager.GetUserCacheByID(account_ID).profilepic != null)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                sai.Source = ChatManager.GetUserCacheByID(account_ID).profilepic;
                            });
                            return;
                        }
                    }

                    new Thread(delegate ()
                    {
                        int tries = 0;
                        bool check = true;
                        while (check == true)
                        {
                            Action a = (Action)(() =>
                            {
                                check = ChatManager.GetUserCacheByID(saidataaccount_ID) == null && NetworkManager.MyAccountID != null || ChatManager.GetUserCacheByID(saidataaccount_ID).profilepic == null && NetworkManager.MyAccountID != null;
                            });
                            RunOnPrimaryActionThread(a);
                            //
                            tries++;
                            if (tries == 10)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    sai.Source = UndefinedSource as ImageSource;
                                });
                                return;
                            }
                            Thread.Sleep(500);
                        }
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            if (ChatManager.GetUserCacheByID(saidataaccount_ID) != null && ChatManager.GetUserCacheByID(saidataaccount_ID).profilepic != null)
                            {
                                ImageSource source = ChatManager.GetUserCacheByID(saidataaccount_ID).profilepic;
                                this.Dispatcher.Invoke(() =>
                                {
                                    sai.Source = source;
                                });
                            }
                        });
                    }).Start();
                });
            }


        }
        private void OnProfilePicRemoved(object sender, RoutedEventArgs e)
        {
            if ((sender as Image).DataContext.GetType() == typeof(UserList))
            {
                if ((sender as Image) != null && (sender as Image).DataContext != null && ((sender as Image).DataContext as Message) != null)
                {
                    int account_ID = ((sender as Image).DataContext as Message).accountid;
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.GetChatUserByID(account_ID) != null)
                        {
                            if (ChatManager.GetChatUserByID(account_ID).ProfileImages.Contains(sender as Image))
                            {
                                ChatManager.GetChatUserByID(account_ID).ProfileImages.Remove(sender as Image);
                            }
                        }
                    });
                }

            }
        }
        bool mouse_down = false;
        Point move_point;
        Point mouse_point;
        private void Inspect_Zoom(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? .1 : -.1;

            double potentialx = scale_t.ScaleX + zoom * scale_t.ScaleX;
            double potentialy = scale_t.ScaleY + zoom * scale_t.ScaleY;
            if (potentialx <= 1 || potentialy <= 1)
            {

                Point pt = Mouse.GetPosition(openedimage);

                double X = pt.X * scale_t.ScaleX + trans_t.X;
                double Y = pt.Y * scale_t.ScaleY + trans_t.Y;

                scale_t.ScaleX = 1;
                scale_t.ScaleY = 1;

                trans_t.X = X - pt.X * scale_t.ScaleX;
                trans_t.Y = Y - pt.Y * scale_t.ScaleY;
            }
            else
            {
                Point pt = Mouse.GetPosition(openedimage);
                double X = pt.X * scale_t.ScaleX + trans_t.X;
                double Y = pt.Y * scale_t.ScaleY + trans_t.Y;

                scale_t.ScaleX += zoom * scale_t.ScaleX;
                scale_t.ScaleY += zoom * scale_t.ScaleY;

                trans_t.X = X - pt.X * scale_t.ScaleX;
                trans_t.Y = Y - pt.Y * scale_t.ScaleY;
            }
        }

        private void Inspec_down(object sender, MouseButtonEventArgs e)
        {
            mouse_point = Mouse.GetPosition(pic_grids);
            move_point = new Point(trans_t.X, trans_t.Y);
            mouse_down = true;
        }

        private void Inspect_Up(object sender, MouseButtonEventArgs e)
        {
            mouse_down = false;
        }

        private void Inspect_drag(object sender, MouseEventArgs e)
        {
            if (mouse_down == true)
            {
                Vector pos = mouse_point - Mouse.GetPosition(pic_grids);
                trans_t.X = move_point.X - pos.X;
                trans_t.Y = move_point.Y - pos.Y;
            }
        }
        private void Inspect_Leave(object sender, MouseEventArgs e)
        {
            mouse_down = false;
        }
        private void gobackmovie_Click(object sender, RoutedEventArgs e)
        {
            if (VideoElement.Source != null)
            {
                VideoElement.Stop();
                isMediaPaused = false;
                playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
                VideoElement.Source = null;
            }
            openedimage.Source = null;
            scale_t.ScaleX = 1;
            scale_t.ScaleY = 1;
            trans_t.X = 0;
            trans_t.Y = 0;
            SetFormVisibility(3);
        }
        private void LeaveVoiceChat_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(UserList))
            {
                if (((sender as Button).DataContext as UserList).AccountID == NetworkManager.MyAccountID)
                {
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        //leave
                        SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
                    });
                }

            }
        }
        private void RemoveFromRole_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext.GetType() == typeof(UserList))
            {
                int id = (RolesEditListBox.SelectedItem as RolesList).id;
                int account_ID = ((sender as Button).DataContext as UserList).AccountID;
                if (id < 2)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("This role can't be modified like this.");
                    }));
                    return;
                }
                EnableorDisableLoading("Changing user's role...", true);
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.RemoveOrAddPLRole((int)ChatManager.CurrentSavedServer, account_ID, id, false);
                });
            }
        }
        private void MoveRoleUp(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext.GetType() == typeof(RolesList))
            {
                RolesList role = (sender as Button).DataContext as RolesList;
                EnableorDisableLoading("Changing role precedence...", true);
                int id = role.id;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.ChangeRolePrecedence((int)ChatManager.CurrentSavedServer, id, true);
                });
            }
        }
        private void MoveRoleDown(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext.GetType() == typeof(RolesList))
            {
                RolesList role = (sender as Button).DataContext as RolesList;
                EnableorDisableLoading("Changing role precedence...", true);
                int id = role.id;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.ChangeRolePrecedence((int)ChatManager.CurrentSavedServer, id, false);
                });
            }
        }
        private void RemoveFromChannelRole_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(RolesList))
            {
                if (AddingRoleToChannel == false)
                {
                    RolesList r = (sender as Button).DataContext as RolesList;
                    if ((ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) == null && (ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom) == null)
                    {
                        return;
                    }
                    EnableorDisableLoading("Changing channel access...", true);
                    int key = (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) != null ? (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom).key : (ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom).key;
                    int id = r.id;
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        SendPackets.RemoveOrAddChannelRole((int)ChatManager.CurrentSavedServer, key, id, false);
                    });
                }
            }
        }
        private void AcceptFriend_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(UserList))
            {
                EnableorDisableLoading("Adding friend...", true);
                int account_ID = ((sender as Button).DataContext as UserList).AccountID;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.AddOrRemoveFriend(0, account_ID);
                });
            }
        }
        private void RejectFriend_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(UserList))
            {
                EnableorDisableLoading("Rejecting friend...", true);
                int account_ID = ((sender as Button).DataContext as UserList).AccountID;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.AddOrRemoveFriend(1, account_ID);
                });
            }
        }
        private void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(UserList))
            {
                EnableorDisableLoading("Removing friend...", true);
                int account_ID = ((sender as Button).DataContext as UserList).AccountID;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.AddOrRemoveFriend(2, account_ID);
                });
            }
        }
        private void DMFriend_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(UserList))
            {
                StartDM(((sender as Button).DataContext as UserList));
            }
        }
        private void MessageAudioPlay_Click(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).DataContext.GetType() == typeof(AudioMessage))
            {
                AudioMessage msg = (sender as Button).DataContext as AudioMessage;
             
                if ( String.IsNullOrEmpty(msg.MyAudio) == false &&  String.IsNullOrEmpty(msg.AudioCachePath) == false && File.Exists(msg.AudioCachePath) == false ||  String.IsNullOrEmpty(msg.MyAudio) == false &&  String.IsNullOrEmpty(msg.AudioCachePath) == false && File.Exists(msg.AudioCachePath) == true && ESMA.Methods.GetFileMD5Signature(msg.AudioCachePath) != msg.MyAudio)
                {
                    ESMA.BackgroundWorker.Download_Actions.Add((Action)(() =>
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            (sender as Button).IsEnabled = false;
                        });
                        string link = ESMA.DatabaseCalls.host + "/" + Methods.GetServerDir();

                        string webString = "";
                        try
                        {
                            using (System.Net.WebClient webclient = new System.Net.WebClient())
                            {
                                webString = webclient.DownloadString(link + msg.MyAudio);
                            }
                            if (String.IsNullOrEmpty(webString) == true)
                            {
                                throw new Exception();
                            }
                        }
                        catch   //fail to retrieve
                        {
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                             
                                (sender as Button).IsEnabled = true;
                            });
                            return;
                        }

                        byte[] decrypted_bytes;
                        string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);
                        if (Decrypt != null)
                        {
                            decrypted_bytes = Convert.FromBase64String(Decrypt); //convert decrypted string back in to bytes
                        }
                        else //decryption failed
                        {
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                            
                                (sender as Button).IsEnabled = true;
                            });
                            return;
                        }
                        if (File.Exists(msg.AudioCachePath) == false || File.Exists(msg.AudioCachePath) == true && ESMA.Methods.GetFileMD5Signature(msg.AudioCachePath) != msg.MyAudio)
                        {
                            try
                            {
                                if (File.Exists(msg.AudioCachePath))
                                {
                                    File.Delete(msg.AudioCachePath);
                                }
                                File.WriteAllBytes(msg.AudioCachePath, decrypted_bytes); // create file with bytes
                            }
                            catch //failed to write file to cache
                            {
                                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                                {
                                  
                                    (sender as Button).IsEnabled = true;
                                });
                                return;
                            }
                        }
                        msg.FileExists = true;
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                           
                            (sender as Button).IsEnabled = true;
                            MessageAudioPlay_Click(sender, e);
                        });
                    }));
                    return;
                }
                else if (msg != null && msg.isMediaPaused == true || msg != null && msg.MediaSource == null || msg != null && msg.MediaSource.Source == null)
                {
                    if (msg.MediaSource == null || msg.MediaSource.Source == null)
                    {
                        DependencyObject slider = VisualTreeHelper.GetChild((VisualTreeHelper.GetParent(sender as Button) as DependencyObject), 0);
                      
                        msg.TimeSlider = (Slider)slider;
                        msg.TimeSlider.IsEnabled = true;
                        DependencyObject media = VisualTreeHelper.GetChild((VisualTreeHelper.GetParent(sender as Button) as DependencyObject), 2);
                        msg.MediaSource = (MediaElement)media;
                       
                        msg.MediaSource.Source = new Uri(msg.AudioCachePath);
                        msg.MediaSource.LoadedBehavior = MediaState.Manual;


                        this.PreviewMouseUp += (s, a) =>
                        {
                            if (msg.isseeking == true)
                            {
                                if (msg.MediaSource.Source != null)
                                {
                                    msg.isseeking = false;
                                    msg.MediaSource.Play();
                                  
                                    msg.isMediaPaused = false;
                                    (sender as Button).Content = "■";
                                }
                            }

                        };
                        msg.MediaSource.MediaEnded += (s, a) =>
                        {
                            msg.isMediaPaused = true;
                            (sender as Button).Content = "▶";
                        };

                    }
                    else
                    {
                        if (msg.isseeking == false && msg.MediaSource.Position >= msg.MediaSource.NaturalDuration)
                        {
                            msg.MediaSource.Position = new TimeSpan();
                        }
                    }
                    if (msg.isseeking == false)
                    {
                        msg.isMediaPaused = false;
                        msg.MediaSource.Play();
                       
                    }
                    (sender as Button).Content = "■";
                }
                else if (msg.isseeking == false)
                {
                    (sender as Button).Content = "▶";
                    msg.MediaSource.Pause();
                    msg.isMediaPaused = true;
                }

            }
        }
        private void mybutton_Click(object sender, RoutedEventArgs e)
        {
            int channel = ChatManager.CurrentChannel;
            if ((sender as Button).DataContext.GetType() == typeof(ImageMessage))
            {

                ImageMessage msg = (sender as Button).DataContext as ImageMessage;
                string _dir = "/" + Methods.GetServerDir();
                ESMA.BackgroundWorker.Download_Actions.Add((Action)(() =>
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        (sender as Button).Visibility = Visibility.Hidden;
                    });

                    try
                    {

                        string webString = "";
                        
                        using (System.Net.WebClient webclient = new System.Net.WebClient())
                        {
                            webString = webclient.DownloadString(DatabaseCalls.host + _dir + msg.MyImage_Thumbnail);
                        }
                        if (String.IsNullOrEmpty(webString) == true)
                        {
                            throw new Exception();
                        }
                        byte[] decrypted_bytes;
                        string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);

                        if (Decrypt != null)
                        {
                            decrypted_bytes = Convert.FromBase64String(Decrypt);

                        }
                        else
                        {
                            throw new Exception();
                        }

                        string exportfile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyImage);
                        string exportfile_t = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyImage_Thumbnail);
                        if (File.Exists(exportfile_t))
                        {
                            File.Delete(exportfile_t);
                        }

                        File.WriteAllBytes(exportfile_t, decrypted_bytes);
                        ImageMessage reborn = new ImageMessage(msg.accountid, msg.sender, msg.message, msg.idofmessage, msg.MyImage, exportfile, msg.MyImage_Thumbnail, exportfile_t, msg.date);
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            ChatRefreshList(2, reborn, msg, channel, (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count > 0);
                        });
                    }
                    catch
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                          {
                              (sender as Button).Visibility = Visibility.Visible;
                          });
                    }
                }));
            }
            if ((sender as Button).DataContext.GetType() == typeof(VideoMessage))
            {

                VideoMessage msg = (sender as Button).DataContext as VideoMessage;
                ESMA.BackgroundWorker.Download_Actions.Add((Action)(() =>
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        (sender as Button).Visibility = Visibility.Hidden;
                    });

                    try
                    {
                     
                        string _dir = "/" + Methods.GetServerDir();
                        string webString = "";
                        using (System.Net.WebClient webclient = new System.Net.WebClient())
                        {
                            webString = webclient.DownloadString(DatabaseCalls.host + _dir + msg.MyImage_Thumbnail);
                        }
                        if (String.IsNullOrEmpty(webString) == true)
                        {
                            throw new Exception();
                        }
                        byte[] decrypted_bytes;
                        string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);

                        if (Decrypt != null)
                        {
                            decrypted_bytes = Convert.FromBase64String(Decrypt);

                        }
                        else
                        {
                            throw new Exception();
                        }

                        string exportfile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyVideo);
                        string exportfile_t = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyImage_Thumbnail);
                        if (File.Exists(exportfile_t))
                        {
                            File.Delete(exportfile_t);
                        }

                        File.WriteAllBytes(exportfile_t, decrypted_bytes);
                        VideoMessage reborn = new VideoMessage(msg.accountid, msg.sender, msg.message, msg.idofmessage, msg.MyVideo, exportfile, msg.MyImage_Thumbnail, exportfile_t, msg.date);
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            ChatRefreshList(2, reborn, msg, channel, (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count > 0);
                        });
                    }
                    catch
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            (sender as Button).Visibility = Visibility.Visible;
                        });
                    }
                }));
            }
            if ((sender as Button).DataContext.GetType() == typeof(AudioMessage))
            {

                AudioMessage msg = (sender as Button).DataContext as AudioMessage;
                ESMA.BackgroundWorker.Download_Actions.Add((Action)(() =>
                {
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        (sender as Button).Visibility = Visibility.Hidden;
                    });

                    try
                    {
                        string _dir = "/" + Methods.GetServerDir(); 
                        string webString = "";
                        using (System.Net.WebClient webclient = new System.Net.WebClient())
                        {
                            webString = webclient.DownloadString(DatabaseCalls.host + _dir + msg.MyAudio);
                        }
                        if (String.IsNullOrEmpty(webString) == true)
                        {
                            throw new Exception();
                        }
                        byte[] decrypted_bytes;
                        string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);

                        if (Decrypt != null)
                        {
                            decrypted_bytes = Convert.FromBase64String(Decrypt);

                        }
                        else
                        {
                            throw new Exception();
                        }

                        string exportfile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatapptemp\\" + msg.MyAudio);
                        if (File.Exists(exportfile))
                        {
                            File.Delete(exportfile);
                        }

                        File.WriteAllBytes(exportfile, decrypted_bytes);
                        AudioMessage reborn = new AudioMessage(msg.accountid, msg.sender, msg.message, msg.idofmessage, msg.MyAudio, exportfile, msg.date);
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            ChatRefreshList(2, reborn, msg, channel, (ChatManager.channels[ChatManager.CurrentChannel] as ChannelsForTempRoom).searchedmessages.Count > 0);
                        });
                    }
                    catch
                    {
                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                        {
                            (sender as Button).Visibility = Visibility.Visible;
                        });
                    }
                }));
            }
        }
        private void DownloadFile_Click(object sender, RoutedEventArgs e)
        {

            if (FileDialogOpened == true)
            {
                return;
            }
            FileMessage msg = (sender as Button).DataContext as FileMessage;

            
            MainWindow.FileDialogOpened = true;
            new Thread(delegate ()
            {
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Visible;
                });
                try
                {

                    SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                    dialog.DefaultExt = "." + msg.Extension;

                    string builtstring = String.Format("{0} (.{1})|*.{1}", msg.Extension.ToUpper(), msg.Extension.ToLower());
                    dialog.Filter = builtstring;

    
                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        ESMA.BackgroundWorker.Download_Actions.Add((Action)(() =>
                        {
                            string webString = "";
                            string _dir = "/" + Methods.GetServerDir();
                            using (System.Net.WebClient webclient = new System.Net.WebClient())
                            {
                                webString = webclient.DownloadString(DatabaseCalls.host + _dir + msg.MyFile);
                            }
                            if (String.IsNullOrEmpty(webString) == true)
                            {
                                throw new Exception();
                            }
                            byte[] decrypted_bytes;
                            string Decrypt = ESMA.Methods.DecryptString(webString, ESMA.ChatManager.selectedroompassasbytes);

                            if (Decrypt != null)
                            {
                                decrypted_bytes = Convert.FromBase64String(Decrypt);

                            }
                            else
                            {
                                throw new Exception();
                            }
                            string exportfile = dialog.FileName;
                            if (File.Exists(exportfile))
                            {
                                File.Delete(exportfile);
                            }

                            File.WriteAllBytes(exportfile, decrypted_bytes);
                        }));
                    }
                }
                catch
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not save file.");
                    }));
                }
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
            }).Start();
        }
        private void saveimagebutton_Click(object sender, RoutedEventArgs e)
        {
            if (FileDialogOpened == true)
            {
                return;
            }
            new Thread(delegate ()
            {
                string path = Methods.Show_SaveFileDialog(new string[] { "png" }, 0);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;

                    if (path != null)
                    {
                        PngBitmapEncoder pngencoder = new PngBitmapEncoder();
                        pngencoder.Frames.Add(BitmapFrame.Create((BitmapSource)openedimage.Source));
                        using (FileStream stream = new FileStream(path, FileMode.Create))
                        {
                            pngencoder.Save(stream);
                        }
                    }
                });
            }).Start();
        }
        private void MovieVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoElement.Source != null)
            {
                    VideoElement.Volume = Methods.Clamp((float)(sender as Slider).Value / 100,0,1);
            }
        }
        private void MovieSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoElement.Source != null)
            {

                if (seeking == true)
                {
                    VideoElement.Position = TimeSpan.FromMilliseconds((int)(((double)(MovieSlider.Value / 1000)) * ((double)VideoElement.NaturalDuration.TimeSpan.TotalMilliseconds)));

                }


            }
        }
        private void MovieSlider_MouseDown(object sender, RoutedEventArgs e)
        {
            if (VideoElement.Source != null)
            {

                if (seeking == false)
                {

                    isMediaPaused = true;
                    VideoElement.Pause();

                }
                seeking = true;


            }
        }
        private void RoleColor_Click(object sender, RoutedEventArgs e)
        {
            HexcodeInput.Text = "FFFFFF";
            NewHexWindow.Visibility = Visibility.Visible;
        }
        private void MSGAudioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((sender as Slider).DataContext.GetType() == typeof(AudioMessage))
            {
                AudioMessage msg = (sender as Slider).DataContext as AudioMessage;
                if (String.IsNullOrEmpty(msg.AudioCachePath) == true)
                {
                    return;
                }
                if (msg.MediaSource != null)
                {  
                        msg.MediaSource.Volume = Methods.Clamp((float)(sender as Slider).Value / 100, 0, 1);
                }
            }
        }
        private void MSGMovieSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((sender as Slider).DataContext.GetType() == typeof(AudioMessage))
            {
                AudioMessage msg = (sender as Slider).DataContext as AudioMessage;
                if ( String.IsNullOrEmpty(msg.AudioCachePath) == true)
                {
                    return;
                }
                if (msg.MediaSource.Source != null)
                {
                    if (msg.isseeking == true && msg.MediaSource.NaturalDuration.HasTimeSpan)
                    {

                        msg.MediaSource.Position = TimeSpan.FromMilliseconds((int)(((double)(msg.TimeSlider.Value / 1000)) * ((double)msg.MediaSource.NaturalDuration.TimeSpan.TotalMilliseconds)));
                    }
                }
            }
        } 
        
        private void MSGMovieSlider_MouseDown(object sender, RoutedEventArgs e)
        {
            if ((sender as Slider).DataContext.GetType() == typeof(AudioMessage))
            {

                AudioMessage msg = (sender as Slider).DataContext as AudioMessage;
                if ( String.IsNullOrEmpty(msg.AudioCachePath) == true)
                {
                    return;
                }
                if (msg.MediaSource == null || msg.MediaSource.Source == null)
                {
                    msg.TimeSlider = (sender as Slider);
                    DependencyObject media = VisualTreeHelper.GetChild((VisualTreeHelper.GetParent(sender as Slider) as DependencyObject), 2);
                    msg.MediaSource = (MediaElement)media;
                    msg.MediaSource.Source = new Uri(msg.AudioCachePath);
                    msg.MediaSource.LoadedBehavior = MediaState.Manual;

                    this.PreviewMouseUp += (s, a) =>
                    {
                        if (msg.isseeking == true)
                        {
                            if (msg.MediaSource.Source != null)
                            {
                                msg.isseeking = false;
                                msg.MediaSource.Play();
                              
                                msg.isMediaPaused = false;
                                (VisualTreeHelper.GetChild((VisualTreeHelper.GetParent(sender as Slider) as DependencyObject), 1) as Button).Content = "■";
                            }
                        }
                    };
                    msg.MediaSource.MediaEnded += (s, a) =>
                    {
                        msg.isMediaPaused = true;
                        (VisualTreeHelper.GetChild((VisualTreeHelper.GetParent(sender as Slider) as DependencyObject), 1) as Button).Content = "▶";
                    };
                    if (msg.isseeking == false)
                    {
                        msg.isMediaPaused = true;
                        msg.MediaSource.Pause();
                    }
                    msg.isseeking = true;
                }
                else if (msg.MediaSource.Source != null)
                {

                    if (msg.isseeking == false)
                    {

                        msg.isMediaPaused = true;
                        msg.MediaSource.Pause();

                    }
                    msg.isseeking = true;


                }
            }
        }
        private void playbutton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoElement.Position >= VideoElement.NaturalDuration)
            {
                VideoElement.Position = new TimeSpan();

            }
            VideoElement.Play();
            isMediaPaused = false;
            playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
        }
        private void Movie_Click(object sender, RoutedEventArgs e)
        {
            if (VideoElement.Source != null && seeking == false)
            {
                if (VideoElement.Position >= VideoElement.NaturalDuration)
                {
                    VideoElement.Position = new TimeSpan();
                    VideoElement.Play();
                    isMediaPaused = false;
                    playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
                    return;
                }
                if (isMediaPaused == true)
                {
                    VideoElement.Play();
                    isMediaPaused = false;
                    playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
                }
                else
                {
                    VideoElement.Pause();
                    isMediaPaused = true;
                    playbutton.Visibility = isMediaPaused ? Visibility.Visible : Visibility.Hidden;
                }

            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {



        }
        
        private void TransMitVoice_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.my_room_user.wave_in == null)
                {
                    ChatManager.my_room_user.StartTransmittingVoice();
                    ChatManager.my_room_user.wave_in.StartRecording();
                }
            });
        }

        private void EditMyMedia_Click(object sender, RoutedEventArgs e)
        {
            UserProfile_MediaTextBox.Text = "";

            for (int i = 0; i < UserProfile_MediaListBox.Items.Count; ++i)
            {
                UserProfile_MediaTextBox.Text += ((MediaFormat)UserProfile_MediaListBox.Items[i]).Raw + '\n';
            }
            UserProfile_MediaTextBox.Text = UserProfile_MediaTextBox.Text.Trim();
            EditMyMedia.Visibility = Visibility.Hidden;
            UserProfile_MediaTextBox.Visibility = Visibility.Visible;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) //replace profile pic
        {
            if (FileDialogOpened == true)
            {
                return;
            }
            if (BackgroundWorker.SFTP_Actions.Count > 0 || SFTPCalls.cur_sftp_action == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please wait for all pending upload actions to complete.");
                }));
                return;
            }
            new Thread(delegate ()
            {
                string path = Methods.Show_OpenFileDialog(new string[] { "png", "jpg", "jpeg", "tiff", "bmp", "gif" }, 0);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {
                    int height = 0;
                    int width = 0;
                    double decimatevalue = 1;
                    BitmapImage source;

                    source = new BitmapImage(new Uri(path));

                    height = source.PixelHeight;
                    width = source.PixelWidth;
                    if (width > 256 || height > 256)
                    {
                        if (width > height)
                        {
                            decimatevalue = (double)width / 256;
                        }
                        else
                        {
                            decimatevalue = (double)height / 256;
                        }
                    }
                    height = (int)((double)height / decimatevalue);
                    width = (int)((double)width / decimatevalue);

                    BitmapImage NewSource = new BitmapImage();
                    ImageSource thesource = null;
                    using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(path)))
                    {
                        NewSource.BeginInit();
                        NewSource.StreamSource = stream;
                        NewSource.DecodePixelHeight = height;
                        NewSource.DecodePixelWidth = width;
                        NewSource.EndInit();

                      
                        if (height != width)
                        {
                            thesource = Methods.BitmapSourceToBitmapImage(new CroppedBitmap(NewSource as BitmapImage, new Int32Rect((int)((double)(height < width ? ((double)width - (double)height) / 2d : 0)), (int)((double)(width < height ? ((double)height - (double)width) / 2d : 0)), (height < width ? height : width), (height < width ? height : width)))) as ImageSource;
                        }
                        else
                        {
                            thesource = NewSource as ImageSource;
                        }
                    }
                    byte[] ProfileByte = Methods.ImageSourceToBytes((BitmapImage)thesource);
                    SFTPCalls.UploadProfilePicture(ProfileByte, NetworkManager.myusername, false);
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not open image.");
                    }));
                }
            }).Start();
        }
        private void SelectServerPic_Click(object sender, RoutedEventArgs e) //replace server pic
        {
            if (FileDialogOpened == true)
            {
                return;
            }

            new Thread(delegate ()
            {
                string path = Methods.Show_OpenFileDialog(new string[] { "png", "jpg", "jpeg", "tiff", "bmp", "gif" }, 0);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        int height = 0;
                        int width = 0;
                        double decimatevalue = 1;
                        BitmapImage source;

                        source = new BitmapImage(new Uri(path));

                        height = source.PixelHeight;
                        width = source.PixelWidth;
                        if (width > 256 || height > 256)
                        {
                            if (width > height)
                            {
                                decimatevalue = (double)width / 256;
                            }
                            else
                            {
                                decimatevalue = (double)height / 256;
                            }
                        }
                        height = (int)((double)height / decimatevalue);
                        width = (int)((double)width / decimatevalue);

                        BitmapImage NewSource = new BitmapImage();
                        ImageSource thesource = null;
                        using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(path)))
                        {
                            NewSource.BeginInit();
                            NewSource.StreamSource = stream;
                            NewSource.DecodePixelHeight = height;
                            NewSource.DecodePixelWidth = width;
                            NewSource.EndInit();

                        
                            if (height != width)
                            {
                                thesource = Methods.BitmapSourceToBitmapImage(new CroppedBitmap(NewSource as BitmapImage, new Int32Rect((int)((double)(height < width ? ((double)width - (double)height) / 2d : 0)), (int)((double)(width < height ? ((double)height - (double)width) / 2d : 0)), (height < width ? height : width), (height < width ? height : width)))) as ImageSource;
                            }
                            else
                            {
                                thesource = NewSource as ImageSource;
                            }
                        }
                        EditServerImage.Source = thesource;
                    });
                }
                else
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Could not open image.");
                    }));
                }
            }).Start();
        }
        public void EnableorDisableLoading(string text, bool open)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (open == false && text == LoadingText.Text)
                {
                    LoadingObj.Visibility = Visibility.Hidden;
                }
                if (open == true)
                {
                    LoadingObj.Visibility = Visibility.Visible;
                    LoadingText.Text = text;
                }
            });
        }
        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            EditProfileWindow.Visibility = Visibility.Visible;
            Image sai = EditProfilePImage;
            sai.Source = DefaultProfilePicture;
            int msgaccount_ID = (int)NetworkManager.MyAccountID;

            if (NetworkManager.MyAccountID == null)
                return;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.GetUserCacheByID(msgaccount_ID) == null)
                {
                    DatabaseCalls.AddUserCache(msgaccount_ID);
                }
                else
                {
                    if (ChatManager.GetUserCacheByID(msgaccount_ID) != null && ChatManager.GetUserCacheByID(msgaccount_ID).profilepic != null)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            sai.Source = ChatManager.GetUserCacheByID(msgaccount_ID).profilepic;
                        });
                        return;
                    }
                }
                new Thread(delegate ()
                {
                    int tries = 0;
                    bool check = true;
                    while (check == true)
                    {
                        Action a = (Action)(() =>
                        {
                            check = ChatManager.GetUserCacheByID(msgaccount_ID) == null && NetworkManager.MyAccountID != null || ChatManager.GetUserCacheByID(msgaccount_ID).profilepic == null && NetworkManager.MyAccountID != null;
                        });
                        RunOnPrimaryActionThread(a);
                        //
                        tries++;
                        if (tries == 10)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                sai.Source = UndefinedSource as ImageSource;
                            });
                            return;
                        }
                        Thread.Sleep(500);
                    }
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.GetUserCacheByID(msgaccount_ID) != null && ChatManager.GetUserCacheByID(msgaccount_ID).profilepic != null)
                        {
                            ImageSource source = ChatManager.GetUserCacheByID(msgaccount_ID).profilepic;
                            this.Dispatcher.Invoke(() =>
                            {
                                sai.Source = source;
                            });
                        }
                    });
                }).Start();
            });
        }

        private void MicOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Content)
            {
                case "🔈":
                    Mic_Status = MicStatus.Off;
                    (sender as Button).Content = "🔇";
                    break;
                case "🔊":
                    Mic_Status = MicStatus.Off;
                    (sender as Button).Content = "🔇";
                    break;
                case "🔇":
                    if (ChatManager.my_room_user != null && ChatManager.my_room_user.wave_in == null && ChatManager.MyTempuserWhileinCall != null && ChatManager.MyTempuserWhileinCall.wave_in == null && NetworkManager.MyAccountID != null ||
                        ChatManager.my_room_user == null && ChatManager.MyTempuserWhileinCall != null && ChatManager.MyTempuserWhileinCall.wave_in == null && NetworkManager.MyAccountID != null ||
                        ChatManager.my_room_user != null && ChatManager.my_room_user.wave_in == null && ChatManager.MyTempuserWhileinCall == null && NetworkManager.MyAccountID != null)
                    {
                        Mic_Status = MicStatus.Available;
                        (sender as Button).Content = "🔈";
                    }
                    else
                    {
                        Mic_Status = MicStatus.Active;
                        (sender as Button).Content = "🔊";
                    }
                    break;
                default:
                    Mic_Status = MicStatus.Off;
                    (sender as Button).Content = "🔇";
                    break;
            }
        }

        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            if ( String.IsNullOrEmpty(CreateServername.Text) == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name field required.");
                }));
                return;
            }
            if (EditServerImage.Source == null)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server icon required.");
                }));
                return;
            }
            if (BackgroundWorker.SFTP_Actions.Count > 0 || SFTPCalls.cur_sftp_action == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please wait for all pending upload actions to complete.");
                }));
                return;
            }
            string root = "";
            for (int i = 0; i < 50; ++i)
            {
                root += WordVerififier.Awords[Methods.RandomRange.Next(0, WordVerififier.Awords.Length)] + " ";
            }
            string servername = CreateServername.Text;
            string pas = PasswordForRoom.Text;
            if (String.IsNullOrEmpty(pas))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server password is required.");
                }));
                return;
            }
            if (String.IsNullOrEmpty(servername))
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name is required.");
                }));
                return;
            }
            if (Methods.Allowed(servername) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid server name.");
                }));
                return;
            }
            if (Methods.Allowed(pas) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid server password.");
                }));
                return;
            }
            new Thread(delegate ()
            {
                string passfetch = ServerPasswordCacheMethod(servername, pas);
                if ( String.IsNullOrEmpty(passfetch) == true)
                {
                    checkinghash = false;
                    return;
                }
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    ChatManager.selectedroompass = passfetch;
                    using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                    {
                        byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(pas));
                        ChatManager.selectedroompassasbytes = keys;
                    }
                    ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(pas);
                    root = Methods.EncryptString(root, ChatManager.selectedroompassasbytes);
                    ChatManager.selectedencryptionseed = root;

                    if (ChatManager.CurrentSavedServer != null || ChatManager.CurrentChatWithUser != null)
                    {
                        int serverid = ChatManager.CurrentSavedServer ?? ((-ChatManager.CurrentChatWithUser) ?? -1);
                        SavedServersOnMemory ss = ChatManager.GetSavedServerByID(serverid);
                        if (ss != null)
                        {
                            int[] keyarray = new int[ss.channels.Keys.Count];
                            ss.channels.Keys.CopyTo(keyarray, 0);
                            foreach (int k in keyarray)
                            {
                                SavedChannelsForTempRoom scftr = (ss.channels[k] as SavedChannelsForTempRoom);
                                if (scftr == null)
                                {
                                    continue;
                                }
                                for (int i = 0; i < scftr.messages.Count; ++i)
                                {
                                    if (scftr.messages[i].loaded == true)
                                    {
                                        if (scftr.messages[i].GetType() == typeof(AudioMessage))
                                        {
                                            (scftr.messages[i] as AudioMessage).VolumeSlider = null;
                                            (scftr.messages[i] as AudioMessage).MediaSource = null;
                                            (scftr.messages[i] as AudioMessage).TimeSlider = null;
                                            (scftr.messages[i] as AudioMessage).isMediaPaused = false;
                                            (scftr.messages[i] as AudioMessage).isseeking = false;
                                        }
                                        if (scftr.savedmessages.Any(x => x.idofmessage == scftr.messages[i].idofmessage) == false)
                                        {
                                            scftr.savedmessages.Add(scftr.messages[i]);
                                        }
                                    }
                                }

                            }
                        }
                    }
               
                    SendPackets.LeaveAllServers();
                    this.Dispatcher.Invoke(() =>
                    {
                        string a = SFTPCalls.UploadServerPicture(Methods.ImageSourceToBytes((BitmapImage)EditServerImage.Source));
                        NetworkManager.TaskTo_PrimaryActionThread(() =>
                        {
                            SendPackets.CreateSavedRoom(servername, root, a);
                        });
                    });
                });
            }).Start();
        }

        private void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            if ( String.IsNullOrEmpty(CreateServername.Text) == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Server name field required.");
                }));
                return;
            }
            if (ChatManager.StillGettingVerificationOfJoinRoom == false)
            {

                string servername = CreateServername.Text;
                string pas = PasswordForRoom.Text;
                if (String.IsNullOrEmpty(pas))
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Server password is required.");
                    }));
                    return;
                }
                if (String.IsNullOrEmpty(servername))
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Server name is required.");
                    }));
                    return;
                }
                if (Methods.Allowed(servername) == false)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Invalid server name.");
                    }));
                    return;
                }
                if (Methods.Allowed(pas) == false)
                {
                    ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                    {
                        MainWindow.instance.MessageBoxShow("Invalid server password.");
                    }));
                    return;
                }
                new Thread(delegate ()
                {
                    string passfetch = ServerPasswordCacheMethod(servername, pas);
                    if ( String.IsNullOrEmpty(passfetch) == true)
                    {
                        checkinghash = false;
                        return;
                    }
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        EnableorDisableLoading("Joining room...", true);
                        ChatManager.selectedroompass = passfetch;
                        using (MD5CryptoServiceProvider mdS = new MD5CryptoServiceProvider())
                        {
                            byte[] keys = mdS.ComputeHash(UTF8Encoding.UTF8.GetBytes(pas));
                            ChatManager.selectedroompassasbytes = keys;
                        }
                        ChatManager.selectedroompassasbytes2 = Encoding.Unicode.GetBytes(pas);
                        ChatManager.selectedencryptionseed = "";
                        SendPackets.LeaveAllServers();
                        this.Dispatcher.Invoke(() =>
                        {
                            SendPackets.ClientRequestJoinRoom(servername, true);
                        });
                    });
                }).Start();
            }
        }
        private void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            bool? a = CreateTextRadioButton.IsChecked;
            bool? b = CreateVoiceRadioButton.IsChecked;
            string c = CreateChannelInput.Text;
            string d = CreateChannelInput.Text;
            if (Methods.Allowed(c) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid channel name.");
                }));
                return;
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.CurrentSavedServer == null)
                {
                    return;
                }
                EnableorDisableLoading("Creating channel...", true);
                if (a == true)
                {
                    SendPackets.CreateChannel((int)ChatManager.CurrentSavedServer, c, 0);
                }
                if (b == true)
                {
                    SendPackets.CreateChannel((int)ChatManager.CurrentSavedServer, d, 1);
                }
            });
        }
        private void ShowManageChannel(object sender, RoutedEventArgs e)
        {
            ManageChannels.Visibility = Visibility.Visible;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                List<object> templist = new List<object>();
                foreach (int k in ChatManager.channels.Keys)
                {
                    if (ChatManager.channels[k].GetType() == typeof(ChannelsForTempRoom))
                    {
                        templist.Add(ChatManager.channels[k] as ChannelsForTempRoom);
                    }
                    if (ChatManager.channels[k].GetType() == typeof(VoiceChannelsForTempRoom))
                    {
                        templist.Add(ChatManager.channels[k] as VoiceChannelsForTempRoom);
                    }
                }
                templist.Reverse();
                this.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.ChannelsEditListBox.ItemsSource = templist;
                    ChannelsEditListBox.Items.Refresh();
                    MainWindow.instance.ChannelsEditListBox.SelectedIndex = 0;
                });
            });
        }
        private void ShowAddChannel(object sender, RoutedEventArgs e)
        {
            CreateChannel.Visibility = Visibility.Visible;
        }
        private void ShowRoles(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                List<RolesList> a = new List<RolesList>(ChatManager.RoleList.OrderBy(x => x.precedence));
                this.Dispatcher.Invoke(() =>
                {
                    MainWindow.instance.RolesEditListBox.ItemsSource =a ;
                    MainWindow.instance.RolesEditListBox.Items.Refresh();
                    MainWindow.instance.RolesEditListBox.SelectedIndex = 0;
                    MainWindow.instance.ManageRoles.Visibility = Visibility.Visible;
                    addusertorole.Content = "Add Users To Role";
                    addusertorole.IsEnabled = false;
                });
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    AddingPeopletoRoles = false;
                    RefreshUserRoleListBox();
                });
            });
        }
        private void ShowCreateNewRole(object sender, RoutedEventArgs e)
        {

            CreateNewRole.Visibility = Visibility.Visible;

        }
        private void CreateVoiceRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (CreateTextRadioButton != null)
            {
                CreateTextRadioButton.IsChecked = false;
            }
        }

        private void CreateTextRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (CreateVoiceRadioButton != null)
            {
                CreateVoiceRadioButton.IsChecked = false;
            }
        }

        private void AcceptCall_Click(object sender, RoutedEventArgs e)
        {
            new Thread(delegate ()
            {
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel != null)
                        SendPackets.SendNewChannel(-1, ChatManager.CurrentSavedServer ?? -1);
                });
                bool looped = true;
                int tries = 0;
                while (looped == true && NetworkManager.MyAccountID != null)
                {
                    tries++;
                    if (tries > 20)
                    {
                        return;
                    }
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        if (ChatManager.my_room_user != null && ChatManager.my_room_user.CurrentVoiceChannel == null || ChatManager.my_room_user == null)
                        {
                            looped = false;
                        }
                    });
                    Thread.Sleep(200);
                }
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.CallResponse(true);
                });
                this.Dispatcher.Invoke(() =>
                {
                    if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                        MainWindow.CallPendingSound.Stop();
                    if (MainWindow.CallSound != null && MainWindow.CallSound.IsLoadCompleted)
                        MainWindow.CallSound.Stop();
                });
            }).Start();
        }

        private void DeclineCall_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                SendPackets.CallResponse(false);
            });
            if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                MainWindow.CallPendingSound.Stop();
            if (MainWindow.CallSound != null && MainWindow.CallSound.IsLoadCompleted)
                MainWindow.CallSound.Stop();
        }

        private void InitiateCall(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.CurrentChatWithUser != null)
                {
                    EnableorDisableLoading("Calling user...", true);
                    new Thread(delegate ()
                    {
                        using (System.IO.Stream stream = ESMA.Properties.Resources.dialing)
                        {
                            CallPendingSound = new System.Media.SoundPlayer(stream);
                            CallPendingSound.Play();
                        }
                    }).Start();

                    SendPackets.CallSomeone((int)ChatManager.CurrentChatWithUser);
                }
            });
        }

        private void EndCall(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                SendPackets.CallResponse(false);
            });
            if (MainWindow.CallPendingSound != null && MainWindow.CallPendingSound.IsLoadCompleted)
                MainWindow.CallPendingSound.Stop();
            if (MainWindow.CallSound != null && MainWindow.CallSound.IsLoadCompleted)
                MainWindow.CallSound.Stop();
        }

        private void ProceedToServerButton_Click(object sender, RoutedEventArgs e)
        {
            if ( String.IsNullOrEmpty(HashInput.Text) == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Please enter a password.");
                }));
                return;
            }
            if (Methods.Allowed(HashInput.Text) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid hash input.");
                }));
                return;
            }
            AskHash.Visibility = Visibility.Hidden;
            HashKeyPromptOpen = false;
            EnableorDisableLoading("Please wait... (1)", true);
        }

        private void AddFriendClick(object sender, RoutedEventArgs e)
        {
            string a = FriendToAdd.Text;
            if (Methods.Allowed(a) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid username.");
                }));
                return;
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Adding friend...", true);
                SendPackets.FriendRequest(a);
            });
        }

        private void AddFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            AddFriendTab.Visibility = Visibility.Visible;
            AllFriendsTab.Visibility = Visibility.Hidden;
            PendingFriendsTab.Visibility = Visibility.Hidden;
        }

        private void MyFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Loading friends...", true);
                SendPackets.RequestFriends(1);
            });
            MainWindow.instance.AllFriendsListBox.ItemsSource = NetworkManager.Friends;

        }

        private void PendingFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Loading pending friends...", true);
                SendPackets.RequestPendingFriends();
            });
        }

        private void FriendsButton_Click(object sender, RoutedEventArgs e)
        {
            FriendGrid.Visibility = Visibility.Visible;
            ChatCover.Visibility = Visibility.Visible;
            AddFriendTab.Visibility = Visibility.Visible;
            AllFriendsTab.Visibility = Visibility.Hidden;
            PendingFriendsTab.Visibility = Visibility.Hidden;
        }

        private void dmfromwindow_Click(object sender, RoutedEventArgs e)
        {
            if (CurPLWindow == NetworkManager.MyAccountID)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("You cant DM yourself.");
                }));
                return;
            }
            if (Methods.Allowed(UserProfile_Name.Text) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid username.");
                }));
                return;
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                LoadingFriends = true;
                EnableorDisableLoading("Loading private messages...", true);
                SendPackets.RequestFriends(0);
                new Thread(delegate ()
                {
                    while (LoadingFriends == true && NetworkManager.MyAccountID != null)
                    {
                        Thread.Sleep(10);
                    }
                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                    {
                        GoTODMFunction();
                        this.Dispatcher.Invoke(() =>
                        {

                            StartDM(new UserList(UserProfile_Name.Text, CurPLWindow));
                            UserProfileWindow.Visibility = Visibility.Hidden;
                        });
                    });
                }).Start();
            });
        }
        
        private void addfriendfromwindow_Click(object sender, RoutedEventArgs e)
        {
            string a = UserProfile_Name.Text;
            if (Methods.Allowed(a) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid username.");
                }));
                return;
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Adding friend...", true);
                SendPackets.FriendRequest(a);
            });
            addfriendfromwindow.IsEnabled = false;
        }
        private void removefriendfromwindow_Click(object sender, RoutedEventArgs e)
        {
            int a = CurPLWindow;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Removing friend...", true);
                SendPackets.AddOrRemoveFriend(2, a);
            });
            removefriendfromwindow.IsEnabled = false;
        }
   
        private void Button_Click_4(object sender, RoutedEventArgs e) // save changes
        {
            //role
            int a = (RolesEditListBox.SelectedItem as RolesList).id;
            string b = (RolesEditListBox.SelectedItem as RolesList).rolename;
            int c = editkick.IsChecked == true ? 1 : 0;
            int d = editmute.IsChecked == true ? 1 : 0;
            int ee = editban.IsChecked == true ? 1 : 0;
            int f = editdelete.IsChecked == true ? 1 : 0;
            int g = editmanagechannels.IsChecked == true ? 1 : 0;
            int h = editmanageroles.IsChecked == true ? 1 : 0;
            string i = (RoleColor.Fill as SolidColorBrush).Color.ToString();

            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                int? ss = ChatManager.CurrentSavedServer;
                if (ss == null)
                {
                    return;
                }
                SendPackets.UpdateRoles((int)ss, a, b, c, d, ee, f, g, h, i);
            });
        }
        public void RefreshUserRoleListBox()
        {
            switch (AddingPeopletoRoles)
            {
                case true:
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        addusertorole.Content = "Cancel";
                        {

                            RolesList _r = RolesEditListBox.SelectedItem as RolesList;
                            if (_r != null)
                            {
                                NetworkManager.TaskTo_PrimaryActionThread(() =>
                                {
                                    List<UserList> tpll = ChatManager.UserList_FULL.FindAll(x => x._Roles.Contains(_r.id) == false);
                                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        UserRoleListBox.ItemsSource = tpll;
                                        UserRoleListBox.Items.Refresh();
                                    });
                                });

                            }

                        }
                    });
                    break;
                case false:
                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                    {
                        addusertorole.Content = "Add Users To Role";
                        {

                            RolesList _r = RolesEditListBox.SelectedItem as RolesList;
                            if (_r != null)
                            {
                                NetworkManager.TaskTo_PrimaryActionThread(() =>
                                {
                                    List<UserList> tpll = ChatManager.UserList_FULL.FindAll(x => x._Roles.Contains(_r.id));
                                    ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                                    {
                                        UserRoleListBox.ItemsSource = tpll;
                                        UserRoleListBox.Items.Refresh();
                                    });
                                });
                            }

                        }
                    });
                    break;
            }
        }
        public void RefreshChannelRoleListBox()
        {
            switch (AddingRoleToChannel)
            {
                case true:
                    this.Dispatcher.Invoke(() =>
                    {
                        addroletochannel.Content = "Cancel";
                        {
                            ChannelsForTempRoom tc = ChannelsEditListBox.SelectedItem as ChannelsForTempRoom;
                            if (tc != null)
                            {
                                NetworkManager.TaskTo_PrimaryActionThread(() =>
                                {
                                    List<RolesList> a = ChatManager.RoleList.FindAll(x => tc.Roles.Contains(x.id) == false);
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        ChannelsRoleListBox.ItemsSource = a; 
                                        ChannelsRoleListBox.Items.Refresh();
                                    });
                                });
                            }
                            else
                            {
                                VoiceChannelsForTempRoom vc = ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom;
                                if (vc != null)
                                {
                                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                                    {
                                        List<RolesList> a = ChatManager.RoleList.FindAll(x => vc.Roles.Contains(x.id) == false);
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            ChannelsRoleListBox.ItemsSource = a; 
                                            ChannelsRoleListBox.Items.Refresh();
                                        });
                                    });
                                }
                            }

                        }
                    });
                    break;
                case false:
                    this.Dispatcher.Invoke(() =>
                    {
                        addroletochannel.Content = "Add Role To Channel";
                        {
                            ChannelsForTempRoom tc = ChannelsEditListBox.SelectedItem as ChannelsForTempRoom;
                            if (tc != null)
                            {
                                NetworkManager.TaskTo_PrimaryActionThread(() =>
                                {
                                    List<RolesList> a = ChatManager.RoleList.FindAll(x => tc.Roles.Contains(x.id));
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        ChannelsRoleListBox.ItemsSource = a;
                                        ChannelsRoleListBox.Items.Refresh();
                                    });
                                });
                            }
                            else
                            {
                                VoiceChannelsForTempRoom vc = ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom;
                                if (vc != null)
                                {
                                    NetworkManager.TaskTo_PrimaryActionThread(() =>
                                    {
                                        List<RolesList> a = ChatManager.RoleList.FindAll(x => vc.Roles.Contains(x.id));
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            ChannelsRoleListBox.ItemsSource = ChatManager.RoleList.FindAll(x => vc.Roles.Contains(x.id));
                                            ChannelsRoleListBox.Items.Refresh();
                                        });
                                    });
                                }
                            }
                        }
                    });
                    break;
            }
        }
        private void Button_Click_6(object sender, RoutedEventArgs e) // add users
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                AddingPeopletoRoles = !AddingPeopletoRoles;
                RefreshUserRoleListBox();
            });
        }

        private void CreateNewRoleButton_Click(object sender, RoutedEventArgs e)
        {
            string a = CreateNewRoleInput.Text;
            if (Methods.Allowed(a) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid role name.");
                }));
                return;
            }
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Fetching roles...", true);
                SendPackets.UpdateRoles((int)ChatManager.CurrentSavedServer, -1, a, 0, 0, 0, 0, 0, 0, "#FFFFFF");
            });
        }

        private void DeleteRoll_Click(object sender, RoutedEventArgs e)
        {
            int a = (RolesEditListBox.SelectedItem as RolesList).id;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Fetching roles...", true);
                SendPackets.RemoveRole((int)ChatManager.CurrentSavedServer,a );
            });
        }
        private void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            int a = (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) != null ? (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom).key : (ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom).key;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Deleting channel...", true);
                SendPackets.DeleteChannel((int)ChatManager.CurrentSavedServer, a);
            });
        }
        private void ADDROLETOCHANNEL_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                AddingRoleToChannel = !AddingRoleToChannel;
                RefreshChannelRoleListBox();
            });
        }

        private void savechannelchanges_Click_4(object sender, RoutedEventArgs e)
        {
            //role
            int? ss = ChatManager.CurrentSavedServer;
            if (ss == null || ChannelsEditListBox.SelectedItem == null)
            {
                return;
            }
            if ((ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) != null)
            {
                int a = (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom) != null ? (ChannelsEditListBox.SelectedItem as ChannelsForTempRoom).key : (ChannelsEditListBox.SelectedItem as VoiceChannelsForTempRoom).key;
                int b = readonlychannel.IsChecked == true ? 1 : 0;
                int c = incominguser.IsChecked == true ? 1 : 0;
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    SendPackets.UpdateChannelSettings((int)ss, a, b, c);
                });
            }
        }

        private void mutefromwindow_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                SendPackets.PunishUser((int)ChatManager.CurrentSavedServer, (int)NetworkManager.MyAccountID, CurPLWindow, 1);
            });
        }

        private void kickfromwindow_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                SendPackets.PunishUser((int)ChatManager.CurrentSavedServer, (int)NetworkManager.MyAccountID, CurPLWindow, 0);
            });
        }

        private void banfromwindow_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                SendPackets.PunishUser((int)ChatManager.CurrentSavedServer, (int)NetworkManager.MyAccountID, CurPLWindow, 2);
            });
        }

        private void SaveHex_Click(object sender, RoutedEventArgs e)
        {
            string input = HexcodeInput.Text;
            if (Methods.Allowed(input) == false)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid hexcode.");
                }));
                return;
            }
            if (String.IsNullOrWhiteSpace(input) || input.Length != 6)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid Hexcode.");
                }));
                return;
            }
            try
            {
                RoleColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#" + HexcodeInput.Text));
            }
            catch
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Invalid Hexcode.");
                }));
                return;
            }
            NewHexWindow.Visibility = Visibility.Hidden;
        }
        private void islocalkey_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                ESMA.BackgroundWorker.MessageBoxactions.Add((Action)(() =>
                {
                    MainWindow.instance.MessageBoxShow("Your private key for message encryption/decryption will be stored on your documents as opposed to on the database. This method is far more secure. Never share this file with anyone, and if this file is lost, you wont be able to login. If it is the incorrect private key info, direct messages wont work. If a key is stored on the database, it will be encrypted.");
                }));
            }
        }

        private void savevideobutton_Click(object sender, RoutedEventArgs e)
        {
            string file = VideoElement.Source.AbsolutePath;

            if (FileDialogOpened == true)
            {
                return;
            }

            new Thread(delegate ()
            {
                string path = Methods.Show_SaveFileDialog(new string[] { "mp4" }, 0);
                MainWindow.FileDialogOpened = false;
                this.Dispatcher.Invoke(() =>
                {
                    FileDialogLoading.Visibility = Visibility.Hidden;
                });
                if (path != null)
                {
                    File.Copy(file, path);
                }
            }).Start();
        }

        private void transferownership_Click(object sender, RoutedEventArgs e)
        {
            transferownership.IsEnabled = false;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                EnableorDisableLoading("Changing user's role...", true);
                SendPackets.RemoveOrAddPLRole((int)ChatManager.CurrentSavedServer, CurPLWindow, 1, true);
            });
        }


        private void slider_move(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ((((Slider)sender).Template.FindName("PART_Track", ((Slider)sender)) as System.Windows.Controls.Primitives.Track).Thumb).RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent, Source = e.Source
                });
            }
        }

        private void LeaveServer_Click(object sender, RoutedEventArgs e)
        {
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                if (ChatManager.my_room_user != null)
                    ChatManager.my_room_user.StopTransmission();
                if (ChatManager.CurrentSavedServer != null)
                {
                    EnableorDisableLoading("Please wait a moment...", true);
                    SendPackets.LeaveServer((int)ChatManager.CurrentSavedServer);
                }
                else if (ChatManager.CurrentChatWithUser != null)
                {
                    EnableorDisableLoading("Removing DM...", true);
                    SendPackets.LeaveDMS((int)ChatManager.CurrentChatWithUser);
                }
                else if (ChatManager.CurTempRoom != null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        ChannelCover.Visibility = Visibility.Visible;
                        FriendHud.Visibility = Visibility.Hidden;
                        DMSListBoxScroll.Visibility = Visibility.Hidden;
                        ChatCover.Visibility = Visibility.Visible;
                        ChatListBox.Items.Clear();
                        SendPackets.LeaveAllServers();
                    });
                    EnableorDisableLoading("Leaving current room...", true);
                  
                }
              
            });
        }

        private void LogOut_CLick(object sender, RoutedEventArgs e)
        {
            EditProfileWindow.Visibility = Visibility.Hidden;
            NetworkManager.TaskTo_PrimaryActionThread(() =>
            {
                NetworkManager.instance.FullDisconnect();
            });
        
        }


    }
}
public enum NotificationSound
{
    Message,
    Online,
    CallConnect,
    CallDisconnect
}