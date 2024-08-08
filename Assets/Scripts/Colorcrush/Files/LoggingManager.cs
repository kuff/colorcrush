// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Colorcrush.Util;
using UnityEngine;

#endregion

namespace Colorcrush.Files
{
    public class LoggingManager : MonoBehaviour
    {
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
        private bool _isFirstLog = true;
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

            InitializeNewLogFile();
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

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (LogSeverity[type] >= LogSeverity[ProjectConfig.InstanceConfig.minimumLogSeverity])
            {
                LogEvent(new ConsoleOutputEvent(type, logString));
            }
        }

        private void InitializeNewLogFile()
        {
            _startTime = DateTime.Now;
            var timestamp = _startTime.ToString("yyyyMMdd_HHmmss");
            _currentLogFilePath = Path.Combine(Application.persistentDataPath, $"{ProjectConfig.InstanceConfig.logFilePrefix}{timestamp}{ProjectConfig.InstanceConfig.logFileExtension}");
            _isFirstLog = true;
        }

        public static void LogEvent(ILogEvent logEvent)
        {
            Instance.LogEventInternal(logEvent);
        }

        private void LogEventInternal(ILogEvent logEvent)
        {
            var timestamp = (long)(DateTime.Now - _startTime).TotalMilliseconds;
            _eventQueue.Enqueue((timestamp, logEvent));
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

                    if (_isFirstLog && updatedLines.Count == 0)
                    {
                        writer.WriteLine($"0,starttime,{_startTime:yyyy-MM-dd HH:mm:ss}");
                        _isFirstLog = false;
                    }

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

                Debug.Log($"Deleted {logFiles.Length} log files.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting all log files: {e.Message}");
            }
        }

        public void StartNewLogFile()
        {
            SaveLog(); // Save any remaining logs in the current file
            InitializeNewLogFile();
        }
    }
}