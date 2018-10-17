using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class MethodOperand : IOperand
    {
        public MethodBase MethodBase { get; }
        public OperandType OperandType { get; } = OperandType.InlineMethod;

        public MethodOperand(MethodBase methodInfo)
        {
            MethodBase = methodInfo;
        }

        public MethodInfo MethodInfo
        {
            get
            {
                if (MethodBase is MethodInfo methInfo)
                    return methInfo;

                // TODO: Generic arguments?
                return MethodBase.DeclaringType.GetMethod(MethodBase.Name, MethodBase.GetParameters().Select(p => p.ParameterType).ToArray());
            }
        }
    }
}
