using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Windows.Controls;
using System.Windows.Media;

namespace ESMA
{
    public class BackgroundWorker
    {
        public static List<Action> SFTP_Actions = new List<Action>();
        public static List<Action> Download_Actions = new List<Action>();
        public static List<Action> Searchactions = new List<Action>();
        public static List<Action> MessageBoxactions = new List<Action>();

        private static int mainiteration = 0;
        public static void Start()
        {
            new Thread(new ThreadStart(UIUpdate)).Start();
            new Thread(new ThreadStart(SFTPActionThread)).Start();
            new Thread(new ThreadStart(MessageBoxThread)).Start();
            new Thread(new ThreadStart(SearchActionThread)).Start();
        }
        private static void SFTPActionThread()
        {
            while ( MainWindow.isrunning)
            {
                while (SFTP_Actions.Count > 0 && SFTPCalls.cur_sftp_action == false)
                {
                    if (NetworkManager.MyAccountID == null)
                    {
                        SFTP_Actions.Clear();
                        break;
                    }

                    Action a = SFTP_Actions[0];
                    SFTP_Actions.RemoveAt(0);
                    a();
                    
                }
                Thread.Sleep(33);
            }
        }
        private static void UIUpdate()
        {
            while ( MainWindow.isrunning)
            {
                ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                {
                    if (MainWindow._VideoElement.Source != null) // update main video player slider
                    {
                        try
                        {
                            TimeSpan curtime = TimeSpan.FromSeconds(MainWindow._VideoElement.Position.TotalSeconds);
                            TimeSpan goaltime = TimeSpan.FromSeconds(MainWindow._VideoElement.NaturalDuration.TimeSpan.TotalSeconds);
                            MainWindow.instance.MovieTimeLabel.Text = curtime.ToString(@"hh\:mm\:ss") + " / " + goaltime.ToString(@"hh\:mm\:ss");
                            MainWindow._MovieSlider.Value = (MainWindow._VideoElement.Position.TotalMilliseconds / MainWindow._VideoElement.NaturalDuration.TimeSpan.TotalMilliseconds) * 1000;
                        }
                        catch { }
                    }
                    else
                    {
                        MainWindow.instance.MovieTimeLabel.Text = " / ";
                        MainWindow._MovieSlider.Value = 0;
                    }
                });
                NetworkManager.TaskTo_PrimaryActionThread(() =>
                {
                    int[] keyarray = new int[ChatManager.channels.Keys.Count];
                    ChatManager.channels.Keys.CopyTo(keyarray, 0);
                    foreach (int ch in keyarray)// update all audio messages slider            
                    {
                        if ((ChatManager.channels[ch]).GetType() == typeof(ChannelsForTempRoom))
                        {
                            ChannelsForTempRoom channel = (ChannelsForTempRoom)ChatManager.channels[ch];
                            if (channel == null)
                            {
                                break;
                            }
                            for (int i = 0; i < channel.messages.Count; i++)
                            {
                                if (channel.messages[i] != null && channel.messages[i].GetType() == typeof(AudioMessage))
                                {
                                    AudioMessage msg = (AudioMessage)channel.messages[i];
                                    if (msg.MediaSource != null && msg.TimeSlider != null)
                                    {

                                        ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                                        {
                                            if (msg.MediaSource.NaturalDuration.HasTimeSpan)
                                            {
                                                if (msg.VolumeSlider == null)
                                                {
                                                    msg.VolumeSlider = (VisualTreeHelper.GetChild((VisualTreeHelper.GetParent(msg.MediaSource) as DependencyObject), 3) as Slider);
                                                    msg.MediaSource.Volume = msg.VolumeSlider.Value / 100;
                                                }
                                                msg.TimeSlider.Value = (msg.MediaSource.Position.TotalMilliseconds / msg.MediaSource.NaturalDuration.TimeSpan.TotalMilliseconds) * 1000;
                                            }
                                        });

                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < ChatManager.UserList.Count; ++i)
                    {
                        ChatManager.UserList[i].TransmissionIconTimer = Methods.Clamp(ChatManager.UserList[i].TransmissionIconTimer - 1, 0, 3000);
                        if (ChatManager.UserList[i].TransmissionIconTimer == 0)
                        {
                            ESMA.MainWindow.instance.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    if (ChatManager.UserList[i].TransmissionStatus != null)
                                    {
                                        ChatManager.UserList[i].TransmissionStatus.Text = " ";
                                    }
                                }
                                catch
                                {

                                }
                            });
                        }
                    }
                });
          
                if (mainiteration >= 30)
                {
     
                    mainiteration = 0;
                }
                mainiteration++;
                Thread.Sleep(200);
            }
        }
        private static void SearchActionThread()
        {
            while ( MainWindow.isrunning)
            {
                if (ChatManager.StillFetchingMessages == false)
                {
                    while (Searchactions.Count > 0)
                    {
                        if (NetworkManager.MyAccountID == null)
                        {
                            Searchactions.Clear();
                            break;
                        }
                        if (ChatManager.StillFetchingMessages == false)
                        {
                            Searchactions[0]();
                            try
                            {
                                Searchactions.RemoveAt(0);
                            }
                            catch { }
                        }
                    }
                    while (Download_Actions.Count > 0)
                    {
                        if (NetworkManager.MyAccountID == null)
                        {
                            Download_Actions.Clear();
                            break;
                        }
                        if (ChatManager.StillFetchingMessages == false)
                        {
                            Download_Actions[0]();
                            try
                            {
                                Download_Actions.RemoveAt(0);
                            }
                            catch { }
                        }
                    }
                }
                Thread.Sleep(33);
            }
        }
        private static void MessageBoxThread()
        {
            while ( MainWindow.isrunning)
            {
                while (MessageBoxactions.Count > 0)
                {
                    if (MainWindow.msgboxopen == false)
                    {
                        MessageBoxactions[0]();
                        MessageBoxactions.RemoveAt(0);
                    }
                }
                Thread.Sleep(33);
            }
        }
    }
}
