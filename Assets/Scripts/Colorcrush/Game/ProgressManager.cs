using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Colorcrush.Logging;
using UnityEngine.SceneManagement;

namespace Colorcrush.Game
{
    public class ProgressManager : MonoBehaviour
    {
        private static ProgressManager _instance;
        private static Queue<ILogEvent> _logEventQueue = new Queue<ILogEvent>();

        public static HashSet<string> CompletedTargetColors { get; private set; } = new HashSet<string>();
        public static HashSet<string> RewardedEmojis { get; private set; } = new HashSet<string>();

        public static string MostRecentCompletedTargetColor { get; private set; }
        public static string MostRecentRewardedEmoji { get; private set; }

        public static ProgressManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ProgressManager");
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
            RefreshProgressionState();
        }

        private void OnLogEventQueued(ILogEvent logEvent)
        {
            _logEventQueue.Enqueue(logEvent);
        }

        public static void RefreshProgressionState(List<string> logLines = null)
        {
            if (logLines != null)
            {
                ProcessLogLines(logLines);
            }
            else
            {
                // Re-analyze the entire log file
                List<string> allLogLines = LoggingManager.GetLogDataLines();
                CompletedTargetColors.Clear();
                RewardedEmojis.Clear();
                ProcessLogLines(allLogLines);
                ProcessLogEventQueue();
            }

            // Log a summary of the refreshed progression state
            Debug.Log($"Progression State Refreshed: " +
                      $"{CompletedTargetColors.Count} target colors, " +
                      $"{RewardedEmojis.Count} rewarded emojis. " +
                      $"Most recent target color: {MostRecentCompletedTargetColor}, " +
                      $"Most recent emoji: {MostRecentRewardedEmoji}");
        }

        public static void ResetProgressionState() {
            LoggingManager.StartNewLogFile();
            RefreshProgressionState();
        }

        private static void ProcessLogLines(List<string> logLines)
        {
            foreach (string line in logLines)
            {
                string[] parts = line.Split(',');
                if (parts.Length < 2) continue;

                string eventName = parts[1];
                string eventData = parts.Length > 2 ? parts[2] : string.Empty;

                ProcessEvent(eventName, eventData);
            }
        }

        private static void ProcessLogEventQueue()
        {
            List<string> queuedLogLines = new List<string>();
            while (_logEventQueue.Count > 0)
            {
                ILogEvent logEvent = _logEventQueue.Dequeue();
                queuedLogLines.Add($"0,{logEvent.EventName},{logEvent.GetStringifiedData()}");
            }
            ProcessLogLines(queuedLogLines);
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static void ProcessEvent(string eventName, string eventData)
        {
            if (eventName == "newtargetvalue")
            {
                CompletedTargetColors.Add(eventData);
                MostRecentCompletedTargetColor = eventData;
            }
            else if (eventName == "rewardemoji")
            {
                RewardedEmojis.Add(eventData);
                MostRecentRewardedEmoji = eventData;
            }
        }
    }
}
