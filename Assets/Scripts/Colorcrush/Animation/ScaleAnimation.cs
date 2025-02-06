// Copyright (C) 2025 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class ScaleAnimation : AnimationManager.Animation
    {
        private readonly Vector3 _targetScale;

        public ScaleAnimation(Vector3 targetScale, float duration)
        {
            _targetScale = targetScale;
            Duration = duration;
            IsTemporary = false;
        }

        public override void Play(CustomAnimator customAnimator, float progress)
        {
            var startScale = customAnimator.GetScale();
            var easedProgress = EaseInOutQuad(progress);
            customAnimator.SetScale(Vector3.Lerp(startScale, _targetScale, easedProgress), this);
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }
    }
}