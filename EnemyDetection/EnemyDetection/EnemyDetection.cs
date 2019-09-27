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

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// Class to use sensors and turrets to find enemies.
        /// </summary>
        public class EnemyDetection
        {

            readonly IMySensorBlock [] sensors;
            readonly IMyLargeTurretBase [] turrets;
            readonly List<MyDetectedEntityInfo> sensorCache = new List<MyDetectedEntityInfo>();

            public EnemyDetection (IMyGridTerminalSystem gridSystem, bool useSensors = true, bool useTurrets = true)
            {
                if (useSensors)
                {
                    List<IMySensorBlock> tempS = new List<IMySensorBlock>();
                    gridSystem.GetBlocksOfType(tempS);
                    sensors = tempS.ToArray();
                }

                if (useTurrets)
                {
                    List<IMyLargeTurretBase> tempT = new List<IMyLargeTurretBase>();
                    gridSystem.GetBlocksOfType(tempT);
                    turrets = tempT.ToArray();
                }
            }

            public MyDetectedEntityInfo? GetResult ()
            {
                if (turrets != null)
                {
                    foreach (IMyLargeTurretBase t in turrets)
                    {
                        if (t.HasTarget)
                            return t.GetTargetedEntity();
                    }
                }

                if (sensors != null)
                {
                    foreach (IMySensorBlock s in sensors)
                    {
                        if (!s.DetectEnemy || !s.IsActive)
                            continue;

                        sensorCache.Clear();
                        s.DetectedEntities(sensorCache);
                        foreach (MyDetectedEntityInfo info in sensorCache)
                        {
                            if (info.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                                return info;
                        }
                    }
                }

                return null;
            }
        }
    }
}
