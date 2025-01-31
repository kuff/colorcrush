// Copyright (C) 2025 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class MoveToAnimation : AnimationManager.Animation
    {
        private readonly Vector3? _endScale;
        private readonly Vector3 _targetPosition;

        public MoveToAnimation(Vector3 targetPosition, float duration, Vector3? endScale = null)
        {
            _targetPosition = targetPosition;
            _endScale = endScale;
            Duration = duration;
            IsTemporary = false;
        }

        public override void Play(Animator animator, float progress)
        {
            var easedProgress = EaseInOutQuad(progress);

            // Handle position animation
            var startPosition = animator.GetOriginalPosition();
            animator.SetPosition(Vector3.Lerp(startPosition, _targetPosition, easedProgress), this);

            // Handle scale animation if endScale is provided
            if (_endScale.HasValue)
            {
                var startScale = animator.GetOriginalScale();
                animator.SetScale(Vector3.Lerp(startScale, _endScale.Value, easedProgress), this);
            }
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }
    }
}