using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

// From Veldrid NeoDemo

namespace SimpleGui
{
    [Obsolete("Impement better API for input in engine")]
    public static class InputTracker
    {
        static readonly HashSet<Key> CurrentlyPressedKeys = new HashSet<Key>();
        static readonly HashSet<Key> NewKeysThisFrame = new HashSet<Key>();

        static readonly HashSet<MouseButton> CurrentlyPressedMouseButtons = new HashSet<MouseButton>();
        static readonly HashSet<MouseButton> NewMouseButtonsThisFrame = new HashSet<MouseButton>();

        public static Vector2 MousePosition;
        public static InputSnapshot FrameSnapshot { get; private set; }

        public static bool GetKey(Key key)
        {
            return CurrentlyPressedKeys.Contains(key);
        }

        public static bool GetKeyDown(Key key, out bool Repeat)
        {
            Repeat = false;
            return CurrentlyPressedKeys.Contains(key);
        }

        public static bool GetMouseButton(MouseButton button)
        {
            return CurrentlyPressedMouseButtons.Contains(button);
        }

        public static bool GetMouseButtonDown(MouseButton button)
        {
            return NewMouseButtonsThisFrame.Contains(button);
        }

        public static void UpdateFrameInput(InputSnapshot snapshot)
        {
            FrameSnapshot = snapshot;
            NewKeysThisFrame.Clear();
            NewMouseButtonsThisFrame.Clear();

            MousePosition = snapshot.MousePosition;
            foreach (KeyEvent ke in snapshot.KeyEvents)
            {
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            foreach (MouseEvent me in snapshot.MouseEvents)
            {
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }
        }

        private static void MouseUp(MouseButton mouseButton)
        {
            CurrentlyPressedMouseButtons.Remove(mouseButton);
            NewMouseButtonsThisFrame.Remove(mouseButton);
        }

        private static void MouseDown(MouseButton mouseButton)
        {
            if (CurrentlyPressedMouseButtons.Add(mouseButton))
            {
                NewMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private static void KeyUp(Key key)
        {
            CurrentlyPressedKeys.Remove(key);
            NewKeysThisFrame.Remove(key);
        }

        private static void KeyDown(Key key)
        {
            if (CurrentlyPressedKeys.Add(key))
            {
                NewKeysThisFrame.Add(key);
            }
        }
    }
}
