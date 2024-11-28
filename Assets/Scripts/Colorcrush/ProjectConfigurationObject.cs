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
        [Header("Editor Configuration")] [FormerlySerializedAs("startScenePath")] [Tooltip("The path to the initiating scene. This is used when 'Use Initiating Scene' is enabled.")]
        public string initiatingScenePath;

        [Tooltip("If true, the game will start from the initiating scene specified above. If false, it will use the default Unity scene loading behavior.")]
        public bool useInitiatingScene = true;

        [FormerlySerializedAs("resourcesPath")] [Tooltip("The path where Colorcrush resources are stored. This should be a subfolder of the Unity 'Resources' folder.")]
        public string mainResourcesPath = "Assets/Resources/Colorcrush";

        [FormerlySerializedAs("scenesPath")] [Tooltip("The path where Colorcrush scenes are stored.")]
        public string mainScenesPath = "Assets/Scenes";

        [Tooltip("The path where Colorcrush scripts are stored.")]
        public string mainScriptsPath = "Assets/Scripts/Colorcrush";

        [Tooltip("If true, all log files will be deleted on startup when running in the Unity Editor.")]
        public bool deleteAllLogFilesOnEditorStartup = true;

        [Tooltip("The path where generated emoji materials will be saved.")]
        public string generatedMaterialsPath = "Assets/Resources/GeneratedMaterials";

        [Tooltip("The prefix used for generated emoji materials.")]
        public string emojiMaterialPrefix = "EmojiMaterial_";

        [Tooltip("If true, masking will be disabled for images when generating materials.")]
        public bool disableMaskingOnGenerate = true;

        [Header("Game Configuration")] [Tooltip("The seed used for random number generation. Using the same seed will produce the same sequence of random numbers.")]
        public int randomSeed = 42;

        [Tooltip("If true, all shaders will be reset to their initial state when the game is shut down.")]
        public bool resetShadersOnShutdown = true;

        [Tooltip("The file path for loading color data.")]
        public string colorDataFilePath = "Assets/Resources/Colorcrush/ColorData.txt";

        [Tooltip("The regex pattern used to split color values in the data file.")]
        public string colorSplitRegex = @"\s+";

        [Tooltip("The format of the color data in the file.")]
        public ColorManager.ColorFormat colorDataFormat = ColorManager.ColorFormat.SRGBZeroToOne;

        [Tooltip("The default skin color mode for the ColorTransposeShader. If true, all non-white pixels become skin colored. If false, only pixels matching the skin color within tolerance are changed.")]
        public bool useSkinColorMode;

        [Tooltip("If true, all levels will be unlocked and available from the start. If false, levels must be unlocked through progression.")]
        public bool unlockAllLevelsFromStart;

        [Tooltip("The name of the color experiment to run. This string will be matched against pre-programmed experiment setups in the ColorManager.")]
        public string colorExperimentName;

        [Tooltip("If true, allows toggling skin color mode with three-time tap in MenuSceneController.")]
        public bool enableTripleTapToggleSkinColorMode = true;
        
        [Tooltip("If true, enables the result button in the menu scene. If false, the result button will be hidden.")]
        public bool enableResultButton = true;

        [Header("Emoji Configuration")] [Tooltip("The name of the default emoji sprite (without the file extension).")]
        public string defaultEmojiName = "reshot-icon-blank-XN4TPFSGQ8";

        [Tooltip("The name of the default happy emoji sprite (without the file extension).")]
        public string defaultHappyEmojiName = "reshot-icon-ok-QBCJ4DSA8U";

        [Tooltip("The path to the folder containing happy emoji sprites.")]
        public string happyEmojiFolder = "Colorcrush/Emoji/Happy";

        [Tooltip("The path to the folder containing sad emoji sprites.")]
        public string sadEmojiFolder = "Colorcrush/Emoji/Sad";

        [Header("Logging Configuration")] [Tooltip("The prefix used for log file names.")]
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

        [Header("Audio Configuration")] [Tooltip("The global gain factor applied to all audio. This is a multiplier, where 1 is normal volume.")]
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

        [Tooltip("The encoding to use for the StreamWriter in LoggingManager.")]
        public Encoding logFileEncoding = Encoding.UTF8;
    }
}