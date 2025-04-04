using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode
{
    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public string? PlayerPicture { get; set; }
        public string? PlayerColor { get; set; }
    }
    public class GameCommand
    {
        // Your command properties
        public string SendToClientFunctionName { get; set; }
        public string commandValue1 { get; set; }
        public string commandValue2 { get; set; }
        public string commandValue3 { get; set; }
        // Index to uniquely identify the command order
        public int Index { get; set; }
    }
}