// Copyright (C) 2024 Peter Guld Leth

#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public abstract class Animator : MonoBehaviour
    {
        private readonly Dictionary<string, int> _currentPropertyOwners = new();
        private readonly Dictionary<string, HashSet<int>> _previousPropertyOwners = new();
        protected float OriginalOpacity;
        protected Vector3 OriginalPosition;
        protected Quaternion OriginalRotation;
        protected Vector3 OriginalScale;

        protected virtual void Awake()
        {
            OriginalPosition = GetPosition();
            OriginalRotation = GetRotation();
            OriginalScale = GetScale();
            OriginalOpacity = GetOpacity();
        }

        protected void SetIfOwned(string propertyName, AnimationManager.Animation self, Action setter)
        {
            if (self == null)
            {
                Debug.LogWarning($"Animator: Set {propertyName} with a null Animation reference. The ownership model may break.");
                setter();
                return;
            }

            var selfId = self.GetHashCode();

            if (!_currentPropertyOwners.TryGetValue(propertyName, out var currentOwnerId))
            {
                _currentPropertyOwners[propertyName] = selfId;
                setter();
                return;
            }

            if (currentOwnerId == selfId)
            {
                setter();
                return;
            }

            if (_previousPropertyOwners.TryGetValue(propertyName, out var previousOwnerIds) && previousOwnerIds.Contains(selfId))
            {
                throw new InvalidOperationException($"Animator: Animation {self.GetType().Name} attempted to set {propertyName} after losing ownership.");
            }

            if (!_previousPropertyOwners.ContainsKey(propertyName))
            {
                _previousPropertyOwners[propertyName] = new HashSet<int>();
            }

            _previousPropertyOwners[propertyName].Add(currentOwnerId);

            _currentPropertyOwners[propertyName] = selfId;
            setter();
        }

        public abstract Vector3 GetPosition();
        public abstract void SetPosition(Vector3 position, AnimationManager.Animation self);
        public abstract Quaternion GetRotation();
        public abstract void SetRotation(Quaternion rotation, AnimationManager.Animation self);
        public abstract Vector3 GetScale();
        public abstract void SetScale(Vector3 scale, AnimationManager.Animation self);
        public abstract float GetOpacity();
        public abstract void SetOpacity(float opacity, AnimationManager.Animation self);

        public Vector3 GetOriginalPosition()
        {
            return OriginalPosition;
        }

        public Quaternion GetOriginalRotation()
        {
            return OriginalRotation;
        }

        public Vector3 GetOriginalScale()
        {
            return OriginalScale;
        }

        public float GetOriginalOpacity()
        {
            return OriginalOpacity;
        }
    }
}