// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Util
{
    public class ShaderManager : MonoBehaviour
    {
        private static ShaderManager _instance;

        private readonly Dictionary<GameObject, Material> _originalMaterials = new();
        private readonly Dictionary<GameObject, Material> _temporaryMaterials = new();
        private readonly Dictionary<Material, Dictionary<string, object>> _originalValues = new();

        public static ShaderManager Instance
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
            }
        }

        private void OnApplicationQuit()
        {
            CleanupTemporaryMaterials();
            ResetValues();
        }

        private Material GetOrCreateTemporaryMaterial(GameObject targetObject)
        {
            if (_temporaryMaterials.TryGetValue(targetObject, out var tempMaterial))
            {
                return tempMaterial;
            }

            var originalMaterial = GetOriginalMaterial(targetObject);
            if (originalMaterial == null) return null;

            tempMaterial = new Material(originalMaterial);
            _temporaryMaterials[targetObject] = tempMaterial;
            return tempMaterial;
        }

        private Material GetOriginalMaterial(GameObject targetObject)
        {
            if (_originalMaterials.TryGetValue(targetObject, out var originalMaterial))
            {
                return originalMaterial;
            }

            // Try to get material from Image component
            var image = targetObject.GetComponent<Image>();
            if (image != null)
            {
                originalMaterial = image.material;
                _originalMaterials[targetObject] = originalMaterial;
                return originalMaterial;
            }

            // Try to get material from Renderer component
            var renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.sharedMaterial;
                _originalMaterials[targetObject] = originalMaterial;
                return originalMaterial;
            }

            Debug.LogError($"ShaderManager: No material found on GameObject {targetObject.name}");
            return null;
        }

        public static void SetColor(GameObject targetObject, string propertyName, Color color)
        {
            var tempMaterial = Instance.GetOrCreateTemporaryMaterial(targetObject);
            if (tempMaterial == null) return;

            var originalMaterial = Instance.GetOriginalMaterial(targetObject);
            Instance.SaveOriginalValue(originalMaterial, propertyName, originalMaterial.GetColor(propertyName));
            tempMaterial.SetColor(propertyName, color);
            
            // Update the material reference on the target object
            UpdateObjectMaterial(targetObject, tempMaterial);
        }

        public static void SetFloat(GameObject targetObject, string propertyName, float value)
        {
            var tempMaterial = Instance.GetOrCreateTemporaryMaterial(targetObject);
            if (tempMaterial == null) return;

            var originalMaterial = Instance.GetOriginalMaterial(targetObject);
            Instance.SaveOriginalValue(originalMaterial, propertyName, originalMaterial.GetFloat(propertyName));
            tempMaterial.SetFloat(propertyName, value);
            
            // Update the material reference on the target object
            UpdateObjectMaterial(targetObject, tempMaterial);
        }

        private static void UpdateObjectMaterial(GameObject targetObject, Material temporaryMaterial)
        {
            // Update UI Image
            var image = targetObject.GetComponent<Image>();
            if (image != null)
            {
                image.material = temporaryMaterial;
                return;
            }

            // Update Renderer
            var renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = temporaryMaterial;
            }
        }

        private void SaveOriginalValue(Material material, string propertyName, object value)
        {
            if (!_originalValues.ContainsKey(material))
            {
                _originalValues[material] = new Dictionary<string, object>();
            }

            if (!_originalValues[material].ContainsKey(propertyName))
            {
                _originalValues[material][propertyName] = value;
                Debug.Log($"ShaderManager: Saved original value of {value} for {propertyName} on {material.name}");
            }
        }

        private void CleanupTemporaryMaterials()
        {
            foreach (var tempMaterial in _temporaryMaterials.Values)
            {
                if (tempMaterial != null)
                {
                    Destroy(tempMaterial);
                }
            }
            _temporaryMaterials.Clear();
            _originalMaterials.Clear();
        }

        public static void ResetValues()
        {
            if (!ProjectConfig.InstanceConfig.resetShadersOnShutdown)
            {
                return;
            }

            var resetCount = 0;
            foreach (var (gameObject, originalMaterial) in Instance._originalMaterials)
            {
                if (gameObject == null || originalMaterial == null) continue;

                // Reset the original material's values
                if (Instance._originalValues.TryGetValue(originalMaterial, out var values))
                {
                    foreach (var (propertyName, value) in values)
                    {
                        if (value is Color color)
                        {
                            originalMaterial.SetColor(propertyName, color);
                            resetCount++;
                        }
                        else if (value is float floatValue)
                        {
                            originalMaterial.SetFloat(propertyName, floatValue);
                            resetCount++;
                        }
                    }
                }

                // Restore the original material on the game object
                UpdateObjectMaterial(gameObject, originalMaterial);
            }

            Instance.CleanupTemporaryMaterials();
            Instance._originalValues.Clear();
            Debug.Log($"ShaderManager: Reset {resetCount} shader properties to their original values.");
        }
    }
}