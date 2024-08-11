// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;
using UnityEngine.Serialization;

#endregion

namespace Colorcrush.Util
{
    [CreateAssetMenu(fileName = "ProjectConfigurationObject", menuName = "Colorcrush/Project Configuration")]
    public class ProjectConfigurationObject : ScriptableObject
    {
        [Header("Editor Configuration")]
        [FormerlySerializedAs("startScenePath")] [Tooltip("The path to the initiating scene. This is used when 'Use Initiating Scene' is enabled.")]
        public string initiatingScenePath;

        [Tooltip("If true, the game will start from the initiating scene specified above. If false, it will use the default Unity scene loading behavior.")]
        public bool useInitiatingScene = true;

        [Tooltip("The path where Colorcrush resources are stored. This should be a subfolder of the Unity 'Resources' folder.")]
        public string resourcesPath = "Assets/Resources/Colorcrush";

        [Tooltip("The path where Colorcrush scenes are stored.")]
        public string scenesPath = "Assets/Scenes";

        [Tooltip("If true, the game will attempt to balance pixel colors in the final image. This may affect performance.")]
        public bool doPixelBalancing = true;

        [Header("Game Configuration")]
        [Tooltip("The seed used for random number generation. Using the same seed will produce the same sequence of random numbers.")]
        public int randomSeed = 42;

        [Tooltip("The number of colors that need to be filtered before the game progresses to the next stage.")]
        public int numColorsToFilter = 12;

        [Header("Emoji Configuration")]
        [Tooltip("The name of the default emoji sprite (without the file extension).")]
        public string defaultEmojiName = "reshot-icon-blank-XN4TPFSGQ8";

        [Tooltip("The name of the default happy emoji sprite (without the file extension).")]
        public string defaultHappyEmojiName = "reshot-icon-ok-QBCJ4DSA8U";

        [Tooltip("The path to the folder containing happy emoji sprites.")]
        public string happyEmojiFolder = "Colorcrush/Emoji/Happy";

        [Tooltip("The path to the folder containing sad emoji sprites.")]
        public string sadEmojiFolder = "Colorcrush/Emoji/Sad";

        [Tooltip("The folder name within Resources where emoji materials are stored.")]
        public string emojiMaterialsFolder = "GeneratedMaterials";

        [Tooltip("The prefix used for emoji material names.")]
        public string emojiMaterialPrefix = "EmojiMaterial_";

        [Header("Logging Configuration")]
        [Tooltip("The prefix used for log file names.")]
        public string logFilePrefix = "game_log_";

        [Tooltip("The file extension for log files.")]
        public string logFileExtension = ".txt";

        [Tooltip("The symbol used to mark the end of a log file. Make sure this symbol cannot be mistaken for logging data, as file lines are periodically deleted using it.")]
        public string endOfFileSymbol = "EOF";

        [Tooltip("The interval (in seconds) between automatic log saves.")]
        public float logSaveInterval = 5f;

        [Tooltip("The minimum severity level for logs to be recorded. The severity scale is: Log (0), Warning (1), Assert (2), Error (3), Exception (4). Only logs with this severity or higher will be recorded.")]
        public LogType minimumLogSeverity = LogType.Log;

        [Header("Audio Configuration")]
        [Tooltip("The global gain factor applied to all audio. This is a multiplier, where 1 is normal volume.")]
        public float globalGain = 1f;

        [Tooltip("The maximum number of simultaneous audio sources that can play at once.")]
        public int maxAudioSources = 32;

        [Tooltip("The target RMS (Root Mean Square) value for audio normalization.")]
        public float targetRMS = 0.01f;

        [Tooltip("The path within Resources where audio clips are stored.")]
        public string audioPath = "Colorcrush/Audio/";

        [Tooltip("The gain applied to the audio mixer in decibels.")]
        public float mixerGainDB = 10f;

        [Tooltip("The minimum volume adjustment factor for audio clips.")]
        public float minVolumeAdjustment = 0.01f;

        [Tooltip("The maximum volume adjustment factor for audio clips.")]
        public float maxVolumeAdjustment = 2f;
    }
}