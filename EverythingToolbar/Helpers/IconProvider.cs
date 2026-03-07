using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EverythingToolbar.Helpers
{
    public static class ThumbnailProvider
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory shellItem
        );

        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage([In] Size size, [In] int flags, [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size
        {
            public int cx;
            public int cy;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        private const int SiigbfResizetofit = 0x00;
        private const int SiigbfIcononly = 0x04;

        public static ImageSource? GetImage(string filePath, int size = 32)
        {
            return GetShellItemImage(filePath, size, SiigbfResizetofit);
        }

        public static ImageSource? GetIcon(string filePath, int size = 32)
        {
            return GetShellItemImage(filePath, size, SiigbfIcononly);
        }

        private static ImageSource? GetShellItemImage(string filePath, int size, int flags)
        {
            try
            {
                double dpi = GetDpiForSystem();
                if (dpi < 96) dpi = 96;
                int scaledSize = (int)Math.Ceiling(size * dpi / 96.0);

                Guid shellItemImageFactoryGuid = new("BCC18B79-BA16-442F-80C4-8A59C30C463B");
                SHCreateItemFromParsingName(
                    filePath,
                    IntPtr.Zero,
                    shellItemImageFactoryGuid,
                    out IShellItemImageFactory imageFactory
                );

                Size imageSize = new() { cx = scaledSize, cy = scaledSize };
                imageFactory.GetImage(imageSize, flags, out IntPtr hBitmap);

                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );
                    imageSource.Freeze();
                    return CorrectDpi(imageSource, dpi);
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource CorrectDpi(BitmapSource source, double dpi)
        {
            if (Math.Abs(source.DpiX - dpi) < 0.1)
                return source;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            var format = source.Format;
            int stride = (width * format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * height];
            source.CopyPixels(pixels, stride, 0);
            var result = BitmapSource.Create(width, height, dpi, dpi, format, source.Palette, pixels, stride);
            result.Freeze();
            return result;
        }
    }

    public static class IconProvider
    {
        private static readonly ConcurrentDictionary<string, ImageSource> IconIndexCache = new();
        private static readonly ConcurrentDictionary<string, ImageSource> ExtensionCache = new();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct Shfileinfo
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref Shfileinfo psfi,
            uint cbSizeFileInfo,
            uint uFlags
        );

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", PreserveSig = true)]
        private static extern int SHGetImageList(
            int iImageList,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IImageList ppv
        );

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(IntPtr pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, out IntPtr picon);
        }

        private const uint ShgfiIcon = 0x000000100;
        private const uint ShgfiLargeicon = 0x000000000;
        private const uint ShgfiSmallicon = 0x000000001;
        private const uint ShgfiSysiconindex = 0x000004000;
        private const uint ShgfiUsefileattributes = 0x000000010;
        private const uint FileAttributeNormal = 0x00000080;
        private const int IldTransparent = 0x00000001;
        private const int ShilSmall = 0;
        private const int ShilLarge = 1;
        private const int ShilExtralarge = 2;
        private const int ShilJumbo = 4;

        private static int _fallbackIconIndex = -1;

        public static ImageSource? GetImage(string path, Action<ImageSource>? onUpdated = null, int iconSize = 16)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                extension = Path.GetFileName(path).ToLowerInvariant();
            string sizeKey = GetSizeCacheKey(iconSize);
            string extensionKey = $"{extension}|{sizeKey}";

            if (!ExtensionCache.TryGetValue(extensionKey, out ImageSource? iconByExtension))
            {
                iconByExtension = GetIconByPath(path, iconSize);
                if (iconByExtension != null)
                {
                    ExtensionCache.TryAdd(extensionKey, iconByExtension);
                }
            }

            if (onUpdated == null)
                return iconByExtension;

            Task.Run(() =>
            {
                int iconIndex = GetIconIndex(path, false);
                if (iconIndex < 0)
                    iconIndex = GetFallbackIconIndex();
                string iconIndexKey = $"{iconIndex}|{sizeKey}";

                if (IconIndexCache.TryGetValue(iconIndexKey, out var cachedIcon))
                {
                    onUpdated.Invoke(cachedIcon);
                    return;
                }

                ImageSource? exactIcon = GetIconByPath(path, iconSize);
                if (exactIcon != null)
                {
                    IconIndexCache.TryAdd(iconIndexKey, exactIcon);
                    onUpdated.Invoke(exactIcon);
                }
            });

            return iconByExtension;
        }

        private static ImageSource? GetIconByPath(string path, int iconSize)
        {
            Shfileinfo shfi = new();
            uint sizeFlag = iconSize <= 16 ? ShgfiSmallicon : ShgfiLargeicon;
            uint flags = ShgfiIcon | sizeFlag;
            SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (shfi.hIcon == IntPtr.Zero)
                return null;

            try
            {
                var imageSource = Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(iconSize, iconSize));
                imageSource.Freeze();
                return imageSource;
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }

        public static ImageSource? GetExactImage(string path, int iconSize = 32)
        {
            int iconIndex = GetIconIndex(path, false);
            if (iconIndex < 0)
                iconIndex = GetFallbackIconIndex();

            var exactIcon = GetIconFromSystemImageList(iconIndex, iconSize);
            return exactIcon ?? GetIconByPath(path, iconSize);
        }

        private static ImageSource? GetIconFromSystemImageList(int iconIndex, int iconSize)
        {
            IImageList? imageList = null;
            try
            {
                int imageListType = GetImageListType(iconSize);
                Guid iImageListGuid = new("46EB5926-582E-4017-9FDF-E8998DAA0950");
                int hr = SHGetImageList(imageListType, iImageListGuid, out imageList);
                if (hr != 0)
                    return null;

                hr = imageList.GetIcon(iconIndex, IldTransparent, out IntPtr hIcon);
                if (hr != 0 || hIcon == IntPtr.Zero)
                    return null;

                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(iconSize, iconSize));
                    imageSource.Freeze();
                    return imageSource;
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (imageList != null && Marshal.IsComObject(imageList))
                    Marshal.ReleaseComObject(imageList);
            }
        }

        private static int GetImageListType(int iconSize)
        {
            if (iconSize <= 16)
                return ShilSmall;
            if (iconSize <= 32)
                return ShilLarge;
            if (iconSize <= 48)
                return ShilExtralarge;
            return ShilJumbo;
        }

        private static int GetFallbackIconIndex()
        {
            if (_fallbackIconIndex < 0)
                _fallbackIconIndex = GetIconIndex("", true);

            return _fallbackIconIndex;
        }

        private static string GetSizeCacheKey(int iconSize)
        {
            return iconSize.ToString();
        }

        private static int GetIconIndex(string path, bool useFilename)
        {
            Shfileinfo shfi = new();
            uint flags = ShgfiSysiconindex | ShgfiSmallicon;
            uint fileAttributes = 0;
            if (useFilename)
            {
                fileAttributes = FileAttributeNormal;
                flags |= ShgfiUsefileattributes;
            }
            IntPtr result = SHGetFileInfo(path, fileAttributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
            return result == IntPtr.Zero ? -1 : shfi.iIcon;
        }
    }
}
