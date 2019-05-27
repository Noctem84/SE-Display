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
        public class EnergyService
        {
            Program program;
            static Dictionary<string, List<IMyPowerProducer>> powerProducerMap = new Dictionary<string, List<IMyPowerProducer>>();
            DisplayService display;
            static readonly int TYPE_OVERVIEW = 0;
            static readonly int TYPE_REACTOR = 1;
            static readonly int TYPE_BATTERY = 2;
            static readonly int TYPE_RENEWABLE = 3;
            static readonly int TYPE_ENGINE = 4;

            static int availableProducer = 0;

            static readonly int LARGE_BATTERY_MAX = 12;
            static readonly double SMALL_BATTERY_MAX = 4.32;
            static readonly double SMALL_SMALL_BATTERY_MAX = 0.2;
            static readonly double WIND_TURBINE_MAX = 0.4;
            static readonly double LARGE_SOLAR_PANEL_MAX = 0.16;
            static readonly double SMALL_SOLAR_PANEL_MAX = 0.03;

            static Dictionary<string, int> typeIdMapping = new Dictionary<string, int>();
            static Dictionary<string, string> blockTypeDisplayNames = new Dictionary<string, string>();
            static Dictionary<string, EnergyStats> energyStats = new Dictionary<string, EnergyStats>();
            static bool calculated = false;

            static readonly int ID_REACTOR = 0;
            static readonly int ID_BATTERY = 1;
            static readonly int ID_ENGINE = 2;
            static readonly int ID_SOLAR_PANEL = 3;
            static readonly int ID_WIND_TURBINE = 4;

            static readonly string TYPE_ID_OVERVIEW = "ov";

            static readonly string TYPE_ID_PREFIX = "MyObjectBuilder_";
            static readonly string TYPE_ID_REACTOR = TYPE_ID_PREFIX + "Reactor";
            static readonly string TYPE_ID_BATTERY = TYPE_ID_PREFIX + "BatteryBlock";
            static readonly string TYPE_ID_ENGINE = TYPE_ID_PREFIX + "HydrogenEngine";
            static readonly string TYPE_ID_SOLAR_PANEL = TYPE_ID_PREFIX + "SolarPanel";
            static readonly string TYPE_ID_WIND_TURBINE = TYPE_ID_PREFIX + "WindTurbine";

            public EnergyService(Program program, DisplayService display)
            {
                this.program = program;
                this.display = display;

                blockTypeDisplayNames.Add(TYPE_ID_REACTOR, "Reactor");
                blockTypeDisplayNames.Add(TYPE_ID_BATTERY, "Battery");
                blockTypeDisplayNames.Add(TYPE_ID_ENGINE, "Hydrogen Engine");
                blockTypeDisplayNames.Add(TYPE_ID_SOLAR_PANEL, "Solar Panel");
                blockTypeDisplayNames.Add(TYPE_ID_WIND_TURBINE, "Wind Turbine");

                typeIdMapping.Add(TYPE_ID_REACTOR, ID_REACTOR);
                typeIdMapping.Add(TYPE_ID_BATTERY, ID_BATTERY);
                typeIdMapping.Add(TYPE_ID_ENGINE, ID_ENGINE);
                typeIdMapping.Add(TYPE_ID_SOLAR_PANEL, ID_SOLAR_PANEL);
                typeIdMapping.Add(TYPE_ID_WIND_TURBINE, ID_WIND_TURBINE);

                calculate();
            }

            private void calculate()
            {
                if (!calculated)
                {
                    List<IMyPowerProducer> powerProducer;
                    int previousAvailableProducer = availableProducer;
                    availableProducer = getAllPowerProducer(out powerProducer);
                    if (previousAvailableProducer != availableProducer)
                    {
                        findProducer(powerProducer);
                    }
                    energyStats.Clear();
                    EnergyStats totalStats = new EnergyStats();
                    energyStats.Add(TYPE_ID_OVERVIEW, totalStats);
                    foreach (string key in powerProducerMap.Keys)
                    {
                        List<IMyPowerProducer> producers;
                        if (powerProducerMap.TryGetValue(key, out producers))
                        {
                            int typeId = typeIdMapping.GetValueOrDefault(key);
                            EnergyStats stats = new EnergyStats();
                            energyStats.Add(key, stats);
                            bool turbine = ID_WIND_TURBINE == typeId;
                            bool panel = ID_SOLAR_PANEL == typeId;

                            if (ID_BATTERY == typeId)
                            {
                                stats.Battery = true;
                            }
                            else if (ID_SOLAR_PANEL == typeId || ID_WIND_TURBINE == typeId)
                            {
                                stats.Renewable = true;
                            }

                            foreach (IMyPowerProducer producer in producers)
                            {

                                bool smallGrid = isSmallGrid(producer);
                                if (producer.CurrentOutput != 0)
                                {
                                    stats.running++;
                                }
                                stats.OutPut += producer.CurrentOutput;
                                if (stats.Battery)
                                {
                                    IMyBatteryBlock battery = producer as IMyBatteryBlock;
                                    stats.Stored += battery.CurrentStoredPower;
                                    stats.StoredMax += battery.MaxStoredPower;
                                    stats.CurrentMax += !battery.HasCapacityRemaining && battery.IsCharging ? 0 : smallGrid ? SMALL_BATTERY_MAX : LARGE_BATTERY_MAX;
                                    stats.Max += smallGrid ? SMALL_BATTERY_MAX : LARGE_BATTERY_MAX;
                                }
                                else if (turbine)
                                {
                                    stats.CurrentMax += producer.MaxOutput;
                                    stats.Max += WIND_TURBINE_MAX;
                                    stats.AvailableRenewable += producer.MaxOutput - producer.CurrentOutput;
                                }
                                else if (panel)
                                {
                                    stats.CurrentMax += producer.MaxOutput;
                                    stats.Max += smallGrid ? SMALL_SOLAR_PANEL_MAX : LARGE_SOLAR_PANEL_MAX;
                                    stats.AvailableRenewable += producer.MaxOutput - producer.CurrentOutput;
                                }
                                else
                                {
                                    stats.Max += producer.MaxOutput;
                                    stats.CurrentMax += producer.MaxOutput;
                                }
                            }
                            totalStats.Add(stats);
                        }
                    }
                    calculated = true;
                }
            }

            private bool isSmallGrid(IMyPowerProducer myPowerProducer)
            {
                return (myPowerProducer.CubeGrid.GridSizeEnum & MyCubeSize.Small) != 0;
            }

            private void findProducer(List<IMyPowerProducer> powerProducer)
            {
                powerProducerMap.Clear();
                foreach(IMyPowerProducer powerProducerBlock in powerProducer)
                {
                    string typeIdString = powerProducerBlock.BlockDefinition.TypeIdString;
                    List<IMyPowerProducer> producers;
                    if (!powerProducerMap.TryGetValue(typeIdString, out producers))
                    {
                        program.Echo(typeIdString);
                        producers = new List<IMyPowerProducer>();
                        powerProducerMap.Add(typeIdString, producers);
                    }
                    producers.Add(powerProducerBlock);
                }
            }

            public void print()
            {
                calculated = false;
                calculate();
                if (display.hasDeviceForType(TYPE_OVERVIEW))
                {
                    printOverview();
                }
                if (display.hasDeviceForType(TYPE_REACTOR))
                {
                    printReactors();
                }
                if (display.hasDeviceForType(TYPE_BATTERY))
                {
                    printBattery();
                }
                if (display.hasDeviceForType(TYPE_RENEWABLE))
                {
                    printRenewable();
                }
                if (display.hasDeviceForType(TYPE_ENGINE))
                {
                    printEngines();
                }
            }

            private void printEngines()
            {
                StringBuilder sb = new StringBuilder();
                List<IMyPowerProducer> engines;

                display.writeToDisplays(sb, false, TYPE_ENGINE);
            }

            private void printRenewable()
            {
                StringBuilder sb = new StringBuilder();

                List<IMyPowerProducer> panels;
                List<IMyPowerProducer> turbines;

                if (powerProducerMap.TryGetValue(TYPE_ID_SOLAR_PANEL, out panels))
                {
                    sb.Append(blockTypeDisplayNames.GetValueOrDefault(TYPE_ID_SOLAR_PANEL)).Append("\n");
                    int i = 0;
                    int lineBreak = 2;
                    foreach(IMyPowerProducer producer in panels)
                    {
                        double percent = (isSmallGrid(producer) ? SMALL_SOLAR_PANEL_MAX : LARGE_SOLAR_PANEL_MAX) / 100;
                        EnergyStats stats = energyStats.GetValueOrDefault(TYPE_ID_SOLAR_PANEL);
                        sb.Append(producer.CustomName).Append(": ").AppendFormat("{0:0}", producer.MaxOutput / percent).Append("% ")
                            .AppendFormat("{0:0.##}", producer.CurrentOutput).Append(" MW / ").AppendFormat("{0:0.##}", producer.MaxOutput).Append(" MW");
                        if (i % lineBreak != 0)
                        {
                            sb.Append("\n");
                        }
                        else
                        {
                            sb.Append("\t");
                        }
                        i++;
                    }
                    sb.Append("\n");
                }
                if (powerProducerMap.TryGetValue(TYPE_ID_WIND_TURBINE, out turbines))
                {
                    sb.Append(blockTypeDisplayNames.GetValueOrDefault(TYPE_ID_WIND_TURBINE)).Append("\n");
                    int i = 1;
                    int lineBreak = 10;
                    foreach (IMyPowerProducer producer in turbines)
                    {
                        double percent = WIND_TURBINE_MAX / 100;
                        EnergyStats stats = energyStats.GetValueOrDefault(TYPE_ID_WIND_TURBINE);
                        sb.Append(i < 10 ? "0" : "").Append(i).Append(": ").AppendFormat("{0:0}", producer.MaxOutput / percent).Append("%");
                        if (i % lineBreak == 0)
                        {
                            sb.Append("\n");
                        }
                        i++;
                    }
                }

                if (sb.Length == 0)
                {
                    sb.Append("No renewables available");
                }
                display.writeToDisplays(sb, false, TYPE_RENEWABLE);
            }

            private void printBattery()
            {
                StringBuilder sb = new StringBuilder();


                display.writeToDisplays(sb, false, TYPE_BATTERY);
            }

            private void printReactors()
            {
                StringBuilder sb = new StringBuilder();


                display.writeToDisplays(sb, false, TYPE_REACTOR);
            }

            private void printOverview()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Power producer").Append("\n");

                EnergyStats totalStats = energyStats.GetValueOrDefault(TYPE_ID_OVERVIEW);

                foreach (string key in powerProducerMap.Keys)
                {
                    List<IMyPowerProducer> producers;
                    if (powerProducerMap.TryGetValue(key, out producers))
                    {
                        StringBuilder subStringBuilder = new StringBuilder();
                        EnergyStats stats = energyStats.GetValueOrDefault(key);
                        string displayName = blockTypeDisplayNames.GetValueOrDefault(key);
                        subStringBuilder.Append("+ ").Append(producers.Count).Append(" ").Append(displayName);
                        sb.Append(subStringBuilder.ToString()).Append("\t");
                        sb.Append(display.format(stats.OutPut)).Append(" / ").Append(display.format(stats.CurrentMax));
                        if (stats.Max - stats.CurrentMax > 0.0004)
                        {
                            sb.Append(" (").Append(stats.Max).Append(")");
                        }
                        sb.Append(" MW\n");
                    }
                }
                sb.Append("\n").Append("Total Power\n");
                sb.Append("+ Usage: ").Append("\t").Append(display.format(totalStats.OutPut)).Append(" MW\n");
                sb.Append("+ Available: ").Append("\t").Append(display.format(totalStats.CurrentMax));
                if (totalStats.Max - totalStats.CurrentMax > 0.0004)
                {
                    sb.Append(" (").Append(display.format(totalStats.Max)).Append(")");
                }
                sb.Append(" MW\n");
                sb.Append("+ Stored: ").Append("\t").Append(display.format(totalStats.Stored));
                if (totalStats.Stored != totalStats.StoredMax)
                {
                    sb.Append(" (").Append(display.format(totalStats.StoredMax)).Append(")");
                }
                sb.Append(" MWh\n");

                display.writeToDisplays(sb, false, TYPE_OVERVIEW);
            }

            private int getAllPowerProducer(out List<IMyPowerProducer> producer)
            {
                producer = new List<IMyPowerProducer>();
                program.GridTerminalSystem.GetBlocksOfType(producer);
                return producer.Count;
            }
        }
    }
}
