using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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

            width = this.Width;
            height = this.Height;
            msgTextBox = textBox1;
            addressTextBox = materialSingleLineTextField2;
            portTextBox = materialSingleLineTextField3;

            AppentMsgText_Fast("客户端未启动");
        }
        private static async Task<bool> Connect(string ip, int port)
        {
            try
            {
                // 创建客户端 Socket 实例
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (Client == null)
                {
                    AppentMsgText_Fast("客户端启动失败");
                    return false;
                }
                await Client.ConnectAsync(ip, port);
                MessageBox.Show("已连接服务端，请在信息发送处发送您的昵称");
                return true;
            }
            catch (Exception ex)
            {
                AppentMsgText_Fast($"连接失败：{ex.Message}");
                return false;
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
                    AppentMsgText_Fast(message);
                }
            }
            catch (Exception ex)
            {
                AppentMsgText_Fast($"无法接收到信息，连接已断开：{ex.Message}");
                Client.Close();
                MessageBox.Show("连接已断开，点按确认后应用将重启");
                RestartClient();
            }
        }
        private static void RestartClient()
        {
            Application.Restart();
        }
        private void ClientForm_SizeChanged(object sender, EventArgs e)
        {
            this.ClientSize = new System.Drawing.Size(width, height);
        }

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
        internal static void AppentMsgText_Fast(string msg)
        {
            Task.Run(() => AppentMsgText(msg));
        }

        private async void materialFlatButton1_Click(object sender, EventArgs e)
        {
            if (Client==null)
            {
                msgTextBox.Clear();
                AppentMsgText_Fast("正在尝试连接服务端...");
                try
                {
                    int port = int.Parse(portTextBox.Text);
                    if (port < 0 || port > 65535)
                    {
                        AppentMsgText_Fast("端口号范围错误");
                        return;
                    }
                    if (addressTextBox.Text == "")
                    {
                        AppentMsgText_Fast("IP地址为空");
                        return;
                    }
                    if (await Connect(addressTextBox.Text, port))
                    {
                        materialSingleLineTextField1.Enabled = true;
                        materialRaisedButton1.Enabled = true;
                        materialFlatButton2.Enabled = true;
                        materialSingleLineTextField2.Enabled = false;
                        materialSingleLineTextField3.Enabled = false;
                        materialSingleLineTextField1.Focus();
                        materialSingleLineTextField1.Hint = "Please enter your nickname";
                        AppentMsgText_Fast("已连接服务端，等待输入昵称...");
                        materialFlatButton1.Text = "Disconnect";
                        return;
                    }
                    Client.Close();
                    Client = null;
                }
                catch (Exception ex)
                {
                    AppentMsgText_Fast(ex.Message);
                    return;
                }
            }
            else
            {
                RestartClient();
            }
        }

        private void  materialRaisedButton1_Click(object sender, EventArgs e)
        {
            if (Client == null)
            {
                AppentMsgText_Fast("客户端未启动");
                return;
            }
            try
            {
                string msg = materialSingleLineTextField1.Text;
                _ = Task.Run(() => Client.Send(Encoding.UTF8.GetBytes(msg)));
            }
            catch (Exception ex)
            {
                AppentMsgText_Fast($"发送失败，连接已断开：{ex.Message}");
                Client.Close();
            }

            if (materialSingleLineTextField1.Hint == "Please enter your nickname")
            {
                MessageBox.Show($"注册成功，您的昵称是：{materialSingleLineTextField1.Text}");
                materialSingleLineTextField1.Hint = "Please enter your message";
                materialSingleLineTextField1.Text = "";
                _ = Task.Run(() => ReceiveMsg());
                return;
            }
            AppentMsgText_Fast($"You:{materialSingleLineTextField1.Text}");
            materialSingleLineTextField1.Text = "";
            materialSingleLineTextField1.Focus();
        }

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
