﻿/*
	Copyright 2010 MCLawl Team - Written by Valek
 
    Licensed under the
	Educational Community License, Version 2.0 (the "License"); you may
	not use this file except in compliance with the License. You may
	obtain a copy of the License at
	
	http://www.osedu.org/licenses/ECL-2.0
	
	Unless required by applicable law or agreed to in writing,
	software distributed under the License is distributed on an "AS IS"
	BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
	or implied. See the License for the specific language governing
	permissions and limitations under the License.
*/

using System;

namespace MCLawl
{
    public class CmdDevs : Command
    {
        public override string name { get { return "devs"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return "information"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public CmdDevs() { }

        public override void Use(Player p, string message)
        {
            if (message != "") { Help(p); return; }
            string devlist = "";
            string temp;
            foreach (string dev in Server.devs)
            {
                temp = dev.Substring(0, 1);
                temp = temp.ToUpper() + dev.Remove(0, 1);
                devlist += temp + ", ";
            }
            devlist = devlist.Remove(devlist.Length - 2);
            Player.SendMessage(p, "&9MCLawl Development Team: " + Server.DefaultColor + devlist);
        }

        public override void Help(Player p)
        {
            Player.SendMessage(p, "/devs - Displays the list of MCLawl developers.");
        }
    }
}