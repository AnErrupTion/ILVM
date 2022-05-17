using System.Reflection;
using System.Reflection.Emit;
using static ILVM.IL.Helpers;

namespace ILVM.IL
{
    internal class ILInstruction
    {
        public OpCode Code { get; set; }

        public object? Operand { get; set; }

        public int Offset { get; set; }

        public override string ToString()
        {
            var result = $"{GetExpandedOffset(Offset)} : {Code}";

            switch (Code.OperandType)
            {
                case OperandType.InlineField:
                    var fOperand = (FieldInfo?)Operand;
                    result += $" {ProcessSpecialTypes(fOperand?.FieldType)} {ProcessSpecialTypes(fOperand?.ReflectedType)}::{fOperand?.Name}";
                    break;

                case OperandType.InlineMethod:
                    try
                    {
                        var mOperand = (MethodInfo?)Operand;

                        result += " ";
                        if (mOperand?.IsStatic == false)
                            result += "instance ";

                        result += $"{ProcessSpecialTypes(mOperand?.ReturnType)} {ProcessSpecialTypes(mOperand?.ReflectedType)}::{mOperand?.Name}()";
                    }
                    catch
                    {
                        try
                        {
                            var mOperand = (ConstructorInfo?)Operand;

                            result += " ";
                            if (mOperand?.IsStatic == false)
                                result += "instance ";

                            result += $"void {ProcessSpecialTypes(mOperand?.ReflectedType)}::{mOperand?.Name}()";
                        } catch { }
                    }
                    break;

                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    result += $" {GetExpandedOffset((int?)Operand)}";
                    break;

                case OperandType.InlineType:
                    result += $" {ProcessSpecialTypes(Operand)}";
                    break;

                case OperandType.InlineString:
                    var str = Operand?.ToString();

                    if (str == "\r\n")
                        result += " \"\\r\\n\"";
                    else
                        result += $" \"{str}\"";
                    break;

                case OperandType.InlineI:
                case OperandType.InlineI8:
                case OperandType.InlineR:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineR:
                case OperandType.ShortInlineVar:
                    result += Operand?.ToString();
                    break;

                case OperandType.InlineTok:
                    if (Operand is Type type)
                        result += type.FullName;
                    else
                        result += "not supported";
                    break;

                /*default:
                    result += "not supported";
                    break;*/
            }

            return result;
        }
    }
}
