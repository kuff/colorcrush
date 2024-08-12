// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Colorcrush.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

#endregion

namespace Colorcrush.Game
{
    public class ProgressManager : MonoBehaviour
    {
        private static ProgressManager _instance;
        private static readonly Queue<ILogEvent> LogEventQueue = new();

        private static readonly List<string> _completedTargetColors = new();
        private static readonly List<string> _rewardedEmojis = new();
        private static readonly List<List<string>> _selectedColors = new();
        private static string _mostRecentCompletedTargetColor;
        private static bool _currentLevelCompleted = true;
        private static string _currentTargetColor;
        private static readonly Dictionary<int, string> _generatedColors = new();
        private static readonly List<string> _currentLevelSelectedColors = new();

        public static List<string> CompletedTargetColors
        {
            get
            {
                ProcessLogEventQueue();
                return _completedTargetColors;
            }
        }

        public static List<string> RewardedEmojis
        {
            get
            {
                ProcessLogEventQueue();
                return _rewardedEmojis;
            }
        }

        public static List<List<string>> SelectedColors
        {
            get
            {
                ProcessLogEventQueue();
                return _selectedColors;
            }
        }

        public static string MostRecentCompletedTargetColor
        {
            get
            {
                ProcessLogEventQueue();
                return _mostRecentCompletedTargetColor;
            }
            private set => _mostRecentCompletedTargetColor = value;
        }

        public static ProgressManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ProgressManager");
                    _instance = go.AddComponent<ProgressManager>();
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
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            LoggingManager.OnLogEventQueued += OnLogEventQueued;

            // Initial refresh using existing log data
            RefreshProgressionState();
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            LoggingManager.OnLogEventQueued -= OnLogEventQueued;
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
            _completedTargetColors.Clear();
            _rewardedEmojis.Clear();
            _selectedColors.Clear();
            _currentLevelCompleted = true;
            _currentTargetColor = null;
            _generatedColors.Clear();
            _currentLevelSelectedColors.Clear();

            // Process all existing log data
            var allLogLines = LoggingManager.GetLogDataLines();
            ProcessLogLines(allLogLines);

            // Log a summary of the refreshed progression state
            Debug.Log("Progression State Refreshed: " +
                      $"{_completedTargetColors.Count} completed target colors, " +
                      $"{_rewardedEmojis.Count} rewarded emojis, " +
                      $"{_selectedColors.Count} selected color sets. " +
                      $"Most recent completed target color: {_mostRecentCompletedTargetColor}");
        }

        public static void ResetProgressionState()
        {
            LoggingManager.StartNewLogFile();
            RefreshProgressionState();
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
                case "colorssubmitted":
                    if (!_currentLevelCompleted)
                    {
                        _rewardedEmojis.Add(eventData);
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
            }
        }
    }
}