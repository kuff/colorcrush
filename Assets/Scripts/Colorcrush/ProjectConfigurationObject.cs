// Copyright (C) 2024 Peter Guld Leth

#region

using System.Text;
using Colorcrush.Game;
using UnityEngine;
using UnityEngine.Serialization;

#endregion

namespace Colorcrush
{
    [CreateAssetMenu(fileName = "ProjectConfigurationObject", menuName = "Colorcrush/Project Configuration")]
    public class ProjectConfigurationObject : ScriptableObject
    {
        [Header("Editor Configuration")]
        [FormerlySerializedAs("startScenePath")] [Tooltip("The file path to the initial scene that the game will load when 'Use Initiating Scene' is enabled. This should be a valid scene path within the project.")]
        public string initiatingScenePath;

        [Tooltip("Determines whether the game starts from the specified initiating scene. If set to false, the game will follow Unity's default scene loading sequence.")]
        public bool useInitiatingScene = true;

        [FormerlySerializedAs("resourcesPath")] [Tooltip("The directory path where Colorcrush resources are stored. This path should be a subfolder within Unity's 'Resources' folder to ensure proper resource loading.")]
        public string mainResourcesPath = "Assets/Resources/Colorcrush";

        [FormerlySerializedAs("scenesPath")] [Tooltip("The directory path where all Colorcrush game scenes are stored. Ensure this path is correctly set to manage scene assets effectively.")]
        public string mainScenesPath = "Assets/Scenes";

        [Tooltip("The directory path where all Colorcrush scripts are stored. This path should point to the folder containing the game's script files.")]
        public string mainScriptsPath = "Assets/Scripts/Colorcrush";

        [Tooltip("If enabled, all log files will be automatically deleted when the Unity Editor starts. This is useful for maintaining a clean log environment during development.")]
        public bool deleteAllLogFilesOnEditorStartup = true;

        [Header("Game Configuration")]
        [Tooltip("The seed value used for random number generation. Setting a specific seed ensures that the sequence of random numbers is reproducible, which is useful for running experiments.")]
        public int randomSeed = 42;

        [Tooltip("The file path from which color data will be loaded. This should point to a valid text file containing color information.")]
        public string colorDataFilePath = "Assets/Resources/Colorcrush/ColorData.txt";

        [Tooltip("The regular expression pattern used to split color values in the data file. This pattern should match the format of the color data entries.")]
        public string colorSplitRegex = @"\s+";

        [Tooltip("Specifies the format of the color data within the file. This setting should match the format used in the color data file for accurate color processing.")]
        public ColorManager.ColorFormat colorDataFormat = ColorManager.ColorFormat.SrgbZeroToOne;

        [Tooltip("The default mode for the ColorTransposeShader regarding skin color. If true, all non-white pixels are converted to skin color (no emoji face and shade). If false, only pixels closely matching the skin color are changed to the provided target color (leaving the emoji face and shade visible).")]
        public bool useSkinColorMode;

        [Tooltip("If enabled, all game levels will be accessible from the start without needing to unlock them through gameplay progression.")]
        public bool unlockAllLevelsFromStart;

        [Tooltip("The identifier for the color experiment to execute. This name should correspond to a predefined experiment setup.")]
        public string colorExperimentName;

        [Tooltip("If enabled, allows users to toggle the skin color mode by tapping three times in the MenuScene, providing a quick way to switch modes when the experiments are run by a conductor in-person.")]
        public bool enableTripleTapToggleSkinColorMode = true;

        [FormerlySerializedAs("enableResultButton")] [Tooltip("If enabled, the reset button will be visible in the menu scene, allowing the experiment conductor to reset the game state. If disabled, the button will be hidden.")]
        public bool enableResetButton = true;

        [Header("Emoji Configuration")]
        [Tooltip("The name of the default emoji sprite (without the file extension).")]
        public string defaultEmojiName = "reshot-icon-blank-XN4TPFSGQ8";

        [Tooltip("The name of the default happy emoji sprite (without the file extension).")]
        public string defaultHappyEmojiName = "reshot-icon-ok-QBCJ4DSA8U";

        [Tooltip("The path to the folder containing happy emoji sprites.")]
        public string happyEmojiFolder = "Colorcrush/Emoji/Happy";

        [Tooltip("The path to the folder containing sad emoji sprites.")]
        public string sadEmojiFolder = "Colorcrush/Emoji/Sad";

        [Header("Logging Configuration")]
        [Tooltip("The prefix used for log file names.")]
        public string logFilePrefix = "game_log_";

        [Tooltip("If true, timestamps in log files will be relative to the creation of the log file, and not reset between restarts.")]
        public bool useAdditiveTimestamps = true;

        [Tooltip("The file extension for log files.")]
        public string logFileExtension = ".txt";

        [Tooltip("The interval (in seconds) between automatic log saves.")]
        public float logSaveInterval = 5f;

        [Tooltip("The minimum severity level for logs to be recorded. The severity scale is: Log (0), Warning (1), Assert (2), Error (3), Exception (4). Only logs with this severity or higher will be recorded.")]
        public LogType minimumLogSeverity = LogType.Log;

        [Tooltip("If true, a new log file will always be created on startup, regardless of existing log files.")]
        public bool alwaysCreateNewLogFileOnStartup;

        [Tooltip("The buffer size for the StreamWriter in LoggingManager.")]
        public int logFileBufferSize = 65536;

        [Tooltip("If true, Console output will not be logged when running in the Unity Editor, regardless of the minimum log severity setting.")]
        public bool suppressConsoleLoggingInEditor;

        [Header("Audio Configuration")]
        [Tooltip("The global gain factor applied to all audio. This is a multiplier, where 1 is normal volume.")]
        public float globalGain = 1f;

        [Tooltip("The maximum number of simultaneous audio sources that can play at once.")]
        public int maxAudioSources = 32;

        [FormerlySerializedAs("targetRMS")] [Tooltip("The target RMS (Root Mean Square) value for audio normalization.")]
        public float targetRms = 0.01f;

        [Tooltip("The path within Resources where audio clips are stored.")]
        public string audioPath = "Colorcrush/Audio/";

        [Tooltip("The gain applied to the audio mixer in decibels.")]
        public float mixerGainDB = 10f;

        [Tooltip("The minimum volume adjustment factor for audio clips.")]
        public float minVolumeAdjustment = 0.01f;

        [Tooltip("The maximum volume adjustment factor for audio clips.")]
        public float maxVolumeAdjustment = 2f;

        [Tooltip("The encoding to use for the StreamWriter in LoggingManager.")]
        public Encoding logFileEncoding = Encoding.UTF8;
    }
}