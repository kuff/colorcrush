// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class BumpAnimation : AnimationManager.Animation
    {
        private readonly float _duration;
        private readonly float _targetScaleFactor;

        public BumpAnimation(float duration, float targetScaleFactor)
        {
            Duration = duration;
            IsTemporary = true;
            _targetScaleFactor = targetScaleFactor;
            _duration = duration;
        }

        public override void Play(Animator animator, float progress)
        {
            var originalScale = animator.GetOriginalScale();
            var targetScale = originalScale * _targetScaleFactor;

            // Calculate eased progress
            var easedProgress = EaseInOutQuad(progress / _duration);

            // Determine if we're in the shrink or expand phase
            if (progress < _duration / 2)
            {
                // Shrink phase
                animator.SetScale(Vector3.Lerp(originalScale, targetScale, easedProgress * 2));
            }
            else
            {
                // Expand phase
                animator.SetScale(Vector3.Lerp(targetScale, originalScale, (easedProgress - 0.5f) * 2));
            }
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }
    }
}