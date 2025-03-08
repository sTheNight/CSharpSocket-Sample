using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Net.Sockets;
using System.Net;
using Application = System.Windows.Forms.Application;

namespace SocketServer_Winform
{
    public partial class ServerForm : MaterialSkin.Controls.MaterialForm
    {
        private static Dictionary<Socket, string> clients = new Dictionary<Socket, string>();
        private static Socket Server;
        internal static TextBox msgTextBox;
        public static MaterialSingleLineTextField portTextBox;

        public static int width;
        public static int height;
        public ServerForm()
        {
            InitializeComponent();
            // 初始化变量及 MaterialSkin
            width = this.Width;
            height = this.Height;
            msgTextBox = textBox1;
            portTextBox = materialSingleLineTextField2;

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

            AppendMsgText_Fast("服务端未启动");
        }
        public static bool StartServer()
        {
            // 创建服务端 Socket 实例
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                int port = int.Parse(portTextBox.Text);
                Server.Bind(new IPEndPoint(IPAddress.Any, port));
                Server.Listen(10);
            }
            catch(ArgumentException) 
            {
                AppendMsgText_Fast("端口号不能为空");
                return false;
            }
            catch (FormatException)
            {
                AppendMsgText_Fast("端口号格式错误");
                return false;
            }
            catch (Exception ex)
            {
                AppendMsgText_Fast(ex.Message);
                return false;
            }
            AppendMsgText_Fast($@"服务端已启动，等待客户端连接...");
            Task.Run(() => AcceptClient());
            return true;
        }
        private static async Task AcceptClient() // 接受客户端连接的 Task，需要使用 await 关键字因此是 async 方法
        {
            while (true)
            {

                Socket client = await Server.AcceptAsync(); // 等待客户端连接
                AppendMsgText_Fast($"监听到新的用户尝试连接: {client.RemoteEndPoint}");
                _ = Task.Run(() => ReceiveMsg(client)); // 异步执行接收消息的 Task
            }
        }
        private static async Task ReceiveMsg(Socket client) // 从客户端接收消息的 Task，需要使用 await 关键字因此是 async 方法
        {
            byte[] buffer = new byte[1024];
            try
            {
                // 新用户第一条消息必定是注册昵称，因此单独处理
                int userName_num = client.Receive(buffer);
                clients.Add(client, Encoding.UTF8.GetString(buffer, 0, userName_num));
                AppendMsgText_Fast($"{client.RemoteEndPoint}({clients[client]})已注册");
                _ = Task.Run(() => Boardcast($"{client.RemoteEndPoint}({clients[client]})已注册", client)); // 广播消息，异步执行防止阻塞线程

                while (true)
                {
                    // 其后的消息都是聊天消息
                    int num = client.Receive(buffer);
                    if (num == 0) // num 为 0 说明客户端已断开连接
                        break;
                    string message = Encoding.UTF8.GetString(buffer, 0, num);
                    AppendMsgText_Fast($"{client.RemoteEndPoint}({clients[client]}):{message}");
                    _ = Task.Run(() => Boardcast($"{client.RemoteEndPoint}({clients[client]}):{message}", client)); // 广播消息，异步执行防止阻塞线程
                }
            }
            catch (Exception ex)
            {
                // 客户端异常断开连接
                AppendMsgText_Fast($"{client.RemoteEndPoint}({clients[client]}): {ex.Message}");
                AppendMsgText_Fast($"{client.RemoteEndPoint}({clients[client]})已离开");
                await Boardcast($"{client.RemoteEndPoint}({clients[client]})已离开", client);// 需等待广播完成否则会出现异常
                clients.Remove(client);
            }
        }
        private static async Task Boardcast(string message, Socket sender) // 广播消息的Task，由于部分场景需要等待广播完成因此是 async 方法
        {
            if (message == "")
            {
                string msg = sender == null ? "服务端发送内容为空" : $"{sender.RemoteEndPoint}发送内容为空";
                AppendMsgText_Fast($"广播失败，{msg}");
                return;
            }
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                foreach (var client in clients.Keys)
                {
                    if (client != sender)
                        _ =  Task.Run(() => client.Send(buffer));
                }
            }
            catch (Exception ex)
            {
                AppendMsgText_Fast($"广播失败: {ex.Message}({sender.RemoteEndPoint})");
            }
        }
        // 由于无法跨线程操作控件，因此使用委托代理控件操作
        internal static void AppendMsgText(string msg)
        {
            if (msgTextBox.InvokeRequired)
            {
                // 倘若当前处在 UI 线程之外，则使用 Invoke 方法将操作委托到 UI 线程
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
        internal static void AppendMsgText_Fast(string msg)
        {
            Task.Run(() => AppendMsgText(msg));
        }
        // 锁定布局大小，实现方式较粗糙
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            this.ClientSize = new System.Drawing.Size(width, height);
        }

        /**
         * 
         *  控件事件区
         *  
         */

        // 启动/停止服务端按钮的方法
        private void materialFlatButton2_Click(object sender, EventArgs e)
        {
            if (Server == null) // 若 Server 未实例化则说明服务端未启动
            {
                textBox1.Clear();
                if (StartServer()) // 判断服务端是否启动成功
                {
                    materialFlatButton1.Enabled = true;
                    materialFlatButton3.Enabled = true;
                    materialSingleLineTextField1.Enabled = true;
                    materialSingleLineTextField2.Enabled = false;
                    materialRaisedButton1.Enabled = true;
                    materialFlatButton2.Text = "Stop";
                    return;
                }
                Server.Close();
                Server = null;
                return;
            }
            // 关闭服务端，由于停止 Task 较为麻烦，因此直接重启程序
            Application.Restart();

        }
        // 查看服务端信息按钮的方法
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
        // 查看用户信息按钮的方法
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
        // 发送消息按钮的方法
        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            string msg = materialSingleLineTextField1.Text;
            if (msg == "")
            {
                AppendMsgText_Fast("发送失败: 信息为空");
                return;
            }
            AppendMsgText_Fast($"Server: {msg}");
            materialSingleLineTextField1.Text = "";
            materialSingleLineTextField1.Focus();
            // 使用广播方法来发送信息，以免重复造轮子
            _ = Task.Run(() => Boardcast($"Server: {msg}", null));
        }
    }
}