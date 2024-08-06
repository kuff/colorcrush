// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Colorcrush.Game
{
    public class FilteredEmojiTracker
    {
        public readonly int TargetFilteredCount;
        public int FilteredEmojiCount;

        public FilteredEmojiTracker(int targetCount)
        {
            TargetFilteredCount = targetCount;
            FilteredEmojiCount = 0;
            TargetReached = false;
        }

        public bool TargetReached { get; private set; }

        public void TrackFilteredEmojis(List<(int buttonIndex, Material buttonMaterial)> filteredEmojis)
        {
            FilteredEmojiCount += filteredEmojis.Count;
            foreach (var (buttonIndex, buttonMaterial) in filteredEmojis)
            {
                Debug.Log($"Filtered emoji at index {buttonIndex} with material {buttonMaterial.name}.");
            }

            Debug.Log($"Total filtered: {FilteredEmojiCount}");

            if (FilteredEmojiCount >= TargetFilteredCount && !TargetReached)
            {
                TargetReached = true;
                Debug.Log($"Target of {TargetFilteredCount} filtered emojis reached!");
            }
        }

        public void Reset()
        {
            FilteredEmojiCount = 0;
            TargetReached = false;
        }
    }
}