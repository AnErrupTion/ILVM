using System.Text;

namespace ILVMGUI.Console
{
    internal class TextBoxReader : TextReader
    {
        private readonly TextBox Input;
        private readonly StringBuilder Builder;

        private bool Done, OneKey;

        public TextBoxReader(TextBox input)
        {
            Input = input;
            Builder = new StringBuilder();

            Input.KeyPress += (object? sender, KeyPressEventArgs e) =>
            {
                if (OneKey)
                {
                    Builder.Append(e.KeyChar);
                    Done = true;
                }
                else
                {
                    try
                    {
                        if (e.KeyChar == (char)Keys.Enter) Done = true;
                        else if (e.KeyChar == (char)Keys.Back) Builder.Remove(Builder.Length - 1, 1);
                        else Builder.Append(e.KeyChar);
                    } catch { }
                }
            };
        }

        public override int Read()
        {
            OneKey = true;

            while (!Done) { }
            Done = false;

            var chr = Builder[0];
            Builder.Clear();

            return chr;
        }

        public override string? ReadLine()
        {
            OneKey = false;

            while (!Done) { }
            Done = false;

            var str = Builder.ToString();
            Builder.Clear();

            return str;
        }
    }
}
