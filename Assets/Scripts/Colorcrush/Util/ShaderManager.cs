// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Util
{
    public class ShaderManager : MonoBehaviour
    {
        private static ShaderManager _instance;
        private readonly Dictionary<GameObject, Material> _materialCopies = new();

        private static ShaderManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ShaderManager");
                    _instance = go.AddComponent<ShaderManager>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

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
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            CleanupMaterialCopies();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("ShaderManager: Number of material copies before cleanup: " + _materialCopies.Count);
            CleanupMaterialCopies();
        }

        private Material GetOrCreateMaterialCopy(GameObject targetObject)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException(nameof(targetObject));
            }

            if (_materialCopies.TryGetValue(targetObject, out var existingCopy))
            {
                return existingCopy;
            }

            var image = targetObject.GetComponent<Image>();
            var originalMaterial = image != null
                ? image.material
                : targetObject.GetComponent<Renderer>()?.sharedMaterial;

            if (originalMaterial == null)
            {
                throw new InvalidOperationException($"ShaderManager: No material found on GameObject {targetObject.name}");
            }

            return _materialCopies[targetObject] = new Material(originalMaterial);
        }

        public static void SetColor(GameObject targetObject, string propertyName, Color color)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException(nameof(targetObject));
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var materialCopy = Instance.GetOrCreateMaterialCopy(targetObject);
            try
            {
                materialCopy.SetColor(propertyName, color);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to set color property '{propertyName}' on material", e);
            }

            UpdateObjectMaterial(targetObject, materialCopy);
        }

        public static void SetFloat(GameObject targetObject, string propertyName, float value)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException(nameof(targetObject));
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var materialCopy = Instance.GetOrCreateMaterialCopy(targetObject);
            try
            {
                materialCopy.SetFloat(propertyName, value);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to set float property '{propertyName}' on material", e);
            }

            UpdateObjectMaterial(targetObject, materialCopy);
        }

        private static void UpdateObjectMaterial(GameObject targetObject, Material material)
        {
            var image = targetObject.GetComponent<Image>();
            if (image != null)
            {
                image.material = material;
                return;
            }

            var renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
                return;
            }

            throw new InvalidOperationException($"ShaderManager: No Image or Renderer component found on GameObject {targetObject.name}");
        }

        private void CleanupMaterialCopies()
        {
            _materialCopies.Clear();
        }
    }
}