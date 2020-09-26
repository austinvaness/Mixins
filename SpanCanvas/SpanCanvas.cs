using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program
    {
        // Requires Canvas shared project.
        public class SpanCanvas : Canvas
        {
            Dictionary<IMyTextPanel, PanelRenderer> panels = new Dictionary<IMyTextPanel, PanelRenderer>();
            private Vector2 globalSize;
            const float pixelDensity = 512;

            public SpanCanvas (IMyTextPanel corner, int width, int height) : base()
            {
                if (width < 1)
                    width = 1;
                if (height < 1)
                    height = 1;

                Vector3I xAxis, yAxis;
                GetAxis(corner, out xAxis, out yAxis);
                Vector3I start = GetCorner(corner, xAxis, yAxis);

                LoadPanels(corner, start, xAxis, yAxis, width, height);
            }

            public SpanCanvas (IMyTextPanel corner) : base()
            {
                if (corner == null)
                    throw new Exception("Corner panel not found.");


                Vector3I xAxis, yAxis;
                GetAxis(corner, out xAxis, out yAxis);
                Vector3I start = GetCorner(corner, xAxis, yAxis);

                // Determine the size of the panel area
                Vector3I v = start;
                Vector2I size = new Vector2I();
                while (GetPanel(corner, v) != null)
                {
                    v += xAxis;
                    size.X++;
                }
                v = start;

                while (GetPanel(corner, v) != null)
                {
                    v += yAxis;
                    size.Y++;
                }


                globalSize = new Vector2(size.X, size.Y) * pixelDensity;

                LoadPanels(corner, start, xAxis, yAxis, size.X, size.Y);
            }

            // Finds the grid direction for left (x) and down (y)
            private void GetAxis(IMyTextPanel panel, out Vector3I xAxis, out Vector3I yAxis)
            {
                Base6Directions.Direction xAxisDir = Base6Directions.GetFlippedDirection(panel.Orientation.Left);
                xAxis = Vector3I.Round(Base6Directions.GetVector(xAxisDir));

                Base6Directions.Direction yAxisDir = Base6Directions.GetFlippedDirection(panel.Orientation.Up);
                yAxis = Vector3I.Round(Base6Directions.GetVector(yAxisDir));
            }

            // Finds the upper left corner
            private Vector3I GetCorner(IMyTextPanel panel, Vector3I xAxis, Vector3I yAxis)
            {
                Vector3I start = panel.Position;
                while (GetPanel(panel, start) == panel)
                    start -= xAxis;
                start += xAxis;

                while (GetPanel(panel, start) == panel)
                    start -= yAxis;
                start += yAxis;
                return start;
            }

            private void LoadPanels (IMyTextPanel corner, Vector3I start, Vector3I xAxis, Vector3I yAxis, int width, int height)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3I v = start + (xAxis * x);
                    for (int y = 0; y < height; y++)
                    {
                        IMyTextPanel panel = GetPanel(corner, v);
                        if (panel != null && SameOrientation(corner, panel) && !panels.ContainsKey(panel))
                        {
                            Prep(panel);
                            Vector2 offset = (new Vector2(x, y) + GetOffset(panel)) * pixelDensity;

                            if (x == 0)
                            {
                                int i = 1;
                                while (GetPanel(panel, v - (xAxis * i)) == panel)
                                    i++;
                                offset.X -= pixelDensity * (i - 1);
                            }

                            if (y == 0)
                            {
                                int i = 1;
                                while (GetPanel(panel, v - (yAxis * i)) == panel)
                                    i++;
                                offset.Y -= pixelDensity * (i - 1);
                            }

                            Vector3 size = ((panel.Max - panel.Min) + Vector3I.One) * pixelDensity;
                            float xDen = Math.Abs(ScalarProjection(size, xAxis)) / panel.TextureSize.X;
                            float yDen = Math.Abs(ScalarProjection(size, yAxis)) / panel.TextureSize.Y;
                            Vector2 den = new Vector2(xDen, yDen) * GetDensity(panel);
                            panel.WriteText($"Pos: {offset}\nSize:{size}\nDensity:{den}");
                            panels [panel] = new PanelRenderer(offset, den, globalSize);
                        }

                        v += yAxis;
                    }
                }
            }

            private Vector2 GetOffset (IMyTextPanel panel)
            {
                // Panel specific offsets
                if (panel.BlockDefinition.SubtypeId == "SmallTextPanel")
                    return new Vector2(0.1f, 0.1f);
                return Vector2.Zero;
            }

            private Vector2 GetDensity (IMyTextPanel panel)
            {
                // Panel specific offsets
                if (panel.BlockDefinition.SubtypeId == "SmallTextPanel")
                    return new Vector2(0.9f, 0.9f);
                return Vector2.One;
            }

            private IMyTextPanel GetPanel(IMyCubeBlock reference, Vector3I pos)
            {
                IMySlimBlock slim = reference.CubeGrid.GetCubeBlock(pos);
                if (slim == null)
                    return null;
                return slim.FatBlock as IMyTextPanel;
            }

            private bool SameOrientation (IMyCubeBlock a, IMyCubeBlock b)
            {
                return a.Orientation.Left == b.Orientation.Left && a.Orientation.Up == b.Orientation.Up && a.Orientation.Forward == b.Orientation.Forward;
            }

            private void Prep (IMyTextPanel p)
            {
                p.ContentType = ContentType.SCRIPT;
                p.Script = "";
            }

            /// <summary>
            /// Projects a value onto another vector.
            /// </summary>
            /// <param name="guide">Must be of length 1.</param>
            private static float ScalarProjection (Vector3 value, Vector3 guide)
            {
                float returnValue = Vector3.Dot(value, guide);
                if (float.IsNaN(returnValue))
                    return 0;
                return returnValue;
            }

            public override Vector2 Size
            {
                get
                {
                    return globalSize;
                }
            }

            public override void LoadState (CanvasState state)
            {
                currentFrame.Clear();
                currentFrame.AddRange(state.State);
            }

            protected override void AddObject (MySprite s)
            {
                if (s.Position.HasValue)
                    s.Position = s.Position.Value - sizeOffset;
                currentFrame.Add(s);
            }

            public override void EndDraw ()
            {
                foreach(KeyValuePair<IMyTextPanel, PanelRenderer> kv in panels)
                {
                    using(var frame = kv.Key.DrawFrame())
                    {
                        foreach(MySprite s in currentFrame)
                        {
                            MySprite sprite = s;
                            kv.Value.Adjust(ref sprite);
                            frame.Add(sprite);
                        }
                    }
                }

                lastFrame = new CanvasState(currentFrame);
                currentFrame = new List<MySprite>();
            }

            public override Vector2 GetStringSize (string s, string font, float scale)
            {
                KeyValuePair<IMyTextPanel, PanelRenderer> kv = panels.First();
                return kv.Key.MeasureStringInPixels(new StringBuilder(s), font, scale) * kv.Value.density;
            }

            private class PanelRenderer
            {
                Vector2 offset;
                Vector2 globalSize;
                public Vector2 density;

                public PanelRenderer(Vector2 offset, Vector2 density, Vector2 globalSize)
                {
                    this.offset = offset;
                    this.density = density;
                    this.globalSize = globalSize;
                }

                public void Adjust(ref MySprite s)
                {
                    if(s.Type == SpriteType.TEXT)
                    {
                        s.RotationOrScale /= (Math.Max(density.X, density.Y) * 0.5f);
                    }
                    else
                    {
                        Vector2 size;
                        if (s.Size.HasValue)
                            size = s.Size.Value;
                        else
                            size = globalSize;
                        s.Size = size / density;
                    }

                    Vector2 pos;
                    if (s.Position.HasValue)
                        pos = s.Position.Value;
                    else
                        pos = globalSize * 0.5f;

                    s.Position = (pos - offset) / density;
                }
            }
        }
    }
}
