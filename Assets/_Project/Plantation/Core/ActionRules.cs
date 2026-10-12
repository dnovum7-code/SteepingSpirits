namespace SteepingSpirits.Plantation
{
    public enum GardenAction
    {
        Hoe,
        Sow,
        Water,
        Harvest,
        Shovel
    }

    /// <summary>What an action would do on a cell.</summary>
    public enum ActionEffect
    {
        None,
        Till,
        Sow,
        Water,
        Harvest,

        /// <summary>Shovel on tilled, empty soil: back to untouched earth.</summary>
        Flatten,

        /// <summary>Shovel on a growing plant: the seed comes back, the soil stays tilled.</summary>
        DigUp
    }

    /// <summary>Why an action does nothing here (shown calmly in the preview, no error sound).</summary>
    public enum ActionReason
    {
        Ok,
        NotABed,
        AlreadyTilled,
        PlantGrowing,
        NeedsTilling,
        AlreadyPlanted,
        NoSeedSelected,
        NoSeedsLeft,
        AlreadyWatered,
        CanEmpty,
        NotRipe,
        NothingHere,
        HarvestFirst
    }

    /// <summary>The result of checking an action on a cell.</summary>
    public struct ActionCheck
    {
        public ActionEffect effect;
        public ActionReason reason;

        public bool Valid => effect != ActionEffect.None;

        public static ActionCheck Do(ActionEffect e) => new ActionCheck { effect = e, reason = ActionReason.Ok };
        public static ActionCheck No(ActionReason r) => new ActionCheck { effect = ActionEffect.None, reason = r };

        public override string ToString() => Valid ? effect.ToString() : "no: " + reason;
    }

    /// <summary>What the player holds besides the cell (seed choice, can level).</summary>
    public struct ToolContext
    {
        /// <summary>Plant chosen in the seed bag (null = none).</summary>
        public string seedPlantId;

        /// <summary>Seeds of that plant the player has.</summary>
        public int seedCount;

        /// <summary>Water left in the can.</summary>
        public int canWater;
    }

    /// <summary>
    /// Which action does what on which cell. A pure function of the plot state
    /// (null = not a bed cell) and the tool context – the whole truth table is tested.
    /// </summary>
    public static class ActionRules
    {
        public static ActionCheck Check(Plot plot, GardenAction action, ToolContext ctx)
        {
            if (plot == null)
            {
                return ActionCheck.No(ActionReason.NotABed);
            }

            switch (action)
            {
                case GardenAction.Hoe:
                    if (plot.HasCrop) return ActionCheck.No(ActionReason.PlantGrowing);
                    if (plot.soil == Soil.Tilled) return ActionCheck.No(ActionReason.AlreadyTilled);
                    return ActionCheck.Do(ActionEffect.Till);

                case GardenAction.Sow:
                    if (plot.HasCrop) return ActionCheck.No(ActionReason.AlreadyPlanted);
                    if (plot.soil != Soil.Tilled) return ActionCheck.No(ActionReason.NeedsTilling);
                    if (string.IsNullOrEmpty(ctx.seedPlantId)) return ActionCheck.No(ActionReason.NoSeedSelected);
                    if (ctx.seedCount <= 0) return ActionCheck.No(ActionReason.NoSeedsLeft);
                    return ActionCheck.Do(ActionEffect.Sow);

                case GardenAction.Water:
                    if (plot.soil != Soil.Tilled) return ActionCheck.No(ActionReason.NeedsTilling);
                    if (plot.watered) return ActionCheck.No(ActionReason.AlreadyWatered);
                    if (ctx.canWater <= 0) return ActionCheck.No(ActionReason.CanEmpty);
                    return ActionCheck.Do(ActionEffect.Water);

                case GardenAction.Harvest:
                    if (!plot.HasCrop) return ActionCheck.No(ActionReason.NothingHere);
                    if (!plot.crop.ripe) return ActionCheck.No(ActionReason.NotRipe);
                    return ActionCheck.Do(ActionEffect.Harvest);

                case GardenAction.Shovel:
                    if (plot.HasCrop)
                    {
                        return plot.crop.ripe ? ActionCheck.No(ActionReason.HarvestFirst) : ActionCheck.Do(ActionEffect.DigUp);
                    }

                    if (plot.soil == Soil.Tilled) return ActionCheck.Do(ActionEffect.Flatten);
                    return ActionCheck.No(ActionReason.NothingHere);

                default:
                    return ActionCheck.No(ActionReason.NothingHere);
            }
        }
    }
}
