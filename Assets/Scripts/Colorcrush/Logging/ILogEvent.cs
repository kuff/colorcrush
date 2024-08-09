// Copyright (C) 2024 Peter Guld Leth

#region

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

    public class NewTargetColorEvent : ILogEvent
    {
        public NewTargetColorEvent(Color targetValue)
        {
            TargetValue = targetValue;
        }

        public Color TargetValue { get; }
        public string EventName => "newtargetvalue";

        public string GetStringifiedData()
        {
            return ColorUtility.ToHtmlStringRGBA(TargetValue);
        }
    }

    public class ColorGeneratedEvent : ILogEvent
    {
        public ColorGeneratedEvent(int buttonIndex, Color colorValue)
        {
            ButtonIndex = buttonIndex;
            ColorValue = ColorUtility.ToHtmlStringRGBA(colorValue);
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
        public ColorSelectedEvent(int buttonIndex, string sadSpriteName)
        {
            ButtonIndex = buttonIndex;
            SadSpriteName = sadSpriteName;
        }

        public int ButtonIndex { get; }
        public string SadSpriteName { get; }
        public string EventName => "colorselected";

        public string GetStringifiedData()
        {
            return $"{ButtonIndex} {SadSpriteName}";
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
        public string EventName => "colorssubmitted";

        public string GetStringifiedData()
        {
            return "";
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
}