// Copyright (C) 2024 Peter Guld Leth

#region

using Colorcrush;
using Colorcrush.Util;
using UnityEditor;
using UnityEngine;
using static Colorcrush.ProjectConfig;

#endregion

namespace Editor
{
    public class ProjectMenu : MonoBehaviour
    {
        private const string ConfigPath = "Assets/Resources/Colorcrush/ProjectConfigurationObject.asset";
        private const string MenuItemPrefix = "Colorcrush/";

        [MenuItem(MenuItemPrefix + "Edit Configuration %#e", false, 10)]
        public static void ShowConfiguration()
        {
            var config = AssetDatabase.LoadAssetAtPath<ProjectConfigurationObject>(ConfigPath);

            if (config == null)
            {
                Debug.LogError("Project Configuration not found at: " + ConfigPath);
                return;
            }

            Selection.activeObject = config;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem(MenuItemPrefix + "Create Configuration", false, 10)]
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

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Colorcrush"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Colorcrush");
            }

            config = ScriptableObject.CreateInstance<ProjectConfigurationObject>();

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }

        [MenuItem(MenuItemPrefix + "Open Main Resources Path %#y")]
        private static void OpenMainResourcesPath()
        {
            RevealInProjectWindow(InstanceConfig.mainResourcesPath);
        }

        [MenuItem(MenuItemPrefix + "Open Main Scenes Path %#u")]
        private static void OpenMainScenesPath()
        {
            RevealInProjectWindow(InstanceConfig.mainScenesPath);
        }

        [MenuItem(MenuItemPrefix + "Open Main Scripts Path %#i")]
        private static void OpenMainScriptsPath()
        {
            RevealInProjectWindow(InstanceConfig.mainScriptsPath);
        }

        [MenuItem(MenuItemPrefix + "Toggle Use Initiating Scene %#t")]
        private static void ToggleUseInitiatingScene()
        {
            var config = InstanceConfig;
            config.useInitiatingScene = !config.useInitiatingScene;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"Use Initiating Scene: {config.useInitiatingScene}");
        }

        [MenuItem(MenuItemPrefix + "Open Persistent Data Path %#o")]
        public static void OpenPersistentDataPath()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        private static void RevealInProjectWindow(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Path is null or empty.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            else
            {
                Debug.LogWarning($"Asset not found at path: {path}");
            }
        }
    }
}