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
        public class EnergyStats
        {
            public bool Battery { get; set; }
            public bool Renewable { get; set; }
            public int running { get; set; }
            public double OutPut { get; set; }
            public double Max { get; set; }
            public double Stored { get; set; }
            public double StoredMax { get; set; }
            public double CurrentMax { get; set; }
            public double AvailableRenewable { get; set; }

            public EnergyStats()
            {
                Battery = false;
                Renewable = false;
                OutPut = 0;
                Max = 0;
                Stored = 0;
                StoredMax = 0;
                CurrentMax = 0;
                AvailableRenewable = 0;
            }

            public void Add(EnergyStats energyStats)
            {
                OutPut += energyStats.OutPut;
                Max += energyStats.Max;
                Stored += energyStats.Stored;
                StoredMax += energyStats.StoredMax;
                CurrentMax += energyStats.CurrentMax;
                AvailableRenewable += energyStats.AvailableRenewable;
            }
        }
    }
}
