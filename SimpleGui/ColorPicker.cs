using System;
using System.Drawing;
using System.Numerics;

namespace SimpleGui
{
    public class ColorPicker : Control
    {
        public ImageColorSample SamplerImage;
        public ColoredQuad PreviewQuad;
        private Text text;

        public Color SelectedColor;

        public bool Result;
        public Button OkButton;
        public Button CloseButton;

        public Action Closed = () => { };

        public ColorPicker()
        {
            Size = new Vector2(430, 390);
        }

        public override void Initialize()
        {
            base.Initialize();

            SamplerImage = new ImageColorSample("gui/color.png")
            {
                Size = new Vector2(320, 240),
                Position = new Vector2(5, 5),
            };
            SamplerImage.Initialize();
            AddChild(SamplerImage);

            PreviewQuad = new ColoredQuad
            {
                Size = new Vector2(32, 32),
                Position = new Vector2(5, 245),
                Color = Color.White,
            };
            PreviewQuad.Initialize();
            AddChild(PreviewQuad);

            text = new Text(PreviewQuad.Color.ToString());
            text.Size = new Vector2(250, 32);
            text.Initialize();
            text.Position = new Vector2(32, 245);
            AddChild(text);
            
            


            int x = 5;
            int y = 355;
            OkButton = new Button("OK")
            {
                Position = new Vector2(x, y),
                Size = new Vector2(90, 31),
            };
            OkButton.Initialize();
            AddChild(OkButton);
            OkButton.MouseUp = () =>
            {
                Result = true;
                Closed();
            };

            x += (int)OkButton.Size.X + 5;

            CloseButton = new Button("Cancel")
            {
                Position = new Vector2(x, y),
                Size = OkButton.Size,
            };
            CloseButton.Initialize();
            AddChild(CloseButton);
            CloseButton.MouseUp = () =>
            {
                Result = false;
                Closed();
            };

            SamplerImage.ColorSampled = () =>
            {
                SelectedColor = SamplerImage.SelectedColor;
                PreviewQuad.Color = SamplerImage.SelectedColor;
                PreviewQuad.Recreate();
                text.Content = SamplerImage.SelectedColor.ToString();
                text.Recreate();
            };

            Closed += () =>
            {
                Parent.RemoveChild(this);
                Dispose();
            };
        }
    }
}
