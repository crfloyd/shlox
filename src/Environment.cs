using System.Collections.Generic;
using shlox.Exceptions;

namespace shlox
{
    public class Environment
    {
        public Environment Enclosing { get; }
        private Dictionary<string, object> Values { get; }

        public Environment() : this(null) { }

        public Environment(Environment enclosing)
        {
            Enclosing = enclosing;
            Values = new Dictionary<string, object>();
        }

        public void Define(string name, object value) => Values[name] = value;

        public object Get(Token name)
            => Values.TryGetValue(name.Lexeme, out var v)
                ? v
                : Enclosing != null
                    ? Enclosing.Get(name)
                    : throw new RuntimeException(name, $"Undefined variable {name.Lexeme}.");

        public void Assign(Token name, object value)
        {
            if (!Values.ContainsKey(name.Lexeme))
            {
                if (Enclosing != null)
                {
                    Enclosing.Assign(name, value);
                    return;
                }
                throw new RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
            }
            Values[name.Lexeme] = value;
        }
    }
}
