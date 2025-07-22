﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public class AcrylicWindow : Window
    {
        [DllImport("DWMAPI")]
        private static extern IntPtr DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("DWMAPI")]
        private static extern IntPtr DwmSetWindowAttribute(
            IntPtr hwnd,
            DwmWindowAttribute attribute,
            ref int value,
            uint dataSize
        );

        [DllImport("User32")]
        private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("User32")]
        private static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr hwndInsertAfter,
            int x,
            int y,
            int width,
            int height,
            SetWindowPosFlags flags
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr DataPointer;
            public uint DataSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public AccentFlags AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int LeftWidth;
            public int RightWidth;
            public int TopHeight;
            public int BottomHeight;
        }

        private enum AccentState
        {
            Disabled,
            EnableGradient = 1,
            EnableTransparent = 2,
            EnableBlurBehind = 3,
            EnableAcrylicBlurBehind = 4,
            EnableHostBackdrop = 5,
            InvalidState = 6,
        }

        [Flags]
        private enum AccentFlags
        {
            None = 0,
            ExtendSize = 0x4,
            LeftBorder = 0x20,
            TopBorder = 0x40,
            RightBorder = 0x80,
            BottomBorder = 0x100,
            AllBorder = LeftBorder | TopBorder | RightBorder | BottomBorder,
        }

        private enum WindowCompositionAttribute
        {
            WcaAccentPolicy = 19,
        }

        private enum DwmWindowAttribute
        {
            WINDOW_CORNER_PREFERENCE = 33,
        }

        public enum WindowCorner
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3,
        }

        [Flags]
        private enum SetWindowPosFlags
        {
            ASYNCWINDOWPOS = 16384,
            DEFERERASE = 8192,
            DRAWFRAME = 32,
            FRAMECHANGED = 32,
            HIDEWINDOW = 128,
            NOACTIVATE = 16,
            NOCOPYBITS = 256,
            NOMOVE = 2,
            NOOWNERZORDER = 512,
            NOREDRAW = 8,
            NOREPOSITION = 512,
            NOSENDCHANGING = 1024,
            NOSIZE = 1,
            NOZORDER = 4,
            SHOWWINDOW = 64,
        }

        public bool IsAcrylicEnabled
        {
            get => (bool)GetValue(IsAcrylicEnabledProperty);
            set => SetValue(IsAcrylicEnabledProperty, value);
        }

        public static readonly DependencyProperty IsAcrylicEnabledProperty = DependencyProperty.Register(
            nameof(IsAcrylicEnabled),
            typeof(bool),
            typeof(AcrylicWindow),
            new PropertyMetadata(true, OnAcrylicPropertyChanged)
        );

        public Color AcrylicGradientColor
        {
            get => (Color)GetValue(AcrylicGradientColorProperty);
            set => SetValue(AcrylicGradientColorProperty, value);
        }

        public static readonly DependencyProperty AcrylicGradientColorProperty = DependencyProperty.Register(
            nameof(AcrylicGradientColor),
            typeof(Color),
            typeof(AcrylicWindow),
            new PropertyMetadata(Colors.Transparent, OnAcrylicPropertyChanged)
        );

        public bool ShowAccentBorder
        {
            get => (bool)GetValue(ShowAccentBorderProperty);
            set => SetValue(ShowAccentBorderProperty, value);
        }

        public static readonly DependencyProperty ShowAccentBorderProperty = DependencyProperty.Register(
            nameof(ShowAccentBorder),
            typeof(bool),
            typeof(AcrylicWindow),
            new PropertyMetadata(false, OnAcrylicPropertyChanged)
        );

        protected AcrylicWindow()
        {
            WindowChrome.SetWindowChrome(
                this,
                new WindowChrome
                {
                    GlassFrameThickness = new Thickness(0),
                    CaptionHeight = 0,
                    ResizeBorderThickness = new Thickness(5),
                    CornerRadius = new CornerRadius(0),
                }
            );

            Background = Brushes.Transparent;

            Loaded += OnWindowLoaded;
            SourceInitialized += OnSourceInitialized;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyAcrylicEffect();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            ApplyAcrylicEffect();
            ApplyWindowCorner();
        }

        private static void OnAcrylicPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AcrylicWindow { IsLoaded: true } window)
            {
                window.ApplyAcrylicEffect();
            }
        }

        private void ApplyAcrylicEffect()
        {
            if (!IsAcrylicEnabled)
                return;

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource?.Handle == IntPtr.Zero)
                return;

            var handle = hwndSource!.Handle;

            var accentPolicy = new AccentPolicy
            {
                AccentState = AccentState.EnableAcrylicBlurBehind,
                AccentFlags = ShowAccentBorder ? AccentFlags.AllBorder : AccentFlags.None,
                GradientColor = CreateColorInteger(AcrylicGradientColor),
                AnimationId = 0,
            };

            var accentPolicyPtr = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>());
            try
            {
                Marshal.StructureToPtr(accentPolicy, accentPolicyPtr, false);

                var windowCompositionAttributeData = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WcaAccentPolicy,
                    DataPointer = accentPolicyPtr,
                    DataSize = (uint)Marshal.SizeOf<AccentPolicy>(),
                };

                hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;

                var margins = new Margins
                {
                    LeftWidth = 0,
                    RightWidth = 0,
                    TopHeight = 0,
                    BottomHeight = 0,
                };

                DwmExtendFrameIntoClientArea(handle, ref margins);
                SetWindowCompositionAttribute(handle, ref windowCompositionAttributeData);

                SetWindowPos(
                    handle,
                    IntPtr.Zero,
                    0,
                    0,
                    0,
                    0,
                    SetWindowPosFlags.DRAWFRAME
                        | SetWindowPosFlags.NOACTIVATE
                        | SetWindowPosFlags.NOMOVE
                        | SetWindowPosFlags.NOOWNERZORDER
                        | SetWindowPosFlags.NOSIZE
                        | SetWindowPosFlags.NOZORDER
                );
            }
            finally
            {
                Marshal.FreeHGlobal(accentPolicyPtr);
            }
        }

        private void ApplyWindowCorner()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource?.Handle == IntPtr.Zero)
                return;

            var handle = hwndSource!.Handle;

            // Determine corner style based on Windows version
            var windowsVersion = Utils.GetWindowsVersion();
            var cornerStyle =
                windowsVersion >= Utils.WindowsVersion.Windows11 ? WindowCorner.Round : WindowCorner.RoundSmall;

            var corner = (int)cornerStyle;

            DwmSetWindowAttribute(handle, DwmWindowAttribute.WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        }

        private static int CreateColorInteger(Color color)
        {
            return color.R << 0 | color.G << 8 | color.B << 16 | color.A << 24;
        }
    }
}
