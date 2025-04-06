﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Search
{
    public sealed class SearchState : INotifyPropertyChanged
    {
        public static readonly SearchState Instance = new SearchState();

        private string _searchTerm = "";
        public string SearchTerm
        {
            get
            {
                var searchTermWithReplacedMacros = _searchTerm;
                foreach (var f in FilterLoader.Instance.DefaultUserFilters)
                {
                    searchTermWithReplacedMacros = searchTermWithReplacedMacros.Replace(f.Macro + ":", f.Search + " ");
                }
                return searchTermWithReplacedMacros;
            }
            set
            {
                if (_searchTerm != value)
                {
                    _searchTerm = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _sortBy;
        public int SortBy
        {
            get => _sortBy;
            private set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isSortDescending;
        public bool IsSortDescending
        {
            get => _isSortDescending;
            private set
            {
                if (_isSortDescending != value)
                {
                    _isSortDescending = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatchCase;
        public bool IsMatchCase
        {
            get => _isMatchCase;
            private set
            {
                if (_isMatchCase != value)
                {
                    _isMatchCase = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatchPath;
        public bool IsMatchPath
        {
            get => _isMatchPath;
            private set
            {
                if (_isMatchPath != value)
                {
                    _isMatchPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatchWholeWord;
        public bool IsMatchWholeWord
        {
            get => _isMatchWholeWord;
            private set
            {
                if (_isMatchWholeWord != value)
                {
                    _isMatchWholeWord = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isRegExEnabled;
        public bool IsRegExEnabled
        {
            get => _isRegExEnabled;
            private set
            {
                if (_isRegExEnabled != value)
                {
                    _isRegExEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private Filter _currentFilter = FilterLoader.Instance.GetLastFilter();
        public Filter Filter
        {
            get => _currentFilter;
            set
            {
                if (!_currentFilter.Equals(value))
                {
                    _currentFilter = value;
                    ToolbarSettings.User.LastFilter = value.Name;
                    OnPropertyChanged();
                }
            }
        }

        private SearchState()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
        }

        public void Reset()
        {
            if (ToolbarSettings.User.IsEnableHistory)
                HistoryManager.Instance.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            if (!ToolbarSettings.User.IsRememberFilter && Filter.Equals(FilterLoader.Instance.DefaultFilters[0]))
            {
                Filter = FilterLoader.Instance.DefaultFilters[0];
            }
        }

        public void CycleFilters(int offset = 1)
        {
            var defaultSize = FilterLoader.Instance.DefaultFilters.Count;
            var userSize = FilterLoader.Instance.UserFilters.Count;
            var defaultIndex = FilterLoader.Instance.DefaultFilters.IndexOf(Filter);
            var userIndex = FilterLoader.Instance.UserFilters.IndexOf(Filter);

            var d = defaultIndex >= 0 ? defaultIndex : defaultSize;
            var u = userIndex >= 0 ? userIndex : 0;
            var i = (d + u + offset + defaultSize + userSize) % (defaultSize + userSize);

            if (i < defaultSize)
                Filter = FilterLoader.Instance.DefaultFilters[i];
            else
                Filter = FilterLoader.Instance.UserFilters[i - defaultSize];
        }

        public void SelectFilterFromIndex(int index)
        {
            var defaultCount = FilterLoader.Instance.DefaultFilters.Count;
            var userCount = FilterLoader.Instance.UserFilters.Count;

            if (index < defaultCount)
                Filter = FilterLoader.Instance.DefaultFilters[index];
            else if (index - defaultCount < userCount)
                Filter = FilterLoader.Instance.UserFilters[index - defaultCount];
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            // TODO: Instead of reacting to settings changes, the search state should manage the state and save it to settings
            switch (e.PropertyName)
            {
                case nameof(ToolbarSettings.User.SortBy):
                    SortBy = ToolbarSettings.User.SortBy;
                    break;
                case nameof(ToolbarSettings.User.IsSortDescending):
                    IsSortDescending = ToolbarSettings.User.IsSortDescending;
                    break;
                case nameof(ToolbarSettings.User.IsMatchCase):
                    IsMatchCase = ToolbarSettings.User.IsMatchCase;
                    break;
                case nameof(ToolbarSettings.User.IsMatchPath):
                    IsMatchPath = ToolbarSettings.User.IsMatchPath;
                    break;
                case nameof(ToolbarSettings.User.IsMatchWholeWord):
                    IsMatchWholeWord = ToolbarSettings.User.IsMatchWholeWord;
                    break;
                case nameof(ToolbarSettings.User.IsRegExEnabled):
                    IsRegExEnabled = ToolbarSettings.User.IsRegExEnabled;
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}