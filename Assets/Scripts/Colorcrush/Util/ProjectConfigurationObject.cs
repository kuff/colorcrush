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
        [FormerlySerializedAs("startScenePath")]
        public string initiatingScenePath;

        public bool useInitiatingScene = true;
        public string resourcesPath = "Assets/Resources/Colorcrush";
        public string scenesPath = "Assets/Scenes";

        public bool doPixelBalancing = true;
        public int randomSeed = 42;
    }
}