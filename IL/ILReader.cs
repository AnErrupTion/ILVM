using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ILVM.IL
{
    internal class ILReader
    {
        public Dictionary<MethodInfo, List<ILInstruction>> Instructions { get; }

        public Assembly Assembly { get; }

        private int Position;
        private readonly byte[]? IL;

        public ILReader(string path)
        {
            Helpers.Initialize();

            Instructions = new();
            Position = 0;
            Assembly = Assembly.LoadFrom(path);

            foreach (var type in Assembly.GetTypes())
                foreach (var method in type.GetMethods((BindingFlags)(-1)))
                {
                    var instructions = new List<ILInstruction>();

                    IL = method.GetMethodBody()?.GetILAsByteArray();

                    while (Position < IL?.Length)
                    {
                        OpCode code;
                        ushort value = IL[Position++];

                        if (value == 0xfe)
                        {
                            value = IL[Position++]; // Second byte
                            code = Helpers.Multi[value];
                        }
                        else code = Helpers.Single[value];

                        var module = method.Module;
                        var inst = new ILInstruction()
                        {
                            Code = code,
                            Offset = Position - 1
                        };

                        switch (code.OperandType)
                        {
                            case OperandType.InlineBrTarget:
                                inst.Operand = ReadInt32() + Position;
                                break;

                            case OperandType.InlineField:
                                inst.Operand = module.ResolveField((int)ReadInt32());
                                break;

                            case OperandType.InlineMethod:
                                var token = (int)ReadInt32();
                                try { inst.Operand = module.ResolveMethod(token); } catch { inst.Operand = module.ResolveMember(token); }
                                break;

                            case OperandType.InlineSig:
                                inst.Operand = module.ResolveSignature((int)ReadInt32());
                                break;

                            case OperandType.InlineTok:
                                try { inst.Operand = module.ResolveType((int)ReadInt32()); } catch { }
                                break;

                            case OperandType.InlineType:
                                // Now we call the ResolveType always using the generic attributes type in order
                                // to support decompilation of generic methods and classes
                                // Thanks to the guys from code project who commented on this missing feature
                                inst.Operand = module.ResolveType((int)ReadInt32(), method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments());
                                break;

                            case OperandType.InlineI:
                                inst.Operand = ReadInt32();
                                break;

                            case OperandType.InlineI8:
                                inst.Operand = ReadInt64();
                                break;

                            case OperandType.InlineNone:
                                inst.Operand = null;
                                break;

                            case OperandType.InlineR:
                                inst.Operand = ReadDouble();
                                break;

                            case OperandType.InlineString:
                                inst.Operand = module.ResolveString((int)ReadInt32());
                                break;

                            case OperandType.InlineSwitch:
                                var count = (int)ReadInt32();

                                var casesAddresses = new int?[count];
                                for (int i = 0; i < count; i++)
                                    casesAddresses[i] = ReadInt32();

                                var cases = new int?[count];
                                for (int i = 0; i < count; i++)
                                    cases[i] = Position + casesAddresses[i];
                                break;

                            case OperandType.InlineVar:
                                inst.Operand = ReadUInt16();
                                break;

                            case OperandType.ShortInlineBrTarget:
                                inst.Operand = ReadSByte() + Position;
                                break;

                            case OperandType.ShortInlineI:
                                inst.Operand = ReadSByte();
                                break;

                            case OperandType.ShortInlineR:
                                inst.Operand = ReadSingle();
                                break;

                            case OperandType.ShortInlineVar:
                                inst.Operand = ReadByte();
                                break;

                            default:
                                throw new Exception("Unknown operand type.");
                        }

                        instructions.Add(inst);
                    }

                    Instructions.Add(method, instructions);
                }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var inst in Instructions)
                builder.AppendLine(inst.ToString());

            return builder.ToString();
        }

        private ushort? ReadUInt16()
        {
            return (ushort?)(IL?[Position++] | (IL?[Position++] << 8));
        }

        private int? ReadInt32()
        {
            return IL?[Position++] | (IL?[Position++] << 8) | (IL?[Position++] << 0x10) | (IL?[Position++] << 0x18);
        }

        private ulong? ReadInt64()
        {
            return (ulong?)(IL?[Position++] | (IL?[Position++] << 8) | (IL?[Position++] << 0x10) | (IL?[Position++] << 0x18) | (IL?[Position++] << 0x20) | (IL?[Position++] << 0x28) | (IL?[Position++] << 0x30) | (IL?[Position++] << 0x38));
        }

        private double? ReadDouble()
        {
            return IL?[Position++] | (IL?[Position++] << 8) | (IL?[Position++] << 0x10) | (IL?[Position++] << 0x18) | (IL?[Position++] << 0x20) | (IL?[Position++] << 0x28) | (IL?[Position++] << 0x30) | (IL?[Position++] << 0x38);
        }

        private sbyte? ReadSByte()
        {
            return (sbyte?)IL?[Position++];
        }

        private byte? ReadByte()
        {
            return IL?[Position++];
        }

        private float? ReadSingle()
        {
            return IL?[Position++] | (IL?[Position++] << 8) | (IL?[Position++] << 0x10) | (IL?[Position++] << 0x18);
        }
    }
}
