using System.Collections.Generic;
using shlox.Exceptions;

namespace shlox
{
    public class Environment
    {
        private Dictionary<string, object> Values { get; }

        public Environment()
        {
            Values = new Dictionary<string, object>();
        }

        public void Define(string name, object value) => Values[name] = value;

        public object Get(Token name)
            => Values.TryGetValue(name.Lexeme, out var v)
                ? v
                : throw new RuntimeException(name, $"Undefined variable {name.Lexeme}.");

        public void Assign(Token name, object value)
        {
            if (!Values.ContainsKey(name.Lexeme))
            {
                throw new RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
            }
            Values[name.Lexeme] = value;
        }
    }
}
