using DBClientFiles.NET.Utils.Reflection.Instructions;
using System.Linq.Expressions;

namespace DBClientFiles.NET.Utils.Reflection
{
    public sealed class BranchBuilder : IInstructionVisitor
    {
        private bool _canVisit;
        private int _targetInstruction;

        public Expression Condition { get; set; }
        public Instruction TargetInstruction
        {
            set {
                _canVisit = true;
                _targetInstruction = value.Offset;

            }
        }

        private IInstructionVisitor TrueBlock { get; set; }

        public Expression Visit(ref Instruction instruction)
        {
            if (!_canVisit)
                return null;


        }
    }
}
