// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public abstract class Animator : MonoBehaviour
    {
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

        public abstract Vector3 GetPosition();
        public abstract void SetPosition(Vector3 position);
        public abstract Quaternion GetRotation();
        public abstract void SetRotation(Quaternion rotation);
        public abstract Vector3 GetScale();
        public abstract void SetScale(Vector3 scale);
        public abstract float GetOpacity();
        public abstract void SetOpacity(float opacity);

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