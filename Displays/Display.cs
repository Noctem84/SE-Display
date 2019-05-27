using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Display
        {
            public IMyTextSurface Surface { get; set; }
            public int Role { get; set; }
            public IMyTerminalBlock Block { get; set; }
            public int DisplayIndex { get; set; }
            public float SpaceSize { get; set; }

            public Display()
            {
                
            }
        }
    }
}
