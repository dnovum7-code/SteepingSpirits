using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Player;
using SteepingSpirits.World;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// The player's hands in the garden: toolbar (1–5, wheel, shoulders), seed choice (Q / d-pad),
    /// the target cell (the cell in front, or the cell under a nearby mouse cursor) with its calm
    /// preview, and using the tool or the object in front (well, lantern, chest, compost bin, gate).
    /// Invalid uses do nothing – no error sound; the preview already says why.
    /// </summary>
    public class GardenController : MonoBehaviour
    {
        private const float MouseFocusSeconds = 2f;

        private TeaGarden root;
        private PlayerController2D mover;
        private GardenTool tool = GardenTool.Hoe;
        private readonly List<string> seedEntries = new List<string>();
        private string selectedSeed;
        private readonly GridPos[] cursorCells = new GridPos[3];
        private float lastMouseMove = -100f;
        private float repeatAt;
        private GridPos lastUsed = new GridPos(int.MinValue, 0);
        private float endDayConfirmUntil;
        private GridPos lastPlayerCell = new GridPos(int.MinValue, 0);
        private bool movedOnce;

        private string seedKey = "";
        private int seedCountKey = -1;
        private int dayKey = -1;
        private bool rainKey;
        private int optionsKey = -1;

        public GardenTool Tool => tool;
        public GridPos Target { get; private set; }
        public ActionCheck Preview { get; private set; }

        /// <summary>Inventory item chosen in the seed bag (seed or compost), or null.</summary>
        public string SelectedSeed => selectedSeed;

        public void Init(TeaGarden garden, PlayerController2D player)
        {
            root = garden;
            mover = player;
            root.Hud.SetTool(tool);
        }

        public void SelectTool(GardenTool t)
        {
            if (t == tool) return;
            tool = t;
            root.Hud.SetTool(t);
            root.Feedback.Play(GardenSound.Tool, 0.6f);
        }

        private void Update()
        {
            if (root == null || root.Garden == null) return;
            RefreshSeedEntries();
            UpdateHud();
            if (root.Busy || GamePause.IsBlocked)
            {
                root.View.SetCursor(cursorCells, 0, false);
                return;
            }

            ReadToolInput();
            CheckGateStep();

            GridPos dir;
            Target = FindTarget(out dir);
            bool onObject = root.ObjectAt(Target, out GardenTile obj);
            int cursorCount = 1;
            cursorCells[0] = Target;
            bool valid;
            if (onObject)
            {
                valid = true;
                Preview = default;
                root.Hud.SetHint(ObjectLabel(obj));
            }
            else
            {
                Preview = root.Garden.Check(Target, CurrentAction(), Context());
                valid = Preview.Valid;
                if (tool == GardenTool.Can && root.State.wideCan && Preview.effect != ActionEffect.Harvest)
                {
                    cursorCells[1] = Target + dir;
                    cursorCells[2] = Target + dir * 2;
                    cursorCount = 3;
                }

                root.Hud.SetHint(HintFor(Preview));
            }

            root.View.SetCursor(cursorCells, root.Garden.IsBed(Target) || onObject ? cursorCount : 0, valid);

            bool pressed = GardenInput.UsePressed;
            bool repeat = !pressed && GardenInput.UseHeld && Time.time >= repeatAt && !onObject && !Target.Equals(lastUsed);
            if (!pressed && !repeat) return;
            repeatAt = Time.time + root.Settings.comfort.repeatSeconds;
            if (onObject)
            {
                if (pressed) UseObject(obj, Target);
                return;
            }

            if (Perform(Target, dir)) lastUsed = Target;
        }

        // ---------------- Input ----------------

        private void ReadToolInput()
        {
            int key = GardenInput.ToolKey;
            if (key >= 0)
            {
                // Pressing 2 again while the seed bag is out picks the next seed.
                if (key == (int)GardenTool.Seeds && tool == GardenTool.Seeds) StepSeed(1);
                SelectTool((GardenTool)key);
            }

            int step = GardenInput.ToolStep;
            if (step != 0) SelectTool((GardenTool)(((int)tool + step + 5) % 5));

            int seedStep = GardenInput.SeedStep;
            if (seedStep != 0)
            {
                StepSeed(seedStep);
                SelectTool(GardenTool.Seeds);
            }

            if (GardenInput.WideCanTogglePressed && root.Settings.comfort.wideCanAvailable)
            {
                root.State.wideCan = !root.State.wideCan;
                root.Hud.Message(root.State.wideCan ? GardenTexts.WideCanOn : GardenTexts.WideCanOff);
                root.MarkDirty();
            }

            if (GardenInput.HelpTogglePressed)
            {
                root.State.gardenHelp = !root.State.gardenHelp;
                root.View.SetHelp(root.State.gardenHelp);
                root.Hud.Message(root.State.gardenHelp ? GardenTexts.HelpOn : GardenTexts.HelpOff);
                root.MarkDirty();
            }
        }

        private void StepSeed(int step)
        {
            if (seedEntries.Count == 0)
            {
                root.Hud.Message(GardenTexts.NoSeeds);
                return;
            }

            int i = seedEntries.IndexOf(selectedSeed);
            i = i < 0 ? 0 : (i + step + seedEntries.Count) % seedEntries.Count;
            selectedSeed = seedEntries[i];
        }

        private void RefreshSeedEntries()
        {
            GardenItems.CarriedSeeds(root.Book, seedEntries);
            if (GardenItems.Count(GardenItems.CompostItemId) > 0) seedEntries.Add(GardenItems.CompostItemId);
            if (seedEntries.Count == 0) selectedSeed = null;
            else if (selectedSeed == null || !seedEntries.Contains(selectedSeed)) selectedSeed = seedEntries[0];
        }

        // ---------------- Target ----------------

        private GridPos FindTarget(out GridPos dir)
        {
            Vector2 pos = mover != null ? (Vector2)mover.transform.position : Vector2.zero;
            Vector2 facing = mover != null ? mover.Facing : Vector2.down;
            GridPos here = GardenView.CellAt(pos);

            Camera cam = Camera.main;
            Vector2 screen = GardenInput.MouseScreen(out bool moved);
            if (moved) lastMouseMove = Time.time;
            if (cam != null && Time.time - lastMouseMove < MouseFocusSeconds)
            {
                Vector2 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                GridPos cell = GardenView.CellAt(world);
                if (!cell.Equals(here) && (GardenView.CellCenter(cell) - pos).magnitude <= root.Settings.comfort.mouseReach)
                {
                    Vector2 d = GardenView.CellCenter(cell) - pos;
                    dir = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new GridPos(d.x > 0 ? 1 : -1, 0) : new GridPos(0, d.y > 0 ? 1 : -1);
                    if (mover != null && !mover.IsMoving) mover.FaceTowards(GardenView.CellCenter(cell));
                    return cell;
                }
            }

            dir = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? new GridPos(facing.x > 0 ? 1 : -1, 0) : new GridPos(0, facing.y > 0 ? 1 : -1);
            return here + dir;
        }

        private void CheckGateStep()
        {
            if (mover == null) return;
            GridPos here = GardenView.CellAt(mover.transform.position);
            if (!here.Equals(lastPlayerCell))
            {
                if (lastPlayerCell.x != int.MinValue) movedOnce = true;
                lastPlayerCell = here;
                if (movedOnce && root.ObjectAt(here, out GardenTile t) && t == GardenTile.Gate) root.Leave();
            }
        }

        // ---------------- Using ----------------

        /// <summary>
        /// The tool's action – except on a ripe plant, where every tool harvests (nothing else would
        /// make sense there, and switching to the hand just to pick would only slow the ritual down).
        /// </summary>
        private GardenAction CurrentAction()
        {
            Plot plot = root.Garden.PlotAt(Target);
            if (plot != null && plot.crop != null && plot.crop.ripe) return GardenAction.Harvest;
            return tool == GardenTool.Seeds && selectedSeed == GardenItems.CompostItemId ? GardenAction.Compost : GardenTexts.ActionOf(tool);
        }

        private ToolContext Context()
        {
            PlantParams seed = selectedSeed != null ? root.Book.BySeed(selectedSeed) : null;
            return new ToolContext
            {
                seedPlantId = seed?.id,
                seedCount = seed != null ? GardenItems.Count(seed.seedItemId) : 0,
                compost = GardenItems.Count(GardenItems.CompostItemId)
            };
        }

        private bool Perform(GridPos target, GridPos dir)
        {
            GardenAction action = CurrentAction();
            ToolContext ctx = Context();
            ActionOutcome o = root.Garden.Apply(target, action, ctx);
            if (!o.Done) return false;

            Vector2 c = GardenView.CellCenter(target);
            GardenFeedback fx = root.Feedback;
            switch (o.check.effect)
            {
                case ActionEffect.Till:
                    fx.Crumbs(c);
                    fx.Play(GardenSound.Hoe);
                    break;
                case ActionEffect.Sow:
                    GardenItems.RemoveOne(root.Book.Get(ctx.seedPlantId).seedItemId);
                    fx.Seeds(c);
                    fx.Play(GardenSound.Sow);
                    break;
                case ActionEffect.Water:
                    fx.Drops(c);
                    fx.Play(GardenSound.Water);
                    if (root.State.wideCan && tool == GardenTool.Can) WaterBehind(target, dir, ctx);
                    break;
                case ActionEffect.Harvest:
                    Harvest(o, c);
                    break;
                case ActionEffect.Flatten:
                    fx.Crumbs(c);
                    fx.Play(GardenSound.Dig);
                    break;
                case ActionEffect.DigUp:
                    fx.Crumbs(c);
                    fx.Play(GardenSound.Dig);
                    if (o.seedReturned != null)
                    {
                        int left = GardenItems.Add(o.seedReturned, 1);
                        if (left > 0) root.AddToChest(o.seedReturned, left);
                        root.Hud.Message(GardenTexts.SeedBack(root.Book.BySeed(o.seedReturned)?.id));
                    }

                    break;
                case ActionEffect.Compost:
                    GardenItems.RemoveOne(GardenItems.CompostItemId);
                    fx.Crumbs(c);
                    fx.Play(GardenSound.Dig, 0.7f);
                    break;
            }

            root.View.Refresh(target);
            root.RaiseAction(o, action);
            root.MarkDirty();
            return true;
        }

        /// <summary>1x3 can: the two cells behind the target get water too, if they want it.</summary>
        private void WaterBehind(GridPos target, GridPos dir, ToolContext ctx)
        {
            for (int i = 1; i <= 2; i++)
            {
                GridPos p = target + dir * i;
                ActionOutcome o = root.Garden.Apply(p, GardenAction.Water, ctx);
                if (!o.Done) continue;
                root.Feedback.Drops(GardenView.CellCenter(p));
                root.View.Refresh(p);
                root.RaiseAction(o, GardenAction.Water);
            }
        }

        private void Harvest(ActionOutcome o, Vector2 at)
        {
            HarvestResult h = o.harvest;
            GardenDelivery where = GardenItems.DeliverHarvest(h.ingredientId, h.quality, h.count);
            PlantDefinition def = root.Settings.Definition(h.plantId);
            Color color = def != null ? def.ripeColor : PlantPalette.Ripe(h.plantId);
            root.Feedback.HarvestHop(at, color, h.count);
            root.Feedback.Play(GardenSound.Harvest);
            if (h.quality == HarvestQuality.Excellent) root.Feedback.Sparkle(at, color);
            string line = GardenTexts.Harvested(h.ingredientId, h.quality, h.count);
            if (h.seeds > 0 && !string.IsNullOrEmpty(h.seedItemId))
            {
                int left = GardenItems.Add(h.seedItemId, h.seeds);
                if (left > 0) root.AddToChest(h.seedItemId, left);
                line += " · " + GardenTexts.SeedFound(h.plantId);
            }

            if (where == GardenDelivery.Pantry) line += GardenTexts.ToPantry;
            root.Hud.Message(line);
        }

        private void UseObject(GardenTile obj, GridPos at)
        {
            switch (obj)
            {
                case GardenTile.Well:
                    root.RefillCan(at);
                    break;
                case GardenTile.Chest:
                    root.OpenChest(at);
                    break;
                case GardenTile.Compost:
                    root.UseCompostBin(at);
                    break;
                case GardenTile.Gate:
                    root.Leave();
                    break;
                case GardenTile.Lantern:
                    if (Time.unscaledTime <= endDayConfirmUntil)
                    {
                        endDayConfirmUntil = 0f;
                        root.EndDay();
                    }
                    else
                    {
                        endDayConfirmUntil = Time.unscaledTime + root.Settings.comfort.endDayConfirmSeconds;
                        root.Hud.Message(GardenTexts.EndDayConfirm);
                    }

                    break;
            }
        }

        // ---------------- HUD ----------------

        private static string ObjectLabel(GardenTile t)
        {
            switch (t)
            {
                case GardenTile.Well: return GardenTexts.WellLabel;
                case GardenTile.Lantern: return GardenTexts.LanternLabel;
                case GardenTile.Chest: return GardenTexts.ChestLabel;
                case GardenTile.Compost: return GardenTexts.CompostLabel;
                case GardenTile.Gate: return GardenTexts.GateLabel;
                default: return "";
            }
        }

        private string HintFor(ActionCheck check)
        {
            if (check.Valid && check.effect == ActionEffect.Harvest) return GardenTexts.Effect(check.effect, null);
            if (check.Valid) return GardenTexts.ToolName(tool) + ": " + GardenTexts.Effect(check.effect, Context().seedPlantId);
            return check.reason == ActionReason.NotABed ? "" : GardenTexts.Reason(check.reason);
        }

        private void UpdateHud()
        {
            int count = selectedSeed != null ? GardenItems.Count(selectedSeed) : 0;
            if (!ReferenceEquals(selectedSeed, seedKey) || count != seedCountKey)
            {
                seedKey = selectedSeed;
                seedCountKey = count;
                string name = selectedSeed == GardenItems.CompostItemId ? GardenTexts.CompostName : GardenTexts.SeedName(root.Book.BySeed(selectedSeed)?.id);
                root.Hud.SetSeed(GardenTexts.SeedSlot(selectedSeed == null ? null : name, count));
            }

            root.Hud.SetCan(root.Garden.CanWater, root.Settings.garden.canCapacity);
            GameClock clock = GameClock.Instance;
            int day = clock != null ? clock.TotalDays : root.Garden.Day;
            if (day != dayKey || root.Garden.RainyToday != rainKey)
            {
                dayKey = day;
                rainKey = root.Garden.RainyToday;
                root.Hud.SetDay(clock != null ? GardenTexts.DateLine(clock.DateLabel, rainKey) : GardenTexts.DayLine(root.Garden.Day, rainKey));
            }

            int options = (root.State.wideCan ? 1 : 0) + (root.State.gardenHelp ? 2 : 0);
            if (options != optionsKey)
            {
                optionsKey = options;
                root.Hud.SetOptions(GardenTexts.Options(root.State.wideCan, root.State.gardenHelp));
            }
        }
    }
}
