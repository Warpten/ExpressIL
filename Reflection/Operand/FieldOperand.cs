using System.Reflection;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class FieldOperand : IOperand
    {
        public FieldInfo FieldInfo { get; }
        public OperandType OperandType { get; } = OperandType.InlineField;

        public FieldOperand(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }
    }
}
