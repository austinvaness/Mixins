using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using System;
using VRageMath;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program
    {
        public class Canvas : IEquatable<Canvas>
        {
            private MySpriteDrawFrame? frame = null;
            public Vector2 cornerOffset;
            protected IMyTextSurface panel;
            protected CanvasState lastFrame = new CanvasState();
            protected List<MySprite> currentFrame = new List<MySprite>();

            public virtual Color BackgroundColor
            {
                get
                {
                    return panel.ScriptBackgroundColor;
                }
                set
                {
                    panel.ScriptBackgroundColor = value;
                }
            }

            public virtual Vector2 Size { get; }

            public virtual Vector2 Center { get; }

            public Canvas (IMyTextSurface panel)
            {
                if (panel == null)
                    throw new Exception("Panel not found.");

                this.panel = panel;
                panel.ContentType = ContentType.SCRIPT;
                panel.Script = "";
                Vector2 diff = panel.SurfaceSize - panel.TextureSize;
                cornerOffset = diff * 0.5f;

                Size = panel.SurfaceSize;
                Center = Size / 2;
            }

            protected Canvas()
            {

            }

            public CanvasState GetWorkingState()
            {
                return new CanvasState(new List<MySprite>(currentFrame));
            }

            public CanvasState GetState ()
            {
                return lastFrame;
            }

            public virtual void LoadState (CanvasState state)
            {
                AddAllObjects(state.State, false);
            }

            public void DrawAllCustom(IEnumerable<MySprite> sprites)
            {
                AddAllObjects(sprites, true);
            }

            public void DrawCustom (MySprite s)
            {
                AddObject(s);
            }

            public void DrawCustom (CanvasShape shape, Vector2 position, Vector2 size, Color c, float rotation = 0)
            {
                AddObject(new MySprite(SpriteType.TEXTURE, shape.Value, position, size, c, null, TextAlignment.CENTER, rotation));
            }

            public void DrawElipse (Vector2 center, Vector2 size, Color c, float rotation = 0, bool hollow = true)
            {
                MySprite s;
                if (hollow)
                    s = new MySprite(SpriteType.TEXTURE, "CircleHollow", center, size, c, null, TextAlignment.CENTER, rotation);
                else
                    s = new MySprite(SpriteType.TEXTURE, "Circle", center, size, c, null, TextAlignment.CENTER, rotation);
                AddObject(s);
            }


            public void DrawLine (Vector2 start, Vector2 end, float width, Color c)
            {
                Vector2 diff = end - start;
                Vector2 mid = (end + start) / 2;
                float len = diff.Length();
                MySprite s = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: mid, size: new Vector2(len, width), color: c)
                {
                    RotationOrScale = (float)Math.Atan(diff.Y / diff.X)
                };
                AddObject(s);
            }

            public void DrawCircle (Vector2 center, float radius, Color c, bool hollow = true)
            {
                DrawElipse(center, new Vector2(radius, radius), c, 0, hollow);
            }

            public void DrawRect (Vector2 center, Vector2 size, Color c, bool hollow = true)
            {
                MySprite s;
                if (hollow)
                    s = new MySprite(SpriteType.TEXTURE, "SquareHollow", center, size, c);
                else
                    s = new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, c);
                AddObject(s);
            }

            public void DrawRectByPoints (Vector2 topLeft, Vector2 bottomRight, Color c, bool hollow = true)
            {
                Vector2 size = bottomRight - topLeft;
                Vector2 center = topLeft + (size / 2);
                DrawRect(center, size, c, hollow);
            }

            public void DrawRectByTopLeft (Vector2 topLeft, Vector2 size, Color c, bool hollow = true)
            {
                Vector2 center = topLeft + (size / 2);
                DrawRect(center, size, c, hollow);
            }

            public virtual Vector2 GetStringSize (string s, string font, float scale)
            {
                return panel.MeasureStringInPixels(new StringBuilder(s), font, scale);
            }

            public void DrawString (Vector2 position, string value, string font, Color c, TextAlignment alignment = TextAlignment.LEFT, float scale = 1)
            {
                MySprite s = MySprite.CreateText(value, font, c, scale, alignment);
                s.Position = position;
                AddObject(s);
            }

            protected virtual void ApplyOffsets(ref MySprite s)
            {
                if (s.Position.HasValue)
                    s.Position = s.Position.Value - cornerOffset;
            }

            protected void AddAllObjects(IEnumerable<MySprite> sprites, bool offsets)
            {
                if (!frame.HasValue)
                {
                    frame = panel.DrawFrame();
                    FrameCreated(frame.Value);
                }
                else if (currentFrame.Count == 0)
                {
                    FrameCreated(frame.Value);
                }

                foreach (MySprite sprite in sprites)
                {
                    MySprite s = sprite;
                    if(offsets)
                        ApplyOffsets(ref s);
                    frame.Value.Add(s);
                    currentFrame.Add(s);
                }
            }

            protected void AddObject (MySprite s)
            {
                if (frame == null)
                {
                    frame = panel.DrawFrame();
                    FrameCreated(frame.Value);
                }
                else if(currentFrame.Count == 0)
                {
                    FrameCreated(frame.Value);
                }
                ApplyOffsets(ref s);
                frame.Value.Add(s);
                currentFrame.Add(s);
            }

            public virtual void EndDraw ()
            {
                if (frame == null)
                {
                    lastFrame = new CanvasState();
                    return;
                }

                lastFrame = new CanvasState(currentFrame);
                currentFrame = new List<MySprite>();
                frame.Value.Dispose();
            }

            protected virtual void FrameCreated (MySpriteDrawFrame frame)
            { }

            public void Clear ()
            {
                if (currentFrame.Count > 0)
                    frame = null;
                var temp = panel.DrawFrame();
                FrameCreated(temp);
                temp.Dispose();
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Canvas);
            }

            public bool Equals(Canvas other)
            {
                return other != null &&
                       EqualityComparer<IMyTextSurface>.Default.Equals(panel, other.panel);
            }

            public override int GetHashCode()
            {
                return 267788301 + EqualityComparer<IMyTextSurface>.Default.GetHashCode(panel);
            }

            public static bool operator ==(Canvas left, Canvas right)
            {
                return EqualityComparer<Canvas>.Default.Equals(left, right);
            }

            public static bool operator !=(Canvas left, Canvas right)
            {
                return !(left == right);
            }
        }

        public struct CanvasShape
        {
            private CanvasShape (string value) { Value = value; }

            public string Value { get; set; }

            public static CanvasShape Square { get { return new CanvasShape("SquareSimple"); } }
            public static CanvasShape SquareTapered { get { return new CanvasShape("SquareTapered"); } }
            public static CanvasShape SquareHollow { get { return new CanvasShape("SquareHollow"); } }
            public static CanvasShape Circle { get { return new CanvasShape("Circle"); } }
            public static CanvasShape CircleHollow { get { return new CanvasShape("CircleHollow"); } }
            public static CanvasShape SemiCircle { get { return new CanvasShape("SemiCircle"); } }
            public static CanvasShape Triangle { get { return new CanvasShape("Triangle"); } }
            public static CanvasShape RightTriangle { get { return new CanvasShape("RightTriangle"); } }
        }

        public class CanvasState
        {
            public IEnumerable<MySprite> State { get; private set; }

            public CanvasState()
            {
                State = new List<MySprite>(0);
            }

            public CanvasState (IEnumerable<MySprite> frame)
            {
                if (frame == null)
                    State = new List<MySprite>(0);
                else
                    State = frame;
            }
        }
    }
}
