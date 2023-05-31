using System.Numerics;

namespace SimpleGui
{
    public class Button : Control
    {
        public Text Label { get; protected set; }

        public string Text { get; set; }

        public Button(string text)
        {
            Text = text;

            IsClickable = true;
            IsHoverable = true;
        }

        public override void Initialize()
        {
            base.Initialize();

            Label = new Text(Text)
            {
                Size = Size,
                Position = Vector2.Zero,
            };
            Label.Initialize();
            AddChild(Label);
        }
    }
}
