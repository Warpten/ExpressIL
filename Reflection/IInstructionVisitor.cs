using DBClientFiles.NET.Utils.Reflection.Instructions;
using System.Linq.Expressions;

namespace DBClientFiles.NET.Utils.Reflection
{
    public interface IInstructionVisitor
    {
        Expression Visit(ref Instruction instruction);
    }
}
