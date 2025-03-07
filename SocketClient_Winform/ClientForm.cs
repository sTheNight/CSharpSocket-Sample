using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Net.Sockets;

namespace SocketClient_Winform
{
    public partial class ClientForm : MaterialSkin.Controls.MaterialForm
    {
        public static int width;
        public static int height;
        internal static TextBox msgTextBox;
        internal static Socket Client;

        internal static MaterialSingleLineTextField addressTextBox;
        internal static MaterialSingleLineTextField portTextBox;
        public ClientForm()
        {
            InitializeComponent();

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);
            // 初始化变量
            width = this.Width;
            height = this.Height;
            msgTextBox = textBox1;
            addressTextBox = materialSingleLineTextField2;
            portTextBox = materialSingleLineTextField3;
            // 未启动时的信息提示
            AppendMsgText_Fast("未连接到服务端");
        }
        private static async Task<bool> Connect(string ip, int port)
        {
            try
            {
                // 创建客户端 Socket 实例
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (Client == null)
                {
                    AppendMsgText_Fast("客户端启动失败");
                    return false;
                }
                await Client.ConnectAsync(ip, port); // 由于需要等待连接完成再进行操作，因此使用异步方法 + await，不然就会阻塞 UI 线程
                MessageBox.Show("已连接服务端，请在信息发送处发送您的昵称");
                return true;
            }
            catch (Exception ex)
            {
                AppendMsgText_Fast($"连接失败：{ex.Message}");
                return false;
            }
        }
        // 接收信息的方法
        private static async void ReceiveMsg()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int num = Client.Receive(buffer); // 异步版方法有点麻烦，因此用同步版
                    if (num == 0)
                        break;
                    string message = Encoding.UTF8.GetString(buffer, 0, num);
                    AppendMsgText_Fast(message);
                }
            }
            catch (Exception ex)
            {
                // 在接收信息时发生异常，说明连接已断开
                AppendMsgText_Fast($"无法接收到信息，连接已断开：{ex.Message}");
                Client.Close();
                MessageBox.Show("连接已断开，点按确认后应用将重启");
                Application.Restart();
            }
        }
        // 请看 Server 中同名方法的注释
        internal static void AppendMsgText(string msg)
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
        internal static void AppendMsgText_Fast(string msg)
        {
            Task.Run(() => AppendMsgText(msg));
        }
        /**
         * 
         *  窗口事件区
         *  
         */
        private void ClientForm_SizeChanged(object sender, EventArgs e)
        {
            // 锁定窗口大小
            this.ClientSize = new System.Drawing.Size(width, height);
        }
        // 连接/断开按钮的方法
        private async void materialFlatButton1_Click(object sender, EventArgs e)
        {
            // 如果未连接服务端，则尝试连接
            if (Client==null)
            {
                msgTextBox.Clear();
                AppendMsgText_Fast("正在尝试连接服务端...");
                try
                {
                    int port = int.Parse(portTextBox.Text);
                    if (port < 0 || port > 65535)
                    {
                        AppendMsgText_Fast("端口号范围错误");
                        return;
                    }
                    if (addressTextBox.Text == "")
                    {
                        AppendMsgText_Fast("IP地址为空");
                        return;
                    }
                    if (await Connect(addressTextBox.Text, port))
                    {
                        // 连接成功后，禁用地址和端口输入框，启用昵称输入框
                        materialSingleLineTextField1.Enabled = true;
                        materialRaisedButton1.Enabled = true;
                        materialFlatButton2.Enabled = true;
                        materialSingleLineTextField2.Enabled = false;
                        materialSingleLineTextField3.Enabled = false;
                        materialSingleLineTextField1.Focus();
                        materialSingleLineTextField1.Hint = "Please enter your nickname";
                        AppendMsgText_Fast("已连接服务端，等待输入昵称...");
                        materialFlatButton1.Text = "Disconnect";
                        return;
                    }
                    // 若连接失败，关闭客户端
                    Client.Close();
                    Client = null;
                }
                catch (Exception ex)
                {
                    AppendMsgText_Fast(ex.Message);
                    return;
                }
            }
            else
            {
                // 若已连接服务端，则采用重启客户端的方式断开连接
                Application.Restart();
            }
        }
        // 信息发送按钮的方法
        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            if (Client == null)
            {
                // 虽然在未启动时已经禁用了发送按钮，但还是加上判断为好
                AppendMsgText_Fast("客户端未启动");
                return;
            }
            try
            {
                // 发送信息
                string msg = materialSingleLineTextField1.Text;
                _ = Task.Run(() => Client.Send(Encoding.UTF8.GetBytes(msg)));
            }
            catch (Exception ex)
            {
                AppendMsgText_Fast($"发送失败，连接已断开：{ex.Message}");
                Client.Close();
                return;
            }
            // 如果是第一次发送信息，那么发送的信息是昵称，使用 Hint 属性判断，较为粗糙
            if (materialSingleLineTextField1.Hint == "Please enter your nickname")
            {
                msgTextBox.Clear();
                AppendMsgText_Fast("注册成功，您的昵称是：" + materialSingleLineTextField1.Text);
                MessageBox.Show($"注册成功，您的昵称是：{materialSingleLineTextField1.Text}");
                materialSingleLineTextField1.Hint = "Please enter your message";
                materialSingleLineTextField1.Text = "";
                _ = Task.Run(() => ReceiveMsg()); // 开始接收信息
                return;
            }
            AppendMsgText_Fast($"You:{materialSingleLineTextField1.Text}");
            materialSingleLineTextField1.Text = "";
            materialSingleLineTextField1.Focus();
        }
        // 客户端信息按钮的方法
        private void materialFlatButton2_Click(object sender, EventArgs e)
        {
            MessageBox.Show($@"Local EndPoint: {Client.LocalEndPoint}
Remote EndPoint: {Client.RemoteEndPoint}
AddressFamily: {Client.AddressFamily}
SocketType: {Client.SocketType}
ProtocolType: {Client.ProtocolType}");
        }
    }
}
