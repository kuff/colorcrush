// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colorcrush.Util;
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
        private DateTime _startTime;

        public static LoggingManager Instance
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

#if DEBUG
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
        }

        public static event LogEventQueuedHandler OnLogEventQueued;

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
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
                LogEvent(new StartTimeEvent(_startTime));
                Debug.Log($"Set most recent log file: {_currentLogFilePath}, Start time: {_startTime}");
            }
            else
            {
                InitializeNewLogFile();
                Debug.LogWarning($"No existing log files found. Initialized new log file: {_currentLogFilePath}");
            }
        }

        private void InitializeNewLogFile()
        {
            _startTime = DateTime.Now;
            var timestamp = _startTime.ToString("yyyyMMdd_HHmmss");
            _currentLogFilePath = Path.Combine(Application.persistentDataPath, $"{ProjectConfig.InstanceConfig.logFilePrefix}{timestamp}{ProjectConfig.InstanceConfig.logFileExtension}");
            LogEvent(new StartTimeEvent(_startTime));
        }

        public static void LogEvent(ILogEvent logEvent)
        {
            Instance.LogEventInternal(logEvent);
        }

        private void LogEventInternal(ILogEvent logEvent)
        {
            var timestamp = (long)(DateTime.Now - _startTime).TotalMilliseconds;
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
                var existingLines = File.Exists(_currentLogFilePath) ? File.ReadAllLines(_currentLogFilePath) : Array.Empty<string>();
                var updatedLines = new List<string>(existingLines);

                // Check if the file appears corrupted
                if (existingLines.Length > 0 && existingLines[^1] != ProjectConfig.InstanceConfig.endOfFileSymbol)
                {
                    Debug.LogWarning($"Log file appears corrupted: {_currentLogFilePath}. Last line is not EOF symbol.");
                }

                // Remove the EOF symbol if it exists
                if (updatedLines.Count > 0 && updatedLines[^1] == ProjectConfig.InstanceConfig.endOfFileSymbol)
                {
                    updatedLines.RemoveAt(updatedLines.Count - 1);
                }

                using var writer = new StreamWriter(_currentLogFilePath, false);
                // Write existing lines (without EOF)
                foreach (var line in updatedLines)
                {
                    writer.WriteLine(line);
                }

                // Write new log entries
                while (_eventQueue.Count > 0)
                {
                    var (timestamp, logEvent) = _eventQueue.Dequeue();
                    var stringifiedData = logEvent.GetStringifiedData();
                    var logEntry = string.IsNullOrEmpty(stringifiedData)
                        ? $"{timestamp},{logEvent.EventName}"
                        : $"{timestamp},{logEvent.EventName},{stringifiedData}";

                    writer.WriteLine(logEntry);
                }

                // Write the EOF symbol at the end
                writer.WriteLine(ProjectConfig.InstanceConfig.endOfFileSymbol);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving log: {e.Message}");
            }
        }

        public void DeleteLogFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Log file deleted: {filePath}");
                }
                else
                {
                    Debug.LogWarning($"Log file not found: {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting log file: {e.Message}");
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

                Debug.Log($"Deleted {logFiles.Length} log file(s).");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting all log files: {e.Message}");
            }
        }

        public static void StartNewLogFile()
        {
            LogEvent(new ResetEvent());
            Instance.SaveLog(); // Save any remaining logs in the current file
            Instance.InitializeNewLogFile();
        }

        public static List<string> GetLogDataLines()
        {
            var logLines = new List<string>();

            try
            {
                var currentLogFilePath = Instance._currentLogFilePath;
                if (File.Exists(currentLogFilePath))
                {
                    logLines = File.ReadAllLines(currentLogFilePath).ToList();
                    if (logLines.Count > 0 && logLines[logLines.Count - 1] == ProjectConfig.InstanceConfig.endOfFileSymbol)
                    {
                        logLines.RemoveAt(logLines.Count - 1);
                    }
                    else
                    {
                        Debug.LogWarning($"End of file symbol '{ProjectConfig.InstanceConfig.endOfFileSymbol}' not found at the end of the log file '{Instance._currentLogFilePath}'. This may indicate an incomplete or corrupted log file.");
                    }

                    logLines.AddRange(Instance._eventQueue.Select(q => $"{q.timestamp},{q.logEvent.EventName},{q.logEvent.GetStringifiedData()}"));
                }
                else
                {
                    logLines = Instance._eventQueue.Select(q => $"{q.timestamp},{q.logEvent.EventName},{q.logEvent.GetStringifiedData()}").ToList();
                    Debug.Log($"Yielding {logLines.Count} log entries from the queue since data is yet to be written to the file.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading log file: {e.Message}");
            }

            return logLines;
        }
    }
}