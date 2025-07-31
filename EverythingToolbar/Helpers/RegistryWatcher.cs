using System;
using System.Management;
using System.Security.Principal;
using Microsoft.Win32;
using NLog;

namespace EverythingToolbar.Helpers
{
    public delegate void RegistryChange();
    public delegate void RegistryChangeValue(object? newValue);

    internal class RegistryEntry(string hive, string keyPath, string valueName)
    {
        public string Hive = hive;
        public string KeyPath = keyPath;
        public string ValueName = valueName;

        public object? GetValue(object? defaultValue = null)
        {
            return Registry.GetValue(Hive + @"\" + KeyPath, ValueName, defaultValue);
        }
    }

    internal class RegistryWatcher
    {
        private readonly ManagementEventWatcher _watcher;
        private readonly RegistryEntry _target;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<RegistryWatcher>();

        public event RegistryChange? OnChange;
        public event RegistryChangeValue? OnChangeValue;

        public RegistryWatcher(RegistryEntry target)
        {
            _target = target;

            _watcher = CreateWatcher();
            _watcher.EventArrived += OnEventArrived;

            Start();
        }

        ~RegistryWatcher()
        {
            _watcher.Dispose();
        }

        private void Start()
        {
            try
            {
                _watcher.Start();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to initialize RegistryWatcher for target {_target}.");
            }
        }

        public void Stop()
        {
            _watcher.Stop();
        }

        private static string EscapeBackticks(string unescaped)
        {
            return unescaped.Replace(@"\", @"\\");
        }

        private ManagementEventWatcher CreateWatcher()
        {
            // Cannot watch HKEY_CURRENT_USER as it is synthetic.
            if (_target.Hive == "HKEY_CURRENT_USER")
            {
                _target.Hive = "HKEY_USERS";
                _target.KeyPath = WindowsIdentity.GetCurrent().User?.Value + @"\" + _target.KeyPath;
            }

            var qu =
                "SELECT * FROM RegistryValueChangeEvent WHERE "
                + $"Hive='{_target.Hive}' "
                + $"AND KeyPath='{EscapeBackticks(_target.KeyPath)}' "
                + $"AND ValueName='{_target.ValueName}'";

            var query = new WqlEventQuery(qu);
            return new ManagementEventWatcher(query);
        }

        private object? GetValue(object? defaultValue = null)
        {
            return _target.GetValue(defaultValue);
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            OnChange?.Invoke();

            // Only read value if required
            if (OnChangeValue?.GetInvocationList().Length > 0)
            {
                OnChangeValue.Invoke(GetValue());
            }
        }
    }
}
