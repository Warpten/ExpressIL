using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    public interface IOperand
    {
        OperandType OperandType { get; }
    }
}
