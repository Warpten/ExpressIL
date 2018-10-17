using System;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Operand
{
    internal sealed class DataBlobOperand : IOperand
    {
        public byte[] Data { get; }
        public OperandType OperandType { get; } = OperandType.InlineSig;

        public DataBlobOperand(byte[] data)
        {
            Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    }
}
