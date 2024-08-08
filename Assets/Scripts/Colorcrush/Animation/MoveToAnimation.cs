// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class MoveToAnimation : AnimationManager.Animation
    {
        private readonly Vector3 _targetPosition;

        public MoveToAnimation(Vector3 targetPosition, float duration)
        {
            _targetPosition = targetPosition;
            Duration = duration;
            IsTemporary = false;
        }

        public override void Play(Animator animator, float progress)
        {
            var startPosition = animator.transform.position;
            var easedProgress = EaseInOutQuad(progress);
            animator.transform.position = Vector3.Lerp(startPosition, _targetPosition, easedProgress);
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }
    }
}