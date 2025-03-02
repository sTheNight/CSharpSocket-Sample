using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketServe
{
    internal class Program
    {
        /**
         * 
         * 借鉴自此处：https://www.bilibili.com/video/BV1SuxhetE11/?share_source=copy_web&vd_source=ded938f96ab6f358803f5b6e194589b5
         * 
         * 客户端信息存储方式由数组改为 Dictionary
         * 所有非主线程逻辑均改为异步执行并增加服务端发送消息的 Task
         * 
         */
        public static Dictionary<Socket,string> clients = new Dictionary<Socket, string>();
        private static Socket Server;
        public static void Main(string[] args)
        {
            using (Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Server.Bind(new IPEndPoint(IPAddress.Any, 12345));
                Server.Listen(10);
                Console.WriteLine(@$"Local EndPoint: {Server.LocalEndPoint}
Remote EndPoint: {Server.RemoteEndPoint}
AddressFamily: {Server.AddressFamily}
SocketType: {Server.SocketType}
ProtocolType: {Server.ProtocolType}
");
                Console.WriteLine("服务端已启动，等待客户端连接...");
                Task.Run(() => SendMsgByServer()); // 异步执行服务端发送消息的 Task
                while (true) // 接收客户端连接跑在主线程上
                {
                    Socket client = Server.Accept(); // 阻塞等待客户端连接
                    Console.WriteLine($"监听到新的用户尝试连接：{client.RemoteEndPoint}");
                    Task.Run(() => ReceiveMsg(client)); // 异步执行接收消息的 Task
                }
            }
        }
        public static async Task SendMsgByServer() // 服务端发送消息的 Task
        {
            while (true)
            {
                string msg = Console.ReadLine();
                await Task.Run(() => Boardcast($"Server:{msg}", null)); // 通过调用广播消息的 Task 实现服务端发送消息，防止重复造轮子
            }
        }
        public static async Task ReceiveMsg(Socket client) // 从客户端接收消息的 Task
        {
            byte[] buffer = new byte[1024];
            try
            {
                // 新用户第一条消息必定是注册昵称，因此单独处理
                int userName_num = client.Receive(buffer);
                clients.Add(client, Encoding.UTF8.GetString(buffer, 0, userName_num));
                Console.WriteLine(clients[client] + "已注册");
                Task.Run(() => Boardcast($"{clients[client]}已注册", client)); // 广播消息，异步执行防止阻塞线程

                while (true)
                {
                    // 其后的消息都是聊天消息
                    int num = client.Receive(buffer);
                    if (num == 0) // num 为 0 说明客户端已断开连接
                        break;
                    string message = Encoding.UTF8.GetString(buffer, 0, num);
                    Console.WriteLine($"{clients[client]}:{message}");
                    Task.Run(() => Boardcast($"{clients[client]}:{message}", client)); // 广播消息，异步执行防止阻塞线程
                }
            }
            catch (Exception ex)
            {
                // 客户端异常断开连接
                Console.WriteLine($"{clients[client]}:{ex.Message}");
                Console.WriteLine($"{clients[client]}已离开");
                Boardcast($"{clients[client]}已离开", client);
                clients.Remove(client);
            }
        }
        public static async Task Boardcast(string message,Socket sender) // 广播消息的Task
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                foreach (var client in clients.Keys)
                {
                    if (client != sender)
                        client.Send(buffer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"广播失败：{ex.Message}");
            }
        }
    }
}
