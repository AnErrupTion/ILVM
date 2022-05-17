using ILVMGUI.Console;
using ILVM.IL;

namespace ILVMGUI
{
    public partial class Debugger : Form
    {
        public Debugger()
        {
            InitializeComponent();

            System.Console.SetIn(new TextBoxReader(textBox2));
            System.Console.SetOut(new TextBoxWriter(textBox2));
        }

        #region External thread operations

        public string[] GetCode()
        {
            return richTextBox1.Invoke(() => richTextBox1.Lines);
        }

        public int GetSleepValue()
        {
            return (int)numericUpDown1.Invoke(() => numericUpDown1.Value);
        }

        public void Highlightline(string[] lines, int line, Color color)
        {
            if (line < 0 || line >= lines.Length)
                return;

            richTextBox1.Invoke(() =>
            {
                richTextBox1.SelectAll();
                richTextBox1.SelectionBackColor = richTextBox1.BackColor;

                var start = richTextBox1.GetFirstCharIndexFromLine(line);
                var length = lines[line].Length;

                richTextBox1.Select(start, length);
                richTextBox1.SelectionBackColor = color;
            });
        }

        public void AddSkip()
        {
            label4.Invoke(() => label4.Text = (int.Parse(label4.Text) + 1).ToString());
        }

        public void Reset(bool start = false)
        {
            button2.Invoke(() => button2.Enabled = !button2.Enabled);
            button3.Invoke(() => button3.Enabled = !button3.Enabled);
            button1.Invoke(() => button1.Text = start ? "Stop" : "Start");
            listBox1.Invoke(() => listBox1.Enabled = !listBox1.Enabled);

            if (start)
            {
                button1.Invoke(() => button1.Text = "Stop");
                label4.Invoke(() => label4.Text = "0");
                textBox2.Invoke(() => textBox2.Clear());
                richTextBox1.Invoke(() => richTextBox1.Focus());
            }
        }

        #endregion

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Program.Reader = new ILReader(openFileDialog1.FileName);

            foreach (var method in Program.Reader.Instructions.Keys)
                listBox1.Items.Add(method.Name);
            listBox1.SelectedIndex = 0;

            button1.Enabled = true;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            foreach (var inst in Program.Reader.Instructions[Program.Reader.Instructions.Keys.ElementAt(listBox1.SelectedIndex)])
                richTextBox1.AppendText(inst + Environment.NewLine);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                Reset(true);
                Interpreter.Start();
            }
            else if (button1.Text == "Stop")
            {
                Reset();
                Interpreter.Stop();
            }
        }
    }
}