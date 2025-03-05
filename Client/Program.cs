using System.Text;
using System.Net.Sockets;

namespace SocketClient
{
    internal class Program
    {
        private static Socket Client;
        public static void Main(string[] args)
        {
            // 创建客户端 Socket 实例
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 连接服务端
            Connect();
            // 向服务端提交此客户端的昵称
            Console.Write("已连接服务端，请输入您的昵称：");
        input_name: string name = Console.ReadLine();
            if (name=="")
            {
                Console.Write("昵称不得为空，请重新输入：");
                goto input_name;
            }
            try
            {
                Client.Send(Encoding.UTF8.GetBytes(name));
            }
            catch (Exception)
            {
                // 若出现错误则说明连接已断开
                Client.Close();
            }
            // 启动接收消息和发送消息的异步 Task
            Task receiveTask =  Task.Run(() => ReceiveMsg());
            Task sendTask = Task.Run(() => SendMsg());
            Console.WriteLine("连接成功！");
            while (true)
            {
                if (!Client.Connected || receiveTask.IsCanceled || sendTask.IsCanceled)
                {
                    Console.WriteLine("服务异常");
                    return;
                }
            }
        }
        private static void Connect()
        {
            try
            {
                if (Client == null)
                {
                    Console.WriteLine("客户端启动失败");
                    return;
                }
                Client.Connect("127.0.0.1", 12345);
            }
            catch (Exception)
            {
                Console.WriteLine("连接失败，重试中...");
                Connect();
            }
        }
        private static void ReceiveMsg()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int num = Client.Receive(buffer);
                    if (num == 0)
                        break;
                    string message = Encoding.UTF8.GetString(buffer, 0, num);
                    Console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法接收到信息，连接已断开：{ex.Message}");
                Client.Close();
            }
        }
        private static void SendMsg()
        {
            try
            {
                while (true)
                {
                    string msg = Console.ReadLine();
                    _ = Task.Run(() => Client.Send(Encoding.UTF8.GetBytes(msg)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败，连接已断开：{ex.Message}");
                Client.Close();
            }
        }
    }
}
