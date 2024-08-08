// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Colorcrush.Animation
{
    public class ButtonAnimator : Animator
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;
        [SerializeField] private Button button;

        protected override void Awake()
        {
            base.Awake();

            // Log error if any component is missing
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform component is not assigned in the inspector for {gameObject.name}.");
            }

            if (image == null)
            {
                Debug.LogError($"Image component is not assigned in the inspector for {gameObject.name}.");
            }

            if (button == null)
            {
                Debug.LogError($"Button component is not assigned in the inspector for {gameObject.name}.");
            }
        }

        public override Vector3 GetPosition()
        {
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform is null for {gameObject.name}. Cannot get position.");
                return Vector3.zero;
            }

            return rectTransform.anchoredPosition3D;
        }

        public override void SetPosition(Vector3 position)
        {
            rectTransform.anchoredPosition3D = position;
        }

        public override Quaternion GetRotation()
        {
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform is null for {gameObject.name}. Cannot get rotation.");
                return Quaternion.identity;
            }

            return rectTransform.localRotation;
        }

        public override void SetRotation(Quaternion rotation)
        {
            rectTransform.localRotation = rotation;
        }

        public override Vector3 GetScale()
        {
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform is null for {gameObject.name}. Cannot get scale.");
                return Vector3.one;
            }

            return rectTransform.localScale;
        }

        public override void SetScale(Vector3 scale)
        {
            rectTransform.localScale = scale;
        }

        public override float GetOpacity()
        {
            if (image == null)
            {
                Debug.LogError($"Image is null for {gameObject.name}. Cannot get opacity.");
                return 1f;
            }

            return image.color.a;
        }

        public override void SetOpacity(float opacity)
        {
            var color = image.color;
            color.a = opacity;
            image.color = color;
        }

        public string GetButtonText()
        {
            if (button == null || button.GetComponentInChildren<Text>() == null)
            {
                Debug.LogError($"Button or Text component is null for {gameObject.name}. Cannot get button text.");
                return string.Empty;
            }

            return button.GetComponentInChildren<Text>().text;
        }

        public void SetButtonText(string text)
        {
            if (button == null || button.GetComponentInChildren<Text>() == null)
            {
                Debug.LogError($"Button or Text component is null for {gameObject.name}. Cannot set button text.");
                return;
            }

            button.GetComponentInChildren<Text>().text = text;
        }

        public bool IsInteractable()
        {
            if (button == null)
            {
                Debug.LogError($"Button component is null for {gameObject.name}. Cannot check interactability.");
                return false;
            }

            return button.interactable;
        }

        public void SetInteractable(bool interactable)
        {
            if (button == null)
            {
                Debug.LogError($"Button component is null for {gameObject.name}. Cannot set interactability.");
                return;
            }

            button.interactable = interactable;
        }
    }
}