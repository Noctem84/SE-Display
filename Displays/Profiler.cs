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
        public class Profiler
        {
            Program program;
            double totalRuntime = 0;
            int totalRuns = 0;

            public Profiler(Program program)
            {
                this.program = program;
            }

            public void print()
            {
                totalRuns++;
                totalRuntime += program.Runtime.LastRunTimeMs;
                program.Echo("\n-Runtime: " + String.Format("{0:0.000}", program.Runtime.LastRunTimeMs) + "ms");
                program.Echo("-Total Runtime: " + String.Format("{0:0.000}", totalRuntime) + " ms;");
                program.Echo("-Avarage Runtime: " + String.Format("{0:0.000}", totalRuntime / totalRuns) + " ms");
            }
        }
    }
}
