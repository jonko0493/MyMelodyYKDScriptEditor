using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyMelodyYKDScriptEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Scr Scr { get; set; } = new Scr();
        public Htx Htx { get; set; } = new Htx();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadHtx_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SCR file|*.scr"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string scrFile = openFileDialog.FileName;
                string htxFile = scrFile.Replace(".scr", ".htx");

                Htx = Htx.ParseFromFile(htxFile);
                Scr = Scr.ParseFromFile(scrFile, Htx);
                scrBox.ItemsSource = Scr.Commands;
            }
        }
        private void SaveHtx_Click(object sender, RoutedEventArgs e)
        {
            SaveHtx_Click_Async(sender, e).GetAwaiter().GetResult();
        }
        private async Task SaveHtx_Click_Async(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SCR file|*.scr",
                AddExtension = true
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string scrFile = saveFileDialog.FileName;
                string htxFile = scrFile.Replace(".scr", ".htx");

                var htx = await Scr.WriteToFile(scrFile);
                await htx.WriteToFile(htxFile);
            }
        }

        private void DialogueCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new DialogueCommand());
        }

        private void DeleteCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Do you want to delete {scrBox.SelectedItem}?", "Deletion Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Scr.Commands.RemoveAt(scrBox.SelectedIndex);
            }
        }

        private void ScrBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            commandDataPanel.Children.Clear();

            if (e.AddedItems.Count == 0)
            {
                return;
            }

            Type type = e.AddedItems[0].GetType();

            if (type == typeof(DialogueCommand))
            {
                var dialogueCommand = (DialogueCommand)e.AddedItems[0];
                DialogueTextBox dialogueBox = new DialogueTextBox { Text = dialogueCommand.Dialogue, Command = dialogueCommand };
                dialogueBox.TextChanged += DialogueBox_TextChanged;

                commandDataPanel.Children.Add(dialogueBox);
            }
            else if (type == typeof(WaitCommand))
            {
                var waitCommand = (WaitCommand)e.AddedItems[0];
                WaitTextBox waitBox = new WaitTextBox(waitCommand);
                waitBox.TextChanged += WaitBox_TextChanged;

                commandDataPanel.Children.Add(waitBox);
            }
            else if (type == typeof(SoundCommand))
            {
                var soundCommand = (SoundCommand)e.AddedItems[0];

                SoundComboBox soundBox = new SoundComboBox
                {
                    Command = soundCommand,
                };
                foreach (string sound in SoundCommand.SoundToByteMap.Keys)
                {
                    soundBox.Items.Add(sound);
                }
                soundBox.SelectedItem = soundCommand.Sound;
                soundBox.SelectionChanged += SoundBox_SelectionChanged;

                commandDataPanel.Children.Add(soundBox);
            }
            else if (type == typeof(FadeInCommand))
            {
                commandDataPanel.Children.Add(new Label { Content = "No customization options." });
            }
            else if (type == typeof(TransitionCommand))
            {

            }
            else if (type == typeof(BackgroundCommand))
            {

            }
            else if (type == typeof(CharacterCommand))
            {

            }
            else if (type == typeof(EndCommand))
            {

            }
        }

        private void SoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (SoundComboBox)sender;
            box.Command.Sound = (string)box.SelectedItem;
            scrBox.Items.Refresh();
        }

        private void WaitBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (WaitTextBox)sender;
            if (double.TryParse(box.Text, out double parsed))
            {
                box.Command.TimeInHundredthsSeconds = (ushort)(parsed * 100);
                scrBox.Items.Refresh();
            }
        }

        private void DialogueBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (DialogueTextBox)sender;
            box.Command.Dialogue = box.Text;
            scrBox.Items.Refresh();
        }
    }
}
