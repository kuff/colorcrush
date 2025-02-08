// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#endregion

namespace Colorcrush.Util
{
    public class SceneManager : MonoBehaviour
    {
        private static SceneManager _instance;
        private Coroutine _activationWarningCoroutine;
        private AsyncOperation _asyncOperation;
        private string _previousSceneName;

        private static SceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SceneManager");
                        _instance = go.AddComponent<SceneManager>();
                    }
                }

                return _instance;
            }
        }

        public static bool IsLoading { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public static event Action<Scene, LoadSceneMode> sceneLoaded;

        public static void LoadSceneAsync(string sceneName)
        {
            LoadSceneAsync(sceneName, null);
        }

        public static void LoadSceneAsync(string sceneName, Action onSceneReady)
        {
            if (IsLoading)
            {
                Debug.LogWarning("SceneManager: A scene is already loading. Cannot load another scene until the current one is done.");
                return;
            }

            Debug.Log($"SceneManager: Starting to load scene: {sceneName} asynchronously.");
            Instance._previousSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            IsLoading = true;
            Instance.StartCoroutine(Instance.LoadSceneAsyncCoroutine(sceneName, onSceneReady));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, Action onSceneReady)
        {
            // Check if the scene is already loaded to avoid reloading it
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName)
            {
                _asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                if (_asyncOperation == null)
                {
                    Debug.LogError("SceneManager: Failed to load scene " + sceneName + ". Aborting.");
                    IsLoading = false;
                    yield break;
                }

                _asyncOperation.allowSceneActivation = false;

                while (_asyncOperation.progress < 0.9f)
                {
                    yield return null;
                }

                Debug.Log("SceneManager: Scene loaded to 90%. Ready for activation.");
                onSceneReady?.Invoke();

                if (onSceneReady == null)
                {
                    Debug.Log("SceneManager: No callback provided. Automatically activating the scene.");
                    ActivateLoadedScene();
                }

                // Wait here until the scene is activated by the caller.
                while (!_asyncOperation.allowSceneActivation)
                {
                    yield return null;
                }

                // Wait for the scene to finish loading
                yield return WaitForSceneActivation();
            }
            else
            {
                Debug.LogWarning("SceneManager: Scene " + sceneName + " is already loaded.");
                onSceneReady?.Invoke();
                IsLoading = false;
            }
        }

        public static void ActivateLoadedScene()
        {
            if (Instance._activationWarningCoroutine != null)
            {
                Instance.StopCoroutine(Instance._activationWarningCoroutine);
                Instance._activationWarningCoroutine = null;
            }

            Debug.Log("SceneManager: Activating loaded scene.");
            Instance.ActivateLoadedSceneInternal();
        }

        private void ActivateLoadedSceneInternal()
        {
            if (_asyncOperation != null && !_asyncOperation.isDone)
            {
                _asyncOperation.allowSceneActivation = true;
            }
            else
            {
                Debug.LogWarning("SceneManager: No scene is currently being loaded or the scene has already been activated.");
                IsLoading = false;
            }
        }

        private IEnumerator WaitForSceneActivation()
        {
            while (!_asyncOperation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            sceneLoaded?.Invoke(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        public static string GetPreviousSceneName()
        {
            return Instance._previousSceneName;
        }
    }
}