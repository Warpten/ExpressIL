using DBClientFiles.NET.Utils.Reflection.Instructions;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class BranchTargetOperand : IOperand
    {
        public uint BranchTarget { get; }
        public Instruction Target { get; set; }
        public OperandType OperandType { get; } = OperandType.InlineBrTarget;

        public BranchTargetOperand(uint branchTarget)
        {
            BranchTarget = branchTarget;
        }
    }
}
