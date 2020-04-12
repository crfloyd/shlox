using System;
using System.Collections.Generic;

namespace shlox
{
    public interface ICallable
    {
        int Arity();
        object Call(Interpreter interpreter, List<object> arguments);
    }
}
