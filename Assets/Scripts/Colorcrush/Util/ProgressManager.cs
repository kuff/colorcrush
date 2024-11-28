// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Colorcrush.Game;
using Colorcrush.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

#endregion

namespace Colorcrush.Util
{
    public class ProgressManager : MonoBehaviour
    {
        private static ProgressManager _instance;
        private static readonly Queue<ILogEvent> LogEventQueue = new();
        private static readonly List<string> _completedTargetColors = new();
        private static readonly List<string> _rewardedEmojis = new();
        private static readonly List<List<string>> _selectedColors = new();
        private static readonly List<ColorManager.ColorMatrixResult> _finalColors = new();
        private static string _mostRecentCompletedTargetColor;
        private static bool _currentLevelCompleted = true;
        private static string _currentTargetColor;
        private static readonly Dictionary<int, string> _generatedColors = new();
        private static readonly List<string> _currentLevelSelectedColors = new();

        public static List<string> CompletedTargetColors
        {
            get
            {
                EnsureInstance();
                ProcessLogEventQueue();
                return _completedTargetColors;
            }
        }

        public static List<string> RewardedEmojis
        {
            get
            {
                EnsureInstance();
                ProcessLogEventQueue();
                return _rewardedEmojis;
            }
        }

        public static List<List<string>> SelectedColors
        {
            get
            {
                EnsureInstance();
                ProcessLogEventQueue();
                return _selectedColors;
            }
        }

        public static List<ColorManager.ColorMatrixResult> FinalColors
        {
            get
            {
                EnsureInstance();
                ProcessLogEventQueue();
                return _finalColors;
            }
        }

        public static string MostRecentCompletedTargetColor
        {
            get
            {
                EnsureInstance();
                ProcessLogEventQueue();
                return _mostRecentCompletedTargetColor;
            }
            private set => _mostRecentCompletedTargetColor = value;
        }

