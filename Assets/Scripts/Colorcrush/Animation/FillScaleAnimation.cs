// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush.Animation
{
    public class FillScaleAnimation : AnimationManager.Animation
    {
        private readonly float _targetFillScale;

        public FillScaleAnimation(float targetFillScale, float duration)
        {
            _targetFillScale = targetFillScale;
            Duration = duration;
            IsTemporary = false;
        }

        public override void Play(Animator animator, float progress)
        {
            var emojiAnimator = animator as EmojiAnimator;
            if (emojiAnimator == null)
            {
                Debug.LogError("FillScaleAnimation requires an EmojiAnimator.");
                return;
            }

            var startFillScale = emojiAnimator.GetFillScale();
            var easedProgress = EaseInOutQuad(progress);
            var newFillScale = Mathf.Lerp(startFillScale, _targetFillScale, easedProgress);
            emojiAnimator.SetFillScale(newFillScale, this);
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }
    }
}