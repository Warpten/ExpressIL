using DBClientFiles.NET.Utils.Reflection.Instructions;
using DBClientFiles.NET.Utils.Reflection.Operand;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DBClientFiles.NET.Utils.Reflection
{
    public class MethodParser
    {
        public MethodInfo MethodInfo { get; }
        public Instruction Instruction { get; }

        public Type DeclaringType => MethodInfo.DeclaringType;
        public Module Module => MethodInfo.Module;

        public IList<LocalVariableInfo> LocalVariables => MethodInfo.GetMethodBody().LocalVariables;

        public bool IsCompilerGenerated { get; }

        public MethodParser(MethodInfo methodInfo)
        {
            IsCompilerGenerated = methodInfo.IsDefined(typeof(CompilerGeneratedAttribute), false);

            MethodInfo = methodInfo;
            Instruction = Parse(methodInfo);
        }

        // Static helpers below.
        static MethodParser()
        {
            var opcodeFields = typeof(OpCodes).GetFields();
            _opcodes = new Dictionary<short, OpCode>(opcodeFields.Length);

            foreach (var fieldInfo in opcodeFields)
            {
                OpCode opcode = (OpCode)fieldInfo.GetValue(null);
                _opcodes[opcode.Value] = opcode;
            }
        }

        private static T Read<T>(byte[] input, ref uint offset) where T : unmanaged
        {
            unsafe
            {
                var value = *(T*)(Unsafe.AsPointer(ref input[offset]));
                offset += (uint) UnsafeCache<T>.Size;
                return value;
            }
        }

        private static Dictionary<short, OpCode> _opcodes;

        private static Instruction Parse(MethodInfo methodInfo)
        {
            Instruction head = null;
            Instruction tail = null;

            var data = methodInfo.GetMethodBody().GetILAsByteArray();

            var offset = 0u;
            while (offset < data.Length)
            {
                IOperand operand = null;

                short opcodeValue = data[offset++];
                if (opcodeValue == 0xFE)
                    opcodeValue |= (short) data[offset++];

                OpCode opcode = _opcodes[opcodeValue];

                switch (opcode.OperandType)
                {
                    case OperandType.InlineBrTarget:
                    {
                        var branchTarget = Read<uint>(data, ref offset) + offset;
                        operand = new BranchTargetOperand(branchTarget);
                        break;
                    }
                    case OperandType.InlineField:
                    {
                        var metadataToken = Read<int>(data, ref offset);
                        try {
                            var fieldInfo = methodInfo.Module.ResolveField(metadataToken);
                            operand = new FieldOperand(fieldInfo);
                        } catch (Exception) {
                            operand = new FieldOperand(null);
                        }
                        break;
                    }
                    case OperandType.InlineI:
                        operand = new ImmediateOperand<uint>(opcode.OperandType, Read<uint>(data, ref offset));
                        break;
                    case OperandType.InlineI8:
                        operand = new ImmediateOperand<ulong>(opcode.OperandType, Read<ulong>(data, ref offset));
                        break;
                    case OperandType.InlineMethod:
                    {
                        var metadataToken = Read<int>(data, ref offset);
                        try {
                            var inlineMethodInfo = methodInfo.Module.ResolveMethod(metadataToken);
                            operand = new MethodOperand(inlineMethodInfo);
                        } catch (Exception) {
                            operand = new MethodOperand(null);
                        }
                        break;
                    }
                    case OperandType.InlineNone:
                        break;
                    case OperandType.InlineR:
                        operand = new ImmediateOperand<double>(opcode.OperandType, Read<double>(data, ref offset));
                        break;
                    case OperandType.InlineSig:
                    {
                        var metadataToken = Read<int>(data, ref offset);
                        try {
                            var blobData = methodInfo.Module.ResolveSignature(metadataToken);
                            operand = new DataBlobOperand(blobData);
                        } catch (Exception) {
                            operand = new DataBlobOperand(null);
                        }
                        break;
                    }
                    case OperandType.InlineString:
                    {
                        var metadataToken = Read<int>(data, ref offset);
                        try {
                            var stringInfo = methodInfo.Module.ResolveString(metadataToken);
                            operand = new StringOperand(stringInfo);
                        } catch (Exception) {
                            operand = new StringOperand(null);
                        }
                        break;
                    }
                    case OperandType.InlineSwitch:
                    {
                        var count = Read<uint>(data, ref offset);
                        var tableOffset = offset;
                        var switchTargets = new uint[count];
                        for (var i = 0; i < count; ++i)
                            switchTargets[i] = Read<uint>(data, ref offset) + tableOffset * count * 4;
                        throw new NotImplementedException();
                    }
                    case OperandType.InlineTok:
                    {
                        var metadataToken = Read<int>(data, ref offset);
                        try {
                            var tokenRef = methodInfo.Module.ResolveType(metadataToken);
                            operand = new TypeOperand(opcode.OperandType, tokenRef);
                        } catch (Exception) {
                            operand = new TypeOperand(opcode.OperandType, null);
                        }
                        break;
                    }
                    case OperandType.InlineType:
                    {
                        var metadataToken = Read<int>(data, ref offset);
                        try {
                            var tokenRef = methodInfo.Module.ResolveType(metadataToken, methodInfo.DeclaringType.GetGenericArguments(), methodInfo.GetGenericArguments());
                            operand = new TypeOperand(opcode.OperandType, tokenRef);
                        } catch (Exception) {
                            operand = new TypeOperand(opcode.OperandType, null);
                        }
                        break;
                    }
                    case OperandType.InlineVar:
                    {
                        var variableOrdinal = Read<ushort>(data, ref offset);
                        operand = new VariableOperand(methodInfo.GetMethodBody().LocalVariables[variableOrdinal]);
                        break;
                    }
                }

                var instruction = new Instruction(opcode, operand);

                if (head == null)
                    head = tail = instruction;
                else
                    tail = tail.Next = instruction;
            }

            // Fix branch instruction targets
            tail = head;
            while (tail != null)
            {
                if (tail.Operand is BranchTargetOperand branchTargetOperand)
                {
                    var next = tail.Next;
                    while (next != null && next.Offset != branchTargetOperand.BranchTarget)
                        next = next.Next;
                    branchTargetOperand.Target = next ?? throw new InvalidProgramException("Unable to resolve branch target");
                }
                tail = tail.Next;
            }

            return head;
        }
    }
}
