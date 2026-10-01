using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Core
{
    /// <summary>
    /// Zentrale Tastenbelegung des Spiels. Alle Skripte fragen hier nach
    /// („wurde Angriff gedrückt?") statt selbst Tasten abzufragen – so ändert
    /// man die Belegung an EINER Stelle.
    ///
    /// Funktioniert mit dem neuen Input System (ENABLE_INPUT_SYSTEM) und dem
    /// alten Input Manager. Mit dem Input System gehen zusätzlich Gamepads.
    ///
    /// Belegung (Tastatur / Gamepad):
    ///   Laufen        WASD / Pfeile        · linker Stick / Steuerkreuz
    ///   Angriff       Leertaste / J / LMB   · X (West)
    ///   Interagieren  E / F                 · A (Süd)
    ///   Dialog weiter E / F / Leertaste / Enter · A (Süd)
    ///   Inventar      I                     · Y (Nord)
    ///   Questlog      B                     · Select/View
    ///   Abbrechen     Esc                   · B (Ost)
    ///   Cheats        F1 / ^
    ///   Hilfe         N
    ///
    /// Jump'n'Run (Platformer-Szene):
    ///   Springen      Leertaste / C         · A (Süd)
    ///   Dash          Shift / X / L         · X (West)
    ///   Greifen       K / Strg (halten)     · RB / LB / RT (halten)
    /// (Tasten nach US-Layout-Position, wie es das Input System macht.)
    /// </summary>
    public static class GameInput
    {
        /// <summary>Taste fürs Interagieren, wie sie im Prompt steht („[E] Reden").</summary>
        public const string InteractKeyLabel = "E";

        // Von OnGUI-Fenstern gemeldete Bereiche (GUI-Koordinaten) – darüber
        // löst ein Mausklick keinen Angriff aus.
        private static readonly List<Rect> guiRects = new List<Rect>();
        private static readonly List<Rect> guiRectsLastFrame = new List<Rect>();
        private static int guiRectsFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            guiRects.Clear();
            guiRectsLastFrame.Clear();
            guiRectsFrame = -1;
        }

        // ---------------------------------------------------------------
        // Bewegung
        // ---------------------------------------------------------------

        /// <summary>Bewegungsrichtung, Länge max. 1.</summary>
        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
                }

                Gamepad g = Gamepad.current;
                if (g != null)
                {
                    Vector2 stick = g.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.04f) v += stick;
                    v += g.dpad.ReadValue();
                }
#else
                v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        // ---------------------------------------------------------------
        // Aktionen (gedrückt in diesem Frame)
        // ---------------------------------------------------------------

        public static bool AttackPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Mouse m = Mouse.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.jKey.wasPressedThisFrame))
                       || (m != null && m.leftButton.wasPressedThisFrame && !IsPointerOverUi())
                       || (g != null && g.buttonWest.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J)
                       || (Input.GetMouseButtonDown(0) && !IsPointerOverUi());
#endif
            }
        }

        public static bool InteractPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame))
                       || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
#endif
            }
        }

        public static bool AdvanceDialoguePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame
                                      || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                       || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F)
                       || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
            }
        }

        public static bool InventoryTogglePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                       || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.I);
#endif
            }
        }

        public static bool JournalTogglePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
                       || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.B);
#endif
            }
        }

        public static bool CancelPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                       || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        public static bool CheatTogglePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                return k != null && (k.f1Key.wasPressedThisFrame || k.backquoteKey.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote);
#endif
            }
        }

        public static bool HelpTogglePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.N);
#endif
            }
        }

        // ---------------------------------------------------------------
        // Jump'n'Run
        // ---------------------------------------------------------------

        public static bool JumpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.cKey.wasPressedThisFrame))
                       || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C);
#endif
            }
        }

        public static bool JumpHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.spaceKey.isPressed || k.cKey.isPressed))
                       || (g != null && g.buttonSouth.isPressed);
#else
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.C);
#endif
            }
        }

        public static bool DashPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame
                                      || k.xKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame))
                       || (g != null && g.buttonWest.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)
                       || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.L);
#endif
            }
        }

        public static bool GrabHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.kKey.isPressed || k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed))
                       || (g != null && (g.rightShoulder.isPressed || g.leftShoulder.isPressed || g.rightTrigger.isPressed));
#else
                return Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
            }
        }

        // ---------------------------------------------------------------
        // Maus über UI?
        // ---------------------------------------------------------------

        /// <summary>
        /// OnGUI-Fenster melden hier jeden Frame ihren Bereich (GUI-Koordinaten,
        /// y von oben). Ein Klick darin löst dann keinen Angriff aus.
        /// </summary>
        public static void ClaimGuiArea(Rect guiRect)
        {
            if (guiRectsFrame != Time.frameCount)
            {
                guiRectsLastFrame.Clear();
                guiRectsLastFrame.AddRange(guiRects);
                guiRects.Clear();
                guiRectsFrame = Time.frameCount;
            }

            guiRects.Add(guiRect);
        }

        /// <summary>Liegt der Mauszeiger über uGUI oder einem gemeldeten OnGUI-Fenster?</summary>
        public static bool IsPointerOverUi()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            Vector2 mouse = MousePosition();
            var guiPoint = new Vector2(mouse.x, Screen.height - mouse.y);

            // Nur frische Meldungen zählen – ein geschlossenes Fenster meldet
            // nichts mehr und blockiert dann auch nicht mehr.
            int frame = Time.frameCount;
            if (guiRectsFrame >= frame - 1 && Contains(guiRects, guiPoint))
            {
                return true;
            }

            return guiRectsFrame == frame && Contains(guiRectsLastFrame, guiPoint);
        }

        private static bool Contains(List<Rect> rects, Vector2 point)
        {
            foreach (Rect r in rects)
            {
                if (r.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
}