        public static ProgressManager Instance
        {
            get
            {
                EnsureInstance();
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
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            LoggingManager.OnLogEventQueued += OnLogEventQueued;

            // Initial refresh using existing log data
            RefreshProgressionState();
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
            LoggingManager.OnLogEventQueued -= OnLogEventQueued;
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject("ProgressManager");
                _instance = go.AddComponent<ProgressManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            ProcessLogEventQueue();
        }

        private void OnLogEventQueued(ILogEvent logEvent)
        {
            LogEventQueue.Enqueue(logEvent);
        }

        public static void RefreshProgressionState()
        {
            EnsureInstance();
            _completedTargetColors.Clear();
            _rewardedEmojis.Clear();
            _selectedColors.Clear();
            _finalColors.Clear();
            _currentLevelCompleted = true;
            _currentTargetColor = null;
            _generatedColors.Clear();
            _currentLevelSelectedColors.Clear();

            // Process all existing log data
            var allLogLines = LoggingManager.GetLogDataLines();
            ProcessLogLines(allLogLines);

            // Log a summary of the refreshed progression state
            Debug.Log("ProgressManager: Progression State Refreshed: " +
                      $"{_completedTargetColors.Count} completed target colors, " +
                      $"{_rewardedEmojis.Count} rewarded emojis, " +
                      $"{_selectedColors.Count} selected color sets. " +
                      $"Most recent completed target color: {_mostRecentCompletedTargetColor}");
        }

        public static void ResetProgressionState()
        {
            EnsureInstance();
            LoggingManager.StartNewLogFile();
            RefreshProgressionState();
        }

        public static void ResetAllProgress()
        {
            EnsureInstance();
            LoggingManager.StartNewLogFile();
            _completedTargetColors.Clear();
            _rewardedEmojis.Clear();
            _selectedColors.Clear();
            _finalColors.Clear();
            _currentLevelCompleted = true;
            _currentTargetColor = null;
            _generatedColors.Clear();
            _currentLevelSelectedColors.Clear();
            _mostRecentCompletedTargetColor = null;
            Debug.Log("ProgressManager: All progress has been reset");
        }

        private static void ProcessLogLines(List<string> logLines)
        {
            foreach (var line in logLines)
            {
                var parts = line.Split(',');
                if (parts.Length < 2)
                {
                    continue;
                }

                var eventName = parts[1];
                var eventData = parts.Length > 2 ? parts[2] : string.Empty;

                ProcessEvent(eventName, eventData);
            }
        }

        private static void ProcessLogEventQueue()
        {
            while (LogEventQueue.Count > 0)
            {
                var logEvent = LogEventQueue.Dequeue();
                ProcessEvent(logEvent.EventName, logEvent.GetStringifiedData());
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static void ProcessEvent(string eventName, string eventData)
        {
            switch (eventName)
            {
                case "gamelevelbegun":
                    if (_currentLevelCompleted)
                    {
                        _currentTargetColor = eventData;
                        _currentLevelCompleted = false;
                        _currentLevelSelectedColors.Clear();
                        _generatedColors.Clear();
                    }

                    break;
                case "gamelevelend":
                    if (!_currentLevelCompleted && _currentTargetColor != null)
                    {
                        _completedTargetColors.Add(_currentTargetColor);
                        _mostRecentCompletedTargetColor = _currentTargetColor;
                        _currentLevelCompleted = true;
                        _currentTargetColor = null;
                        if (_currentLevelSelectedColors.Count > 0)
                        {
                            _selectedColors.Add(new List<string>(_currentLevelSelectedColors));
                        }
                    }

                    break;
                case "emojirewarded":
                    _rewardedEmojis.Add(eventData);
                    break;
                case "colorssubmitted":
                    if (!_currentLevelCompleted)
                    {
                        _selectedColors.Add(new List<string>(_currentLevelSelectedColors));
                        _currentLevelSelectedColors.Clear();
                    }

                    break;
                case "colorsgenerated":
                    var parts = eventData.Split(' ');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out var buttonIndex);
                        _generatedColors[buttonIndex] = parts[1];
                    }

                    break;
                case "colorselected":
                    if (!_currentLevelCompleted)
                    {
                        int.TryParse(eventData, out var selectedIndex);
                        if (_generatedColors.TryGetValue(selectedIndex, out var selectedColor))
                        {
                            _currentLevelSelectedColors.Add(selectedColor);
                        }
                    }

                    break;
                case "colordeselected":
                    if (!_currentLevelCompleted)
                    {
                        int.TryParse(eventData, out var deselectedIndex);
                        if (_generatedColors.TryGetValue(deselectedIndex, out var deselectedColor))
                        {
                            _currentLevelSelectedColors.Remove(deselectedColor);
                        }
                    }

                    break;
                case "finalcolors":
                    if (!_currentLevelCompleted)
                    {
                        var chunks = eventData.Split(' ');

                        // Parse the first 8 chunks as hex colors
                        var colors = new List<ColorManager.ColorObject>();
                        for (var i = 0; i < 8; i++)
                        {
                            ColorUtility.TryParseHtmlString("#" + chunks[i], out var color);
                            colors.Add(new ColorManager.ColorObject(color));
                        }

                        // Parse the remaining chunks as Vector3 encodings
                        var encodings = new List<Vector3>();
                        for (var i = 0; i < 8; i++)
                        {
                            // Combine the three parts of each Vector3
                            var vectorStr = chunks[8 + i * 3] + chunks[9 + i * 3] + chunks[10 + i * 3];
                            // Remove parentheses and split by semicolon
                            var components = vectorStr.Trim('(', ')').Split(';');

                            var x = float.Parse(components[0], CultureInfo.InvariantCulture);
                            var y = float.Parse(components[1], CultureInfo.InvariantCulture);
                            var z = float.Parse(components[2], CultureInfo.InvariantCulture);

                            encodings.Add(new Vector3(x, y, z));
                        }

                        var result = new ColorManager.ColorMatrixResult(encodings, colors);
                        _finalColors.Add(result);
                    }

                    break;
            }
        }
    }
}