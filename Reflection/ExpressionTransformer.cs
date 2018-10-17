using DBClientFiles.NET.Utils.Reflection.Instructions;
using DBClientFiles.NET.Utils.Reflection.Operand;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection
{
    public sealed class ExpressionTransformer : IInstructionVisitor
    {
        public MethodParser MethodParser { get; }

        private Expression[] _arguments;
        private Expression _currentExpression;

        private Stack<object> _evaluationStack;
        private Expression[] _variables;
        private BranchBuilder _branchBuilder;

        public ExpressionTransformer(MethodParser methodParser)
        {
            MethodParser = methodParser;

            int argumentIndex = 0;
            var parameters = methodParser.MethodInfo.GetParameters();

            if (!methodParser.MethodInfo.IsStatic)
                argumentIndex = 1;

            _arguments = new Expression[parameters.Length + argumentIndex];
            foreach (var parameterInfo in methodParser.MethodInfo.GetParameters())
                _arguments[argumentIndex++] = Expression.Variable(parameterInfo.ParameterType);

            var localVariables = methodParser.MethodInfo.GetMethodBody().LocalVariables;
            _variables = new Expression[localVariables.Count];
            for (var i = 0; i < _variables.Length; ++i)
                _variables[localVariables[i].LocalIndex] = Expression.Variable(localVariables[i].LocalType);

            _branchBuilder = new BranchBuilder();
            _evaluationStack = new Stack<object>();
        }

        public Expression Visit(ref Instruction instruction)
        {
            if (instruction == null)
                return null;

            return null;
        }

        private Instruction VisitAndUpdateBranch(Instruction instruction)
        {
            if (instruction.Opcode == OpCodes.Brtrue)
            {
                var testExpression = Expression.IsTrue(Pop<Expression>());
                Instruction targetInstruction = instruction.GetOperand<BranchTargetOperand>()?.Target;
            }

            return null;
        }

        /// <summary>
        /// <para>
        /// Parses stack push instructions.
        /// </para>
        /// </summary>
        /// <param name="instruction"></param>
        /// <returns></returns>
        /// <remarks>
        /// Not implemented instructions include:
        /// <list type="bullet">
        ///     <item>
        ///         <term>ldelem.ref</term>
        ///         <description>Loads the element containing an object reference at a specified array index onto the top of the evaluation stack as type O (object reference).</description>
        ///     </item>
        ///     <item>
        ///         <term>ldind.i</term>
        ///         <description>Loads a value of type native int as a native int onto the evaluation stack indirectly.</description>
        ///     </item>
        ///     <item>
        ///         <term>ldind.i1</term>
        ///     </item>
        ///     <item>
        ///         <term>ldind.i2</term>
        ///     </item>
        ///     <item>
        ///         <term>ldind.i4</term>
        ///     </item>
        ///     <item>
        ///         <term>ldind.i8</term>
        ///     </item>
        ///     <item>
        ///         <term>ldlen</term>
        ///     </item>
        ///     <item>
        ///         <term>ldtok</term>
        ///     </item>
        ///     <item>
        ///         <term>ldobj</term>
        ///     </item>
        ///     <item>
        ///         <term>ldvirtfn</term>
        ///     </item>
        /// </list>
        /// </remarks>
        private Instruction VisitAndUpdatePush(Instruction instruction)
        {
            if (instruction.Opcode.Value >= OpCodes.Ldarg_0.Value && instruction.Opcode.Value <= OpCodes.Ldarg_3.Value)
            {
                Push(_arguments[instruction.Opcode.Value - OpCodes.Ldarg_0.Value]);
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldarga_S)
            {
                var immediateOperand = instruction.Operand as ImmediateOperand<byte>;
                if (immediateOperand == null)
                    throw new InvalidProgramException();

                Push(_arguments[immediateOperand.Immediate]);
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldc_I4)
            {
                Push(Expression.Constant(instruction.GetOperand<ImmediateOperand<int>>().Immediate));
                return instruction.Next;
            }

            if (instruction.Opcode.Value >= OpCodes.Ldc_I4_0.Value && instruction.Opcode.Value <= OpCodes.Ldc_I4_8.Value)
            {
                var immediateValue = instruction.Opcode.Value - OpCodes.Ldc_I4_0.Value;
                Push(Expression.Constant(immediateValue));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldc_I4_M1)
            {
                Push(Expression.Constant(-1, typeof(int)));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldc_I4_S)
            {
                Push(Expression.Constant(instruction.GetOperand<ImmediateOperand<byte>>().Immediate));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldc_I8)
            {
                Push(Expression.Constant(instruction.GetOperand<ImmediateOperand<long>>().Immediate));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldc_R4)
            {
                Push(Expression.Constant(instruction.GetOperand<ImmediateOperand<float>>().Immediate));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldc_R8)
            {
                Push(Expression.Constant(instruction.GetOperand<ImmediateOperand<double>>().Immediate));
                return instruction.Next;
            }

            //! TODO: What's the difference between ldelem and ldelem.i ?
            if (instruction.Opcode.Value >= OpCodes.Ldelem.Value && instruction.Opcode.Value <= OpCodes.Ldelem_R8.Value)
            {
                var index = Pop<Expression>();
                var array = Pop<Expression>();
                Push(Expression.ArrayAccess(array, index));
                return instruction.Next;
            }

            //! TODO: ldelem.ref

            if (instruction.Opcode.Value >= OpCodes.Ldelem_U1.Value && instruction.Opcode.Value <= OpCodes.Ldelem_U4.Value)
            {
                var index = Pop<Expression>();
                var array = Pop<Expression>();
                Push(Expression.ArrayAccess(array, index));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldelema)
            {
                var index = Pop<Expression>();
                var array = Pop<Expression>();

                // We don't need to handle the MakeRef ourselves, apparently.
                var arrayAccess = Expression.ArrayAccess(array, index);
                Push(arrayAccess);
                return instruction.Next;
            }

            if (instruction.Opcode.Value == OpCodes.Ldfld.Value || instruction.Opcode.Value == OpCodes.Ldflda.Value)
            {
                var field = instruction.GetOperand<FieldOperand>().FieldInfo;
                var typeInfo = Pop<Expression>();
                Push(Expression.MakeMemberAccess(typeInfo, field));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldftn)
            {
                //! TODO: Pushes an unmanaged pointer (type native int) to the native code implementing
                //! a specific method onto the evaluation stack.
            }

            //! TODO: all the indirect loads
            //! TODO: ldlen

            if (instruction.Opcode == OpCodes.Ldloc || instruction.Opcode == OpCodes.Ldloca)
            {
                // Ldloca pushes a reference but expressions take care of it.

                var variableIndex = instruction.GetOperand<ImmediateOperand<ushort>>().Immediate;
                var variableInfo = _variables[variableIndex];
                Push(variableInfo);
                return instruction.Next;
            }

            if (instruction.Opcode.Value >= OpCodes.Ldloc_0.Value && instruction.Opcode.Value <= OpCodes.Ldloc_3.Value)
            {
                var variableIndex = instruction.Opcode.Value - OpCodes.Ldloc_0.Value;
                var variableInfo = _variables[variableIndex];
                Push(variableInfo);
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldloc_S || instruction.Opcode == OpCodes.Ldloca_S)
            {
                var variableIndex = instruction.GetOperand<ImmediateOperand<byte>>().Immediate;
                var variableInfo = _variables[variableIndex];
                Push(variableInfo);
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldnull)
            {
                Push(Expression.Constant(null));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldobj)
            {
                // Copies the value type object pointed to by an address to the top of the evaluation stack.
            }

            if (instruction.Opcode == OpCodes.Ldsfld || instruction.Opcode == OpCodes.Ldsflda)
            {
                var fieldInfo = instruction.GetOperand<FieldOperand>().FieldInfo;
                Push(Expression.Field(null, fieldInfo));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldstr)
            {
                var @string = instruction.GetOperand<StringOperand>().String;
                Push(Expression.Constant(@string));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Ldtoken)
            {
                // The ldtoken instruction pushes a RuntimeHandle for the specified metadata token.
                // A RuntimeHandle can be a fieldref/fielddef, a methodref/methoddef, or a typeref/typedef.
            }

            if (instruction.Opcode == OpCodes.Ldvirtftn)
            {
                // Pushes an unmanaged pointer (type native int) to the native code implementing a particular
                // virtual method associated with a specified object onto the evaluation stack.
            }

            return null;
        }

        private Instruction VisitAndUpdate(Instruction instruction)
        {
            if (instruction == null)
                return null;

            var result = VisitAndUpdatePush(instruction);
            if (result != null)
                return result;

            // Alphabetical order

            if (instruction.Opcode == OpCodes.Add)
            {
                Push(Expression.Add(Pop<Expression>(), Pop<Expression>()));
                return instruction.Next;
            }

            // add.ovf, add.ovf.un

            if (instruction.Opcode == OpCodes.And)
            {
                Push(Expression.And(Pop<Expression>(), Pop<Expression>()));
                return instruction.Next;
            }

            // branches ... big oops

            if (instruction.Opcode == OpCodes.Box)
            {
                // TODO Check this gives the same IL
                Push(Expression.Convert(Pop<Expression>(), typeof(object)));
                return instruction.Next;
            }

            // br, br.s

            // Not supported by expression trees (we have no debug symbols)
            if (instruction.Opcode == OpCodes.Break)
                return instruction.Next;

            // More branches... holy fuck

            if (instruction.Opcode == OpCodes.Call || instruction.Opcode == OpCodes.Callvirt)
            {
                var methodInfo = (instruction.Operand as MethodOperand).MethodInfo;
                if (!methodInfo.IsStatic)
                    throw new InvalidProgramException();

                // Pop arguments from the evaluation stack
                var parameters = methodInfo.GetParameters();

                var parameterExprs = new List<Expression>(parameters.Length);
                for (var i = parameters.Length - 1; i >= 0; --i)
                    parameterExprs[i] = Pop<Expression>();

                if (instruction.Opcode == OpCodes.Call)
                    Push(Expression.Call(methodInfo, parameterExprs));
                else if (instruction.Opcode == OpCodes.Callvirt)
                    Push(Expression.Call(Pop<Expression>(), methodInfo, parameterExprs));
                return instruction.Next;
            }

            // calli

            if (instruction.Opcode == OpCodes.Castclass)
            {
                var targetType = (instruction.Operand as TypeOperand).Type;

                Push(Expression.Convert(Pop<Expression>(), targetType));
                return instruction.Next;
            }

            // ceq, cgt, etc

            if (instruction.Opcode == OpCodes.Conv_I || instruction.Opcode == OpCodes.Conv_I4)
            {
                Push(Expression.Convert(Pop<Expression>(), typeof(int)));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Conv_I1)
            {
                Push(Expression.Convert(Expression.Convert(Pop<Expression>(), typeof(byte)), typeof(int)));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Conv_I8)
            {
                Push(Expression.Convert(Pop<Expression>(), typeof(long)));
                return instruction.Next;
            }

            if (instruction.Opcode == OpCodes.Conv_R_Un)
            {
            }

            return null;
        }

        private T Pop<T>() => (T) _evaluationStack.Pop();
        private void Push(object expr) => _evaluationStack.Push(expr);
    }
}
