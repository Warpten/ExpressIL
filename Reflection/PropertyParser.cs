using DBClientFiles.NET.Utils.Reflection.Instructions;
using DBClientFiles.NET.Utils.Reflection.Operand;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DBClientFiles.NET.Utils.Reflection
{
    public class PropertyParser
    {
        public MethodParser Getter { get; }
        public MethodParser Setter { get; }

        public PropertyInfo PropertyInfo { get; }

        public PropertyParser(PropertyInfo propInfo)
        {
            PropertyInfo = propInfo;

            if (propInfo.GetGetMethod() != null)
                Getter = new MethodParser(propInfo.GetGetMethod());

            if (propInfo.GetSetMethod() != null)
                Setter = new MethodParser(propInfo.GetSetMethod());
        }

        private FieldInfo FindGetterBackingField()
        {
            FieldInfo fieldInfo = null;

            return null;
        }

        /// <summary>
        /// Tries really hard to find the backing field of a property.
        /// </summary>
        /// <returns></returns>
        public FieldInfo FindBackingField()
        {

            if (Getter != null)
            {
                if (Getter.IsCompilerGenerated) // && Setter.IsCompilerGenerated)
                {
                    // ldarg.0
                    // ldfld <prop>k_BackingField
                    // ret
                    Instruction head = Getter.Instruction;
                    if (head.Opcode == OpCodes.Ldarg_0)
                    {
                        if (head.Next != null && head.Next.Opcode == OpCodes.Ldfld)
                        {
                            if (head.Next.Next != null && head.Next.Next.Operand is FieldOperand fieldOperand)
                            {
                                if (fieldOperand.FieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), false))
                                    return fieldOperand.FieldInfo;
                            }
                        }
                    }
                }
                else
                {
                }
            }

            return null;
        }
    }
}
