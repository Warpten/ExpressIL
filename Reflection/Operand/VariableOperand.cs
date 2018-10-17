using System.Reflection;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class VariableOperand : IOperand
    {
        public LocalVariableInfo VariableInfo { get; }
        public OperandType OperandType { get; } = OperandType.InlineVar;

        public VariableOperand(LocalVariableInfo variableInfo)
        {
            VariableInfo = variableInfo;
        }
    }
}
