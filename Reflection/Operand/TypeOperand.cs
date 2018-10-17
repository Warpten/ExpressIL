using System;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class TypeOperand : IOperand
    {
        public Type Type { get; }
        public OperandType OperandType { get; }

        public TypeOperand(OperandType operandType, Type type)
        {
            Type = type;
            OperandType = OperandType;
        }
    }
}
