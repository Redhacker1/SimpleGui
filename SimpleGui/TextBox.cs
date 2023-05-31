using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts;
using Veldrid;
using TextAlignment = TextRender.TextAlignment;

namespace SimpleGui
{
    public class TextBox : Control
    {
        public Text Label { get; set; }
        public string Text { get; set; }

        public ColoredQuad CursorControl { get; set; }

        protected int XPadding = 6;
        protected int CursorIndex { get; set; }

        public TextBox()
        {
            IsClickable = false;
            IsHoverable = true;

            ColorType = ControlColorType.Input;
        }

        public override void Initialize()
        {
            base.Initialize();

            Label = new Text(Text)
            {
                Size = Size - new Vector2(XPadding, 0),
                Position = new Vector2(XPadding, 0),
                TextAlignment = TextAlignment.Leading,
            };
            Label.Initialize();
            AddChild(Label);

            CursorControl = new ColoredQuad
            {
                Size = new Vector2(2, Label.Size.Y - 6),
                Position = new Vector2(2, 3),
                Color = Settings.Colors.CursorColor.Color
            };
            CursorControl.Initialize();
            AddChild(CursorControl);
        }

        bool firstBackSpacePressed = false;
        public override void Update()
        {
            base.Update();

            bool repeat;

            if (InputTracker.GetKeyDown(Key.BackSpace, out repeat) && !firstBackSpacePressed)
            {
                firstBackSpacePressed = true;
            }
            else if (!InputTracker.GetKeyDown(Key.BackSpace, out _))
            {
                firstBackSpacePressed = false;
            }
            
            
            if (Text.Length > 0)
            {

                if (firstBackSpacePressed || (!firstBackSpacePressed && repeat))
                {

                    if (CursorIndex > 0)
                    {
                        if (CursorIndex == Text.Length)
                        {
                            Text = Text[..^1];
                        }
                        else
                        {
                            string restOfLine = Text.Substring(CursorIndex, (Text.Length - CursorIndex));
                            Text = Text[..(CursorIndex - 1)] + restOfLine;
                        }
                        Label.Content = Text;
                        Label.Recreate();
                        CursorIndex--;
                        CursorControl.Position = GetTextIndexPosition(CursorIndex);
                    }
                }
                else if (InputTracker.GetKeyDown(Key.Delete, out _))
                {
                    if (CursorIndex < Text.Length)
                    {
                        string startLine = Text[..CursorIndex];
                        Text = string.Concat(startLine, Text.AsSpan(CursorIndex + 1, (Text.Length - CursorIndex) - 1));
                        Label.Content = Text;
                        Label.Recreate();
                    }
                }
                else if (!InputTracker.GetKeyDown(Key.Delete, out _) && !InputTracker.GetKeyDown(Key.BackSpace, out _))
                {
                }
                
            }

            foreach (char k in InputTracker.FrameSnapshot.KeyCharPresses)
            {
                InsertTextAtCursor(k.ToString());
            }
        }


        protected void InsertTextAtCursor(string s)
        {
            if (Text.Length > 0)
            {
                if (CursorIndex == 0)
                {
                    Text = s + Text;
                }
                else if (CursorIndex == Text.Length)
                {
                    Text = Text + s;
                }
                else
                {
                    string restOfLine = Text.Substring(CursorIndex, (Text.Length - CursorIndex));
                    Text = Text[..CursorIndex] + s + restOfLine;
                }
            }
            else
            {
                Text = s;
            }
            
            Label.Content = Text;
            Label.Recreate();
            CursorIndex++;
            CursorControl.Position = GetTextIndexPosition(CursorIndex);
        }


        protected override void OnMouseDown()
        {
            base.OnMouseDown();

            Vector2 cursorPos = GetCursorPosition();
            if (cursorPos != Vector2.Zero)
            {
                CursorControl.Position = cursorPos;
            }
        }

        public Vector2 GetTextIndexPosition(int index)
        {
            string substr = Text[..index];
            FontRectangle size = TextMeasurer.Measure(substr, new TextOptions(Label.DrawableText.Font));
            return new Vector2(size.Width + Label.Position.X - 1, 3);
        }

        public Vector2 GetCursorPosition()
        {
            Vector2 mousePos = InputTracker.MousePosition;
            if (IsMouseHoveringOver)
            {
                // Iterate all combinations of letters and get closest

                List<float> xPosList = new List<float> { 0 };
                for (int i = 1; i <= Text.Length; i++)
                {
                    string substr = Text[..i];
                    FontRectangle size = TextMeasurer.Measure(substr, new TextOptions(Label.DrawableText.Font));

                    xPosList.Add(size.Width);
                }

                Vector2 relativePos = mousePos - (Label.AbsolutePosition + CursorControl.Size with {Y = 0});
                float minDistance = xPosList.Min(n => Math.Abs(relativePos.X - n));
                float closest = xPosList.First(n => Math.Abs(Math.Abs(relativePos.X - n) - minDistance) < float.Epsilon);
                CursorIndex = xPosList.IndexOf(closest);
                
                return new Vector2(closest + Label.Position.X - 1, 3);
            }
            return Vector2.Zero;
        }
    }
}
