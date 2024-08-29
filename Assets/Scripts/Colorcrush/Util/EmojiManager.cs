// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

#endregion

namespace Colorcrush.Util
{
    public class EmojiManager : MonoBehaviour
    {
        private static EmojiManager _instance;

        private Sprite _defaultEmojiSprite;
        private Sprite _defaultHappyEmojiSprite;
        private Dictionary<string, Sprite> _emojiDictionary;

        private Queue<Sprite> _happyEmojiQueue;
        private Queue<Sprite> _sadEmojiQueue;

        public static EmojiManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EmojiManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("EmojiManager");
                        _instance = go.AddComponent<EmojiManager>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEmojiQueues();
                InitializeEmojiDictionary();
            }
        }

        public static void InitializeEmojiQueues()
        {
            Instance._happyEmojiQueue = CreateEmojiQueue(ProjectConfig.InstanceConfig.happyEmojiFolder);
            Instance._sadEmojiQueue = CreateEmojiQueue(ProjectConfig.InstanceConfig.sadEmojiFolder);
        }

        private static Queue<Sprite> CreateEmojiQueue(string folderPath)
        {
            var emojis = Resources.LoadAll<Sprite>(folderPath);
            var emojiList = new List<Sprite>(emojis);

            // Remove the default emoji from the list if it's in this folder
            emojiList.RemoveAll(emoji => emoji.name == ProjectConfig.InstanceConfig.defaultEmojiName);

            // Shuffle the list using the random seed from ProjectConfig
            var random = new Random(ProjectConfig.InstanceConfig.randomSeed);
            emojiList = emojiList.OrderBy(x => random.Next()).ToList();

            return new Queue<Sprite>(emojiList);
        }

        private void InitializeEmojiDictionary()
        {
            _emojiDictionary = new Dictionary<string, Sprite>();
            var happyEmojis = Resources.LoadAll<Sprite>(ProjectConfig.InstanceConfig.happyEmojiFolder);
            var sadEmojis = Resources.LoadAll<Sprite>(ProjectConfig.InstanceConfig.sadEmojiFolder);

            foreach (var emoji in happyEmojis.Concat(sadEmojis))
            {
                _emojiDictionary[emoji.name] = emoji;
            }
        }

        public static Sprite GetNextHappyEmoji()
        {
            return GetNextEmoji(Instance._happyEmojiQueue);
        }

        public static Sprite GetNextSadEmoji()
        {
            return GetNextEmoji(Instance._sadEmojiQueue);
        }

        public static Sprite GetDefaultEmoji()
        {
            if (Instance._defaultEmojiSprite != null)
            {
                return Instance._defaultEmojiSprite;
            }

            // Try to load the default emoji from both folders
            Instance._defaultEmojiSprite = Resources.Load<Sprite>($"{ProjectConfig.InstanceConfig.happyEmojiFolder}/{ProjectConfig.InstanceConfig.defaultEmojiName}");
            if (Instance._defaultEmojiSprite == null)
            {
                Instance._defaultEmojiSprite = Resources.Load<Sprite>($"{ProjectConfig.InstanceConfig.sadEmojiFolder}/{ProjectConfig.InstanceConfig.defaultEmojiName}");
            }

            if (Instance._defaultEmojiSprite == null)
            {
                Debug.LogError("EmojiManager: Default emoji '{ProjectConfig.InstanceConfig.defaultEmojiName}' not found in either Happy or Sad folder.");
            }

            return Instance._defaultEmojiSprite;
        }

        public static Sprite GetDefaultHappyEmoji()
        {
            if (Instance._defaultHappyEmojiSprite != null)
            {
                return Instance._defaultHappyEmojiSprite;
            }

            Instance._defaultHappyEmojiSprite = Resources.Load<Sprite>($"{ProjectConfig.InstanceConfig.happyEmojiFolder}/{ProjectConfig.InstanceConfig.defaultHappyEmojiName}");

            if (Instance._defaultHappyEmojiSprite == null)
            {
                Debug.LogError("EmojiManager: Default happy emoji '{ProjectConfig.InstanceConfig.defaultHappyEmojiName}' not found in Happy folder.");
            }

            return Instance._defaultHappyEmojiSprite;
        }

        private static Sprite GetNextEmoji(Queue<Sprite> queue)
        {
            if (queue.Count == 0)
            {
                Debug.LogWarning("EmojiManager: Emoji queue is empty. Reinitializing...");
                InitializeEmojiQueues();
            }

            var nextEmoji = queue.Dequeue();
            queue.Enqueue(nextEmoji); // Add back onto the end for roll-over behavior
            return nextEmoji;
        }

        public static Sprite GetEmojiByName(string emojiName)
        {
            if (Instance._emojiDictionary.TryGetValue(emojiName, out var emoji))
            {
                return emoji;
            }

            Debug.LogWarning($"EmojiManager: Emoji with name '{emojiName}' not found. Returning default emoji.");
            return GetDefaultEmoji();
        }

        public static Sprite GetRewardEmojiForColor(Color targetColor)
        {
            var targetColorHex = ColorUtility.ToHtmlStringRGB(targetColor);
            var completedColors = ProgressManager.CompletedTargetColors;
            var rewardedEmojis = ProgressManager.RewardedEmojis;

            // Check if the color has been completed before
            var colorIndex = completedColors.IndexOf(targetColorHex);
            if (colorIndex != -1 && colorIndex < rewardedEmojis.Count)
            {
                // Return the previously rewarded emoji for this color
                return GetEmojiByName(rewardedEmojis[colorIndex]);
            }

            // If the color hasn't been completed, find a new emoji to reward
            var usedEmojis = new HashSet<string>(rewardedEmojis);
            while (Instance._happyEmojiQueue.Count > 0)
            {
                var nextEmoji = GetNextHappyEmoji();
                if (!usedEmojis.Contains(nextEmoji.name))
                {
                    return nextEmoji;
                }
            }

            // If we've exhausted all happy emojis, return the default happy emoji
            Debug.LogWarning("EmojiManager: All happy emojis have been used. Returning default happy emoji.");
            return GetDefaultHappyEmoji();
        }
    }
}