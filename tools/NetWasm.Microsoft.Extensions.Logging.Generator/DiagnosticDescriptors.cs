// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    // The upstream generator obtains these descriptions from a generated resource
    // wrapper. Literal descriptors keep this analyzer self-contained for NetWasm.
    public static class DiagnosticDescriptors
    {
        private static DiagnosticDescriptor Create(string id, string message, DiagnosticSeverity severity)
            => new DiagnosticDescriptor(id, "Logging source generator", message, "LoggingGenerator", severity, isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodName { get; } = Create("SYSLIB1001", "The logging method name is invalid.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = Create("SYSLIB1002", "The log level should not be mentioned in the message template.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = Create("SYSLIB1003", "The logging method parameter name is invalid.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor MissingRequiredType { get; } = Create("SYSLIB1005", "The required logging type '{0}' is missing.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = Create("SYSLIB1006", "The event id is reused.", DiagnosticSeverity.Info);
        public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = Create("SYSLIB1007", "The logging method must return void.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor MissingLoggerArgument { get; } = Create("SYSLIB1008", "The logging method must have an ILogger argument.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = Create("SYSLIB1009", "The logging method should be static.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = Create("SYSLIB1010", "The logging method must be partial.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = Create("SYSLIB1011", "The logging method must not be generic.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = Create("SYSLIB1012", "The message template has a redundant qualifier.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = Create("SYSLIB1013", "The exception should not be mentioned in the message template.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = Create("SYSLIB1014", "The message template has no corresponding argument.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = Create("SYSLIB1015", "The argument has no corresponding message template item.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor LoggingMethodHasBody { get; } = Create("SYSLIB1016", "The logging method must not have a body.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor MissingLogLevel { get; } = Create("SYSLIB1017", "The logging method has no log level.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = Create("SYSLIB1018", "The logger should not be mentioned in the message template.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor MissingLoggerField { get; } = Create("SYSLIB1019", "The logger field is missing.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor MultipleLoggerFields { get; } = Create("SYSLIB1020", "Multiple logger fields were found.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor InconsistentTemplateCasing { get; } = Create("SYSLIB1021", "Message template item casing is inconsistent.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor MalformedFormatStrings { get; } = Create("SYSLIB1022", "The message template format is malformed.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor GeneratingForMax6Arguments { get; } = Create("SYSLIB1023", "The generated logging method supports at most six arguments.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor InvalidLoggingMethodParameterOut { get; } = Create("SYSLIB1024", "The logging method parameter cannot be out.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor ShouldntReuseEventNames { get; } = Create("SYSLIB1025", "The event name is reused.", DiagnosticSeverity.Warning);
        public static DiagnosticDescriptor LoggingUnsupportedLanguageVersion { get; } = Create("SYSLIB1026", "The language version is not supported by the logging generator.", DiagnosticSeverity.Error);
        public static DiagnosticDescriptor PrimaryConstructorParameterLoggerHidden { get; } = Create("SYSLIB1027", "The primary constructor logger parameter is hidden.", DiagnosticSeverity.Info);
    }
}
