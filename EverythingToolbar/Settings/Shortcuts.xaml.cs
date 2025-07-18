﻿using EverythingToolbar.Helpers;
using NHotkey.Wpf;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace EverythingToolbar.Settings
{
    public partial class Shortcuts
    {
        public Key Key { get; private set; }
        public ModifierKeys Modifiers { get; private set; }
        private ModifierKeys TempMods { get; set; }

        private static event EventHandler<WinKeyEventArgs> WinKeyEventHandler;

        private static LowLevelKeyboardProc _llKeyboardHookCallback;
        private static IntPtr _llKeyboardHookId = IntPtr.Zero;

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSyskeydown = 0x0104;

        public Shortcuts()
        {
            InitializeComponent();
        }

        private void OnKeyPressedReleased(object sender, WinKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Control : TempMods & ~ModifierKeys.Control;
                    break;
                case Key.LWin:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Windows : TempMods & ~ModifierKeys.Windows;
                    break;
                case Key.LeftAlt:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Alt : TempMods & ~ModifierKeys.Alt;
                    break;
                case Key.LeftShift:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Shift : TempMods & ~ModifierKeys.Shift;
                    break;
                default:
                    if (e.IsDown)
                    {
                        if (TempMods == ModifierKeys.None && e.Key == Key.Escape)
                        {
                            Key = Key.None;
                            Modifiers = ModifierKeys.None;
                        }
                        else
                        {
                            Key = e.Key;
                            Modifiers = TempMods;
                        }
                    }
                    break;
            }

            UpdateTextBox();
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return CallNextHookEx(_llKeyboardHookId, nCode, wParam, lParam);

            var vkCode = (Keys)Marshal.ReadInt32(lParam);
            var isDown = (int)wParam == WmKeydown || (int)wParam == WmSyskeydown;
            switch (vkCode)
            {
                case Keys.Control:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftCtrl));
                    break;
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftShift));
                    break;
                case Keys.Alt:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftAlt));
                    break;
                case Keys.LWin:
                case Keys.RWin:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LWin));
                    break;
                default:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, KeyInterop.KeyFromVirtualKey((int)vkCode)));
                    break;
            }

            return 1;

        }

        private static void CaptureKeyboard(EventHandler<WinKeyEventArgs> callback)
        {
            ReleaseKeyboard();
            WinKeyEventHandler += callback;
            _llKeyboardHookCallback = KeyboardHookCallback;
            _llKeyboardHookId = SetWindowsHookEx(WhKeyboardLl, _llKeyboardHookCallback, 0, 0);
        }

        private static void ReleaseKeyboard()
        {
            WinKeyEventHandler = null;
            UnhookWindowsHookEx(_llKeyboardHookId);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CaptureKeyboard(OnKeyPressedReleased);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ReleaseKeyboard();
        }

        private void UpdateTextBox()
        {
            var shortcutText = new StringBuilder();
            if ((Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyCtrl);
            }
            if ((Modifiers & ModifierKeys.Windows) != 0)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Properties.Resources.KeyWin);
            }
            if ((Modifiers & ModifierKeys.Alt) != 0)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Properties.Resources.KeyAlt);
            }
            if ((Modifiers & ModifierKeys.Shift) != 0)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Properties.Resources.KeyShift);
            }
            if (Key != Key.None)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Key.ToString());
            }

            ShortcutTextBox.Text = shortcutText.ToString();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            StartMenuIntegration.Instance.Disable();
            HotkeyManager.Current.IsEnabled = false;

            Modifiers = (ModifierKeys)ToolbarSettings.User.ShortcutModifiers;
            Key = (Key)ToolbarSettings.User.ShortcutKey;
            UpdateTextBox();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            HotkeyManager.Current.IsEnabled = true;
            ReleaseKeyboard();
            if (ToolbarSettings.User.IsReplaceStartMenuSearch)
                StartMenuIntegration.Instance.Enable();

            ApplyShortcut();
        }

        private void ApplyShortcut()
        {
            if (Modifiers == ModifierKeys.Windows)
            {
                // Windows Explorer reserves many shortcuts with the Windows key. Therefore, we need to update the settings,
                // kill explorer (and the deskband) and let the initialize routine set the shortcut before explorer has time to do so.
                ShortcutManager.UpdateSettings(Key, Modifiers);
                foreach (var exe in Process.GetProcesses())
                {
                    if (exe.ProcessName == "explorer")
                        exe.Kill();
                }
            }

            ShortcutManager.TrySetShortcut(Key, Modifiers);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public class WinKeyEventArgs(bool isDown, Key key) : EventArgs
        {
            public bool IsDown { get; set; } = isDown;
            public Key Key { get; set; } = key;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    }
}