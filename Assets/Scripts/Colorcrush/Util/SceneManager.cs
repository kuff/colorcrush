// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections;
using UnityEngine;

#endregion

namespace Colorcrush.Util
{
    public class SceneManager : MonoBehaviour
    {
        private static SceneManager _instance;
        private AsyncOperation _asyncOperation;

        public static SceneManager Instance
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

        // This function can be called to load a scene in the background without a callback
        public static void LoadSceneAsync(string sceneName)
        {
            LoadSceneAsync(sceneName, null);
        }

        // This function can be called to load a scene in the background with a callback
        public static void LoadSceneAsync(string sceneName, Action onSceneReady)
        {
            Debug.Log($"Starting to load scene: {sceneName} asynchronously.");
            IsLoading = true;
            Instance.StartCoroutine(Instance.LoadSceneAsyncCoroutine(sceneName, onSceneReady));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, Action onSceneReady)
        {
            // Check if the scene is already loaded to avoid reloading it
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName)
            {
                _asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                _asyncOperation!.allowSceneActivation = false;

                while (!_asyncOperation.isDone)
                {
                    if (_asyncOperation.progress >= 0.9f)
                    {
                        if (onSceneReady != null)
                        {
                            Debug.Log("Scene loaded. Invoking onSceneReady callback.");
                            onSceneReady.Invoke();
                        }
                        else
                        {
                            Debug.Log("Scene loaded. Activating loaded scene.");
                            ActivateLoadedScene();
                        }

                        yield break;
                    }

                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("Scene " + sceneName + " is already loaded.");
                onSceneReady?.Invoke();
                IsLoading = false;
            }
        }

        // This function should be called to fully load the scene after the callback
        public static void ActivateLoadedScene()
        {
            Debug.Log("Activating loaded scene.");
            Instance.ActivateLoadedSceneInternal();
        }

        private void ActivateLoadedSceneInternal()
        {
            if (_asyncOperation is { isDone: false, })
            {
                _asyncOperation.allowSceneActivation = true;
                StartCoroutine(WaitForSceneActivation());
            }
            else
            {
                Debug.LogWarning("No scene is currently being loaded or the scene has already been activated.");
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
        }
    }
}