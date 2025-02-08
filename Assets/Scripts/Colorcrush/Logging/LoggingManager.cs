// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// ReSharper disable StringLiteralTypo

#endregion

namespace Colorcrush.Logging
{
    public class LoggingManager : MonoBehaviour
    {
        public delegate void LogEventQueuedHandler(ILogEvent logEvent);

        private static LoggingManager _instance;

        // Define the severity scale
        private static readonly Dictionary<LogType, int> LogSeverity = new()
        {
            { LogType.Log, 0 },
            { LogType.Warning, 1 },
            { LogType.Assert, 2 },
            { LogType.Error, 3 },
            { LogType.Exception, 4 },
        };

        private readonly Queue<(long timestamp, ILogEvent logEvent)> _eventQueue = new();
        private string _currentLogFilePath;
        private long _lastTimestamp;
        private StreamWriter _logWriter;
        private DateTime _startTime;

        private static LoggingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LoggingManager");
                    _instance = go.AddComponent<LoggingManager>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            if (ProjectConfig.InstanceConfig.deleteAllLogFilesOnEditorStartup)
            {
                DeleteAllLogFiles();
            }
#endif

            if (ProjectConfig.InstanceConfig.alwaysCreateNewLogFileOnStartup)
            {
                InitializeNewLogFile();
            }
            else
            {
                InitializeMostRecentLogFile();
            }

            StartCoroutine(SaveLogRoutine());

            Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
            _logWriter?.Dispose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                LogEvent(new AppStandbyEvent());
            }
            else
            {
                LogEvent(new AppOpenedEvent());
            }
        }

        private void OnApplicationQuit()
        {
            LogEvent(new AppClosedEvent());
            SaveLog(); // Ensure all remaining logs are saved before quitting
            _logWriter?.Dispose();
        }

        public static event LogEventQueuedHandler OnLogEventQueued;

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
#if UNITY_EDITOR
            if (ProjectConfig.InstanceConfig.suppressConsoleLoggingInEditor)
            {
                return;
            }
