using System.Text;
using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class ActivationManifestEmitter : IActivationManifestEmitter
{
    private readonly IActivationDescriptorLocator _locator;
    private readonly IGeneratedSequenceSourceEmitter _sequenceEmitter;

    internal ActivationManifestEmitter(
        IActivationDescriptorLocator locator,
        IGeneratedSequenceSourceEmitter sequenceEmitter)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _sequenceEmitter = sequenceEmitter ?? throw new ArgumentNullException(nameof(sequenceEmitter));
    }

    public string Emit(IReadOnlyList<ActivationModel> activations)
        => Emit(activations, Array.Empty<GeneratedSequenceModel>());

    public string Emit(
        IReadOnlyList<ActivationModel> activations,
        IReadOnlyList<GeneratedSequenceModel> sequences)
        => Emit(
            activations,
            sequences,
            Array.Empty<OpenGenericRegistrationInput>());

    public string Emit(
        IReadOnlyList<ActivationModel> activations,
        IReadOnlyList<GeneratedSequenceModel> sequences,
        IReadOnlyList<OpenGenericRegistrationInput> openGenericRegistrations)
    {
        Guard.NotNull(activations, nameof(activations));
        Guard.NotNull(sequences, nameof(sequences));
        Guard.NotNull(openGenericRegistrations, nameof(openGenericRegistrations));
        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection.Generated;");
        builder.AppendLine("namespace Generated;");
        builder.AppendLine("public static class GeneratedActivationManifestSource");
        builder.AppendLine("{");
        builder.AppendLine("    public static GeneratedActivationManifest Create()");
        builder.AppendLine("    {");
        builder.AppendLine("        return new GeneratedActivationManifest(new GeneratedActivationDescriptor[]");
        builder.AppendLine("        {");
        foreach (var activation in activations)
        {
            var located = _locator.Locate(activations, activation.Facts.Identity)
                ?? throw new GeneratorDiagnosticException("NWDI005", "Generated activation identity could not be located.");
            builder.Append("            ").Append(located.Facts.NamespaceName).Append('.').Append(located.Facts.TypeName).AppendLine(".Create(),");
        }

        builder.AppendLine("        },");
        builder.AppendLine("        new GeneratedSequenceDescriptor[]");
        builder.AppendLine("        {");
        for (var index = 0; index < sequences.Count; index++)
        {
            builder.Append("            ").Append(_sequenceEmitter.Emit(sequences[index])).AppendLine(index == sequences.Count - 1 ? string.Empty : ",");
        }

        builder.AppendLine("        },");
        builder.AppendLine("        new GeneratedOpenGenericRegistrationDescriptor[]");
        builder.AppendLine("        {");
        foreach (var registration in openGenericRegistrations)
        {
            Guard.NotNull(registration, nameof(openGenericRegistrations));
            builder.Append("            new GeneratedOpenGenericRegistrationDescriptor(typeof(")
                .Append(registration.ServiceType.ConstructUnboundGenericType()
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append("), typeof(")
                .Append(registration.ImplementationType.ConstructUnboundGenericType()
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append("), isKeyedService: ")
                .Append(registration.ServiceKeyExpression is null ? "false" : "true")
                .Append(", serviceKey: ")
                .Append(registration.ServiceKeyExpression ?? "null")
                .AppendLine("),");
        }

        builder.AppendLine("        });");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static GeneratedActivationManifest Manifest { get; } = Create();");
        builder.AppendLine("}");
        return builder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
