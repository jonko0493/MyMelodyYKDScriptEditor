using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyMelodyYKDScriptEditor
{
    public class DialogueTextBox : TextBox
    {
        public DialogueCommand Command { get; set; }
    }

    public class WaitTextBox : TextBox
    {
        public WaitCommand Command { get; set; }

        public WaitTextBox(WaitCommand waitCommand)
        {
            Command = waitCommand;
            PreviewTextInput += ValidateText;
            Text = $"{waitCommand.TimeInHundredthsSeconds / 100.0}";
        }

        private void ValidateText(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^\d\.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }

    public class SoundComboBox : ComboBox
    {
        public SoundCommand Command { get; set; }
    }
}
