﻿
using MahApps.Metro.Controls;
using System;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace CareYourEyes.Popups
{
    /// <summary>
    /// Interaction logic for Popup.xaml
    /// </summary>
    public partial class PopupWindow : MetroWindow
    {
        private TimeSpan restTimer = new TimeSpan();
        private Timer breakTimer = new Timer();

        public PopupWindow()
        {
            InitializeComponent();

            //timer
            breakTimer = new Timer();
            breakTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            breakTimer.Interval = 1000;
            breakTimer.Enabled = true;

            //update UI
            this.Dispatcher.Invoke(() =>
            {
                Popup1.IsOpen = true;
                restTimer = TimeSpan.FromSeconds(Properties.Settings.Default.breakDuration);
                txt_timer.Text = restTimer.ToString();
            });
        }

        // Specify what you want to happen when the Elapsed event is raised.
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (restTimer.TotalSeconds > 0)
                {
                    restTimer -= TimeSpan.FromSeconds(1);
                    txt_timer.Text = restTimer.ToString();
                }
                else
                    this.Close();
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
