using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BingToday
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //设置自启动
            if( args.Length == 2 && args[0] == "AutoStart" && (args[1] == "0" || args[1] == "1"))
            {
                //检查权限
                if ( !IsAdministrator())
                {
                    Environment.Exit(1);
                    return;
                }

                //设置自启动
                if( args[1] == "0")
                {
                    //disable
                    AutoStartup.SetMeStart(false);
                }
                else
                {
                    //enable
                    AutoStartup.SetMeStart(true);
                }

                Environment.Exit(0);
                return;
            }

            //只允许单实例
            bool isNewInstance;
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            Mutex mtx = new Mutex(true, appName, out isNewInstance);

            if (!isNewInstance)
            {
                Process[] myProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(appName));
                if (null != myProcess.FirstOrDefault())
                {
                    ShowWindow(myProcess.FirstOrDefault().MainWindowHandle, 1);
                }
                Application.Exit();
                return;
            }

            Application.Run(new frmMain());
        }

        /// <summary>
        /// 确定当前主体是否属于具有指定 Administrator 的 Windows 用户组
        /// </summary>
        /// <returns>如果当前主体是指定的 Administrator 用户组的成员，则为 true；否则为 false。</returns>
        private static bool IsAdministrator()
        {
            bool result;
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                result = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                result = false;
            }
            return result;
        }
    }
}
