using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SteepingSpirits.Player;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The player's hands in the workshop: walk to a station, use it, and its window opens with the
    /// batches there, what they are now and where they lead. Rolling and the wok are short minigames;
    /// withering, oxidation and drying run on game time. The game clock keeps running while a window
    /// is open (only the figure stands still) – oxidation keeps moving while you watch it.
    /// </summary>
    public class WorkshopController : MonoBehaviour
    {
        private enum Mode
        {
            Walk,
            Window,
            Rolling,
            Wok
        }

        private TeaWorkshop root;
        private PlayerController2D mover;
        private Mode mode = Mode.Walk;
        private StationView station;
        private int gameBatch = -1;
        private RollSession roll;
        private WokSession wok;
        private float steamAt;
        private float refreshAt;
        private float watched;
        private string buttonKey = "";
        private (int x, int y) lastCell = (int.MinValue, 0);
        private bool movedOnce;

        /// <summary>The batch the debug overlay shows (in a minigame, or the first one at the open station).</summary>
        public int FocusBatch { get; private set; } = -1;

        public StationView Station => station;

        public void Init(TeaWorkshop workshop, PlayerController2D player)
        {
            root = workshop;
            mover = player;
        }

        private ProcessingParams P => root.Model.settings;
        private bool Numbers => root.State.showPercent;

        private void Update()
        {
            if (root == null || root.Model == null) return;
            switch (mode)
            {
                case Mode.Walk:
                    Walk();
                    break;
                case Mode.Window:
                    if (WorkshopInput.ClosePressed)
                    {
                        CloseWindow();
                        break;
                    }

                    WatchBasket(Time.deltaTime);
                    if (Time.unscaledTime >= refreshAt)
                    {
                        refreshAt = Time.unscaledTime + 0.25f;
                        Fill(false);
                    }

                    break;
                case Mode.Rolling:
                    UpdateRolling(Time.deltaTime);
                    break;
                case Mode.Wok:
                    UpdateWok(Time.deltaTime);
                    break;
            }
        }

        // ---------------- Walking ----------------

        private void Walk()
        {
            if (mover == null) return;
            Vector2 pos = mover.transform.position;
            (int x, int y) here = (Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
            if (here != lastCell)
            {
                if (lastCell.x != int.MinValue) movedOnce = true;
                lastCell = here;
                if (movedOnce && root.Layout.At(here.x, here.y) == WorkshopTile.Door)
                {
                    root.Leave();
                    return;
                }
            }

            Vector2 f = mover.Facing;
            (int x, int y) front = Mathf.Abs(f.x) > Mathf.Abs(f.y) ? (here.x + (f.x > 0 ? 1 : -1), here.y) : (here.x, here.y + (f.y > 0 ? 1 : -1));
            StationView target = root.StationAt(front.x, front.y) ?? root.StationAt(here.x, here.y);
            root.Ui.SetHint(target != null ? NameOf(target.Tile) : "");
            if (target != null && WorkshopInput.UsePressed) OpenWindow(target);
        }

        private static string NameOf(WorkshopTile t)
        {
            switch (t)
            {
                case WorkshopTile.Shelf: return ProcessingTexts.ShelfName;
                case WorkshopTile.Store: return ProcessingTexts.StoreName;
                case WorkshopTile.Rack: return ProcessingTexts.RackName;
                case WorkshopTile.RollTable: return ProcessingTexts.RollName;
                case WorkshopTile.Basket: return ProcessingTexts.BasketName;
                case WorkshopTile.Wok: return ProcessingTexts.WokName;
                case WorkshopTile.Dryer: return ProcessingTexts.DryerName;
                case WorkshopTile.BlendTable: return ProcessingTexts.BlendName;
                case WorkshopTile.Door: return ProcessingTexts.ExitLabel;
                default: return "";
            }
        }

        // ---------------- Windows ----------------

        private void OpenWindow(StationView s)
        {
            if (s.Tile == WorkshopTile.Door)
            {
                root.Leave();
                return;
            }

            station = s;
            mode = Mode.Window;
            if (mover != null) mover.MovementLocked = true;
            root.Ui.SetHint("");
            root.Ui.Open(NameOf(s.Tile));
            buttonKey = "";
            Fill(true);
        }

        public void CloseWindow()
        {
            FlushWatched();
            root.Ui.Close();
            mode = Mode.Walk;
            station = null;
            FocusBatch = -1;
            if (mover != null) mover.MovementLocked = false;
        }

        /// <summary>Writes the window for the open station. Buttons are only rebuilt when the choices change.</summary>
        private void Fill(bool focus)
        {
            if (station == null) return;
            root.Model.Sync();
            var text = new StringBuilder();
            var actions = new List<(string label, System.Action act)>();
            FocusBatch = -1;
            switch (station.Tile)
            {
                case WorkshopTile.Store: FillStore(text, actions); break;
                case WorkshopTile.Shelf: FillShelf(text, actions); break;
                case WorkshopTile.Rack: FillPassive(text, actions, BatchPlace.WitherRack, BatchAction.Wither, ProcessingTexts.LayOn, ProcessingTexts.TakeOff); break;
                case WorkshopTile.Basket: FillPassive(text, actions, BatchPlace.OxidationBasket, BatchAction.Oxidize, ProcessingTexts.PutInBasket, ProcessingTexts.StopOxidation); break;
                case WorkshopTile.Dryer: FillPassive(text, actions, BatchPlace.DryBasket, BatchAction.Dry, ProcessingTexts.PutOnEmbers, ProcessingTexts.TakeOff); break;
                case WorkshopTile.RollTable: FillActive(text, actions, BatchAction.Roll, ProcessingTexts.RollStart); break;
                case WorkshopTile.Wok: FillActive(text, actions, BatchAction.Fix, ProcessingTexts.WokStart); break;
                case WorkshopTile.BlendTable: root.Blending.Fill(text, actions); break;
            }

            actions.Add((ProcessingTexts.Close, CloseWindow));
            root.Ui.SetBody(text.ToString());

            var key = new StringBuilder();
            foreach (var a in actions) key.Append(a.label).Append('|');
            if (key.ToString() == buttonKey) return;
            buttonKey = key.ToString();
            root.Ui.ClearButtons();
            foreach (var a in actions)
            {
                System.Action act = a.act;
                root.Ui.AddButton(a.label, () =>
                {
                    act();
                    if (mode == Mode.Window) Fill(true);
                });
            }

            root.Ui.Focus(true);
        }

        private void Describe(StringBuilder text, LeafBatch b)
        {
            if (FocusBatch < 0) FocusBatch = b.id;
            text.Append("<b>").Append(ProcessingTexts.BatchTitle(b)).Append("</b> – ").Append(ProcessingTexts.Place(b.place)).Append('\n');
            text.Append(ProcessingTexts.State(b, P, Numbers)).Append('\n');
            text.Append("<i>").Append(ProcessingTexts.Heading(b, P)).Append(" · ").Append(ProcessingTexts.NotesPreview(b, P)).Append("</i>\n\n");
        }

        private void FillStore(StringBuilder text, List<(string, System.Action)> actions)
        {
            List<LeafSource> sources = WorkshopItems.AvailableLeaves();
            if (sources.Count == 0) text.Append(ProcessingTexts.NoLeaves).Append('\n');
            foreach (LeafSource src in sources)
            {
                text.Append(ProcessingTexts.StarterLine(src.sourceId, src.tier, src.Total)).Append('\n');
                int n = Mathf.Min(Mathf.Max(1, root.Settings.comfort.batchSize), src.Total);
                LeafSource s = src;
                actions.Add((ProcessingTexts.NewBatch + ": " + n + "× " + ProcessingTexts.SourceName(src.sourceId, src.tier), () => root.StartBatch(s, n)));
            }

            if (!root.State.starterGiven) actions.Add((ProcessingTexts.StarterPack, root.GiveStarterPack));
        }

        private void FillShelf(StringBuilder text, List<(string, System.Action)> actions)
        {
            if (root.Model.Batches.Count == 0) text.Append(ProcessingTexts.ShelfEmpty);
            foreach (LeafBatch b in root.Model.Batches)
            {
                Describe(text, b);
                if (BatchRules.Check(b, BatchAction.Finish, P).ok)
                {
                    int id = b.id;
                    actions.Add((ProcessingTexts.Act(ProcessingTexts.Unpack, id), () => root.FinishBatch(id)));
                }
            }

            actions.Add((Numbers ? ProcessingTexts.PercentOff : ProcessingTexts.PercentOn, () =>
            {
                root.State.showPercent = !root.State.showPercent;
                root.MarkDirty();
            }));
        }

        private void FillPassive(StringBuilder text, List<(string, System.Action)> actions, BatchPlace place, BatchAction putAction, string putVerb, string takeVerb)
        {
            bool any = false;
            foreach (LeafBatch b in root.Model.Batches)
            {
                if (b.place != place) continue;
                any = true;
                Describe(text, b);
                int id = b.id;
                actions.Add((ProcessingTexts.Act(takeVerb, id), () => root.Apply(id, BatchAction.TakeOut)));
            }

            if (!any) text.Append(ProcessingTexts.Empty).Append("\n\n");
            AddShelfChoices(text, actions, putAction, putVerb, id => root.Apply(id, putAction));
        }

        private void FillActive(StringBuilder text, List<(string, System.Action)> actions, BatchAction action, string verb)
        {
            text.Append(action == BatchAction.Roll ? (root.State.evenRolling ? ProcessingTexts.RollComfortHint : ProcessingTexts.RollHint) : ProcessingTexts.WokHint).Append("\n\n");
            AddShelfChoices(text, actions, action, verb, id => StartGame(id, action));
            if (action == BatchAction.Roll)
            {
                actions.Add((root.State.evenRolling ? ProcessingTexts.ComfortOn : ProcessingTexts.ComfortOff, () =>
                {
                    root.State.evenRolling = !root.State.evenRolling;
                    root.MarkDirty();
                }));
            }
        }

        /// <summary>Batches from the shelf that may go here; the others say calmly why not.</summary>
        private void AddShelfChoices(StringBuilder text, List<(string, System.Action)> actions, BatchAction action, string verb, System.Action<int> run)
        {
            foreach (LeafBatch b in root.Model.Batches)
            {
                if (b.place != BatchPlace.Shelf) continue;
                BatchCheck c = BatchRules.Check(b, action, P);
                int id = b.id;
                if (c.ok) actions.Add((ProcessingTexts.Act(verb, id), () => run(id)));
                else text.Append(ProcessingTexts.BatchTitle(b)).Append(": ").Append(ProcessingTexts.Reason(c.reason)).Append('\n');
            }
        }

        /// <summary>Watching the oxidation basket counts as active time (telemetry).</summary>
        private void WatchBasket(float dt)
        {
            if (station == null || station.Tile != WorkshopTile.Basket) return;
            watched += dt;
        }

        private void FlushWatched()
        {
            if (watched <= 0f) return;
            foreach (LeafBatch b in root.Model.Batches)
            {
                if (b.place == BatchPlace.OxidationBasket) root.Model.AddActiveSeconds(b.id, watched);
            }

            watched = 0f;
        }

        // ---------------- Minigames ----------------

        private void StartGame(int id, BatchAction action)
        {
            gameBatch = id;
            FocusBatch = id;
            root.Ui.ClearButtons();
            buttonKey = "";
            root.Ui.Focus(false);
            if (action == BatchAction.Roll)
            {
                roll = new RollSession(P.roll, root.State.evenRolling);
                mode = Mode.Rolling;
            }
            else
            {
                wok = new WokSession(P.wok, id * 31 + root.NextWokSeed());
                mode = Mode.Wok;
            }

            root.Ui.SetBody("");
        }

        private void UpdateRolling(float dt)
        {
            bool held = WorkshopInput.ActionHeld;
            int before = roll.Kneads;
            roll.Tick(dt, held);
            if (!root.State.evenRolling && (WorkshopInput.KneadPressed || WorkshopInput.StickCircle())) roll.Knead();
            if (roll.Kneads > before)
            {
                root.Feedback.Play(WorkshopSound.Knead, 0.6f + 0.4f * roll.LastKneadEvenness);
                if (roll.Kneads % 3 == 0) root.Feedback.LeafBits(root.StationCenter(WorkshopTile.RollTable), LeafLook.Colour(root.Model.Get(gameBatch)), 2);
            }

            if (WorkshopInput.ClosePressed) roll.Stop();
            LeafBatch b = root.Model.Get(gameBatch);
            RollOutcome o = roll.Outcome;
            root.Ui.ShowRoll(o.intensity, roll.BeatPhase, roll.LastKneadEvenness, b != null ? LeafLook.Colour(b) : Color.green,
                ProcessingTexts.RollWords(o.intensity, roll.Kneads > 1 ? o.evenness : 1f, Numbers) + "\n\n" + ProcessingTexts.RollStop + ": Esc / B");
            if (!roll.Done) return;

            root.ApplyRoll(gameBatch, o);
            root.Ui.Message(ProcessingTexts.RollDone);
            EndGame();
        }

        private void UpdateWok(float dt)
        {
            wok.Tick(dt, WorkshopInput.ActionHeld);
            root.Feedback.SetSizzle(0.2f + 0.8f * wok.Heat);
            root.StationGlow(WorkshopTile.Wok, wok.Heat);
            if (Time.time >= steamAt)
            {
                steamAt = Time.time + Mathf.Lerp(0.6f, 0.15f, wok.Heat);
                root.Feedback.Steam(root.StationCenter(WorkshopTile.Wok));
            }

            root.Ui.ShowWok(wok.Heat, wok.WindowMin, wok.WindowMax, ProcessingTexts.WokWords(wok.Heat, wok.WindowMin, wok.WindowMax, Numbers));
            if (!wok.Done) return;

            root.Feedback.SetSizzle(0f);
            root.StationGlow(WorkshopTile.Wok, 0f);
            root.ApplyWok(gameBatch, wok.Outcome);
            root.Ui.Message(ProcessingTexts.WokDone);
            EndGame();
        }

        private void EndGame()
        {
            roll = null;
            wok = null;
            gameBatch = -1;
            root.Ui.HideGame();
            mode = Mode.Window;
            buttonKey = "";
            Fill(true);
        }
    }
}
