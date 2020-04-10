using System;
using System.IO;
using shlox.Exceptions;

namespace shlox
{
    class Program
    {
	    static bool _hadError = false;
        static bool _hadRuntimeError = false;
        private static readonly Interpreter _interpreter = new Interpreter();

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: jlox [script]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
            }
        }
        private static void RunFile(string path)
        {
            var text = File.ReadAllText(path);
            Run(text);
            
            // Indicate an error in the exit code.
            if (_hadError) System.Environment.Exit(65);
            if (_hadRuntimeError) System.Environment.Exit(70);
	    }
	
        private static void RunPrompt()
        {
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                Run(input);
                _hadError = false;
            }
        }
        
        private static void Run(string source) 
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var parser = new Parser(tokens);
            var statements = parser.Parse();

            // Stop if there was a syntax error
            if (_hadError) return;

            //Console.WriteLine(new AstPrinter().Print(expression));
            _interpreter.Interpret(statements);
            
        }
        
        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        public static void Error(Token token, string errorMessage)
        {
            if (token.Type == TokenType.EOF)
            {
                Report(token.Line, " at end", errorMessage);
            }
            else
            {
                Report(token.Line, " at '" + token.Lexeme + "'", errorMessage);
            }
        }

        public static void RuntimeError(RuntimeException e)
        {
            Console.Error.WriteLine($"{e.Message}\n[line {e.Token.Line}]");
            _hadRuntimeError = true;
        }

        private static void Report(int line, string where, string message)
        {
            Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
            _hadError = true;
        }
    }
}
