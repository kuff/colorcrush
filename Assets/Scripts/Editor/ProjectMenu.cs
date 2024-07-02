using Colorcrush.Util;
using UnityEditor;
using UnityEngine;
using static Colorcrush.Util.ProjectConfig;

namespace Editor
{
    public class ProjectMenu : MonoBehaviour
    {
        private const string ConfigPath = "Assets/Resources/Colorcrush/ProjectConfigurationObject.asset";

        [MenuItem("Colorcrush/Edit Configuration", isValidateFunction:false, priority:1)]
        public static void ShowConfiguration()
        {
            var config = AssetDatabase.LoadAssetAtPath<ProjectConfigurationObject>(ConfigPath);

            if (config == null)
            {
                Debug.LogError("Project Configuration not found at: " + ConfigPath);
                return;
            }

            Selection.activeObject = config;
        }

        [MenuItem("Colorcrush/Create Configuration", isValidateFunction:false, priority:1)]
        public static void CreateConfiguration()
        {
            var config = AssetDatabase.LoadAssetAtPath<ProjectConfigurationObject>(ConfigPath);

            if (config != null)
            {
                EditorUtility.DisplayDialog("Configuration Exists",
                    "A configuration file already exists at: " + ConfigPath +
                    ". Please delete the existing file first.",
                    "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Colorcrush"))
                AssetDatabase.CreateFolder("Assets/Resources", "Colorcrush");

            config = ScriptableObject.CreateInstance<ProjectConfigurationObject>();

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }

        [MenuItem("Colorcrush/Show Resources Folder")]
        public static void OpenResourcesPath()
        {
            var config = InstanceConfig;
            if (config != null)
            {
                var path = config.resourcesPath;
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                else
                {
                    Debug.LogError("Resources path not found: " + path);
                }
            }
        }

        [MenuItem("Colorcrush/Show Scenes Folder")]
        public static void OpenScenesPath()
        {
            var config = InstanceConfig;
            if (config != null)
            {
                var path = config.scenesPath;
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                else
                {
                    Debug.LogError("Scenes path not found: " + path);
                }
            }
        }

        [MenuItem("Colorcrush/Open Persistent Data Path")]
        public static void OpenPersistentDataPath()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}