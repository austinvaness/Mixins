using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using System.Data.SqlTypes;

namespace IngameScript
{
    partial class Program
    {
        public class BlockGrid<T> where T : IMyTerminalBlock
        {
            public Vector2I Size { get; private set; }

            private T [,] grid;

            public BlockGrid (IMyCubeBlock start, IMyCubeBlock end)
            {
                if (start == null)
                    throw new Exception("Specified start block is null.");
                if (end == null)
                    throw new Exception("Specified end block is null.");
                Init(start, end);
            }

            public void Init (IMyCubeBlock startBlock, IMyCubeBlock endBlock)
            {
                IMyCubeGrid cubeGrid = startBlock.CubeGrid;
                Vector3I start = startBlock.Position;
                Vector3I end = endBlock.Position;
                Vector3I size = (Vector3I.Max(start, end) - Vector3I.Min(start, end)) + Vector3I.One;
                if (size.X != 1 && size.Y != 1 && size.Z != 1)
                    throw new Exception("Blocks must be on a consistent plane.");

                List<List<T>> rows = new List<List<T>>();
                Vector3I inc = Vector3I.Sign(end - start);
                Vector3I v = start;
                end += inc;
                int colSize = 0;
                if (inc.X == 0)
                {
                    for (v.Y = start.Y; v.Y != end.Y; v.Y += inc.Y)
                    {
                        List<T> col = new List<T>();
                        for (v.Z = start.Z; v.Z != end.Z; v.Z += inc.Z)
                        {
                            IMyCubeBlock temp = cubeGrid.GetCubeBlock(v)?.FatBlock;
                            if (temp is T && temp.Position == v)
                            {
                                T b = (T)temp;
                                int y = (v.Z - start.Z) * inc.Z;
                                if (col.Count >= y)
                                    col.Add(b);
                                else
                                    col [y] = b;
                                //this.grid [coords.Y, coords.Z] = b;
                            }
                        }
                        colSize = Math.Max(col.Count, colSize);
                        int x = (v.Y - start.Y) * inc.Y;
                        if (rows.Count >= x)
                            rows.Add(col);
                        else
                            rows [x] = col;
                    }
                }
                else if (inc.Y == 0)
                {
                    for (v.X = start.X; v.X != end.X; v.X += inc.X)
                    {
                        List<T> col = new List<T>();
                        for (v.Z = start.Z; v.Z != end.Z; v.Z += inc.Z)
                        {
                            IMyCubeBlock temp = cubeGrid.GetCubeBlock(v)?.FatBlock;
                            if (temp is T && temp.Position == v)
                            {
                                T b = (T)temp;
                                int y = (v.Z - start.Z) * inc.Z;
                                if (col.Count >= y)
                                    col.Add(b);
                                else
                                    col [y] = b;
                                //Vector3I coords = (v - start) * inc;
                                //this.grid [coords.X, coords.Z] = b;
                            }
                        }
                        colSize = Math.Max(col.Count, colSize);
                        int x = (v.X - start.X) * inc.X;
                        if (rows.Count >= x)
                            rows.Add(col);
                        else
                            rows [x] = col;
                    }
                }
                else
                {
                    for (v.X = start.X; v.X != end.X; v.X += inc.X)
                    {
                        List<T> col = new List<T>();
                        for (v.Y = start.Y; v.Y != end.Y; v.Y += inc.Y)
                        {
                            IMyCubeBlock temp = cubeGrid.GetCubeBlock(v)?.FatBlock;
                            if (temp is T && temp.Position == v)
                            {
                                T b = (T)temp;
                                int y = (v.Y - start.Y) * inc.Y;
                                if (col.Count >= y)
                                    col.Add(b);
                                else
                                    col [y] = b;
                                //Vector3I coords = (v - start) * inc;
                                //this.grid [coords.X, coords.Y] = b;
                            }
                        }
                        colSize = Math.Max(col.Count, colSize);
                        int x = (v.X - start.X) * inc.X;
                        if (rows.Count >= x)
                            rows.Add(col);
                        else
                            rows [x] = col;
                    }
                }

                if (colSize == 0)
                    throw new Exception("No blocks found.");
                    
                grid = new T [rows.Count, colSize];

                //StringBuilder sb = new StringBuilder();
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        List<T> col = rows [x];
                        if (y < col.Count)
                            grid [x, y] = col [y];
                        //if (block == null)
                        //    sb.Append("null ");
                        //else
                        //    sb.Append(block.CustomName).Append(' ');
                    }
                    //sb.AppendLine();
                }
                //me.GetSurface(0).WriteText(sb);

                Size = new Vector2I(grid.GetLength(0), grid.GetLength(1));
            }

            public T Get (int x, int y)
            {
                return grid [x, y];
            }

            public T this[int x, int y]
            {
                get
                {
                    return grid [x, y];
                }
            }
        }
    }
}
