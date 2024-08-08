// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Animation
{
    public class EmojiAnimator : Animator
    {
        [SerializeField] private Image image;
        [SerializeField] private Material material;

        protected override void Awake()
        {
            base.Awake();

            image = GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("EmojiAnimator requires an Image component.");
                return;
            }

            material = image.material;
            if (material == null)
            {
                Debug.LogError("EmojiAnimator requires a material on the Image component.");
            }
        }

        public override Vector3 GetPosition()
        {
            return transform.position;
        }

        public override float GetOpacity()
        {
            if (material != null)
            {
                return material.GetFloat("_Alpha");
            }

            return 1f; // Default opacity if material is not available
        }

        public override void SetOpacity(float opacity)
        {
            if (material != null)
            {
                material.SetFloat("_Alpha", opacity);
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (image != null)
            {
                image.sprite = sprite;
            }
        }

        public override Vector3 GetScale()
        {
            return transform.localScale;
        }

        public override void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
        }

        public override void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public override Quaternion GetRotation()
        {
            return transform.rotation;
        }

        public override void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
    }
}