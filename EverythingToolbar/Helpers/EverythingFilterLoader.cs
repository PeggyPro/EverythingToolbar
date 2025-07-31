using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using EverythingToolbar.Controls;
using EverythingToolbar.Data;
using EverythingToolbar.Properties;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace EverythingToolbar.Helpers
{
    internal class EverythingFilterLoader : INotifyPropertyChanged
    {
        private ObservableCollection<Filter>? _filters;
        public ObservableCollection<Filter>? Filters => _filters ??= LoadFilters();

        public static readonly EverythingFilterLoader Instance = new();
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<EverythingFilterLoader>();
        private FileSystemWatcher? _watcher;

        private EverythingFilterLoader()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;

            if (ToolbarSettings.User.IsImportFilters)
                CreateFileWatcher();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.IsImportFilters))
            {
                if (ToolbarSettings.User.IsImportFilters)
                {
                    CreateFileWatcher();
                }
                else
                {
                    StopFileWatcher();
                }
                ResetFilters();
            }
            else if (e.PropertyName == nameof(ToolbarSettings.User.FiltersPath))
            {
                if (ToolbarSettings.User.IsImportFilters)
                {
                    CreateFileWatcher();
                    ResetFilters();
                }
            }
        }

        private void ResetFilters()
        {
            _filters = null;
            NotifyPropertyChanged(nameof(Filters));
        }

        private ObservableCollection<Filter>? LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (!File.Exists(ToolbarSettings.User.FiltersPath))
            {
                Logger.Info("Filters.csv could not be found at " + ToolbarSettings.User.FiltersPath);

                FluentMessageBox
                    .CreateRegular(Resources.MessageBoxSelectFiltersCsv, Resources.MessageBoxSelectFiltersCsvTitle)
                    .ShowDialogAsync();
                using var openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = Path.Combine(ToolbarSettings.User.FiltersPath, "..");
                openFileDialog.Filter = "Filters.csv|Filters.csv|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ToolbarSettings.User.FiltersPath = openFileDialog.FileName;
                }
                else
                {
                    ToolbarSettings.User.IsImportFilters = false;
                    return null;
                }
            }

            try
            {
                using var csvParser = new TextFieldParser(ToolbarSettings.User.FiltersPath);
                csvParser.CommentTokens = ["#"];
                csvParser.SetDelimiters(",");
                csvParser.HasFieldsEnclosedInQuotes = true;

                var header = csvParser.ReadFields();

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();

                    if (header == null || fields == null)
                        continue;

                    var filterDict = header.Zip(fields, (h, f) => new { h, f }).ToDictionary(x => x.h, x => x.f);
                    var filter = ParseFilterFromDict(filterDict);

                    // Everything's default filters are uppercase and can be localized
                    filter.Name = filter
                        .Name.Replace("EVERYTHING", Resources.DefaultFilterAll)
                        .Replace("FOLDER", Resources.DefaultFilterFolder)
                        .Replace("FILE", Resources.DefaultFilterFile)
                        .Replace("AUDIO", Resources.UserFilterAudio)
                        .Replace("COMPRESSED", Resources.UserFilterCompressed)
                        .Replace("DOCUMENT", Resources.UserFilterDocument)
                        .Replace("EXECUTABLE", Resources.UserFilterExecutable)
                        .Replace("PICTURE", Resources.UserFilterPicture)
                        .Replace("VIDEO", Resources.UserFilterVideo);
                    filters.Add(filter);
                }

                return filters;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Parsing Filters.csv failed.");
            }

            return null;
        }

        private Filter ParseFilterFromDict(Dictionary<string, string> dict)
        {
            return new Filter
            {
                Name = dict["Name"],
                IsMatchCase = dict["Case"] == "1",
                IsMatchWholeWord = dict["Whole Word"] == "1",
                IsMatchPath = dict["Path"] == "1",
                IsRegExEnabled = dict["Regex"] == "1",
                Search = dict["Search"],
                Macro = dict["Macro"],
            };
        }

        private void StopFileWatcher()
        {
            if (_watcher == null)
                return;

            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        private void CreateFileWatcher()
        {
            StopFileWatcher();

            if (!File.Exists(ToolbarSettings.User.FiltersPath))
                return;

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ToolbarSettings.User.FiltersPath)!,
                Filter = Path.GetFileName(ToolbarSettings.User.FiltersPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            ToolbarSettings.User.FiltersPath = e.FullPath;
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            ResetFilters();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
