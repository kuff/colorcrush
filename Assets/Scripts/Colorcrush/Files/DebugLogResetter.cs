// Copyright (C) 2024 Peter Guld Leth

#region

using Colorcrush.Files;
using UnityEngine;

#endregion

public class DebugLogResetter : MonoBehaviour
{
    private void Start()
    {
#if UNITY_EDITOR
        LoggingManager.DeleteAllLogFiles();
        Debug.Log("All log files deleted on game start in editor mode.");
#endif
    }
}