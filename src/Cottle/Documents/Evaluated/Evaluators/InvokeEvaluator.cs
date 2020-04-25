﻿using System.Collections.Generic;
using System.IO;
using Cottle.Documents.Compiled;

namespace Cottle.Documents.Evaluated.Evaluators
{
    internal class InvokeEvaluator : IEvaluator
    {
        private readonly IReadOnlyList<IEvaluator> _arguments;

        private readonly IEvaluator _caller;

        public InvokeEvaluator(IEvaluator caller, IReadOnlyList<IEvaluator> arguments)
        {
            _arguments = arguments;
            _caller = caller;
        }

        public Value Evaluate(Frame frame, TextWriter output)
        {
            var source = _caller.Evaluate(frame, output);
            var function = source.AsFunction;

            if (function == null)
                return Value.Undefined;

            var values = new Value[_arguments.Count];

            for (var i = 0; i < _arguments.Count; ++i)
                values[i] = _arguments[i].Evaluate(frame, output);

            return function.Invoke(frame, values, output);
        }
    }
}