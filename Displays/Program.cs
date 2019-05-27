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
    partial class Program : MyGridProgram
    {
        private DisplayService displayService;
        private Profiler profiler;
        private EnergyService energyService;

        public Program()
        {
            profiler = new Profiler(this);
            displayService = new DisplayService(this, "display");
            energyService = new EnergyService(this, displayService);
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            //Runtime.UpdateFrequency |= UpdateFrequency.None;
        }

        public void Save()
        {
        }

        /* 0 - power overview
         * 1 - reactor details
         * 2 - battery details
         * 3 - Hydrogen details
         * 4 - storage details
         */
        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Length == 0)
            {
                energyService.print();
                profiler.print();
                displayService.printDisplayNames();
            }
            else
            {
                // Command processing
                string command = argument;
                if (command.StartsWith("ds"))
                {
                    displayService.processCommand(command);
                }
            }
        }
    }
}