using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Items
{
    public enum ToolType
    {
        None,
        Screwdriver,
        Wrench,
        Crowbar,
        Wirecutters,
        Welder,
        Multitool
    }

    public class DM_tool : DM_item
    {
        public ToolType TypeOfTool { get; set; }
        public double ToolSpeed { get; set; }
        public double FuelUnits { get; set; }

        public DM_tool(ToolType toolType, string name = null) : base(name ?? toolType.ToString())
        {
            TypeOfTool = toolType;
            IsTool = true;
            ToolSpeed = 1.0;
            FuelUnits = (toolType == ToolType.Welder) ? 20.0 : 0.0;
        }
    }

    public class ToolInteractionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ToolInteractionResult(bool success, string msg)
        {
            Success = success;
            Message = msg;
        }
    }

    public static class ToolInteractions
    {
        public static ToolInteractionResult ApplyTool(DM_tool tool, DM_atom target)
        {
            if (tool == null || target == null)
            {
                return new ToolInteractionResult(false, "Invalid tool or target.");
            }

            string targetName = target.name.AsString.ToLowerInvariant();

            // 1. Crowbar Actions
            if (tool.TypeOfTool == ToolType.Crowbar)
            {
                if (targetName.Contains("airlock") || targetName.Contains("door"))
                {
                    return new ToolInteractionResult(true, string.Format("You pry at {0} with the crowbar, forcing the mechanism open!", target.name.AsString));
                }
                if (targetName.Contains("floor") || targetName.Contains("plating"))
                {
                    return new ToolInteractionResult(true, string.Format("You pry up the tiles on {0} to reveal the subfloor.", target.name.AsString));
                }
            }

            // 2. Screwdriver Actions
            if (tool.TypeOfTool == ToolType.Screwdriver)
            {
                if (targetName.Contains("airlock") || targetName.Contains("machine") || targetName.Contains("apc"))
                {
                    return new ToolInteractionResult(true, string.Format("You unscrew the maintenance panel of {0}.", target.name.AsString));
                }
            }

            // 3. Welder Actions
            if (tool.TypeOfTool == ToolType.Welder)
            {
                if (tool.FuelUnits < 1.0)
                {
                    return new ToolInteractionResult(false, "The welder is out of fuel!");
                }
                tool.FuelUnits -= 1.0;

                if (targetName.Contains("airlock") || targetName.Contains("door"))
                {
                    return new ToolInteractionResult(true, string.Format("You weld shut the seams of {0}!", target.name.AsString));
                }
                if (targetName.Contains("wall"))
                {
                    return new ToolInteractionResult(true, string.Format("You slice through the outer girder of {0}.", target.name.AsString));
                }
            }

            // 4. Wrench Actions
            if (tool.TypeOfTool == ToolType.Wrench)
            {
                return new ToolInteractionResult(true, string.Format("You unbolt the anchoring fixtures of {0}.", target.name.AsString));
            }

            // 5. Wirecutters Actions
            if (tool.TypeOfTool == ToolType.Wirecutters)
            {
                return new ToolInteractionResult(true, string.Format("You sever the power wiring inside {0}.", target.name.AsString));
            }

            // 6. Multitool Actions
            if (tool.TypeOfTool == ToolType.Multitool)
            {
                return new ToolInteractionResult(true, string.Format("You pulse the circuit test points on {0}.", target.name.AsString));
            }

            return new ToolInteractionResult(false, string.Format("The {0} has no effect on {1}.", tool.name.AsString, target.name.AsString));
        }
    }
}
