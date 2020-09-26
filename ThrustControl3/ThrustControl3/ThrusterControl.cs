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
        class ThrusterControl
        {
            public enum Mode
            {
                Min, OnOff
            }

            const double thrustP = 3;
            const double thrustI = 2;
            const double thrustD = 15;
            const double thrustDecay = 0.2;
            const float minThrust = 1.0000001f;
            const double accuracy = 0.1;

            readonly List<ThrusterGroup> thrust = new List<ThrusterGroup>(6);
            IMyShipController rc;
            VectorPID pid;

            public void Reset ()
            {
                foreach (ThrusterGroup t in thrust)
                    t.Reset();
            }

            public ThrusterControl (IMyShipController rc, UpdateFrequency tickSpeed, List<IMyThrust> thrusters, Mode mode = Mode.Min)
            {
                if (rc == null)
                    throw new Exception("Ship controller null.");

                double factor = 1;
                if (tickSpeed == UpdateFrequency.Update10)
                    factor = 10;
                else if (tickSpeed == UpdateFrequency.Update100)
                    factor = 100;
                double secondsPerTick = (1.0 / 60) * factor;

                this.rc = rc;
                Dictionary<Base6Directions.Direction, ThrusterGroup> temp = new Dictionary<Base6Directions.Direction, ThrusterGroup>(6);
                foreach (IMyThrust t in thrusters)
                {
                    t.ThrustOverride = 0;
                    t.Enabled = true;
                    if (temp.ContainsKey(t.Orientation.Forward))
                    {
                        temp [t.Orientation.Forward].Add(t);
                    }
                    else
                    {
                        ThrusterGroup newThrust = new ThrusterGroup(mode);
                        newThrust.Add(t);
                        temp.Add(t.Orientation.Forward, newThrust);
                    }
                }
                thrust = temp.Values.ToList();

                pid = new VectorPID(thrustP / factor, thrustI / factor, thrustD / factor, thrustDecay / factor, secondsPerTick);
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

                Vector3D force = accel * rc.CalculateShipMass().TotalMass;
                foreach (ThrusterGroup t in thrust)
                {
                    t.CancelForce(force);
                    prg.Echo(t.Count.ToString());
                }
            }

            public double StopDistance()
            {
                Vector3D myVel = rc.GetShipVelocities().LinearVelocity;

                if (Vector3D.IsZero(myVel))
                    return 0;

                Vector3D gravity = rc.GetNaturalGravity(); 
                Vector3D accel =  2 * myVel + gravity;

                if (accel.Equals(Vector3D.Zero, 0.1))
                    return 0;

                Vector3D dir = Vector3D.Normalize(accel);
                double appliedForce = 0;
                foreach (ThrusterGroup t in thrust)
                    appliedForce += t.AvailibleThrust(dir);
                double appliedAccel = appliedForce / rc.CalculateShipMass().TotalMass;
                //if (appliedAccel < accel.Length())
                //    return double.PositiveInfinity;
                return myVel.LengthSquared() / (2 * appliedAccel);
            }

            class ThrusterGroup
            {
                List<IMyThrust> thrust = new List<IMyThrust>();
                double prevOutput = 0;
                bool useMin;

                public int Count => thrust.Count;

                public ThrusterGroup(Mode mode)
                {
                    useMin = mode == Mode.Min;
                }

                public double AvailibleThrust(Vector3D direction)
                {
                    Vector3D forward = this.thrust [0].WorldMatrix.Forward;
                    double scaler = Vector3D.Dot(direction, forward);
                    if (scaler <= 0)
                        return 0;

                    double thrust = 0;
                    foreach (IMyThrust t in this.thrust)
                        thrust += t.MaxEffectiveThrust;
                    return thrust * scaler;
                }

                public void Add (IMyThrust t)
                {
                    thrust.Add(t);
                }

                public void CancelForce (Vector3D force)
                {
                    if (thrust.Count == 0)
                        return;

                    Vector3D forward = thrust [0].WorldMatrix.Forward;
                    double outputThrust = Vector3D.Dot(force, forward);
                    if (EqualsPrecision(outputThrust, prevOutput, accuracy))
                        return;

                    outputThrust = Vector3D.Dot(force, forward);
                    if (outputThrust > minThrust)
                        ApplyForce(outputThrust);
                    else
                        ApplyZero();

                    prevOutput = outputThrust;
                }

                public void Reset ()
                {
                    foreach (IMyThrust t in thrust)
                    {
                        if (t.ThrustOverride != 0)
                        {
                            t.ThrustOverride = 0;
                            t.Enabled = true;
                        }
                    }
                }

                void ApplyZero()
                {
                    foreach(IMyThrust t in thrust)
                    {
                        if(useMin)
                        {
                            if (t.ThrustOverride != minThrust)
                                t.ThrustOverride = minThrust;
                        }
                        else
                        {
                            if (t.Enabled)
                                t.Enabled = false;
                        }
                    }
                }

                void ApplyForce (double outputThrust)
                {
                    double output;
                    double maxThrust;
                    float percent;
                    foreach (IMyThrust t in thrust)
                    {
                        if (!t.IsWorking)
                            continue;

                        maxThrust = t.MaxEffectiveThrust;
                        if(useMin)
                        {
                            output = MathHelper.Clamp(outputThrust, minThrust, maxThrust);
                            percent = (float)(output / maxThrust);
                            if (t.ThrustOverridePercentage != output)
                                t.ThrustOverridePercentage = percent;
                            outputThrust -= output;
                        }
                        else
                        {
                            if (outputThrust < minThrust)
                            {
                                if(t.Enabled)
                                    t.Enabled = false;
                                if (t.ThrustOverride != 0)
                                    t.ThrustOverride = 0;
                            }
                            else
                            {
                                output = Math.Min(outputThrust, maxThrust);
                                if(!t.Enabled)
                                    t.Enabled = true;
                                percent = (float)(output / maxThrust);
                                if (t.ThrustOverridePercentage != percent)
                                    t.ThrustOverridePercentage = percent;
                                outputThrust -= output;
                            }
                        }

                    }
                }

                bool EqualsPrecision (double d1, double d2, double precision)
                {
                    return Math.Abs(d1 - d2) <= precision;
                }
            }
        }

    }
}
