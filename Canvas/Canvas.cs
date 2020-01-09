using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program
    {
        public class Canvas
        {
            MySpriteDrawFrame? frame = null;
            CanvasState lastFrame = new CanvasState();
            List<MySprite> currentFrame = new List<MySprite>();

            protected IMyTextSurface panel;
            //public Vector2 viewportSize;
            public Vector2 Size
            {
                get
                {
                    return panel.SurfaceSize;
                }
            }
            //Vector2 scale;

            public Canvas (IMyTextSurface panel)
            {
                if (panel == null)
                    throw new Exception("Panel not found.");

                this.panel = panel;
                panel.ContentType = ContentType.SCRIPT;
                panel.Script = "";

            }

            public CanvasState GetState ()
            {
                return lastFrame;
            }
            public void LoadState (CanvasState state)
            {
                frame = panel.DrawFrame();
                frame.Value.AddRange(state.State);
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

            public void DrawSquare (Vector2 position, Vector2 size, Color c, bool hollow = true)
            {
                MySprite s;
                if (hollow)
                    s = new MySprite(SpriteType.TEXTURE, "SquareHollow", position, size, c);
                else
                    s = new MySprite(SpriteType.TEXTURE, "SquareSimple", position, size, c);
                AddObject(s);
            }

            public void DrawString (Vector2 position, string value, string font, Color c, float scale = 1)
            {
                MySprite s = MySprite.CreateText(value, font, c, scale, TextAlignment.LEFT);
                s.Position = position;
                AddObject(s);
            }

            private void AddObject (MySprite s)
            {
                if (frame == null)
                    frame = panel.DrawFrame();
                frame.Value.Add(s);
                currentFrame.Add(s);
            }

            public void EndDraw ()
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

            public void Clear ()
            {
                frame = null;
                panel.DrawFrame();
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
