using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        //Whip's PID controller class v6 - 11/22/17
        public class PID
        {
            double _kP = 0;
            double _kI = 0;
            double _kD = 0;
            double _integralDecayRatio = 0;
            double _lowerBound = 0;
            double _upperBound = 0;
            double _timeStep = 0;
            double _inverseTimeStep = 0;
            double _errorSum = 0;
            double _lastError = 0;
            bool _firstRun = true;
            bool _integralDecay = false;
            public double Value
            {
                get; private set;
            }

            public PID (double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _lowerBound = lowerBound;
                _upperBound = upperBound;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecay = false;
            }

            public PID (double kP, double kI, double kD, double integralDecayRatio, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                _integralDecayRatio = integralDecayRatio;
                _integralDecay = true;
            }

            public double Control (double error)
            {
                //Compute derivative term
                var errorDerivative = (error - _lastError) * _inverseTimeStep;

                if (_firstRun)
                {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Compute integral term
                if (!_integralDecay)
                {
                    _errorSum += error * _timeStep;

                    //Clamp integral term
                    if (_errorSum > _upperBound)
                        _errorSum = _upperBound;
                    else if (_errorSum < _lowerBound)
                        _errorSum = _lowerBound;
                }
                else
                {
                    _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
                }

                //Store this error as last error
                _lastError = error;

                //Construct output
                this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
                return this.Value;
            }

            public double Control (double error, double timeStep)
            {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
                return Control(error);
            }

            public void Reset ()
            {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }

        public class VectorPID
        {
            private PID X;
            private PID Y;
            private PID Z;

            public VectorPID (double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep)
            {
                X = new PID(kP, kI, kD, lowerBound, upperBound, timeStep);
                Y = new PID(kP, kI, kD, lowerBound, upperBound, timeStep);
                Z = new PID(kP, kI, kD, lowerBound, upperBound, timeStep);
            }

            public VectorPID (double kP, double kI, double kD, double integralDecayRatio, double timeStep)
            {
                X = new PID(kP, kI, kD, integralDecayRatio, timeStep);
                Y = new PID(kP, kI, kD, integralDecayRatio, timeStep);
                Z = new PID(kP, kI, kD, integralDecayRatio, timeStep);
            }

            public Vector3D Control (Vector3D error)
            {
                return new Vector3D(X.Control(error.X), Y.Control(error.Y), Z.Control(error.Z));
            }

            public void Reset ()
            {
                X.Reset();
                Y.Reset();
                Z.Reset();
            }
        }

    }
}
