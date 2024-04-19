using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace id5_login
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsPythonInstalled, IsMitmproxyInstalled1, startScript;
        public static MainWindow mainWindow;
        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
        }
        private void CheckPythonAndPipInstallationAndUpdateButton()
        {
            var (isPythonInstalled, isPipInstalled) = PythonChecker.CheckPythonAndPipInstallation();
            if (isPythonInstalled && isPipInstalled)
            {
                PythonCheck.Content = "重新安装Python";
                IsPythonInstalled = true;
            }
            else if (!isPythonInstalled)
            {
                PythonCheck.Content = "安装Python";
                IsPythonInstalled = false;
            }
        }
        public void DownloadHttpFile(String http_url, String save_url)
        {
            WebResponse response = null;
            //获取远程文件
            WebRequest request = WebRequest.Create(http_url);
            response = request.GetResponse();
            if (response == null) return;
            //读远程文件的大小
            PbDown.Maximum = response.ContentLength;
            //下载远程文件
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                Stream netStream = response.GetResponseStream();
                Stream fileStream = new FileStream(save_url, FileMode.Create);
                byte[] read = new byte[1024];
                long progressBarValue = 0;
                int realReadLen = netStream.Read(read, 0, read.Length);
                while (realReadLen > 0)
                {
                    fileStream.Write(read, 0, realReadLen);
                    progressBarValue += realReadLen;
                    PbDown.Dispatcher.BeginInvoke(new ProgressBarSetter(SetProgressBar), progressBarValue);
                    realReadLen = netStream.Read(read, 0, read.Length);
                }
                netStream.Close();
                fileStream.Close();
            }, null);
        }
        /// <summary>
        ///  判断远程文件是否存在
        /// </summary>
        /// <param name="fileUrl">文件URL</param>
        /// <returns>存在-true，不存在-false</returns>
        private bool HttpFileExist(string http_file_url)
        {
            WebResponse response = null;
            bool result = false;//下载结果
            try
            {
                response = WebRequest.Create(http_file_url).GetResponse();
                result = response == null ? false : true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return result;
        }
        public delegate void ProgressBarSetter(double value);
        public void SetProgressBar(double value)
        {
            //显示进度条
            PbDown.Value = value;
            double persent = (value / PbDown.Maximum) * 100;
            int persentToInt = (int)persent;
            //显示百分比
            label1.Content = persentToInt + "%";
            if (persentToInt == 100)
            {
                label1.Content = "下载完成";
                if (Environment.Is64BitOperatingSystem)
                {
                    // 64-bit system
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + "python-3.12.2-amd64.exe");
                }
                else
                {
                    // 32-bit system
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + "python-3.12.3.exe");
                }

            }
        }
        private void PythonCheck_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("是否安装Python", "Python Installation", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                string http_url, save_url;
                if (Environment.Is64BitOperatingSystem)
                {
                    // 64-bit system
                    http_url = "https://gitee.com/plfjy/id5-login-resource/releases/download/V1.0/python-3.12.2-amd64.exe";
                    save_url = AppDomain.CurrentDomain.BaseDirectory + "python-3.12.2-amd64.exe";
                }
                else
                {
                    // 32-bit system
                    http_url = "https://gitee.com/plfjy/id5-login-resource/releases/download/V1.0/python-3.12.3.exe";
                    save_url = AppDomain.CurrentDomain.BaseDirectory + "python-3.12.3.exe";
                }
                DownloadHttpFile(http_url, save_url);
                Pb.Visibility = Visibility.Visible;
            }
        }
        public bool IsMitmproxyInstalled()
        {
            bool isInstalled = false;
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "-m pip show mitmproxy",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    using (System.IO.StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        isInstalled = !string.IsNullOrEmpty(result);
                    }
                }
            }
            catch (Exception)
            {
                isInstalled = false;
            }
            return isInstalled;
        }

        private void InitCheck_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("是否安装mitmproxy?", "mitmproxy Installation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo($"cmd.exe", "/k \"python idv-login-main\\setUp.py\"");
                    processInfo.Verb = "runas";
                    Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while running the setup: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //弹出确认框
            MessageBoxResult result = MessageBox.Show("是否退出并关闭脚本?", "Exit", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            //关闭python.exe进程
            Process[] processes = Process.GetProcessesByName("python");
            foreach (Process process in processes)
            {
                process.Kill();
            }
            //关闭cmd.exe进程
            Process[] cmdProcesses = Process.GetProcessesByName("cmd");
            foreach (Process cmdProcess in cmdProcesses)
            {
                cmdProcess.Kill();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //启动About窗口
            About about = new About();
            about.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckPythonAndPipInstallationAndUpdateButton();
            if (IsMitmproxyInstalled() == true)
            {
                InitCheck.Content = "mitmproxy已安装";
                IsMitmproxyInstalled1 = true;
            }
            else
            {
                InitCheck.Content = "安装mitmproxy";
                IsMitmproxyInstalled1 = false;
            }
            mainWindow.Title = "第五人格PC端登录工具";
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (startScript)
            {
                //关闭python.exe进程
                Process[] processes = Process.GetProcessesByName("python");
                foreach (Process process in processes)
                {
                    process.Kill();
                }
                //关闭cmd.exe进程
                Process[] cmdProcesses = Process.GetProcessesByName("cmd");
                foreach (Process cmdProcess in cmdProcesses)
                {
                    cmdProcess.Kill();
                }
                Start.Content = "启动登录脚本";
                startScript = false;
            }
            else
            {
                if (IsPythonInstalled && IsMitmproxyInstalled1)
                {
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + "idv-login-main\\run.bat");
                    Start.Content = "关闭登录脚本";
                    startScript = true;
                }
                else
                {
                    MessageBox.Show("请先安装Python或mitmproxy", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    public class PythonChecker
    {
        public static (bool IsPythonInstalled, bool IsPipInstalled) CheckPythonAndPipInstallation()
        {
            bool isPythonInstalled = CheckPythonInstalled();
            bool isPipInstalled = false;
            if (isPythonInstalled)
            {
                isPipInstalled = CheckPipInstalled();
            }
            return (isPythonInstalled, isPipInstalled);
        }
        private static bool CheckPythonInstalled()
        {
            using (var pythonProcess = new Process())
            {
                pythonProcess.StartInfo.FileName = "python";
                pythonProcess.StartInfo.Arguments = "--version";
                pythonProcess.StartInfo.UseShellExecute = false;
                pythonProcess.StartInfo.RedirectStandardOutput = true;
                pythonProcess.StartInfo.CreateNoWindow = true;
                try
                {
                    pythonProcess.Start();
                    pythonProcess.WaitForExit();
                    int exitCode = pythonProcess.ExitCode;
                    // 假设如果python能正常启动并返回0，则认为它已安装  
                    return exitCode == 0;
                }
                catch (Exception ex)
                {
                    // 如果出现任何异常（如文件未找到），则认为Python未安装  
                    Console.WriteLine("Error checking Python installation: " + ex.Message);
                    return false;
                }
            }
        }
        private static bool CheckPipInstalled()
        {
            using (var pipProcess = new Process())
            {
                pipProcess.StartInfo.FileName = "pip";
                pipProcess.StartInfo.Arguments = "--version";
                pipProcess.StartInfo.UseShellExecute = false;
                pipProcess.StartInfo.RedirectStandardOutput = true;
                pipProcess.StartInfo.CreateNoWindow = true;
                try
                {
                    pipProcess.Start();
                    pipProcess.WaitForExit();
                    int exitCode = pipProcess.ExitCode;
                    // 假设如果pip能正常启动并返回0，则认为它已安装  
                    return exitCode == 0;
                }
                catch (Exception ex)
                {
                    // 如果出现任何异常（如文件未找到），则认为pip未安装  

                    Console.WriteLine("Error checking pip installation: " + ex.Message);

                    return false;
                }
            }
        }
    }
}