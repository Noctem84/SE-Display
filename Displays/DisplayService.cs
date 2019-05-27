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
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program
    {
        /**
         * Command Syntax
         * 
         * Add display:
         * ds add [block_Name] {','display_index default:0} {':'type_id default:0}
         * 
         * Remove display:
         * ds rm [block_Name] {','display_index default:0}
         * 
         * Auto Initialization
         * 
         * Add init phrase lower case to Custom Data
         * */
        public class DisplayService
        {
            private Program program;
            private Dictionary<int, HashSet<Display>> displays;
            private List<IMyTextSurface> surfaces;
            private string initPhrase;
            public string Format { get; set; }
            static readonly string ADD_DISPLAY_COMMAND = "add";
            static readonly string REMOVE_DISPLAY_COMMAND = "rm";
            public static readonly string DISPLAY_COMMAND = "ds";
            static readonly string SPACER = "                        ";

            public DisplayService(Program program, string initPhrase)
            {
                Format = "{0:0.###}";
                this.program = program;
                displays = new Dictionary<int, HashSet<Display>>();
                surfaces = new List<IMyTextSurface>();
                this.initPhrase = initPhrase;
                initScreens();
            }

            public bool isDisplayCommand(string command)
            {
                return command.StartsWith(DISPLAY_COMMAND);
            }

            private void initScreens()
            {
                List<IMyTextSurfaceProvider> surfaceProviders = new List<IMyTextSurfaceProvider>();
                program.GridTerminalSystem.GetBlocksOfType(surfaceProviders);
                foreach(IMyTextSurfaceProvider surfaceProvider in surfaceProviders)
                {
                    IMyTerminalBlock block = surfaceProvider as IMyTerminalBlock;
                    if (block.CustomData.Contains(initPhrase))
                    {
                        int type = 0;
                        int index = 0;
                        string[] customDataParts = block.CustomData.Split('\n');
                        for (int i = 1; i < customDataParts.Length; i++)
                        {
                            if (customDataParts[i].StartsWith("type"))
                            {
                                type = int.Parse(customDataParts[i].Split('=')[1]);
                            }
                            if (customDataParts[i].StartsWith("index"))
                            {
                                index = int.Parse(customDataParts[i].Split('=')[1]);
                            }
                        }
                        AddDisplay(block.CustomName + (index != 0 ? "," + index : "") + (type != 0 ? ":" + type : ""));
                    }
                }
            }

            public bool processCommand(string command)
            {
                bool status = false;
                string displayCommand = command.StartsWith(DISPLAY_COMMAND) ? command.Substring(3) : command;
                if (displayCommand.StartsWith(ADD_DISPLAY_COMMAND))
                {
                    status = AddDisplay(displayCommand.Substring(ADD_DISPLAY_COMMAND.Length + 1));
                } else if (displayCommand.StartsWith(REMOVE_DISPLAY_COMMAND))
                {
                    status = removeDisplay(displayCommand.Substring(REMOVE_DISPLAY_COMMAND.Length + 1));
                }
                return status;
            }

            private bool AddDisplay(string command)
            {
                int type;
                if (command.Contains(':'))
                {
                    string[] commandParts = command.Split(':');
                    type = int.Parse(commandParts[1]);
                    command = commandParts[0];
                }
                else
                {
                    type = 0;
                }
                Display display = getDisplay(command);
                if (display != null)
                {
                    HashSet<Display> hashSet;
                    if (!displays.TryGetValue(type, out hashSet))
                    {
                        hashSet = new HashSet<Display>();
                        displays.Add(type, hashSet);
                    }
                    display.Role = type;
                    display.Surface.WriteText("Display " + display.Block.CustomName + " initialized.\n\n standby...");
                    hashSet.Add(display);
                }
                return true;
            }

            private bool removeDisplay(string displayName)
            {
                Display display = getDisplay(displayName);
                if (display != null)
                {
                    foreach (int type in displays.Keys)
                    {
                        HashSet<Display> hashSet;
                        displays.TryGetValue(type, out hashSet);
                        if (hashSet != null)
                        {
                            hashSet.Remove(display);
                        }
                    }
                    return true;
                }
                return false;
            }

            private Display getDisplay(string displayName)
            {
                int displayIndex = 0;
                if (displayName.Contains(','))
                {
                    displayIndex = int.Parse(displayName.Split(',')[1]);
                    if (displayIndex < 0)
                    {
                        displayIndex = 0;
                    }
                    displayName = displayName.Split(',')[0];

                }
                
                Display display = null;
                IMyTextSurface displayBlock = program.GridTerminalSystem.GetBlockWithName(displayName) as IMyTextSurface;
                if (displayBlock != null)
                {
                    display = new Display();
                    display.Block = displayBlock as IMyFunctionalBlock;
                    display.DisplayIndex = displayIndex;
                    display.Surface = displayBlock;
                }
                else
                {
                    IMyTextSurfaceProvider provider = program.GridTerminalSystem.GetBlockWithName(displayName) as IMyTextSurfaceProvider;
                    if (provider != null)
                    {
                        if (provider.SurfaceCount > 0)
                        {
                            display = new Display();
                            int surfaceIndex = provider.SurfaceCount > displayIndex ? displayIndex : 0;
                            display.Surface = provider.GetSurface(surfaceIndex);
                            display.DisplayIndex = surfaceIndex;
                            display.Block = program.GridTerminalSystem.GetBlockWithName(displayName) as IMyTerminalBlock;
                        }
                    }
                    else
                    {
                    }
                }
                if (display != null)
                {
                    #pragma warning disable ProhibitedMemberRule // Prohibited Type Or Member
                    display.Surface.ContentType |= ContentType.TEXT_AND_IMAGE;
                    display.Surface.ContentType &= ~ContentType.NONE;
                    #pragma warning restore ProhibitedMemberRule // Prohibited Type Or Member
                    setSpaceSize(display);
                }
                return display;
            }

            public void printDisplayNames()
            {
                program.Echo("Displays:");
                HashSet<Display> allDisplays = new HashSet<Display>();
                foreach (HashSet<Display> displaySet in displays.Values)
                {
                    allDisplays.UnionWith(displaySet);
                }
                foreach(Display display in allDisplays)
                {
                    program.Echo(display.Block.CustomName + ":" + display.DisplayIndex + ":" + display.Role);
                }
            }

            public void writeToDisplays(StringBuilder text, bool append, int type)
            {
                HashSet<Display> displaysOfType;
                if (displays != null && displays.TryGetValue(type, out displaysOfType))
                {
                    #pragma warning disable ProhibitedMemberRule // Prohibited Type Or Member
                    List<StringBuilder> textParts = text.Split('\n');
                    #pragma warning restore ProhibitedMemberRule // Prohibited Type Or Member
                    foreach (Display display in displaysOfType)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (StringBuilder part in textParts)
                        {
                            if (part.ToString().Contains("\t"))
                            {
                                program.Echo("found tab");
                                #pragma warning disable ProhibitedMemberRule // Prohibited Type Or Member
                                sb.AppendStringBuilder(addTab(part, display)).Append("\n");
                                #pragma warning restore ProhibitedMemberRule // Prohibited Type Or Member
                            }
                            else
                            {
                                #pragma warning disable ProhibitedMemberRule // Prohibited Type Or Member
                                sb.AppendStringBuilder(part).Append("\n");
                                #pragma warning restore ProhibitedMemberRule // Prohibited Type Or Member
                            }
                        }
                        display.Surface.WriteText(sb, append);
                    }
                }
            }

            private StringBuilder addTab(StringBuilder line, Display display)
            {
                IMyTextSurface surface = display.Surface;
                string[] tabbedParts = line.ToString().Split('\t');
                Vector2 leftPartSize = surface.MeasureStringInPixels(new StringBuilder(tabbedParts[0]), surface.Font, surface.FontSize);
                Vector2 rightPartSize = surface.MeasureStringInPixels(new StringBuilder(tabbedParts[1]), surface.Font, surface.FontSize);
                Vector2 displaySize = surface.SurfaceSize;
                //float size = 0;
                //program.Echo("display size: " + displaySize.X);
                //program.Echo("padding: " + surface.TextPadding);
                //program.Echo("text Size l: " + leftPartSize.X);
                //program.Echo("text Size r: " + rightPartSize.X);
                float emptySpace = (displaySize.X / 2) - (leftPartSize.X + (surface.TextPadding * surface.FontSize));
                //size += leftPartSize.X;
                //size += rightPartSize.X;
                //size += (surface.TextPadding * surface.FontSize * 2);
                //program.Echo("empty space: " + emptySpace);
                //program.Echo("space size: " + display.SpaceSize);

                StringBuilder sb = new StringBuilder(tabbedParts[0]);
                //int i = 0;
                while ((emptySpace - (display.SpaceSize)) > 0)
                {
                    sb.Append(" ");
                    emptySpace -= display.SpaceSize;
                    //i++;
                }
                //size += i * display.SpaceSize;
                //program.Echo("Added " + i + " spaces");
                sb.Append(tabbedParts[1]);
                Vector2 finalSize = surface.MeasureStringInPixels(sb, surface.Font, surface.FontSize);
                //program.Echo("final size: " + finalSize);
                //program.Echo("difference: " + (finalSize.X - size));
                return sb;
            }

            public bool hasDeviceForType(int type)
            {
                HashSet<Display> displaysForType;
                bool available = (displays.TryGetValue(type, out displaysForType) && displaysForType.Count > 0);
                //program.Echo("Type " + type + " is " + (available ? "" : "not ") + "available");
                return available;
            }

            public string format(float value)
            {
                return String.Format(Format, value);
            }

            public string format(double value)
            {
                return String.Format(Format, value);
            }

            public string tab(string text)
            {
                return SPACER.Substring(text.Length);
            }

            private void setSpaceSize(Display display)
            {
                display.SpaceSize = display.Surface.MeasureStringInPixels(new StringBuilder(" "), display.Surface.Font, display.Surface.FontSize).X;
            }
        }
    }
}
