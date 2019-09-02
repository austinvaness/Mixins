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

        const double thrustP = 3;
        const double thrustI = 2;
        const double thrustD = 15;
        const double thrustDecay = 0.2;
        const float minThrust = 1.0000001f;

        class ThrusterControl
        {
            private List<IMyThrust> thrusters = new List<IMyThrust>();
            IMyShipController rc;

            VectorPID pid;

            public void Reset ()
            {
                foreach (IMyThrust t in thrusters)
                    t.ThrustOverride = 0;
            }

            public ThrusterControl (IMyShipController rc, UpdateFrequency tickSpeed, List<IMyThrust> thrusters)
            {
                if (rc == null)
                    throw new Exception("Ship controller null.");

                double factor = 1;
                if (tickSpeed == UpdateFrequency.Update10)
                    factor = 10;
                else if (tickSpeed == UpdateFrequency.Update100)
                    factor = 100;
                double secondsPerTick = (1.0 / 60) * factor;

                pid = new VectorPID(thrustP / factor, thrustI / factor, thrustD / factor, thrustDecay / factor, secondsPerTick);
                this.rc = rc;
                this.thrusters = thrusters;
                Reset();
            }

            public Vector3D ControlPosition (Vector3D targetPosition, Vector3D targetVelocity, double maxSpeed = double.PositiveInfinity)
            {
                return ControlVelocity(targetVelocity + VectorClamp(-pid.Control(rc.GetPosition() - targetPosition), maxSpeed));
            }

            public Vector3D VectorClamp (Vector3D v, double max)
            {
                double len2 = v.LengthSquared();
                if (len2 > max * max)
                    v = (v / Math.Sqrt(len2)) * max;
                return v;
            }

            public Vector3D ControlVelocity (Vector3D targetVelocity)
            {
                // Calculate the needed thrust to get to velocity
                Vector3D myVel = rc.GetShipVelocities().LinearVelocity;
                Vector3D deltaV = myVel - targetVelocity;

                if (Vector3D.IsZero(deltaV))
                    return Vector3D.Zero;

                Vector3D gravity = rc.GetNaturalGravity();
                return 2 * deltaV + gravity;
            }

            public void ApplyAccel (Vector3D accel)
            {
                if (accel.Equals(Vector3D.Zero, 0.1))
                    accel = Vector3D.Zero;

                Vector3D thrust = accel * rc.CalculateShipMass().TotalMass;
                foreach (IMyThrust t in thrusters)
                {
                    if (!t.IsFunctional)
                        continue;

                    float outputThrust = (float)Vector3D.Dot(t.WorldMatrix.Forward, thrust);
                    if (outputThrust > 0)
                    {
                        float outputProportion = MathHelper.Clamp(outputThrust / t.MaxEffectiveThrust, minThrust / t.MaxThrust, 1);
                        t.ThrustOverridePercentage = outputProportion;
                        thrust -= t.WorldMatrix.Forward * outputProportion * t.MaxEffectiveThrust;
                    }
                    else
                    {
                        t.ThrustOverride = minThrust;
                    }
                }
            }

        }
    }
}
