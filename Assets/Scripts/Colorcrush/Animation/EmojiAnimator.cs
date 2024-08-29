// Copyright (C) 2024 Peter Guld Leth

#region

using Colorcrush.Util;
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
            return material != null ? material.GetFloat("_Alpha") : 1f; // Default opacity if material is not available
        }

        public override void SetOpacity(float opacity, AnimationManager.Animation self)
        {
            SetIfOwned("Opacity", self, () =>
            {
                if (material != null)
                {
                    ShaderManager.SetFloat(material, "_Alpha", opacity);
                }
            });
        }

        public void SetSprite(Sprite sprite, AnimationManager.Animation self)
        {
            SetIfOwned("Sprite", self, () =>
            {
                if (image != null)
                {
                    image.sprite = sprite;
                }
            });
        }

        public override Vector3 GetScale()
        {
            return transform.localScale;
        }

        public override void SetScale(Vector3 scale, AnimationManager.Animation self)
        {
            SetIfOwned("Scale", self, () => transform.localScale = scale);
        }

        public override void SetPosition(Vector3 position, AnimationManager.Animation self)
        {
            SetIfOwned("Position", self, () => transform.position = position);
        }

        public override Quaternion GetRotation()
        {
            return transform.rotation;
        }

        public override void SetRotation(Quaternion rotation, AnimationManager.Animation self)
        {
            SetIfOwned("Rotation", self, () => transform.rotation = rotation);
        }

        public float GetFillScale()
        {
            return material != null ? material.GetFloat("_FillScale") : 1f; // Default fill scale if material is not available
        }

        public void SetFillScale(float fillScale, AnimationManager.Animation self)
        {
            SetIfOwned("FillScale", self, () =>
            {
                if (material != null)
                {
                    ShaderManager.SetFloat(material, "_FillScale", fillScale);
                }
            });
        }
    }
}