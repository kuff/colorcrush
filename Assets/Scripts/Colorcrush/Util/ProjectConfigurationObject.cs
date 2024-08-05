using UnityEngine;
using UnityEngine.Serialization;

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
    }
}