#endif

            if (LogSeverity[type] >= LogSeverity[ProjectConfig.InstanceConfig.minimumLogSeverity])
            {
                var sanitizedStackTrace = stackTrace?.Replace("\n", " ").Replace("\r", "");
                LogEvent(new ConsoleOutputEvent(type, $"{logString} | StackTrace: {sanitizedStackTrace}"));
            }
        }

        private void InitializeMostRecentLogFile()
        {
            var logFiles = Directory.GetFiles(Application.persistentDataPath, $"{ProjectConfig.InstanceConfig.logFilePrefix}*{ProjectConfig.InstanceConfig.logFileExtension}");
            if (logFiles.Length > 0)
            {
                _currentLogFilePath = logFiles.OrderByDescending(f => new FileInfo(f).CreationTime).First();
                _startTime = DateTime.Now;
                if (ProjectConfig.InstanceConfig.useAdditiveTimestamps)
                {
                    _lastTimestamp = GetLastTimestampFromFile(_currentLogFilePath);
                }

                _logWriter = new StreamWriter(_currentLogFilePath, true, ProjectConfig.InstanceConfig.logFileEncoding, ProjectConfig.InstanceConfig.logFileBufferSize);
                LogEvent(new StartTimeEvent(_startTime));
                Debug.Log($"LoggingManager: Set most recent log file: {_currentLogFilePath}, Start time: {_startTime}");
            }
            else
            {
                InitializeNewLogFile();
                Debug.LogWarning($"LoggingManager: No existing log files found. Initialized new log file: {_currentLogFilePath}");
            }
        }

        private long GetLastTimestampFromFile(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath, ProjectConfig.InstanceConfig.logFileEncoding, false, ProjectConfig.InstanceConfig.logFileBufferSize);
                string lastLine = null;
                while (reader.ReadLine() is { } currentLine)
                {
                    lastLine = currentLine;
                }

                if (lastLine != null)
                {
                    var parts = lastLine.Split(',');
                    if (parts.Length > 0 && long.TryParse(parts[0], out var timestamp))
                    {
                        return timestamp;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"LoggingManager: Error reading last timestamp from file: {e.Message}");
            }

            return 0;
        }

        private void InitializeNewLogFile()
        {
            _startTime = DateTime.Now;
            var timestamp = _startTime.ToString("yyyyMMdd_HHmmss");
            _currentLogFilePath = Path.Combine(Application.persistentDataPath, $"{ProjectConfig.InstanceConfig.logFilePrefix}{timestamp}{ProjectConfig.InstanceConfig.logFileExtension}");
            _lastTimestamp = 0;
            _logWriter = new StreamWriter(_currentLogFilePath, false, ProjectConfig.InstanceConfig.logFileEncoding, ProjectConfig.InstanceConfig.logFileBufferSize);
            LogEvent(new StartTimeEvent(_startTime));
        }

        public static void LogEvent(ILogEvent logEvent)
        {
            Instance.LogEventInternal(logEvent);
        }

        private void LogEventInternal(ILogEvent logEvent)
        {
            long timestamp;
            if (ProjectConfig.InstanceConfig.useAdditiveTimestamps)
            {
                timestamp = _lastTimestamp + (long)(DateTime.Now - _startTime).TotalMilliseconds;
                _lastTimestamp = timestamp;
            }
            else
            {
                timestamp = (long)(DateTime.Now - _startTime).TotalMilliseconds;
            }

            _eventQueue.Enqueue((timestamp, logEvent));
            OnLogEventQueued?.Invoke(logEvent);
        }

        private IEnumerator SaveLogRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(ProjectConfig.InstanceConfig.logSaveInterval);
                SaveLog();
            }
        }

        private void SaveLog()
        {
            if (_eventQueue.Count == 0)
            {
                return;
            }

            try
            {
                while (_eventQueue.Count > 0)
                {
                    var (timestamp, logEvent) = _eventQueue.Dequeue();
                    var stringifiedData = logEvent.GetStringifiedData();
                    var logEntry = string.IsNullOrEmpty(stringifiedData)
                        ? $"{timestamp},{logEvent.EventName}"
                        : $"{timestamp},{logEvent.EventName},{stringifiedData}";

                    _logWriter.WriteLine(logEntry);
                }

                _logWriter.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"LoggingManager: Error saving log: {e.Message}");
            }
        }

        public static void DeleteAllLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(Application.persistentDataPath, $"{ProjectConfig.InstanceConfig.logFilePrefix}*{ProjectConfig.InstanceConfig.logFileExtension}");
                foreach (var file in logFiles)
                {
                    File.Delete(file);
                }

                Debug.Log($"LoggingManager: Deleted {logFiles.Length} log file(s).");
            }
            catch (Exception e)
            {
                Debug.LogError($"LoggingManager: Error deleting all log files: {e.Message}");
            }
        }

        public static void StartNewLogFile()
        {
            LogEvent(new ResetEvent());
            Instance.SaveLog(); // Save any remaining logs in the current file
            Instance._logWriter?.Dispose();
            Instance.InitializeNewLogFile();
        }

        public static List<string> GetLogDataLines()
        {
            var logLines = new List<string>();

            try
            {
                if (File.Exists(Instance._currentLogFilePath))
                {
                    using var fileStream = new FileStream(Instance._currentLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fileStream, ProjectConfig.InstanceConfig.logFileEncoding, false, ProjectConfig.InstanceConfig.logFileBufferSize);
                    while (reader.ReadLine() is { } line)
                    {
                        logLines.Add(line);
                    }
                }

                logLines.AddRange(Instance._eventQueue.Select(q => $"{q.timestamp},{q.logEvent.EventName},{q.logEvent.GetStringifiedData()}"));
            }
            catch (Exception e)
            {
                Debug.LogError($"LoggingManager: Error reading log file: {e.Message}");
            }

            return logLines;
        }
    }
}