using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.CommonAIFunctions
{
    public class Axis
    {
        private float _min;
        private float _max;
        private float _result;
        private float _input;

        private readonly Func<float, float> _outputFunction;
        private readonly Func<Target, float> _parameterFunction;
        private readonly float _range;
        private readonly Func<Target, bool> _activationFunction;

        public Axis(float minInput, float maxInput, Func<float, float> outputFunction, Func<Target, float> parameterFunction,
            Func<Target, bool> activationFunction = null)
        {
            _min = minInput;
            _max = maxInput;
            _range = maxInput - minInput;
            _outputFunction = outputFunction;
            _parameterFunction = parameterFunction;
            _activationFunction = activationFunction;
        }

        public float Evaluate(Target target)
        {
            var x = _parameterFunction.Invoke(target);
            _input = x;
            var range = (Math.Max(_min, Math.Min(_max, x)) - _min) / _range;
            var invoke = _outputFunction.Invoke(range);
            var result = Math.Max(0f, Math.Min(1.0f, invoke));
            _result = result;
            return result;
        }

        public bool IsActive(Target target)
        {
            if (_activationFunction == null)
            {
                return true;
            }

            return _activationFunction.Invoke(target);
        }
    }

    public static class AxisExtensions
    {
        public static float GeometricMean(this List<Axis> axes, Target target)
        {
            var activeAxes = axes
                .FindAll(axis => axis.IsActive(target));

            var evaluations = activeAxes
                .Select(axis => axis.Evaluate(target))
                .ToList();

            return target.UtilityValue = !evaluations.Any() ? 0.0f : (float) Math.Pow(evaluations.Aggregate((a, x) => a * x), 1.0 / activeAxes.Count);
        }

        public static double ArithmeticMean(this List<Axis> axes, Target target)
        {
            var activeAxes = axes
                .FindAll(axis => axis.IsActive(target));

            var evaluations = activeAxes
                .Select(axis => axis.Evaluate(target))
                .ToList();

            if (!evaluations.Any()) return 0.0f;
            return evaluations.Aggregate((a, x) => a + x) / activeAxes.Count;
        }
    }
}