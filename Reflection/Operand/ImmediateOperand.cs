using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class ImmediateOperand<T> : IOperand where T : unmanaged
    {
        public T Immediate { get; }
        public OperandType OperandType { get; }

        public ImmediateOperand(OperandType operandType, T immediate)
        {
            OperandType = operandType;
            Immediate = immediate;
        }
    }
}
