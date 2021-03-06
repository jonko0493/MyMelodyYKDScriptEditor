using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new DialogueCommand { DialogueLines = new List<string>() });
        }
        private void WaitCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new WaitCommand { TimeInHundredthsSeconds = 0 });
        }
        private void soundCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new SoundCommand { Sound = SoundCommand.SoundToByteMap.First().Key });
        }
        private void fadeInCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new FadeInCommand { Bytes = new byte[] { 0, 0, 0, 0 } });
        }
        private void transitionCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new TransitionCommand { UnknownByte = 0, Transition = TransitionCommand.TransitionToByteMap.First().Key, Speed = 1 });
        }
        private void backgroundCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new BackgroundCommand { UnknownByte = 0, Background = BackgroundCommand.FileToByteMap.First().Key });
        }
        private void characterCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new CharacterCommand { CharacterName = CharacterCommand.NameToByteMap.First().Key, CharacterPosition = CharacterCommand.Position.LEFT_FACING_RIGHT });
        }
        private void endCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scr.Commands.Insert(scrBox.SelectedIndex + 1, new EndCommand());
        }

        private void DeleteCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (scrBox.SelectedIndex >= 0 && MessageBox.Show($"Do you want to delete {scrBox.SelectedItem}?", "Deletion Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                DialogueTextBox[] dialogueBoxes = new DialogueTextBox[3];
                for (int i = 0; i < dialogueBoxes.Length; i++)
                {
                    string dialogueLine = dialogueCommand.DialogueLines.Count <= i ? "" : dialogueCommand.DialogueLines[i];
                    dialogueBoxes[i] = new DialogueTextBox
                    {
                        Text = dialogueLine,
                        LineNumber = i,
                        Command = dialogueCommand,
                        MaxLength = 12, // number of characters per line in game's text boxes
                    };
                    dialogueBoxes[i].TextChanged += DialogueBox_TextChanged;
                    commandDataPanel.Children.Add(dialogueBoxes[i]);
                }
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
                var transitionCommand = (TransitionCommand)e.AddedItems[0];

                TransitionComboBox transitionBox = new TransitionComboBox
                {
                    Command = transitionCommand,
                };
                foreach (string transition in TransitionCommand.TransitionToByteMap.Keys)
                {
                    transitionBox.Items.Add(transition);
                }
                transitionBox.SelectedItem = transitionCommand.Transition;
                transitionBox.SelectionChanged += TransitionBox_SelectionChanged;

                commandDataPanel.Children.Add(transitionBox);
            }
            else if (type == typeof(BackgroundCommand))
            {
                var backgroundCommand = (BackgroundCommand)e.AddedItems[0];

                BackgroundComboBox backgroundBox = new BackgroundComboBox
                {
                    Command = backgroundCommand,
                };
                foreach (string background in BackgroundCommand.FileToByteMap.Keys)
                {
                    backgroundBox.Items.Add(background);
                }
                backgroundBox.SelectedItem = backgroundCommand.Background;

                backgroundBox.BackgroundImage = GetImageAsResource(backgroundCommand.Background);

                backgroundBox.SelectionChanged += BackgroundBox_SelectionChanged;

                commandDataPanel.Children.Add(backgroundBox);
                commandDataPanel.Children.Add(backgroundBox.BackgroundImage);
            }
            else if (type == typeof(CharacterCommand))
            {
                var characterCommand = (CharacterCommand)e.AddedItems[0];

                CharacterComboBox characterBox = new CharacterComboBox
                {
                    Command = characterCommand,
                };
                foreach (string character in CharacterCommand.NameToByteMap.Keys)
                {
                    characterBox.Items.Add(character);
                }
                characterBox.SelectedItem = characterCommand.CharacterName;
                characterBox.SelectionChanged += CharacterBox_SelectionChanged;

                CharacterComboBox positionBox = new CharacterComboBox
                {
                    Command = characterCommand,
                };
                foreach (string position in Enum.GetNames(typeof(CharacterCommand.Position)))
                {
                    positionBox.Items.Add(position);
                }
                positionBox.SelectedItem = Enum.GetName(typeof(CharacterCommand.Position), characterCommand.CharacterPosition);
                positionBox.SelectionChanged += PositionBox_SelectionChanged;

                commandDataPanel.Children.Add(characterBox);
                commandDataPanel.Children.Add(positionBox);
            }
            else if (type == typeof(EndCommand))
            {
                commandDataPanel.Children.Add(new Label { Content = "No customization options." });
            }
        }

        private Image GetImageAsResource(string imageName)
        {
            return new Image
            {
                Source = new BitmapImage(new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name}" +
                    $";component/bg/{imageName}", UriKind.Absolute)),
            };
        }

        private void DialogueBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (DialogueTextBox)sender;
            if (string.IsNullOrEmpty(box.Text))
            {
                box.Command.DialogueLines.RemoveAt(box.LineNumber);
            }
            else if (box.LineNumber >= box.Command.DialogueLines.Count)
            {
                box.Command.DialogueLines.Add(box.Text);
            }
            else
            {
                box.Command.DialogueLines[box.LineNumber] = box.Text;
            }
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

        private void SoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (SoundComboBox)sender;
            box.Command.Sound = (string)box.SelectedItem;
            scrBox.Items.Refresh();
        }

        private void TransitionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (TransitionComboBox)sender;
            box.Command.Transition = (string)box.SelectedItem;
            scrBox.Items.Refresh();
        }

        private void BackgroundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (BackgroundComboBox)sender;
            box.Command.Background = (string)box.SelectedItem;
            box.BackgroundImage = GetImageAsResource(box.Command.Background);
            scrBox.Items.Refresh();
        }

        private void CharacterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (CharacterComboBox)sender;
            box.Command.CharacterName = (string)box.SelectedItem;
            scrBox.Items.Refresh();
        }

        private void PositionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (CharacterComboBox)sender;
            box.Command.CharacterPosition = (CharacterCommand.Position)Enum.Parse(typeof(CharacterCommand.Position), (string)box.SelectedItem);
            scrBox.Items.Refresh();
        }
    }
}
