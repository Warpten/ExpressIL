using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class StringOperand : IOperand
    {
        public string String { get; }
        public OperandType OperandType { get; } = OperandType.InlineString;

        public StringOperand(string @string)
        {
            String = @string;
        }
    }
}
