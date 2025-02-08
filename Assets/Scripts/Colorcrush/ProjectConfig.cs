// Copyright (C) 2025 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush
{
    public static class ProjectConfig
    {
        private static ProjectConfigurationObject _instance;

        public static ProjectConfigurationObject InstanceConfig
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ProjectConfigurationObject>("Colorcrush/ProjectConfigurationObject");
                    if (_instance == null)
                    {
                        Debug.LogError("ProjectConfigurationObject asset not found in Resources/Colorcrush folder.");
                    }
                }

                return _instance;
            }
        }
    }
}