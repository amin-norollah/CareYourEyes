
using CareYourEyes.Popups;
using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CareYourEyes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private TimerPlus intervalTimer = new TimerPlus();
        private Timer statusTimer = new Timer();
        private PopupWindow popupWindow = new PopupWindow();

        private TaskbarIcon tb;

        public MainWindow()
        {
            InitializeComponent();

            //timers
            intervalTimer = new TimerPlus();
            intervalTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            statusTimer = new Timer();
            statusTimer.Elapsed += new ElapsedEventHandler(OnStatusTimedEvent);
            statusTimer.Interval = 1000;
            statusTimer.Enabled = true;

            //fetch settings
            FetchSetting();

            //initialize NotifyIcon
            tb = new TaskbarIcon();
            tb.Icon = new System.Drawing.Icon("icon.ico");
            tb.ToolTipText = "hello world";
            tb.PopupActivation = PopupActivationMode.DoubleClick;
            tb.Visibility = Visibility.Visible;
            tb.TrayLeftMouseUp += (s, e) =>
            {
                MainPanel.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
            };
        }

        // Specify what you want to happen when the Elapsed event is raised.
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                if (Properties.Settings.Default.breakEnable)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (popupWindow.IsActive)
                            popupWindow.Close();

                        popupWindow = new PopupWindow();
                        popupWindow.ShowDialog();
                    });
                }
            }
            catch { }
        }

        private void OnStatusTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (Properties.Settings.Default.breakEnable)
                    {
                        var timeLeft = (long)intervalTimer.TimeLeft;
                        if (timeLeft >= 0)
                        {
                            var remained = new DateTime().AddMilliseconds(timeLeft);
                            txt_status.Text = $"Next break in {remained.ToString("HH:mm:ss")}";
                        }
                    }
                    else
                    {
                        txt_status.Text = "The application is offline";
                    }
                });
            }
            catch { }
        }

        private void FetchSetting()
        {
            input_enable.IsChecked = Properties.Settings.Default.breakEnable;
            input_duration.Text = Properties.Settings.Default.breakDuration.ToString();
            input_duration.IsEnabled = Properties.Settings.Default.breakEnable;
            input_interval.Text = Properties.Settings.Default.breakInterval.ToString();
            input_interval.IsEnabled = Properties.Settings.Default.breakEnable;

            //starting/stopping interval timer
            intervalTimer.Interval = Properties.Settings.Default.breakInterval * 60 * 1000;
            intervalTimer.Enabled = input_enable.IsChecked == true;
            if (input_enable.IsChecked == true)
                intervalTimer.Start();
            else intervalTimer.Stop();
        }

        private void Registery(bool isSet = false)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        Assembly curAssembly = Assembly.GetExecutingAssembly();
                        if (isSet)
                            key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                        else key.DeleteValue(curAssembly.GetName().Name);
                    }
                }
            }
            catch { }
        }

        //////////////////////////////////
        //////////////////////////////////
        #region Events
        private void Minimize_Button_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.WindowState = WindowState.Minimized;
        }

        private void Background_Button_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to close application?", "Closing application", MessageBoxButton.YesNoCancel);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    Application.Current.Shutdown();
                    break;
                case MessageBoxResult.No:
                case MessageBoxResult.Cancel:
                    return;
            }
        }

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.breakEnable = true;
            Properties.Settings.Default.breakDuration = 90;
            Properties.Settings.Default.breakInterval = 15;
            Properties.Settings.Default.breakRegistery = false;
            Registery(false);
            Properties.Settings.Default.Save();

            FetchSetting();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            bool isParsed1 = int.TryParse(input_duration.Text, out int duration);
            bool isParsed2 = int.TryParse(input_interval.Text, out int interval);

            if (input_enable.IsChecked == true)
            {
                if (!isParsed1 || !isParsed2)
                {
                    MessageBox.Show("The duration or interval values are incorrect!", "Error", MessageBoxButton.OK);
                    return;
                }

                if (duration < 5)
                {
                    MessageBox.Show("The duration MUST bigger than 5 seconds!", "Error", MessageBoxButton.OK);
                    return;
                }

                if (interval < 1)
                {
                    MessageBox.Show("The interval MUST bigger than 1 minutes!", "Error", MessageBoxButton.OK);
                    return;
                }

                Properties.Settings.Default.breakDuration = duration;
                Properties.Settings.Default.breakInterval = interval;
            }

            Properties.Settings.Default.breakEnable = input_enable.IsChecked == true;

            if (input_startup.IsChecked != Properties.Settings.Default.breakRegistery)
            {
                Properties.Settings.Default.breakRegistery = input_startup.IsChecked == true;
                Registery(input_startup.IsChecked == true);
            }

            Properties.Settings.Default.Save();

            FetchSetting();
        }

        private void input_enable_Click(object sender, RoutedEventArgs e)
        {
            input_duration.IsEnabled = input_enable.IsChecked == true;
            input_interval.IsEnabled = input_enable.IsChecked == true;
        }

        private void MainPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    DragMove();
            }
            catch { }
        }

        private void input_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void copywrite_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Process.Start("https://github.com/amin-norollah");
            });
        }

        #endregion
        //////////////////////////////////
        //////////////////////////////////
    }

    public class TimerPlus : Timer
    {
        private DateTime m_dueTime;

        public TimerPlus() : base() => this.Elapsed += this.ElapsedAction;

        protected new void Dispose()
        {
            this.Elapsed -= this.ElapsedAction;
            base.Dispose();
        }

        public double TimeLeft => (this.m_dueTime - DateTime.Now).TotalMilliseconds;
        public new void Start()
        {
            this.m_dueTime = DateTime.Now.AddMilliseconds(this.Interval);
            base.Start();
        }

        private void ElapsedAction(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.AutoReset)
                this.m_dueTime = DateTime.Now.AddMilliseconds(this.Interval);
        }
    }
}
