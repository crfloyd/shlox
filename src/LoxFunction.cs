using System;
using System.Collections.Generic;

namespace shlox
{
    public class LoxFunction : ICallable
    {
        private readonly Function _declaration;

        public LoxFunction(Function declaration)
        {
            _declaration = declaration;
        }

        public int Arity() => _declaration.Parameters.Count;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            // Create a new environment encapsulating the globals
            var environment = new Environment(interpreter.Globals);

            // Bind the arguments to function parameters
            for (int i = 0; i < _declaration.Parameters.Count; i++)
            {
                var parameterName = _declaration.Parameters[i].Lexeme;
                var argumentValue = arguments[i];
                environment.Define(parameterName, argumentValue);
            }

            // Execute the body
            interpreter.ExecuteBlock(_declaration.Body, environment);

            return null;
        }

        public override string ToString()
        {
            return $"<fn {_declaration.Name.Lexeme}>";
        }
    }
}
