// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class TapAnimationObject : AnimationManager.Animation
    {
        private const float LerpDuration = 0.1f; // Very short lerp duration
        private readonly float _shrinkScale;

        public TapAnimationObject(float duration, float shrinkScale)
        {
            Duration = duration;
            IsTemporary = true;
            _shrinkScale = shrinkScale;
        }

        public override void Play(Animator animator, float progress)
        {
            var originalScale = animator.GetOriginalScale();
            var targetScale = originalScale * _shrinkScale;
            var lerpProgress = Mathf.Clamp01(progress / LerpDuration);
            animator.SetScale(Vector3.Lerp(originalScale, targetScale, lerpProgress));
        }
    }
}