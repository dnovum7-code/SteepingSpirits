using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// A small engine-free stand-in for the Unity scene of a level: box
    /// collisions against the tile layout (ground, ghost and leaf platforms,
    /// one-way platforms, dew leaves), wind columns, swings, lanterns, brambles,
    /// ingredients and the goal – driven by the real <see cref="PlayerMotor"/>.
    /// Used to let the route bot play levels in plain dotnet tests. It mirrors
    /// the Unity setup closely but not exactly (leaves do not sink, ghost
    /// platforms are always lit).
    /// </summary>
    public sealed class TileWorldSim
    {
        public const float HalfWidth = 0.35f;
        public const float BodyHeight = 1.4f;

        private struct Box
        {
            public float minX, minY, maxX, maxY;
            public bool oneWay;
            public bool dew;

            public Box(float minX, float minY, float maxX, float maxY)
            {
                this.minX = minX;
                this.minY = minY;
                this.maxX = maxX;
                this.maxY = maxY;
                oneWay = false;
                dew = false;
            }
        }

        private sealed class Swing
        {
            public LevelMarker marker;
            public SwingModel model;
            public float remountTimer;
            public Vec2 pivot;
            public Vec2 Seat => pivot + model.SeatOffset;
        }

        private readonly LevelLayout layout;
        private readonly SpiritElementParams elements;
        private readonly List<Box> boxes = new List<Box>();
        private readonly List<Swing> swings = new List<Swing>();
        private readonly HashSet<int> collected = new HashSet<int>();
        private readonly HashSet<int> litLanterns = new HashSet<int>();
        private readonly LevelReachability reach;

        public readonly PlayerMotor Motor;
        public Vec2 Feet;
        public Vec2 Respawn;
        public int Catches { get; private set; }
        public bool Finished { get; private set; }
        public int Collected => collected.Count;
        public int LanternsLit => litLanterns.Count;
        public float Time { get; private set; }
        public bool Grounded { get; private set; }
        public int RidingSwing { get; private set; } = -1;

        private float dewCooldown;

        public TileWorldSim(LevelLayout layout, MovementParams move = null, SpiritElementParams elements = null)
        {
            this.layout = layout;
            this.elements = elements ?? new SpiritElementParams();
            Motor = new PlayerMotor(move ?? new MovementParams());
            reach = new LevelReachability(layout, Motor.Params, this.elements);
            LevelMarker start = layout.Start;
            Feet = new Vec2(start.x + 0.5f, start.y);
            Respawn = Feet;
            Build();
        }

        public LevelReachability Reachability => reach;

        private void Build()
        {
            foreach (TileRect r in layout.Solids) boxes.Add(new Box(r.x, r.y, r.x + r.width, r.y + r.height));
            foreach (TileRect r in layout.GhostPlatforms) boxes.Add(new Box(r.x, r.y, r.x + r.width, r.y + r.height));
            foreach (TileRect r in layout.LeafPlatforms) boxes.Add(new Box(r.x, r.y + r.height - 0.35f, r.x + r.width, r.y + r.height));
            foreach (TileRect r in layout.OneWays)
            {
                boxes.Add(new Box(r.x, r.y + r.height - 0.35f, r.x + r.width, r.y + r.height) { oneWay = true });
            }

            foreach (LevelMarker d in layout.All(TileKind.DewLeaf))
            {
                boxes.Add(new Box(d.x + 0.5f - 0.8f, d.y, d.x + 0.5f + 0.8f, d.y + 0.35f) { dew = true });
            }

            foreach (LevelMarker o in layout.All(TileKind.Swing))
            {
                var p = CloneSwing(o);
                swings.Add(new Swing { marker = o, model = new SwingModel(p), pivot = new Vec2(o.x + 0.5f, o.y + 0.5f) });
            }
        }

        private SwingParams CloneSwing(LevelMarker o)
        {
            SwingParams p = elements.swing.Clone();
            p.ropeLength = reach.RopeLength(o);
            p.maxAngleDeg = reach.MaxAngleDeg(o);
            return p;
        }

        // ------------------------------------------------------------------
        // Observation for the bot
        // ------------------------------------------------------------------

        public RouteObservation Observe()
        {
            var o = new RouteObservation
            {
                feet = Feet,
                velocity = Motor.Velocity,
                grounded = Grounded,
                riding = RidingSwing >= 0,
                groundAheadRight = GroundAt(Feet.x + HalfWidth + 0.25f, Feet.y),
                groundAheadLeft = GroundAt(Feet.x - HalfWidth - 0.25f, Feet.y),
                catches = Catches
            };

            if (RidingSwing >= 0)
            {
                Swing s = swings[RidingSwing];
                o.swingAngle = s.model.Angle;
                o.swingAngularVelocity = s.model.AngularVelocity;
                o.swingAmplitude = s.model.Amplitude;
                o.swingMaxAngle = s.model.MaxAngle;
                o.swingPivot = s.pivot;
            }

            return o;
        }

        /// <summary>Seat position of the swing whose pivot is closest to a point.</summary>
        public Vec2 SeatNear(Vec2 pivot)
        {
            Swing best = null;
            foreach (Swing s in swings)
            {
                if (best == null || Vec2.Distance(s.pivot, pivot) < Vec2.Distance(best.pivot, pivot)) best = s;
            }

            return best != null ? best.Seat : pivot;
        }

        private bool GroundAt(float x, float feetY)
        {
            foreach (Box b in boxes)
            {
                if (x >= b.minX && x <= b.maxX && feetY - 0.6f <= b.maxY && feetY + 0.05f >= b.maxY) return true;
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Step
        // ------------------------------------------------------------------

        public void Step(float dt, MotorInput input)
        {
            Time += dt;
            dewCooldown -= dt;
            foreach (Swing sw in swings) sw.remountTimer -= dt;
            if (Finished)
            {
                return;
            }

            for (int i = 0; i < swings.Count; i++)
            {
                if (i != RidingSwing) swings[i].model.Step(dt, 0f);
            }

            if (RidingSwing >= 0)
            {
                RideStep(dt, input);
                Triggers();
                return;
            }

            bool rising = Motor.Velocity.y > 0.5f && Motor.Rising;
            Grounded = !rising && Touching(0f, -0.05f, true);
            int wall = 0;
            if (!Grounded)
            {
                if (Touching(0.05f, 0f, false)) wall = 1;
                else if (Touching(-0.05f, 0f, false)) wall = -1;
            }

            MotorEvents ev = Motor.Step(dt, input, new MotorContacts { grounded = Grounded, wallDir = wall });
            Vec2 v = Motor.Velocity;
            v.y += WindAcceleration(v.y) * dt;
            Motor.Velocity = v;

            if (Grounded && dewCooldown <= 0f && OnDew() && (ev & MotorEvents.Jumped) == 0)
            {
                float vy = SpiritMath.DewBounceVelocity(elements.dew, Motor.Params.Gravity, input.jumpHeld);
                Motor.Launch(new Vec2(Motor.Velocity.x, vy), true);
                dewCooldown = elements.dew.cooldown;
            }

            Move(dt);
            TryMount(input);
            Triggers();
        }

        private void RideStep(float dt, MotorInput input)
        {
            Swing s = swings[RidingSwing];
            if (input.jumpPressed)
            {
                Leave(s.model.ReleaseVelocity);
                return;
            }

            if (input.moveY < -0.6f)
            {
                Leave(s.model.SeatVelocity);
                return;
            }

            s.model.Step(dt, input.moveX);
            Feet = s.Seat + new Vec2(0f, 0.1f);
        }

        private void Leave(Vec2 velocity)
        {
            swings[RidingSwing].remountTimer = elements.swing.remountDelay;
            RidingSwing = -1;
            Motor.Launch(velocity, true);
        }

        private void TryMount(MotorInput input)
        {
            for (int i = 0; i < swings.Count; i++)
            {
                Swing s = swings[i];
                if (s.remountTimer <= 0f && Vec2.Distance(Feet, s.Seat) <= elements.swing.mountRadius)
                {
                    RidingSwing = i;
                    s.model.Mount(s.model.SeatOffset, Motor.Velocity);
                    Motor.Reset();
                    return;
                }
            }
        }

        private float WindAcceleration(float vy)
        {
            float total = 0f;
            foreach (LevelMarker w in layout.All(TileKind.WindSpirit))
            {
                int h = reach.ColumnHeight(w);
                float cx = w.x + 0.5f, half = elements.wind.width * 0.5f;
                bool overlap = Feet.x + HalfWidth > cx - half && Feet.x - HalfWidth < cx + half
                               && Feet.y + BodyHeight > w.y && Feet.y < w.y + h;
                if (overlap)
                {
                    total += SpiritMath.Updraft(elements.wind, h, Feet.y - w.y, vy);
                }
            }

            return total;
        }

        private bool OnDew()
        {
            foreach (Box b in boxes)
            {
                if (b.dew && Overlaps(b, Feet.x, Feet.y - 0.05f, false)) return true;
            }

            return false;
        }

        private void Move(float dt)
        {
            Vec2 v = Motor.Velocity;

            // Horizontal, with ledge step-up like the Unity player.
            float nx = Feet.x + v.x * dt;
            if (Blocked(nx, Feet.y, false))
            {
                if (Math.Abs(v.x) > 0.5f && v.y <= 1f
                    && CornerCorrection.TryLedge(dy => Blocked(nx, Feet.y + dy, false), Motor.Params.ledgeCorrection, out float up))
                {
                    Feet.y += up;
                    Feet.x = nx;
                }
                else
                {
                    v.x = 0f;
                }
            }
            else
            {
                Feet.x = nx;
            }

            // Vertical, with head corner correction.
            float ny = Feet.y + v.y * dt;
            if (v.y > 0f && Blocked(Feet.x, ny, false))
            {
                int prefer = Math.Abs(v.x) > 0.1f ? Math.Sign(v.x) : Motor.Facing;
                if (CornerCorrection.TryHead(dx => Blocked(Feet.x + dx, ny, false), Motor.Params.cornerCorrection, prefer, out float dx2))
                {
                    Feet.x += dx2;
                    Feet.y = ny;
                }
                else
                {
                    v.y = 0f;
                }
            }
            else if (v.y <= 0f && Blocked(Feet.x, ny, true, Feet.y))
            {
                Feet.y = TopBelow(Feet.x, Feet.y);
                v.y = 0f;
            }
            else
            {
                Feet.y = ny;
            }

            Motor.Velocity = v;
        }

        private float TopBelow(float x, float feetY)
        {
            float best = float.MinValue;
            foreach (Box b in boxes)
            {
                if (x + HalfWidth > b.minX && x - HalfWidth < b.maxX && b.maxY <= feetY + 0.001f && b.maxY > best) best = b.maxY;
            }

            return best == float.MinValue ? feetY : best;
        }

        private bool Blocked(float x, float y, bool includeOneWay, float prevFeetY = float.MinValue)
        {
            foreach (Box b in boxes)
            {
                if (b.oneWay)
                {
                    if (!includeOneWay || prevFeetY < b.maxY - 0.01f) continue;
                }

                if (Overlaps(b, x, y, false)) return true;
            }

            return false;
        }

        private bool Touching(float dx, float dy, bool includeOneWay)
        {
            foreach (Box b in boxes)
            {
                if (b.oneWay && (!includeOneWay || Feet.y < b.maxY - 0.08f)) continue;
                if (Overlaps(b, Feet.x + dx, Feet.y + dy, false)) return true;
            }

            return false;
        }

        private static bool Overlaps(Box b, float feetX, float feetY, bool unused)
        {
            const float e = 0.001f;
            return feetX + HalfWidth > b.minX + e && feetX - HalfWidth < b.maxX - e
                   && feetY + BodyHeight > b.minY + e && feetY < b.maxY - e;
        }

        private bool BodyOverlapsRect(float cx, float cy, float w, float h)
        {
            return Feet.x + HalfWidth > cx - w * 0.5f && Feet.x - HalfWidth < cx + w * 0.5f
                   && Feet.y + BodyHeight > cy - h * 0.5f && Feet.y < cy + h * 0.5f;
        }

        private void Triggers()
        {
            foreach (LevelMarker m in layout.Markers)
            {
                float cx = m.x + 0.5f, cy = m.y + 0.5f;
                switch (m.kind)
                {
                    case TileKind.Lantern:
                        if (BodyOverlapsRect(cx, cy, 1.2f, 2.5f) && litLanterns.Add(m.index))
                        {
                            Respawn = new Vec2(cx, m.y);
                        }

                        break;
                    case TileKind.Ingredient:
                        if (BodyOverlapsRect(cx, cy, 0.9f, 0.9f)) collected.Add(m.x * 1000 + m.y);
                        break;
                    case TileKind.Goal:
                        if (BodyOverlapsRect(cx, cy + 0.5f, 1.2f, 2f)) Finished = true;
                        break;
                    case TileKind.Bramble:
                        if (RidingSwing < 0 && BodyOverlapsRect(cx, cy - 0.2f, 0.8f, 0.5f)) Catch();
                        break;
                }
            }

            if (Feet.y < -3f)
            {
                Catch();
            }
        }

        private void Catch()
        {
            Catches++;
            Feet = Respawn;
            Motor.Reset();
            RidingSwing = -1;
        }
    }
}
