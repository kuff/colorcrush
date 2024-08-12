// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Util
{
    public class CoreComponentInitializer : MonoBehaviour
    {
        [SerializeField] private List<GameObject> objectsToMove = new();

        private void Awake()
        {
            MoveObjectsToRoot();
        }

        private void MoveObjectsToRoot()
        {
            foreach (var obj in objectsToMove)
            {
                if (obj != null)
                {
                    // Set the parent to null, which moves it to the root of the scene
                    obj.transform.SetParent(null, true);
                }
            }

            // Optionally, destroy this object after moving all children
            Destroy(gameObject);
        }
    }
}