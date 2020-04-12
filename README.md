# shlox
This is a C# implementation of a Lox interpreter. The Lox language is based on Bob Nystrom's Book "[Crafting Interpreters](http://craftinginterpreters.com/introduction.html)". This project can be run in REPL mode or against a file by passing it a the first argument.

## Running the REPL
`dotnet run` from the _/src_ folder

## Running a file
`dotnet run file-name`

## Other Notes
The _/tools_ folder contains a Python script for generating the expression and statement AST classes. When executed, this script will use the data in the _expr-defs.txt_ and _stmnt-defs.txt_ files to generate the classes.
