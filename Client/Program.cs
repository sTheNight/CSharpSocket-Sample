using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketClient
{
    internal class Program
    {
        /**
         * 
         * 借鉴自此处：https://www.bilibili.com/video/BV1DuxhetEMy/?share_source=copy_web&vd_source=ded938f96ab6f358803f5b6e194589b5
         * 
         * 完善了连接失败的逻辑，增加了重连机制
         * 将部分方法改为异步执行
         * 
         */
        private static Socket Client;
        static void Main(string[] args)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Connect();

            Console.Write("已连接服务端，请注册昵称：");
            string name = Console.ReadLine();
            try
            {
                Client.Send(Encoding.UTF8.GetBytes(name));
            }
            catch (Exception)
            {
                // 若出现错误则说明连接已断开
                Client.Close();
            }

            Task receiveTask =  Task.Run(() => ReceiveMsg());
            Task sendTask = Task.Run(() => SendMsg());
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
                Client.Connect("127.0.0.1", 12345);
            }
            catch (Exception)
            {
                Console.WriteLine("连接失败，重试中...");
                Connect();
            }
        }
        public static async Task ReceiveMsg()
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
        public static async Task SendMsg()
        {
            try
            {
                while (true) //发送消息的逻辑运行在主线程上否则程序会直接退出
                {
                    string msg = Console.ReadLine();
                    Task.Run(() => Client.Send(Encoding.UTF8.GetBytes(msg)));
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
