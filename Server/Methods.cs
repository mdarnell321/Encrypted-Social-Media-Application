
using System.Net;

namespace ChatAppServer
{
    public class Methods
    {
        public static Random RandomRange = new Random();
        public static bool foundchannel(int roomid, int channelkey)
        {
            bool found = false;
            if(NetworkManager.GetRoomById(roomid) == null)
            {
                return false;
            }

            foreach (int c in NetworkManager.GetRoomById(roomid).channels.Keys)
            {
                if (c == channelkey)
                {
                    found = true;
                }
            }
            return found;
        }
        public bool URLExists(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static int Clamp(int original, int min, int max)
        {
            int _new = original;
            if (_new > max)
            {
                _new = max;
            }
            if (_new < min)
            {
                _new = min;
            }
            return _new;
        }
    }
}
