using System;
using System.Collections.Generic;
using shlox.Exceptions;

namespace shlox
{
    public class LoxFunction : ICallable
    {
        private readonly Function _declaration;
        private readonly Environment _closure;

        public LoxFunction(Function declaration, Environment closure)
        {
            _declaration = declaration;
            _closure = closure;
        }

        public int Arity() => _declaration.Parameters.Count;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            // Create a new environment encapsulating the globals
            var environment = new Environment(_closure);

            // Bind the arguments to function parameters
            for (int i = 0; i < _declaration.Parameters.Count; i++)
            {
                var parameterName = _declaration.Parameters[i].Lexeme;
                var argumentValue = arguments[i];
                environment.Define(parameterName, argumentValue);
            }

            // Execute the body. Using exceptions for return control
            // flow due to heavily-recursive tree-walk.
            try
            {
                interpreter.ExecuteBlock(_declaration.Body, environment);
            }
            catch (ReturnException ex)
            {
                return ex.Value;
            }

            return null;
        }

        public override string ToString()
        {
            return $"<fn {_declaration.Name.Lexeme}>";
        }
    }
}
