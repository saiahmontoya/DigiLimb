using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiLimbMobile
{
    public static class KeyboardLayout
    {
        public static readonly string[][] Layout = new string[][]
        {
            new string[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace:3" },
            new string[] { "Tab:2", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" },
            new string[] { "Caps:2", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter:3" },
            new string[] { "Shift:2", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift:3" },
            new string[] { "Ctrl:2", "Win", "Alt", "Space:6", "Alt", "Win", "Menu", "Ctrl:2" }
        };
    }
}
