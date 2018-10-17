using DBClientFiles.NET.Utils.Reflection.Operand;
using System.Reflection.Emit;

namespace DBClientFiles.NET.Utils.Reflection.Instructions
{
    public class Instruction
    {
        public OpCode Opcode { get; }
        public int Offset {
            get {
                if (_previous == null)
                    return 0;

                return _previous.Offset + _previous.Length;
            }
        }

        public IOperand Operand { get; }

        private Instruction _previous = null;
        private Instruction _next = null;

        public Instruction Previous {
            get => _previous;
            set {
                if (_previous != null)
                    _previous._next = value;

                value._previous = _previous;
                value._next = this;
                _previous = value;
            }
        }
        public Instruction Next {
            get => _next;
            set
            {
                if (_next != null)
                    _next._previous = value;

                value._next = _next;
                value._previous = this;
                _next = value;
            }
        }

        public Instruction(OpCode opcode, IOperand operand)
        {
            Opcode = opcode;
            Operand = operand;
        }

        public int Length => Opcode.Size;

        protected virtual string Arguments() => string.Empty;

        public override sealed string ToString() => $"{Offset:X4}: {Opcode.Name} {Operand?.ToString() ?? ""}";

        public T GetOperand<T>() where T : IOperand, class {
            return Operand as T;
        }
    }
}
