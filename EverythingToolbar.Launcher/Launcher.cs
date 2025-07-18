using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using EverythingToolbar.Launcher.Properties;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shell;
using Application = System.Windows.Application;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Timer = System.Timers.Timer;

namespace EverythingToolbar.Launcher
{
    internal static class Launcher
    {
        private const string ToggleEventName = "EverythingToolbarToggleEvent";
        private const string StartSetupAssistantEventName = "StartSetupAssistantEvent";
        private const string MutexName = "EverythingToolbar.Launcher";
        private static bool _searchWindowRecentlyClosed;
        private static Timer _searchWindowRecentlyClosedTimer;
        private static NotifyIcon _notifyIcon;

        private class LauncherWindow : Window
        {
            public LauncherWindow(NotifyIcon icon)
            {
                ToolbarLogger.Initialize("Launcher");

                _notifyIcon = icon;
                SetupJumpList();

                _searchWindowRecentlyClosedTimer = new Timer(500);
                _searchWindowRecentlyClosedTimer.AutoReset = false;
                _searchWindowRecentlyClosedTimer.Elapsed += (s, e) => { _searchWindowRecentlyClosed = false; };

                Width = 0;
                Height = 0;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;

                TaskbarStateManager.Instance.IsIcon = true;

                var behavior = new SearchWindowPlacement();
                Interaction.GetBehaviors(SearchWindow.Instance).Add(behavior);

                StartToggleListener();

                if (!Utils.IsTaskbarPinned() && (!ToolbarSettings.User.IsSetupAssistantDisabled || !ToolbarSettings.User.IsTrayIconEnabled))
                    new SetupAssistant(icon).Show();

                ShortcutManager.Initialize(FocusSearchBox);

                if (ToolbarSettings.User.IsReplaceStartMenuSearch)
                    StartMenuIntegration.Instance.Enable();

                SearchWindow.Instance.Hiding += OnSearchWindowHiding;

                ToolbarSettings.User.PropertyChanged += async (sender, e) =>
                {
                    if (e.PropertyName == nameof(ToolbarSettings.User.IsTrayIconEnabled))
                    {
                        _notifyIcon.Visible = ToolbarSettings.User.IsTrayIconEnabled;
                    }
                    else if (e.PropertyName == nameof(ToolbarSettings.User.IconName))
                    {
                        var restartExplorer = await FluentMessageBox.CreateYesNo(
                            Properties.Resources.SetupAssistantRestartExplorerDialogText,
                            Properties.Resources.SetupAssistantRestartExplorerDialogTitle
                            ).ShowDialogAsync() == MessageBoxResult.Primary;
                        Utils.ChangeTaskbarPinIcon(ToolbarSettings.User.IconName, restartExplorer);
                    }
                };
            }

            private void SetupJumpList()
            {
                var jumpList = new JumpList();
                jumpList.JumpItems.Add(new JumpTask
                {
                    Title = Properties.Resources.ContextMenuRunSetupAssistant,
                    Description = Properties.Resources.ContextMenuRunSetupAssistant,
                    ApplicationPath = Environment.ProcessPath,
                    Arguments = "--run-setup-assistant"
                });
                JumpList.SetJumpList(Application.Current, jumpList);
            }

            private static void OnSearchWindowHiding(object sender, EventArgs e)
            {
                _searchWindowRecentlyClosed = true;
                _searchWindowRecentlyClosedTimer.Start();
            }

            private static void FocusSearchBox(object sender, HotkeyEventArgs e)
            {
                SearchWindow.Instance.Toggle();
            }

            private void StartToggleListener()
            {
                Task.Factory.StartNew(() =>
                {
                    var wh = new EventWaitHandle(false, EventResetMode.AutoReset, ToggleEventName);
                    while (true)
                    {
                        wh.WaitOne();
                        ToggleWindow();
                    }
                });
                Task.Factory.StartNew(() =>
                {
                    var wh = new EventWaitHandle(false, EventResetMode.AutoReset, StartSetupAssistantEventName);
                    while (true)
                    {
                        wh.WaitOne();
                        OpenSetupAssistant();
                    }
                });
            }

            private void ToggleWindow()
            {
                // Prevent search window from reappearing after clicking the icon to close
                if (_searchWindowRecentlyClosed)
                    return;

                Dispatcher?.Invoke(() =>
                {
                    SearchWindow.Instance.Toggle();
                });
            }

            private void OpenSetupAssistant()
            {
                Dispatcher?.Invoke(() =>
                {
                    new SetupAssistant(_notifyIcon).Show();
                });
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            using (new Mutex(false, MutexName, out var createdNew))
            {
                if (createdNew)
                {
                    using (var trayIcon = new NotifyIcon())
                    {
                        var app = new Application();
                        trayIcon.Icon = new Icon(Utils.GetThemedAppIconName());
                        trayIcon.ContextMenuStrip = new ContextMenuStrip();
                        var setupItem = new ToolStripMenuItem(
                            Resources.ContextMenuRunSetupAssistant,
                            null,
                            (s, e) => { new SetupAssistant(trayIcon).Show(); }
                        );
                        trayIcon.ContextMenuStrip.Items.Add(setupItem);
                        var quitItem = new ToolStripMenuItem(
                            Resources.ContextMenuQuit,
                            null,
                            (s, e) => { app.Shutdown(); }
                        );
                        trayIcon.ContextMenuStrip.Items.Add(quitItem);
                        trayIcon.Visible = ToolbarSettings.User.IsTrayIconEnabled;
                        app.Run(new LauncherWindow(trayIcon));
                    }
                }
                else
                {
                    try
                    {
                        if (args.Length > 0 && args[0] == "--run-setup-assistant")
                        {
                            EventWaitHandle.OpenExisting(StartSetupAssistantEventName).Set();
                        }
                        else
                        {
                            EventWaitHandle.OpenExisting(ToggleEventName).Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        FluentMessageBox.CreateError(ex.Message, "Error").ShowDialogAsync();
                    }
                }
            }
        }
    }
}