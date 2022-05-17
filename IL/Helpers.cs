using System.Reflection.Emit;

namespace ILVM.IL
{
    internal static class Helpers
    {
        public static OpCode[] Single = new OpCode[0x100], Multi = new OpCode[0x100];

        public static void Initialize()
        {
            foreach (var field in typeof(OpCodes).GetFields())
                if (field.FieldType == typeof(OpCode))
                {
                    var code = (OpCode)field.GetValue(null);
                    var value = (ushort)code.Value;

                    if (value > 0x100)
                    {
                        if ((value & 0xff00) != 0xfe00)
                            throw new Exception("Invalid OpCode.");

                        Multi[value & 0xff] = code;
                    } else Single[value] = code;
                }
        }

        public static string? ProcessSpecialTypes(object? type)
        {
            var name = type?.ToString();
            return name?.ToLower() switch
            {
                "system.string" => "string",
                "system.int32" or "int32" => "int",
                _ => name,
            };
        }

        public static string ProcessSpecializedTypes(string name)
        {
            return name switch
            {
                "string" => "System.String",
                "int" => "System.Int32",
                _ => name,
            };
        }

        public static string? GetExpandedOffset(long? offset)
        {
            var result = offset.ToString();

            for (var i = 0; result?.Length < 4; i++)
                result = $"0{result}";

            return result;
        }
    }
}
