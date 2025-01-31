// Copyright (C) 2025 Peter Guld Leth

#region

using System;
using System.Linq;
using Colorcrush.Game;
using UnityEngine;

#endregion

// ReSharper disable StringLiteralTypo

namespace Colorcrush.Logging
{
    public interface ILogEvent
    {
        string EventName { get; }
        string GetStringifiedData();
    }

    public class AppOpenedEvent : ILogEvent
    {
        public string EventName => "appopened";

        public string GetStringifiedData()
        {
            return "";
        }
    }

    public class AppStandbyEvent : ILogEvent
    {
        public string EventName => "appstandby";

        public string GetStringifiedData()
        {
            return "";
        }
    }

    public class AppClosedEvent : ILogEvent
    {
        public string EventName => "appclosed";

        public string GetStringifiedData()
        {
            return "";
        }
    }

    public class ResetEvent : ILogEvent
    {
        public string EventName => "reset";

        public string GetStringifiedData()
        {
            return "";
        }
    }

    public class GameLevelBeginEvent : ILogEvent
    {
        public GameLevelBeginEvent(Color targetColor)
        {
            TargetValue = targetColor;
        }

        public Color TargetValue { get; }
        public string EventName => "gamelevelbegun";

        public string GetStringifiedData()
        {
            return ColorUtility.ToHtmlStringRGB(TargetValue);
        }
    }

    public class GameLevelEndEvent : ILogEvent
    {
        public string EventName => "gamelevelend";

        public string GetStringifiedData()
        {
            return "";
        }
    }

    public class ColorGeneratedEvent : ILogEvent
    {
        public ColorGeneratedEvent(int buttonIndex, Color colorValue)
        {
            ButtonIndex = buttonIndex;
            ColorValue = ColorUtility.ToHtmlStringRGB(colorValue);
        }

        public int ButtonIndex { get; }
        public string ColorValue { get; }
        public string EventName => "colorsgenerated";

        public string GetStringifiedData()
        {
            return $"{ButtonIndex} {ColorValue}";
        }
    }

    public class ColorSelectedEvent : ILogEvent
    {
        public ColorSelectedEvent(int buttonIndex)
        {
            ButtonIndex = buttonIndex;
        }

        public int ButtonIndex { get; }
        public string EventName => "colorselected";

        public string GetStringifiedData()
        {
            return ButtonIndex.ToString();
        }
    }

    public class ColorDeselectedEvent : ILogEvent
    {
        public ColorDeselectedEvent(int buttonIndex)
        {
            ButtonIndex = buttonIndex;
        }

        public int ButtonIndex { get; }
        public string EventName => "colordeselected";

        public string GetStringifiedData()
        {
            return ButtonIndex.ToString();
        }
    }

    public class ColorsSubmittedEvent : ILogEvent
    {
        public ColorsSubmittedEvent(string targetColorHappySpriteName)
        {
            TargetColorHappySpriteName = targetColorHappySpriteName;
        }

        public string TargetColorHappySpriteName { get; }
        public string EventName => "colorssubmitted";

        public string GetStringifiedData()
        {
            return TargetColorHappySpriteName;
        }
    }

    public class ConsoleOutputEvent : ILogEvent
    {
        public ConsoleOutputEvent(LogType type, string message)
        {
            Type = type;
            Message = message;
        }

        public LogType Type { get; }
        public string Message { get; }
        public string EventName => "consoleoutput";

        public string GetStringifiedData()
        {
            return $"{Type} {Message}";
        }
    }

    public class EmojiRewardedEvent : ILogEvent
    {
        public EmojiRewardedEvent(string emojiName)
        {
            EmojiName = emojiName;
        }

        public string EmojiName { get; }
        public string EventName => "emojirewarded";

        public string GetStringifiedData()
        {
            return EmojiName;
        }
    }

    public class StartTimeEvent : ILogEvent
    {
        public StartTimeEvent(DateTime startTime)
        {
            StartTime = startTime;
        }

        public DateTime StartTime { get; }
        public string EventName => "starttime";

        public string GetStringifiedData()
        {
            return StartTime.ToString("o"); // ISO 8601 format
        }
    }

    public class FinalColorsEvent : ILogEvent
    {
        public FinalColorsEvent(ColorManager.ColorMatrixResult result)
        {
            Result = result;
        }

        public ColorManager.ColorMatrixResult Result { get; }
        public string EventName => "finalcolors";

        public string GetStringifiedData()
        {
            // Print both encodings and colors

            // Cast the result.finalColors.Vector255 to a Color (Unity) object and then to a string
            var colors = string.Join(" ", Result.FinalColors.Select(color => ColorUtility.ToHtmlStringRGB(color.ToDisplayColor())));

            // Encodings
            var encodings = string.Join(" ", Result.AxisEncodings.Select(encoding => encoding.ToString("F3").Replace(",", ";")));

            return $"{colors} {encodings}";
        }
    }

    public class SkinColorModeEvent : ILogEvent
    {
        public SkinColorModeEvent(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        public bool IsEnabled { get; }
        public string EventName => "skincolormode";

        public string GetStringifiedData()
        {
            return IsEnabled.ToString().ToLower();
        }
    }
}