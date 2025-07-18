﻿using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace EverythingToolbar.Helpers
{
    public static class ToolbarLogger
    {
        private static readonly string DebugFlagFileName = Path.Combine(Utils.GetConfigDirectory(), "debug.txt");
        private static readonly LogFactory LogFactory = new LogFactory();

        public static ILogger GetLogger(string name)
        {
            return LogFactory.GetLogger(name);
        }

        public static ILogger GetLogger<T>()
        {
            return LogFactory.GetLogger(typeof(T).FullName);
        }

        private static LogLevel GetLogLevel()
        {
            return File.Exists(DebugFlagFileName) ? LogLevel.Debug : LogLevel.Info;
        }

        private static void LogVersionInformation(ILogger logger)
        {
            logger.Debug("Debug logging enabled.");
            logger.Info($"EverythingToolbar {Assembly.GetExecutingAssembly().GetName().Version} started. OS: {Environment.OSVersion}");

            if (ToolbarSettings.User.OsBuildNumberOverride != 0)
                logger.Info($"OS build number override: {ToolbarSettings.User.OsBuildNumberOverride}");
        }

        private static void InitializeExceptionLoggers(ILogger logger)
        {
            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                logger.Debug(e.Exception, "Unhandled first chance exception");
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                logger.Error((Exception)args.ExceptionObject, "Unhandled exception");
            };

            if (Application.Current != null)
            {
                // Not applicable for deskband
                Application.Current.DispatcherUnhandledException += (_, args) =>
                {
                    logger.Error(args.Exception, "Unhandled exception on UI thread");
                };
            }
        }

        private static void ConfigureLogger()
        {
            var logfile = new FileTarget("logfile")
            {
                FileName = Path.Combine(Path.GetTempPath(), "EverythingToolbar.log"),
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                MaxArchiveFiles = 3,
                KeepFileOpen = true,
                OpenFileCacheTimeout = 30,
                ConcurrentWrites = true,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
            };
            var fileRule = new LoggingRule("*", GetLogLevel(), logfile);
            var config = new LoggingConfiguration();
            config.LoggingRules.Add(fileRule);
            LogFactory.Configuration = config;
        }

        public static void Initialize(string name)
        {
            ConfigureLogger();

            var logger = GetLogger(name);
            LogVersionInformation(logger);
            InitializeExceptionLoggers(logger);
        }
    }
}