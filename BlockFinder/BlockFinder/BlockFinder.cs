using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        #region Block Finder
        T GetBlock<T> (string name, bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            if (useSubgrids)
            {
                return GridTerminalSystem.GetBlockWithName(name) as T;
            }
            else
            {
                List<T> blocks = GetBlocks<T>(false);
                foreach (T block in blocks)
                {
                    if (block.CustomName == name)
                        return block;
                }
                return null;
            }
        }
        T GetBlock<T> (bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            List<T> blocks = GetBlocks<T>(useSubgrids);
            return blocks.FirstOrDefault();
        }
        List<T> GetBlocks<T> (string groupName, bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group == null)
                return new List<T>();
            List<T> blocks = new List<T>();
            if (useSubgrids)
                group.GetBlocksOfType(blocks);
            else
                group.GetBlocksOfType(blocks, b => b.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            return blocks;

        }
        List<T> GetBlocks<T> (bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            List<T> blocks = new List<T>();
            if (useSubgrids)
                GridTerminalSystem.GetBlocksOfType(blocks);
            else
                GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            return blocks;
        }
        #endregion

    }
}
