using System.Text;

namespace ILVMGUI.Console
{
    internal class TextBoxWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.ASCII;

        private readonly TextBox Output;

        public TextBoxWriter(TextBox output)
        {
            Output = output;
        }

        public override void Write(char value)
        {
            Output.Invoke(() =>
            {
                Output.AppendText(value.ToString());
                Output.ScrollToCaret();
            });
        }

        public override void Write(string? value)
        {
            Output.Invoke(() =>
            {
                Output.AppendText(value);
                Output.ScrollToCaret();
            });
        }

        public override void WriteLine()
        {
            Output.Invoke(() =>
            {
                Output.AppendText(Environment.NewLine);
                Output.ScrollToCaret();
            });
        }

        public override void WriteLine(string? value)
        {
            Output.Invoke(() =>
            {
                Output.AppendText(value + Environment.NewLine);
                Output.ScrollToCaret();
            });
        }
    }
}
