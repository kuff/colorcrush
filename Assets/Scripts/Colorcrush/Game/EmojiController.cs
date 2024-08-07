// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using Colorcrush.Util;
using UnityEngine;
using Random = System.Random;

#endregion

namespace Colorcrush.Game
{
    public class EmojiController : MonoBehaviour
    {
        private const string EmojiPath = "Colorcrush/Emoji";
        private const string HappyFolder = "Happy";
        private const string SadFolder = "Sad";

        [SerializeField] private string defaultEmojiName = "reshot-icon-blank-XN4TPFSGQ8";

        private Sprite _defaultEmojiSprite;

        private Queue<Sprite> _happyEmojiQueue;
        private Queue<Sprite> _sadEmojiQueue;

        private void Awake()
        {
            InitializeEmojiQueues();
        }

        public void InitializeEmojiQueues()
        {
            _happyEmojiQueue = CreateEmojiQueue(HappyFolder);
            _sadEmojiQueue = CreateEmojiQueue(SadFolder);
        }

        private Queue<Sprite> CreateEmojiQueue(string folderName)
        {
            var emojis = Resources.LoadAll<Sprite>($"{EmojiPath}/{folderName}");
            var emojiList = new List<Sprite>(emojis);

            // Remove the default emoji from the list if it's in this folder
            emojiList.RemoveAll(emoji => emoji.name == defaultEmojiName);

            // Shuffle the list using the random seed from ProjectConfig
            var random = new Random(ProjectConfig.InstanceConfig.randomSeed);
            emojiList = emojiList.OrderBy(x => random.Next()).ToList();

            return new Queue<Sprite>(emojiList);
        }

        public Sprite GetNextHappyEmoji()
        {
            return GetNextEmoji(_happyEmojiQueue);
        }

        public Sprite GetNextSadEmoji()
        {
            return GetNextEmoji(_sadEmojiQueue);
        }

        public Sprite GetDefaultEmoji()
        {
            if (_defaultEmojiSprite != null)
            {
                return _defaultEmojiSprite;
            }

            // Try to load the default emoji from both folders
            _defaultEmojiSprite = Resources.Load<Sprite>($"{EmojiPath}/{HappyFolder}/{defaultEmojiName}");
            if (_defaultEmojiSprite == null)
            {
                _defaultEmojiSprite = Resources.Load<Sprite>($"{EmojiPath}/{SadFolder}/{defaultEmojiName}");
            }

            if (_defaultEmojiSprite == null)
            {
                Debug.LogError($"Default emoji '{defaultEmojiName}' not found in either Happy or Sad folder.");
            }

            return _defaultEmojiSprite;
        }

        private Sprite GetNextEmoji(Queue<Sprite> queue)
        {
            if (queue.Count == 0)
            {
                Debug.LogWarning("Emoji queue is empty. Reinitializing...");
                InitializeEmojiQueues();
            }

            var nextEmoji = queue.Dequeue();
            queue.Enqueue(nextEmoji); // Add back to the end for roll-over
            return nextEmoji;
        }
    }
}