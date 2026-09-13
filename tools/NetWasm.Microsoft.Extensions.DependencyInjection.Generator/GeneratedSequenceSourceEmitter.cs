using System.Text;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class GeneratedSequenceSourceEmitter : IGeneratedSequenceSourceEmitter
{
    public string Emit(GeneratedSequenceModel sequence)
    {
        Guard.NotNull(sequence, nameof(sequence));
        var builder = new StringBuilder();
        builder.Append("new GeneratedSequenceDescriptor(typeof(")
            .Append(sequence.SequenceTypeDisplay)
            .Append("), typeof(")
            .Append(sequence.ElementTypeDisplay)
            .Append("), static values => { var result = new ")
            .Append(sequence.ElementTypeDisplay)
            .Append("[values.Count]; for (var index = 0; index < values.Count; index++) { result[index] = (")
            .Append(sequence.ElementTypeDisplay)
            .Append(")values[index]!; } return result; }");
        if (sequence.ServiceKeyExpression is not null)
        {
            builder.Append(", serviceKey: ").Append(sequence.ServiceKeyExpression);
        }

        return builder.Append(')').ToString();
    }
}
