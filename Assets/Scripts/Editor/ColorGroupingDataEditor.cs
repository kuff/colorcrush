// Copyright (C) 2024 Peter Guld Leth

#region

using Colorcrush.Game;
using UnityEditor;

#endregion

namespace Editor
{
    [CustomEditor(typeof(ColorGroupingData))]
    public class ColorGroupingDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Inspection of ColorGroupingData is disabled to prevent lag.", MessageType.Warning);
        }
    }
}