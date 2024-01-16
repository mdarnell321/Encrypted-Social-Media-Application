

namespace ChatAppServer
{
    public class Program
    {
        public static bool active = true;

        static void Main(string[] args)
        {
            active = true;
            new Thread(new ThreadStart(NetworkManager.Update)).Start();
            new Thread(new ThreadStart(NetworkManager.UpdateMessagesThread)).Start();
            new Thread(new ThreadStart(NetworkManager.UpdatePrimaryActionThread)).Start();
            new Thread(new ThreadStart(NetworkManager.UpdateReceiveThread)).Start();
            new Thread(new ThreadStart(NetworkManager.SlowThread)).Start();
            NetworkManager.StartIt(5000);
        }
    }
}
