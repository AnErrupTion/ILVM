using System.Reflection;
using static ILVM.IL.Helpers;

namespace ILVMGUI
{
    internal static class Interpreter
    {
        // Unrelated to MSIL
        private static CancellationTokenSource? Source;
        private static readonly List<string> Strings = new();
        private static readonly Stack<object?> Arguments = new();

        // Related to MSIL
        private static readonly Stack<object?> EvaluationStack = new();
        private static readonly object?[] LocalVariableList = new object?[4];

        public static void Start()
        {
            Source = new();

            Task.Run(() =>
            {
                Arguments.Clear();
                Strings.Clear();
                EvaluationStack.Clear();

                var lines = Program.Instance.GetCode();

                int i;
                for (i = 0; i < lines.Length; i++)
                {
                    if (Source.IsCancellationRequested)
                        return;

                    Program.Instance.Highlightline(lines, i, Color.Green);

                    var line = lines[i];

                    // Parse text
                    var text = Parse1(line);
                    var opCode = text.Item1;
                    var operand = text.Item2;

                    // Execute
                    if (opCode == "ldstr")
                    {
                        var str = operand[1..^1];
                        Arguments.Push(str);
                        Strings.Add(str);
                    }
                    else if (opCode == "call")
                    {
                        var func = Parse2(operand);
                        var returnType = func.Item1;
                        var space = func.Item2;
                        var name = func.Item3;
                        var isStatic = func.Item4;

                        if (i > 0)
                        {
                            var previous = Parse1(lines[i - 1]);

                            // Somewhat "prioritize" those instructions' arguments
                            if (previous.Item1 == "call" || previous.Item1.Contains('.'))
                                Arguments.Push(EvaluationStack.Peek());
                        }

                        // Is this correct? (Currently used for string.Concat)
                        if (space == "System.String")
                        {
                            foreach (var str in Strings)
                                Arguments.Push(str);
                            Strings.Clear();
                        }

                        if (space.StartsWith("System"))
                            space += ", mscorlib";

                        var type = Type.GetType(space);
                        var meth = type?.GetMethod(name, (BindingFlags)(-1), Arguments.Select(x => x?.GetType()).ToArray());
                        
                        try
                        {
                            var result = meth?.Invoke(isStatic ? null : Activator.CreateInstance(type), Arguments.ToArray());

                            if (result != null && returnType != "System.Void")
                                EvaluationStack.Push(result);
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"[FAILED to execute ({space})::{name}(): {ex.InnerException?.Message}]");
                        }

                        Arguments.Clear();

                        // Is this correct? (Follow up to the last commented code)
                        var next = Parse2(Parse1(lines[i + 1]).Item2);
                        if (next.Item2 != "System.String")
                            Strings.Clear();
                    }
                    else if (opCode.StartsWith("stloc."))
                    {
                        var index = int.Parse(opCode.Split('.')[1]);
                        if (index > 3)
                            continue;
                        LocalVariableList[index] = EvaluationStack.Pop();
                    }
                    else if (opCode.StartsWith("ldloc."))
                    {
                        var index = int.Parse(opCode.Split('.')[1]);
                        if (index > 3)
                            continue;
                        EvaluationStack.Push(LocalVariableList[index]);
                    }
                    else if (opCode.StartsWith("ldc.i4."))
                    {
                        var data = opCode.Split('.')[2];
                        var value = 0;

                        if (data == "m1")
                            value = -1;
                        else
                            value = int.Parse(data);

                        if (value > 8)
                            continue;

                        EvaluationStack.Push(value);
                    }
                    else if (opCode.StartsWith("ldloca.s")) // Load address, not value, and at specific index, not on top of stack
                    {
                        EvaluationStack.Push(LocalVariableList[int.Parse(opCode.Split('s')[1])]);
                    }
                    else if (opCode == "newarr")
                    {
                        EvaluationStack.Push(new object?[(int)EvaluationStack.Pop()]);
                    }
                    else if (opCode == "dup")
                    {
                        EvaluationStack.Push(EvaluationStack.Peek());
                    }
                    else if (opCode == "stelem.ref")
                    {
                        var index = (int)EvaluationStack.Pop();
                        var array = (object?[]?)EvaluationStack.Pop();
                        array[index] = Arguments.Pop();
                        EvaluationStack.Push(array);
                    }
                    else if (opCode == "brtrue.s")
                    {
                        var obj = EvaluationStack.Pop();
                        if ((obj is bool b && b) || (obj is int j && j != 0) || obj != null)
                            i = lines.IndexOf(operand);

                        Program.Instance.AddSkip();
                    }
                    else if (opCode == "br.s")
                    {
                        i = lines.IndexOf(operand);
                        Program.Instance.AddSkip();
                    }
                    else if (opCode == "add")
                    {
                        var second = (int?)EvaluationStack.Pop();
                        var first = (int?)EvaluationStack.Pop();
                        EvaluationStack.Push(first + second);
                    }
                    else if (opCode == "ldnull") EvaluationStack.Push(null);
                    else if (opCode == "pop") EvaluationStack.Pop();
                    else if (opCode == "nop") { }
                    else if (opCode == "ret") break;

                    Thread.Sleep(Program.Instance.GetSleepValue());
                }

                Program.Instance.Reset();
            });
        }

        private static (string, string) Parse1(string line)
        {
            var text = line.Split(" : ")[1].Trim().Split(new string[] { " " }, 2, StringSplitOptions.None);
            var opCode = text[0];
            var operand = string.Empty;

            if (text.Length > 1)
                operand = text[1];

            return (opCode, operand);
        }

        private static (string, string, string, bool) Parse2(string operand)
        {
            if (string.IsNullOrEmpty(operand))
                return (string.Empty, string.Empty, string.Empty, false);

            var isStatic = true;
            if (operand.StartsWith("instance"))
            {
                isStatic = false;
                operand = operand.Remove(0, 9); // Count the ending whitespace
            }

            var split1 = operand.Split(' ');
            var returnType = ProcessSpecializedTypes(split1[0]);
            var function = split1[1];
            var split2 = function.Split("::");
            var space = ProcessSpecializedTypes(split2[0]);
            var name = split2[1];

            name = name.Remove(name.Length - 2);

            return (returnType, space, name, isStatic);
        }

        public static int IndexOf(this string[] array, string text)
        {
            for (var i = 0; i < array.Length; i++)
                if (array[i].StartsWith(text))
                    return i - 1;

            return -1;
        }

        public static void Stop()
        {
            Source?.Cancel();
        }
    }
}
