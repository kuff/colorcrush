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

        [Tooltip("The seed used for random number generation. Using the same seed will produce the same sequence of random numbers.")]
        public int randomSeed = 42;

        [Tooltip("The number of colors that need to be filtered before the game progresses to the next stage.")]
        public int numColorsToFilter = 12;
    }
}