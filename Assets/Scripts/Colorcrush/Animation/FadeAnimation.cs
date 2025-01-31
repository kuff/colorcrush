// Copyright (C) 2025 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class FadeAnimation : AnimationManager.Animation
    {
        private readonly float _endOpacity;
        private readonly float _startOpacity;

        public FadeAnimation(float endOpacity, float duration)
        {
            _startOpacity = 1f; // Assume starting from fully opaque
            _endOpacity = endOpacity;
            Duration = duration;
        }

        public FadeAnimation(float startOpacity, float endOpacity, float duration)
        {
            _startOpacity = startOpacity;
            _endOpacity = endOpacity;
            Duration = duration;
        }

        public override void Play(Animator animator, float progress)
        {
            var easedProgress = EaseInOutCubic(progress);
            var currentOpacity = Mathf.Lerp(_startOpacity, _endOpacity, easedProgress);
            animator.SetOpacity(currentOpacity, this);
        }

        private float EaseInOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }
}