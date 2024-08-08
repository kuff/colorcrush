// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class ScaleAnimationObject : AnimationManager.Animation
    {
        private readonly Vector3 _targetScale;

        public ScaleAnimationObject(Vector3 targetScale, float duration)
        {
            _targetScale = targetScale;
            Duration = duration;
            IsTemporary = false;
        }

        public override void Play(Animator animator, float progress)
        {
            var startScale = animator.transform.localScale;
            var easedProgress = EaseInOutQuad(progress);
            animator.transform.localScale = Vector3.Lerp(startScale, _targetScale, easedProgress);
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }
    }
}