// Copyright (C) 2024 Peter Guld Leth

#region

using TMPro;
using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class TextAnimator : Animator
    {
        [SerializeField] private TextMeshProUGUI textComponent;

        protected override void Awake()
        {
            base.Awake();

            if (textComponent == null)
            {
                Debug.LogError($"TextMeshProUGUI component is not assigned in the inspector for {gameObject.name}.");
            }
        }

        public override Vector3 GetPosition()
        {
            if (textComponent == null || textComponent.rectTransform == null)
            {
                Debug.LogError($"TextMeshProUGUI or its RectTransform is null for {gameObject.name}. Cannot get position.");
                return Vector3.zero;
            }

            return textComponent.rectTransform.anchoredPosition3D;
        }

        public override void SetPosition(Vector3 position)
        {
            textComponent.rectTransform.anchoredPosition3D = position;
        }

        public override Quaternion GetRotation()
        {
            if (textComponent == null || textComponent.rectTransform == null)
            {
                Debug.LogError($"TextMeshProUGUI or its RectTransform is null for {gameObject.name}. Cannot get rotation.");
                return Quaternion.identity;
            }

            return textComponent.rectTransform.localRotation;
        }

        public override void SetRotation(Quaternion rotation)
        {
            textComponent.rectTransform.localRotation = rotation;
        }

        public override Vector3 GetScale()
        {
            if (textComponent == null || textComponent.rectTransform == null)
            {
                Debug.LogError($"TextMeshProUGUI or its RectTransform is null for {gameObject.name}. Cannot get scale.");
                return Vector3.one;
            }

            return textComponent.rectTransform.localScale;
        }

        public override void SetScale(Vector3 scale)
        {
            textComponent.rectTransform.localScale = scale;
        }

        public override float GetOpacity()
        {
            if (textComponent == null)
            {
                Debug.LogError($"TextMeshProUGUI is null for {gameObject.name}. Cannot get opacity.");
                return 1f;
            }

            return textComponent.alpha;
        }

        public override void SetOpacity(float opacity)
        {
            textComponent.alpha = opacity;
        }

        public void SetText(string text)
        {
            textComponent.text = text;
        }

        public string GetText()
        {
            if (textComponent == null)
            {
                Debug.LogError($"TextMeshProUGUI is null for {gameObject.name}. Cannot get text.");
                return string.Empty;
            }

            return textComponent.text;
        }

        public void SetFontSize(float fontSize)
        {
            textComponent.fontSize = fontSize;
        }

        public float GetFontSize()
        {
            if (textComponent == null)
            {
                Debug.LogError($"TextMeshProUGUI is null for {gameObject.name}. Cannot get font size.");
                return 0f;
            }

            return textComponent.fontSize;
        }

        public void SetColor(Color color)
        {
            textComponent.color = color;
        }

        public Color GetColor()
        {
            if (textComponent == null)
            {
                Debug.LogError($"TextMeshProUGUI is null for {gameObject.name}. Cannot get color.");
                return Color.white;
            }

            return textComponent.color;
        }
    }
}