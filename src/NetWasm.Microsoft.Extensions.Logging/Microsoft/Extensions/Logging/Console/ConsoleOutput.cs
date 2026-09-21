// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    internal static class ConsoleOutput
    {
        public static void WriteSimple<TState>(TextWriter writer, string category, LogLevel level, EventId eventId,
            TState state, string message, Exception? exception, List<object?> scopes, ConsoleLoggerOptions options)
        {
            SimpleConsoleFormatterOptions formatter = options.SimpleFormatterOptions;
            string? timestampFormat = formatter.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTimeOffset timestamp = formatter.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                writer.Write(timestamp.ToString(timestampFormat, CultureInfo.InvariantCulture));
                writer.Write(' ');
            }

            writer.Write(GetShortLevel(level));
            writer.Write(" ");
            writer.Write(category);
            writer.Write('[');
            writer.Write(eventId.Id.ToString(CultureInfo.InvariantCulture));
            writer.Write("]");

            AppendActivity(writer, leadingSeparator: true);
            if (formatter.SingleLine)
            {
                if (scopes.Count > 0)
                {
                    writer.Write(" => ");
                    WriteScopes(writer, scopes);
                }
                writer.Write(" ");
                writer.Write(message);
                if (exception != null)
                {
                    writer.Write(" ");
                    writer.Write(exception);
                }
            }
            else
            {
                writer.WriteLine();
                if (scopes.Count > 0)
                {
                    writer.Write("      => ");
                    WriteScopes(writer, scopes);
                    writer.WriteLine();
                }
                writer.Write("      ");
                writer.Write(message.Replace(Environment.NewLine, Environment.NewLine + "      "));
                if (exception != null)
                {
                    writer.WriteLine();
                    writer.Write("      ");
                    writer.Write(exception);
                }
            }

            writer.WriteLine();
        }

        public static void WriteJson<TState>(TextWriter writer, string category, LogLevel level, EventId eventId,
            TState state, string message, Exception? exception, List<object?> scopes, JsonConsoleFormatterOptions formatter)
        {
            var json = new StringBuilder(256);
            json.Append('{');
            bool first = true;
            AppendJsonProperty(json, "EventId", eventId.Id, ref first);
            AppendJsonProperty(json, "LogLevel", level.ToString(), ref first);
            AppendJsonProperty(json, "Category", category, ref first);
            AppendJsonProperty(json, "Message", message, ref first);

            Activity? activity = Activity.Current;
            if (activity != null)
            {
                AppendJsonProperty(json, "TraceId", activity.GetTraceId(), ref first);
                AppendJsonProperty(json, "SpanId", activity.GetSpanId(), ref first);
            }

            if (exception != null)
            {
                AppendJsonProperty(json, "Exception", exception.ToString(), ref first);
            }

            if (state is IEnumerable<KeyValuePair<string, object?>> stateValues)
            {
                AppendJsonObjectStart(json, "State", ref first);
                bool stateFirst = true;
                foreach (KeyValuePair<string, object?> item in stateValues)
                {
                    AppendJsonProperty(json, item.Key, item.Value, ref stateFirst);
                }
                json.Append('}');
            }

            if (formatter.IncludeScopes && scopes.Count > 0)
            {
                AppendComma(json, ref first);
                AppendJsonString(json, "Scopes");
                json.Append(':').Append('[');
                for (int i = 0; i < scopes.Count; i++)
                {
                    if (i > 0) json.Append(',');
                    AppendJsonString(json, scopes[i]?.ToString());
                }
                json.Append(']');
            }

            json.Append('}');
            writer.WriteLine(json.ToString());
        }

        private static string GetShortLevel(LogLevel level) => level switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none"
        };

        private static void AppendActivity(TextWriter writer, bool leadingSeparator)
        {
            Activity? activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            if (leadingSeparator) writer.Write(' ');
            writer.Write("TraceId=");
            writer.Write(activity.GetTraceId());
            writer.Write(" SpanId=");
            writer.Write(activity.GetSpanId());
        }

        private static void WriteScopes(TextWriter writer, List<object?> scopes)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                if (i > 0) writer.Write(" => ");
                writer.Write(scopes[i]);
            }
        }

        private static void AppendJsonObjectStart(StringBuilder json, string name, ref bool first)
        {
            AppendComma(json, ref first);
            AppendJsonString(json, name);
            json.Append(':').Append('{');
        }

        private static void AppendJsonProperty(StringBuilder json, string name, object? value, ref bool first)
        {
            AppendComma(json, ref first);
            AppendJsonString(json, name);
            json.Append(':');
            AppendJsonValue(json, value);
        }

        private static void AppendJsonValue(StringBuilder json, object? value)
        {
            switch (value)
            {
                case null:
                    json.Append("null");
                    break;
                case bool boolean:
                    json.Append(boolean ? "true" : "false");
                    break;
                case float single when !float.IsFinite(single):
                    AppendJsonString(json, single.ToString(CultureInfo.InvariantCulture));
                    break;
                case double number when !double.IsFinite(number):
                    AppendJsonString(json, number.ToString(CultureInfo.InvariantCulture));
                    break;
                case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    json.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                default:
                    AppendJsonString(json, Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void AppendComma(StringBuilder json, ref bool first)
        {
            if (!first) json.Append(',');
            first = false;
        }

        private static void AppendJsonString(StringBuilder json, string? value)
        {
            json.Append('"');
            if (value != null)
            {
                foreach (char character in value)
                {
                    switch (character)
                    {
                        case '"': json.Append("\\\""); break;
                        case '\\': json.Append("\\\\"); break;
                        case '\b': json.Append("\\b"); break;
                        case '\f': json.Append("\\f"); break;
                        case '\n': json.Append("\\n"); break;
                        case '\r': json.Append("\\r"); break;
                        case '\t': json.Append("\\t"); break;
                        default:
                            if (character < ' ')
                            {
                                json.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                json.Append(character);
                            }
                            break;
                    }
                }
            }
            json.Append('"');
        }
    }
}
