// Copyright (C) 2024 Peter Guld Leth

#region

using TMPro;
using UnityEngine;

#endregion

namespace Colorcrush
{
    public class UpdateVersionText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI versionText;

        private void Start()
        {
            if (versionText != null)
            {
                versionText.text += Application.version;
            }
            else
            {
                Debug.LogError("Version TextMeshProUGUI component is not assigned.");
            }
        }
    }
}