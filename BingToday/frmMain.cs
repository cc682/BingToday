using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BingToday
{

    public partial class frmMain : Form
    {
        const int WM_SYSCOMMAND = 0x112;
        const int SC_CLOSE = 0xF060;
        const int SC_MINIMIZE = 0xF020;
        const int SC_MAXIMIZE = 0xF030;

        const string BING_MAINPAGE_URL = "https://www.bing.com/";
        const string BG_IMG_REGEX = @"#bgDiv{\s*opacity:\s*[0-9]*;background-image:url\((?<url>[^\)]*?)\)";

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.notifyIcon1.Visible = true;
            LoadConfig();

            RefreshBg(false);
        }

        protected override void WndProc(ref Message m)
        {
            if( m.Msg == WM_SYSCOMMAND)
            {
                if( m.WParam.ToInt32() == SC_MINIMIZE)
                {
                    this.Visible = false;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if( e.Button == MouseButtons.Left)
            {
                this.Visible = !this.Visible;
            }
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Tips( string strTitle, string strContent, int nMilliseconds = 1000)
        {
            if( !miShowTips.Checked)
            {
                return;
            }

            ///托盘气泡提示
            int tipShowMilliseconds = nMilliseconds;
            string tipTitle = strTitle;
            string tipContent = strContent;
            ToolTipIcon tipType = ToolTipIcon.Info;
            notifyIcon1.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
        }

        private string LoadBingWeb( string strUrl)
        {
            string htmlStr = "";
            if (!String.IsNullOrEmpty(strUrl))
            {
                WebRequest request = WebRequest.Create(strUrl);            //实例化WebRequest对象
                WebResponse response = request.GetResponse();           //创建WebResponse对象
                Stream datastream = response.GetResponseStream();       //创建流对象
                Encoding ec = Encoding.Default;
                StreamReader reader = new StreamReader(datastream, Encoding.UTF8);
                htmlStr = reader.ReadToEnd();                           //读取数据
                reader.Close();
                datastream.Close();
                response.Close();
            }
            return htmlStr;
        }

        private void miRefresh_Click(object sender, EventArgs e)
        {
            RefreshBg();
        }

        private void RefreshBg(bool blSetSystemBg = true)
        {
            string strUrl = GetBingBgUrl();
            if (strUrl == null)
            {
                Tips("BingToday", "Load bg image failed.");
                return;
            }

            string strFile = DownloadBg(BING_MAINPAGE_URL + strUrl);

            if(blSetSystemBg)
            {
                Wallpaper.SetWallPaper(strFile, Wallpaper.Style.Fill);

                Properties.Settings.Default.LastRefresh = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                Properties.Settings.Default.Save();

                Tips("BingToday", "Load bg ok.");
            }

            this.pictureBox1.Load(strFile);
        }

        private string GetBingBgUrl()
        {
            try
            {
                string strWeb = LoadBingWeb(BING_MAINPAGE_URL);
                Debug.WriteLine(strWeb);

                Regex regex = new Regex(BG_IMG_REGEX);
                Match match = regex.Match(strWeb);

                if (match.Success)
                {
                    string strUrl = match.Groups["url"].Value;

                    Console.WriteLine(BING_MAINPAGE_URL + strUrl);

                    return strUrl;
                }
            }
            catch( Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }

            return null;
        }

        private string DownloadBg( string strUrl)
        {
            string strTmpFile = Path.GetTempFileName();

            try
            {
                Random rd = new Random();
                DateTime nowTime = DateTime.Now;
                WebClient webClient = new WebClient();

                webClient.DownloadFile(strUrl, strTmpFile);
                return strTmpFile;
            }
            catch 
            {
                return null;
            }
        }

        private void LoadConfig()
        {
            this.miShowTips.Checked = Properties.Settings.Default.ShowTips;
            this.miAutoRefresh.Checked = Properties.Settings.Default.AutoRefresh;
            this.miAutoStart.Checked = Properties.Settings.Default.AutoStartup;
            this.miAutoHide.Checked = Properties.Settings.Default.AutoHide;
        }

        private void SaveConfig()
        {
            Properties.Settings.Default.ShowTips = this.miShowTips.Checked;
            Properties.Settings.Default.AutoRefresh = this.miAutoRefresh.Checked;
            Properties.Settings.Default.AutoStartup = this.miAutoStart.Checked;
            Properties.Settings.Default.AutoHide = this.miAutoHide.Checked;

            Properties.Settings.Default.Save();
        }

        private void miShowTips_Click(object sender, EventArgs e)
        {
            miShowTips.Checked = !miShowTips.Checked;
            SaveConfig();
        }

        private void miAutoRefresh_Click(object sender, EventArgs e)
        {
            miAutoRefresh.Checked = !miAutoRefresh.Checked;
            SaveConfig();
        }

        private void miAutoStart_Click(object sender, EventArgs e)
        {
            if( !miAutoStart.Checked)
            {
                miAutoStart.Checked = StartAsAdministrator("AutoStart 1");
            }
            else
            {
                miAutoStart.Checked = !StartAsAdministrator("AutoStart 0");
            }

            SaveConfig();
        }

        private void timerMain_Tick(object sender, EventArgs e)
        {
            //不自动刷新,退出
            if( !miAutoRefresh.Checked)
            {
                return;
            }

            //判断时间
            DateTime dt = (new DateTime(1970, 1, 1, 0, 0, 0, 0)) + Properties.Settings.Default.LastRefresh;
            if( dt.Year == DateTime.Now.Year && dt.DayOfYear == DateTime.Now.DayOfYear)
            {
                return;
            }

            RefreshBg();
        }

        private bool StartAsAdministrator(string strParam)
        {
            //创建启动对象
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Arguments = strParam;
            //设置启动动作,确保以管理员身份运行
            startInfo.Verb = "runas";
            try
            {
                Process ps = System.Diagnostics.Process.Start(startInfo);
                ps.WaitForExit();
                return ps.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private void miAutoHide_Click(object sender, EventArgs e)
        {
            miAutoHide.Checked = !miAutoHide.Checked;

            SaveConfig();
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.AutoHide)
            {
                this.Visible = false;
            }
        }
    }
}
