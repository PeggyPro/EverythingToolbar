using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EverythingToolbar.Data;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Helpers
{
    internal class DefaultFilterLoader : INotifyPropertyChanged
    {
        public static readonly Filter AllFilter = new()
        {
            Name = Resources.DefaultFilterAll,
            Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE71D" : "",
        };

        public readonly ObservableCollection<Filter> DefaultFilters =
        [
            AllFilter,
            new()
            {
                Name = Resources.DefaultFilterFile,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE7C3" : "",
                Search = "file:",
            },
            new()
            {
                Name = Resources.DefaultFilterFolder,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE8B7" : "",
                Search = "folder:",
            },
            new()
            {
                Name = Resources.UserFilterAudio,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE8D6" : "",
                Macro = "audio",
                Search =
                    "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;"
                    + "midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm",
            },
            new()
            {
                Name = Resources.UserFilterCompressed,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE7B8" : "",
                Macro = "zip",
                Search =
                    "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;"
                    + "r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;"
                    + "r25;r26;r27;r28;r29;rar;tar;tgz;z;zip",
            },
            new()
            {
                Name = Resources.UserFilterDocument,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xF585" : "",
                Macro = "doc",
                Search =
                    "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;"
                    + "ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;"
                    + "pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml",
            },
            new()
            {
                Name = Resources.UserFilterExecutable,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xECAA" : "",
                Macro = "exe",
                Search = "ext:bat;cmd;exe;msi;msp;scr",
            },
            new()
            {
                Name = Resources.UserFilterPicture,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE8B9" : "",
                Macro = "pic",
                Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf",
            },
            new()
            {
                Name = Resources.UserFilterVideo,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE714" : "",
                Macro = "video",
                Search =
                    "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;"
                    + "dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;"
                    + "m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;"
                    + "ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;"
                    + "vob;vp6;webm;wm;wmp;wmv",
            },
        ];

        public ObservableCollection<Filter> Filters => GetReorderedFilters();

        public static readonly DefaultFilterLoader Instance = new();

        private DefaultFilterLoader()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.FilterOrder))
            {
                NotifyPropertyChanged(nameof(Filters));
            }
        }

        private ObservableCollection<Filter> GetReorderedFilters()
        {
            var reorderedIndices = GetValidFilterOrder();
            var reordered = reorderedIndices.Select(i => DefaultFilters[i]).ToList();
            return new ObservableCollection<Filter>(reordered);
        }

        public int[] GetValidFilterOrder()
        {
            var order = ToolbarSettings.User.FilterOrder;
            var defaultOrder = Enumerable.Range(0, DefaultFilters.Count);
            if (string.IsNullOrWhiteSpace(order))
                return defaultOrder.ToArray();

            var indices = order.Split(',').Select(s => int.TryParse(s, out var idx) ? idx : -1).ToArray();

            if (!indices.OrderBy(i => i).SequenceEqual(Enumerable.Range(0, DefaultFilters.Count)))
            {
                ToolbarSettings.User.FilterOrder = string.Empty;
                return defaultOrder.ToArray();
            }

            return indices;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
