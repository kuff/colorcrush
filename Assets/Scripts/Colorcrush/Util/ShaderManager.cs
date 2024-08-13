// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Util
{
    public class ShaderManager : MonoBehaviour
    {
        private static ShaderManager _instance;

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
            ResetValues();
        }

        public static void SetColor(Material material, string propertyName, Color color)
        {
            Instance.SaveOriginalValue(material, propertyName, material.GetColor(propertyName));
            material.SetColor(propertyName, color);
        }

        public static void SetFloat(Material material, string propertyName, float value)
        {
            Instance.SaveOriginalValue(material, propertyName, material.GetFloat(propertyName));
            material.SetFloat(propertyName, value);
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
            }
        }

        public static void ResetValues()
        {
            if (!ProjectConfig.InstanceConfig.resetShadersOnShutdown)
            {
                return;
            }

            var resetCount = 0;
            foreach (var (material, value) in Instance._originalValues)
            {
                foreach (var propertyEntry in value)
                {
                    if (propertyEntry.Value is Color color)
                    {
                        material.SetColor(propertyEntry.Key, color);
                        resetCount++;
                    }
                    else if (propertyEntry.Value is float entryValue)
                    {
                        material.SetFloat(propertyEntry.Key, entryValue);
                        resetCount++;
                    }
                }
            }

            Instance._originalValues.Clear();
            Debug.Log($"ShaderManager: Reset {resetCount} shader properties to their original values.");
        }
    }
}