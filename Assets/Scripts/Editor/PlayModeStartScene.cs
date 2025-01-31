// Copyright (C) 2025 Peter Guld Leth

#region

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using static Colorcrush.ProjectConfig;

#endregion

namespace Editor
{
    [InitializeOnLoad]
    public class PlayModeStartScene
    {
        private const string OriginalSceneKey = "OriginalScenePath";

        static PlayModeStartScene()
        {
            // Register the event handler for when the play mode state changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!InstanceConfig.useInitiatingScene)
            {
                return;
            }

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Save the currently open scene(s)
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // Store the path of the currently active scene in EditorPrefs
                    var originalScenePath = SceneManager.GetActiveScene().path;
                    EditorPrefs.SetString(OriginalSceneKey, originalScenePath);

                    // Open the specified start scene
                    var startScenePath = InstanceConfig.initiatingScenePath;
                    EditorSceneManager.OpenScene(startScenePath);
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Return to the original scene if it was stored
                if (EditorPrefs.HasKey(OriginalSceneKey))
                {
                    var originalScenePath = EditorPrefs.GetString(OriginalSceneKey);
                    EditorSceneManager.OpenScene(originalScenePath);

                    // Remove the original scene path from EditorPrefs
                    EditorPrefs.DeleteKey(OriginalSceneKey);
                }
            }
        }
    }
}