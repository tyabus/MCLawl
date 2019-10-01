using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCLawl
{
    public class CmdVersion : Command
    {
        public override string name { get { return "version"; } }
        public override string shortcut { get { return "ver"; } }
        public override string type { get { return "other"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public CmdVersion() { }

        public override void Use(Player p, string message)
        {
            if (message != "") { Help(p); return; }
            Player.GlobalMessageOps(p.color + p + Server.DefaultColor + " used /version");
            Player.SendMessage(p, "%aMCLawl's %aversion: %b" + Server.Version);
        }
        public override void Help(Player p)
        {
            Player.SendMessage(p, "/version - view MCLaws's version");
        }
    }

}
