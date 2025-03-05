using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Net.Sockets;
using System.Net;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Emit;
using System.Threading;

namespace SocketServer_Winform
{
    public partial class Form1 : MaterialSkin.Controls.MaterialForm
    {
        private static Dictionary<Socket, string> clients = new Dictionary<Socket, string>();
        private static Socket Server;
        internal static TextBox msgTextBox;
        public Form1()
        {
            InitializeComponent();

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

            msgTextBox = textBox1;
            msgTextBox.Text = "服务端未启动";
        }
        public static void StartServer()
        {
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                Server.Bind(new IPEndPoint(IPAddress.Any, 12345));
                Server.Listen(10);
            }
            catch (Exception ex)
            {
                msgTextBox.AppendText(ex.Message);
            }
            AppentMsgText_Fast($@"服务端已启动，等待客户端连接...");

            Task.Run(() => AcceptClient());
        }
        private static void AcceptClient()
        {
            while (true)
            {
                Socket client = Server.Accept(); // 阻塞等待客户端连接
                AppentMsgText_Fast($"监听到新的用户尝试连接：{client.RemoteEndPoint}");
                Task.Run(() => ReceiveMsg(client)); // 异步执行接收消息的 Task
            }
        }
        private static async void ReceiveMsg(Socket client) // 从客户端接收消息的 Task
        {
            byte[] buffer = new byte[1024];
            try
            {
                // 新用户第一条消息必定是注册昵称，因此单独处理
                int userName_num = client.Receive(buffer);
                clients.Add(client, Encoding.UTF8.GetString(buffer, 0, userName_num));
                AppentMsgText_Fast($"{client.RemoteEndPoint}({clients[client]})已注册");
                await Task.Run(() => Boardcast($"{client.RemoteEndPoint}({clients[client]})已注册", client)); // 广播消息，异步执行防止阻塞线程

                while (true)
                {
                    // 其后的消息都是聊天消息
                    int num = client.Receive(buffer);
                    if (num == 0) // num 为 0 说明客户端已断开连接
                        break;
                    string message = Encoding.UTF8.GetString(buffer, 0, num);
                    AppentMsgText_Fast($"{client.RemoteEndPoint}({clients[client]}):{message}");
                    await Task.Run(() => Boardcast($"{client.RemoteEndPoint}({clients[client]}):{message}", client)); // 广播消息，异步执行防止阻塞线程
                }
            }
            catch (Exception ex)
            {
                // 客户端异常断开连接
                AppentMsgText_Fast($"{client.RemoteEndPoint}({clients[client]}):{ex.Message}");
                AppentMsgText_Fast($"{client.RemoteEndPoint}({clients[client]})已离开");
                await Boardcast($"{client.RemoteEndPoint}({clients[client]})已离开", client);
                clients.Remove(client);
            }
        }
        private static async Task Boardcast(string message, Socket sender) // 广播消息的Task
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                foreach (var client in clients.Keys)
                {
                    if (client != sender)
                        await Task.Run(() => client.Send(buffer));
                }
            }
            catch (Exception ex)
            {
                AppentMsgText_Fast($"广播失败：{ex.Message}({sender.RemoteEndPoint})");
            }
        }
        private async void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            string msg = materialSingleLineTextField1.Text;
            AppentMsgText_Fast(msg);
            materialSingleLineTextField1.Text = "";
            materialSingleLineTextField1.Focus();
            await Task.Run(() => Boardcast($"Server:{msg}", null));
        }
        // 由于无法跨线程操作控件，因此使用委托代理控件操作
        internal static void AppentMsgText(string msg)
        {
            if (msgTextBox.InvokeRequired)
            {
                msgTextBox.Invoke(new Action(() =>
                {
                    msgTextBox.AppendText(msg + "\r\n");
                }));
            }
            else
            {
                msgTextBox.AppendText(msg + "\r\n");
            }
        }
        // 追加文字的快速方法，不需要额外写 Task.Run
        internal static void AppentMsgText_Fast(string msg)
        {
            Task.Run(() => AppentMsgText(msg));
        }
        // 锁定布局大小，实现方式较粗糙
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            this.ClientSize = new System.Drawing.Size(529, 407);
        }

        private void materialFlatButton2_Click(object sender, EventArgs e)
        {
            if (Server == null)
            {
                textBox1.Clear();
                StartServer();
                materialFlatButton1.Enabled = true;
                materialFlatButton3.Enabled = true;
                materialFlatButton2.Text = "Stop";
                return;
            }
            // 关闭服务端，由于停止 Task 较为麻烦，因此直接重启程序
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Environment.Exit(0);
        }
        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            if (Server != null)
            {
                MessageBox.Show($@"Local EndPoint: {Server.LocalEndPoint}
AddressFamily: {Server.AddressFamily}
SocketType: {Server.SocketType}
ProtocolType: {Server.ProtocolType}");
                return;
            }
            MessageBox.Show("服务端未启动");
        }
        private void materialFlatButton3_Click(object sender, EventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in clients.Keys)
            {
                stringBuilder.Append($"Username: {clients[item]}\nEndPoint: {item.LocalEndPoint}\r\n\r\n");
            }
            // 可爱的三元表达式
            MessageBox.Show(stringBuilder.ToString() == "" ? "未找到用户" : $"{stringBuilder.ToString()}");
        }
    }
}