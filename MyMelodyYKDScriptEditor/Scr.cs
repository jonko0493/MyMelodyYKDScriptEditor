using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMelodyYKDScriptEditor
{
    public class Scr
    {
        public ObservableCollection<IScrCommand> Commands { get; set; } = new ObservableCollection<IScrCommand>();

        public static Scr ParseFromFile(string file, Htx htx)
        {
            var commands = new ObservableCollection<IScrCommand>();
            byte[] data = File.ReadAllBytes(file);

            for (int i = 0; i < data.Length;)
            {
                if (data[i] != 0x23)
                {
                    throw new FileFormatException($"First byte of segment not 0x23 -- {data[i]:X2}");
                }
                else
                {
                    i++;
                }

                switch (data[i])
                {
                    case 0x00:
                        commands.Add(new DialogueCommand { Dialogue = htx.Lines[data[i + 1] + (data[i + 2] << 8)] });
                        i += 3;
                        break;

                    case 0x0F:
                        commands.Add(new WaitCommand
                        {
                            TimeInHundredthsSeconds = (ushort)(data[i + 1] + (data[i + 2] << 8)),
                        });
                        i += 3;
                        break;

                    case 0x13:
                        string sound = SoundCommand.SoundToByteMap
                            .FirstOrDefault(l => l.Value == data[i + 1] + (data[i + 2] << 8)).Key;
                        if (string.IsNullOrEmpty(sound))
                        {
                            throw new FileFormatException($"Encountered unknown sound 0x{data[i + 1] + (data[i + 2] << 8):X4}");
                        }
                        commands.Add(new SoundCommand
                        {
                            Sound = sound
                        });
                        i += 3;
                        break;

                    case 0x15:
                        commands.Add(new FadeInCommand
                        {
                            Bytes = new byte[] { data[i + 1], data[i + 2], data[i + 3], data[i + 4] }
                        });
                        i += 5;
                        break;

                    case 0x16:
                        string transition = TransitionCommand.TransitionToByteMap
                            .FirstOrDefault(l => l.Value == data[i + 2]).Key;
                        if (string.IsNullOrEmpty(transition))
                        {
                            throw new FileFormatException($"Encountered unknown transition 0x{data[i + 2]:X2}");
                        }
                        commands.Add(new TransitionCommand
                        {
                            UnknownByte = data[i + 1],
                            Transition = transition,
                            Speed = (short)(data[i + 3] + (data[i + 4] << 8))
                        });
                        i += 5;
                        break;

                    case 0x1B:
                        string background = BackgroundCommand.FileToByteMap
                            .FirstOrDefault(l => l.Value == data[i + 2]).Key;
                        if (string.IsNullOrEmpty(background))
                        {
                            throw new FileFormatException($"Encountered unknown background 0x{data[i + 2]:X4}");
                        }
                        commands.Add(new BackgroundCommand
                        {
                            Background = background,
                            UnknownByte = data[i + 1]
                        });
                        i += 3;
                        break;

                    case 0x1C:
                        string character = CharacterCommand.NameToByteMap
                            .FirstOrDefault(l => l.Value == data[i + 3] + (data[i + 4] << 8)).Key;
                        if (string.IsNullOrEmpty(character))
                        {
                            throw new FileFormatException($"Encountered unknown character 0x{data[i + 3] + (data[i + 4] << 8):X4}");
                        }
                        CharacterCommand.Position position = (CharacterCommand.Position)(data[i + 1] + (data[i + 2] << 8));
                        if (position > CharacterCommand.Position.RIGHT_FACING_LEFT && position < CharacterCommand.Position.MIDDLE_FACING_LEFT
                            || position > CharacterCommand.Position.RIGHT_FACING_RIGHT)
                        {
                            throw new FileFormatException($"Encountered unknown position 0x{(short)position:X4}");
                        }
                        commands.Add(new CharacterCommand
                        {
                            CharacterPosition = position,
                            CharacterName = character
                        });
                        i += 5;
                        break;

                    case 0xFE: // end code
                        commands.Add(new EndCommand());
                        i += 1;
                        break;

                    default:
                        throw new FileFormatException($"Encountered unknown opcode 0x{data[i]:X2}");
                }
            }

            return new Scr { Commands = commands };
        }

        public async Task<Htx> WriteToFile(string file)
        {
            List<byte> data = new List<byte>();
            Htx htx = new Htx();
            foreach (var command in Commands)
            {
                if (command.GetType() == typeof(DialogueCommand))
                {
                    ((DialogueCommand)command).DialogueIndex = htx.AddLine(((DialogueCommand)command).Dialogue);
                }
                data.AddRange(command.ToByteCode(htx));
            }
            await File.WriteAllBytesAsync(file, data.ToArray());
            return htx;
        }
    }

    public interface IScrCommand
    {
        public byte OpCode { get; }

        public byte[] ToByteCode(Htx htx);
    }

    public class DialogueCommand : IScrCommand, INotifyPropertyChanged
    {
        public byte OpCode => 0x00;

        public string Dialogue { get => string.Join('↓', DialogueLines); 
            set 
            {
                DialogueLines = value.Split('↓', StringSplitOptions.RemoveEmptyEntries).Take(3).ToList();
            }
        }

        public List<string> DialogueLines { get; set; }

        public short DialogueIndex { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return $"Dialogue: {Dialogue}";
        }

        public byte[] ToByteCode(Htx htx)
        {
            var dialogueBytes = BitConverter.GetBytes(DialogueIndex);
            return new byte[] { 0x23, OpCode, dialogueBytes[0], dialogueBytes[1] };
        }
    }

    public class BackgroundCommand : IScrCommand
    {
        public byte OpCode => 0x1B;
        public byte UnknownByte { get; set; }
        public string Background { get; set; }

        public byte[] ToByteCode(Htx htx)
        {
            return new byte[] { 0x23, OpCode, UnknownByte, FileToByteMap[Background] };
        }

        public override string ToString()
        {
            return $"Display Background '{Background}'";
        }

        public static Dictionary<string, byte> FileToByteMap = new Dictionary<string, byte>()
        {
            { "bg01.png", 0x01 },
            { "bg02.png", 0x02 },
            { "bg03.png", 0x03 },
            { "bg04.png", 0x04 },
            { "bg05.png", 0x05 },
            { "bg06.png", 0x06 },
            { "bg07.png", 0x07 },
            { "bg08.png", 0x08 },
        };
    }

    public class SoundCommand : IScrCommand
    {
        public byte OpCode => 0x13;
        public string Sound { get; set; }

        public byte[] ToByteCode(Htx htx)
        {
            var soundBytes = BitConverter.GetBytes(SoundToByteMap[Sound]);
            return new byte[] { 0x23, OpCode, soundBytes[0], soundBytes[1] };
        }

        public override string ToString()
        {
            return $"Play sound '{Sound}'";
        }

        public static Dictionary<string, short> SoundToByteMap = new Dictionary<string, short>()
        {
            { "Silence", 0x0000 },
            { "Title BGM", 0x0001 },
            { "Unknown BGM 02", 0x0002 },
            { "Unknown BGM 03", 0x0003 },
            { "Unknown BGM 04", 0x0004 },
            { "Unknown BGM 05", 0x0005 },
            { "Unknown BGM 06", 0x0006 },
            { "Unknown BGM 07", 0x0007 },
            { "Unknown BGM 08", 0x0008 },
            { "Unknown BGM 09", 0x0009 },
            { "Rolling Hills BGM", 0x000A },
            { "Silence 0B", 0x000B },
            { "Button Select SFX", 0x000C },
            { "Deselect SFX", 0x000D },
            { "Unknown SFX 0E", 0x000E },
            { "Unknown SFX 0F", 0x000F },
            { "Page Turn SFX", 0x0010 },
            { "Incorrect SFX", 0x0011 },
            { "Correct SFX", 0x0012 },
            { "Unknown SFX 13", 0x0013 },
            { "Unknown SFX 14", 0x0014 },
            { "Unknown SFX 15", 0x0015 },
            { "Win SFX", 0x0016 },
            { "Unknown SFX 17", 0x0017 },
            { "Unknown SFX 18", 0x0018 },
            { "Unknown SFX 19", 0x0019 },
            { "Unknown SFX 1A", 0x001A },
            { "Unknown SFX 1B", 0x001B },
            { "Unknown SFX 1C", 0x001C },
            { "Unknown SFX 1D", 0x001D },
            { "Unknown SFX 1E", 0x001E },
            { "Unknown SFX 1F", 0x001F },
            { "Unknown SFX 20", 0x0020 },
            { "Unknown SFX 21", 0x0021 },
            { "Dunk SFX", 0x0022 },
            { "Unknown SFX 23", 0x0023 },
            { "Unknown SFX 24", 0x0024 },
            { "Unknown SFX 25", 0x0025 },
            { "Unknown SFX 26", 0x0026 },
            { "Unknown SFX 27", 0x0027 },
            { "Wind SFX", 0x0028 },
            { "Unknown SFX 29", 0x0029 },
            { "Unknown SFX 2A", 0x002A },
            { "Unknown SFX 2B", 0x002B },
            { "Unknown Sting SFX 2C", 0x002C },
            { "My Melo: Ah, Kuromi-chan!", 0x002D },
            { "My Melo: Kuromi-chan!", 0x002E },
            { "My Melo: Kuromi-chaaaaan!", 0x002F },
            { "My Melo: Ah, Kuromi-chan! (2)", 0x0030 },
            { "My Melo: Kuromi-chan?", 0x0031 },
            { "My Melo: Kuromi-chaan!", 0x0032 },
            { "My Melo: Kuromi-chan! (2)", 0x0033 },
            { "Kuromi: Jaa ikuyo... hirake, yume no tobira!", 0x0034 },
            { "Kuromi: Ikuyo... hirake, yume no tobira!", 0x0035 },
            { "Kuromi: Hirakeeee, yume no tobira!", 0x0036 },
            { "Kuromi: Hirake, yume no tobira!", 0x0037 },
            { "Kuromi: Jaa ikuyo. Hirake, yume no tobira!", 0x0038 },
            { "Kuromi: Ikuyo! Hirake, yume no tobira!", 0x0039 },
            { "Kuromi: Hiirakee! Yume no tobira!", 0x003A },
            { "Kuromi: Hirake! Yume no tobira!", 0x003B },
            { "Unknown SFX 3C", 0x003C },
        };
    }

    public class CharacterCommand : IScrCommand
    {
        public enum Position
        {
            MIDDLE_FACING_RIGHT = 0x0000,
            LEFT_FACING_RIGHT = 0x0001,
            RIGHT_FACING_LEFT = 0x0002,
            MIDDLE_FACING_LEFT = 0x0100,
            LEFT_FACING_LEFT = 0x0101,
            RIGHT_FACING_RIGHT = 0x0102,
        }

        public byte OpCode => 0x1C;

        public string CharacterName { get; set; }
        public Position CharacterPosition { get; set; }

        public byte[] ToByteCode(Htx htx)
        {
            var characterBytes = BitConverter.GetBytes(NameToByteMap[CharacterName]);
            var positionBytes = BitConverter.GetBytes((short)CharacterPosition);

            return new byte[] { 0x23, OpCode, positionBytes[0], positionBytes[1], characterBytes[0], characterBytes[1] };
        }

        public override string ToString()
        {
            return $"Show charater '{CharacterName}' at '{CharacterPosition}'";
        }

        public static Dictionary<string, short> NameToByteMap = new Dictionary<string, short>()
        {
            { "Empty", 0x0000 },
            { "My Melo (Silent)", 0x0001 },
            { "My Melo (Talking)", 0x0002 },
            { "Uta (Silent)", 0x0003 },
            { "Uta (Talking)", 0x0004 },
            { "My Sweet Piano (Silent)", 0x0005 },
            { "My Sweet Piano (Talking)", 0x0006 },
            { "Flat (Silent)", 0x0007 },
            { "Flat (Talking)", 0x0008 },
            { "Kuromi (Silent)", 0x0009 },
            { "Kuromi (Talking)", 0x000A },
            { "Rhythm (Silent)", 0x000B },
            { "Rhythm (Talking)", 0x000C },
            { "Risu (Silent)", 0x000D },
            { "Risu (Talking)", 0x000E },
            { "Hedgehog (Silent)", 0x000F },
            { "Hedgehog (Talking)", 0x0010 },
            { "Kuma (Silent)", 0x0011 },
            { "Kuma (Talking)", 0x0012 },
            { "Grandpa (Silent)", 0x0013 },
            { "Grandpa (Talking)", 0x0014 },
            { "Baku (Silent)", 0x0015 },
            { "Baku (Talking)", 0x0016 },
        };
    }

    public class TransitionCommand : IScrCommand
    {
        public byte OpCode => 0x16;

        public byte UnknownByte { get; set; }
        public string Transition { get; set; }
        public short Speed { get; set; }

        public byte[] ToByteCode(Htx htx)
        {
            var speedBytes = BitConverter.GetBytes(Speed);
            return new byte[] { 0x23, OpCode, UnknownByte, TransitionToByteMap[Transition], speedBytes[0], speedBytes[1] }; // endianness
        }

        public override string ToString()
        {
            return $"Transition '{Transition}' at speed {Speed}";
        }

        public static Dictionary<string, byte> TransitionToByteMap = new Dictionary<string, byte>
        {
            { "Transparent Wipe Right (Clean Screen)", 0x02 },
            { "Transparent Wipe Right (Draw New)", 0x03 },
            { "Pink Wipe Left (Clean)", 0x04 },
            { "Pink Wipe Left", 0x05 },
            { "White Center Wipe Out", 0x06 },
            { "White Center Wipe In (Needs Fade In)", 0x07 },
            { "White Curtains Out (Transparent)", 0x0A },
            { "White Curtains Out (White)", 0x0B },
        };
    }

    public class WaitCommand : IScrCommand
    {
        public byte OpCode => 0x0F;

        public ushort TimeInHundredthsSeconds { get; set; }

        public byte[] ToByteCode(Htx htx)
        {
            var timeBytes = BitConverter.GetBytes(TimeInHundredthsSeconds);
            return new byte[] { 0x23, OpCode, timeBytes[0], timeBytes[1] };
        }

        public override string ToString()
        {
            return $"Wait {TimeInHundredthsSeconds / 100.0} seconds";
        }
    }

    public class FadeInCommand : IScrCommand
    {
        public byte OpCode => 0x15;

        public byte[] Bytes { get; set; }

        public byte[] ToByteCode(Htx htx)
        {
            return new byte[] { 0x23, OpCode, Bytes[0], Bytes[1], Bytes[2], Bytes[3] };
        }

        public override string ToString()
        {
            return "Fade in";
        }
    }

    public class EndCommand : IScrCommand
    {
        public byte OpCode => 0xFE;

        public byte[] ToByteCode(Htx htx)
        {
            return new byte[] { 0x23, OpCode };
        }

        public override string ToString()
        {
            return "End";
        }
    }
}
