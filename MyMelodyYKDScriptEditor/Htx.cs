using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMelodyYKDScriptEditor
{
    public class Htx
    {
        public List<string> Lines { get; set; } = new List<string>();

        public short AddLine(string line)
        {
            if (Lines.Contains(line))
            {
                return (short)Lines.IndexOf(line);
            }
            else
            {
                Lines.Add(line);
                return (short)(Lines.Count - 1);
            }
        }

        public static Htx ParseFromFile(string file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            byte[] data = File.ReadAllBytes(file);
            List<string> lines = Encoding.GetEncoding("shift-jis").GetString(data)
                .Split("\r\n").ToList();
            foreach ((string h, string f) in HalfWidthToFullWidthChar)
            {
                lines = lines.Select(l => l.Replace(f, h)).ToList();
            }
            return new Htx { Lines = lines };
        }

        public async Task WriteToFile(string file)
        {
            var lines = Lines;
            foreach ((string h, string f) in HalfWidthToFullWidthChar)
            {
                lines = lines.Select(l => l.Replace(h, f)).ToList();
            }
            byte[] data = Encoding.GetEncoding("shift-jis").GetBytes(string.Join("\r\n", lines.ToArray()));
            await File.WriteAllBytesAsync(file, data);
        }

        private static Dictionary<string, string> HalfWidthToFullWidthChar = new Dictionary<string, string>
        {
            { "A", "Ａ" },
            { "B", "Ｂ" },
            { "C", "Ｃ" },
            { "D", "Ｄ" },
            { "E", "Ｅ" },
            { "F", "Ｆ" },
            { "G", "Ｇ" },
            { "H", "Ｈ" },
            { "I", "Ｉ" },
            { "J", "Ｊ" },
            { "K", "Ｋ" },
            { "L", "Ｌ" },
            { "M", "Ｍ" },
            { "N", "Ｎ" },
            { "O", "Ｏ" },
            { "P", "Ｐ" },
            { "Q", "Ｑ" },
            { "R", "Ｒ" },
            { "S", "Ｓ" },
            { "T", "Ｔ" },
            { "U", "Ｕ" },
            { "V", "Ｖ" },
            { "W", "Ｗ" },
            { "X", "Ｘ" },
            { "Y", "Ｙ" },
            { "Z", "Ｚ" },
            { "a", "ａ" },
            { "b", "ｂ" },
            { "c", "ｃ" },
            { "d", "ｄ" },
            { "e", "ｅ" },
            { "f", "ｆ" },
            { "g", "ｇ" },
            { "h", "ｈ" },
            { "i", "ｉ" },
            { "j", "ｊ" },
            { "k", "ｋ" },
            { "l", "ｌ" },
            { "m", "ｍ" },
            { "n", "ｎ" },
            { "o", "ｏ" },
            { "p", "ｐ" },
            { "q", "ｑ" },
            { "r", "ｒ" },
            { "s", "ｓ" },
            { "t", "ｔ" },
            { "u", "ｕ" },
            { "v", "ｖ" },
            { "w", "ｗ" },
            { "x", "ｘ" },
            { "y", "ｙ" },
            { "z", "ｚ" },
            { "0", "０" },
            { "1", "１" },
            { "2", "２" },
            { "3", "３" },
            { "4", "４" },
            { "5", "５" },
            { "6", "６" },
            { "7", "７" },
            { "8", "８" },
            { "9", "９" },
            { "?", "？" },
            { "!", "！" },
            { ",", "、" },
            { "-", "－" },
            { "/", "／" },
            { "~", "～" },
            { " ", "　" },
            { "#", "＃" },
            { "%", "％" },
            { "$", "＄" },
        };
    }
}
