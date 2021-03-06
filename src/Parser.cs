﻿using System;
using System.Collections.Generic;
using System.Linq;
using shlox.Exceptions;

namespace shlox
{
    public class Parser
    {
        private readonly Token[] _tokens;
        private int _current = 0;

        public Parser(IEnumerable<Token> tokens)
        {
            _tokens = tokens.ToArray();
        }

        public List<Stmt> Parse()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }
            return statements;
        }

        private Stmt Declaration()
        {
            try
            {
                return Match(TokenType.FUN)
                    ? Function("function")
                    : Match(TokenType.VAR)
                        ? VarDeclaration()
                        : Statement();
            }
            catch (ParseException)
            {
                Synchronize();
                return null;
            }
        }

        private Stmt VarDeclaration()
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

            var initializer = Match(TokenType.EQUAL)
                ? Expression()
                : null;

            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            return new Var(name, initializer);
        }

        private Stmt Statement()
            => Match(TokenType.FOR)
                ? ForStatement()
                : Match(TokenType.IF)
                    ? IfStatement()
                    : Match(TokenType.PRINT)
                        ? PrintStatement()
                        : Match(TokenType.RETURN)
                            ? ReturnStatement()
                            : Match(TokenType.WHILE)
                                ? WhileStatement()
                                : Match(TokenType.LEFT_BRACE)
                                    ? new Block(Block())
                                    : ExpressionStatement();

        private Stmt ForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            // If the token following the ( is a semicolon then the initializer
            // has been omitted. Otherwise, we check for a var keyword to see
            // if it’s a variable declaration. If neither of those matched, it
            // must be an expression. We parse that and wrap it in an expression
            // statement so that the initializer is always of type Stmt
            Stmt initializer = Match(TokenType.SEMICOLON)
                ? null
                : Match(TokenType.VAR)
                    ? VarDeclaration()
                    : ExpressionStatement();

            // If next token is a semicolon then the condition has been omitted.
            Expr condition = Check(TokenType.SEMICOLON) ? null : Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

            // If next token is closing right paren, increment ommitted.
            Expr increment = Check(TokenType.RIGHT_PAREN) ? null : Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

            var body = Statement();

            // Add the increment expression evaluation to end of body if not null
            if (increment != null)
            {
                body = new Block(new List<Stmt> { body, new Expression(increment) });
            }

            // Wrap the body into a while based on condition. If null then set
            // condition to true (infinite loop)
            if (condition is null) condition = new Literal(true);
            body = new While(condition, body);

            // If initializer was set, wrap body in block with initializer to run
            // first before the loop body is executed
            if (initializer != null)
            {
                body = new Block(new List<Stmt> { initializer, body });
            }

            return body;
        }

        /// <summary>
        /// whileStmt → "while" "(" expression ")" statement ;
        /// </summary>
        /// <returns></returns>
        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            var condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after 'while'.");
            var body = Statement();
            return new While(condition, body);
        }

        /// <summary>
        /// ifStmt → "if" "(" expression ")" statement ( "else" statement )? ;
        /// </summary>
        /// <returns></returns>
        private Stmt IfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            var condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

            var thenBranch = Statement();
            var elseBranch = Match(TokenType.ELSE) ? Statement() : null;
            return new If(condition, thenBranch, elseBranch);
        }

        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value.");
            return new Print(value);
        }

        /// <summary>
        /// returnStmt → "return" expression? ";" ;
        /// </summary>
        /// <returns></returns>
        private Stmt ReturnStatement()
        {
            var keyword = Previous();
            var value = Check(TokenType.SEMICOLON)
                ? null
                : Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after return value");
            return new Return(keyword, value);
        }

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value.");
            return new Expression(expr);
        }

        private Function Function(string kind)
        {
            var name = Consume(TokenType.IDENTIFIER, $"Expect {kind} name.");
            Consume(TokenType.LEFT_PAREN, $"Expect '(' after {kind} name.");
            var parameters = new List<Token>();
            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count() >= 255)
                    {
                        Error(Peek(), "Cannot have more then 255 parameters."); 
                    }
                    parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");
            Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {kind} body");
            var body = Block();
            return new Function(name, parameters, body);
        }

        private Expr Assignment()
        {
            var expr = Or();
            if (Match(TokenType.EQUAL))
            {
                var equals = Previous();
                var value = Assignment();
                if (expr is Variable v)
                {
                    var name = v.Name;
                    return new Assign(name, value);
                }
                Error(equals, "Invalid assignment target.");
            }
            return expr;
        }

        /// <summary>
        /// logic_or → logic_and ( "or" logic_and )* ;
        /// </summary>
        /// <returns></returns>
        private Expr Or()
        {
            var expr = And();
            while (Match(TokenType.OR))
            {
                var op = Previous();
                var right = And();
                expr = new Logical(expr, op, right);
            }
            return expr;
        }

        /// <summary>
        /// logic_and → equality ( "and" equality )* ;
        /// </summary>
        /// <returns></returns>
        private Expr And()
        {
            var expr = Equality();
            while (Match(TokenType.AND))
            {
                var op = Previous();
                var right = Equality();
                expr = new Logical(expr, op, right);
            }
            return expr;
        }

        /// <summary>
        /// block → "{" declaration* "}" ;
        /// </summary>
        /// <returns></returns>
        private List<Stmt> Block()
        {
            var statements = new List<Stmt>();
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }
            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        private Expr Expression() => Assignment();

        /// <summary>
        /// equality → comparison ( ( "!=" | "==" ) comparison )* ;
        /// </summary>
        /// <returns></returns>
        private Expr Equality()
            => ParseLeftAssociative(Comparison,
                TokenType.BANG_EQUAL,
                TokenType.EQUAL_EQUAL);


        /// <summary>
        /// comparison → addition ( ( ">" | ">=" | "<" | "<=" ) addition )* ;
        /// </summary>
        /// <returns></returns>
        private Expr Comparison()
            => ParseLeftAssociative(Addition,
                TokenType.GREATER,
                TokenType.GREATER_EQUAL,
                TokenType.LESS,
                TokenType.LESS_EQUAL);

        /// <summary>
        /// addition  → multiplication ( ( "-" | "+" ) multiplication )* ;
        /// </summary>
        /// <returns></returns>
        private Expr Addition()
            => ParseLeftAssociative(Multiplication,
                TokenType.MINUS,
                TokenType.PLUS);

        /// <summary>
        /// multiplication → unary ( ( "/" | "*" ) unary )* ;
        /// </summary>
        /// <returns></returns>
        private Expr Multiplication()
            => ParseLeftAssociative(Unary,
                TokenType.SLASH,
                TokenType.STAR);

        /// <summary>
        /// unary → ( "!" | "-" ) unary | primary ;
        /// </summary>
        /// <returns></returns>
        private Expr Unary()
        {
            if (Match(TokenType.BANG, TokenType.MINUS))
            {
                var op = Previous();
                var right = Unary();
                return new Unary(op, right);
            }
            return Call();
        }

        private Expr Call()
        {
            var expr = Primary();

            while (true)
            {
                if (Match(TokenType.LEFT_PAREN))
                {
                    expr = FinishCall(expr);
                }
                else
                {
                    break;
                }
            }
            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            var arguments = new List<Expr>();
            if (!Check(TokenType.RIGHT_PAREN))
            {
                // Parse Arguments
                do
                {
                    arguments.Add(Expression());
                    if (arguments.Count() >= 255)
                    {
                        Error(Peek(), "Cannot have more than 255 arguments.");
                    }
                } while (Match(TokenType.COMMA));
            }

            var paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
            return new Call(callee, paren, arguments);
        }

        /// <summary>
        /// primary → NUMBER | STRING | "false" | "true" | "nil" | "(" expression ")" ;
        /// </summary>
        /// <returns></returns>
        private Expr Primary()
        {
            if (Match(TokenType.FALSE)) return new Literal(false);
            if (Match(TokenType.TRUE)) return new Literal(true);
            if (Match(TokenType.NIL)) return new Literal(null);
            if (Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Literal(Previous().Literal);
            }
            if (Match(TokenType.IDENTIFIER))
            {
                return new Variable(Previous());
            }
            if (Match(TokenType.LEFT_PAREN))
            {
                var expr = Expression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Grouping(expr);
            }
            throw Error(Peek(), "Expect expression.");
        }

        private Token Consume(TokenType type, string errorMessage)
        {
            if (Check(type)) return Advance();
            throw Error(Peek(), errorMessage);
        }


        /// <summary>
        /// checks to see if the current token is any of the
        /// given types. If so, it consumes the token and
        /// returns true. Otherwise, it returns false and
        /// leaves the token as the current one
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        private bool Match(params TokenType[] types)
        {
            foreach (var tokenType in types)
            {
                if (Check(tokenType))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the current token is of the
        /// given type. Unlike match(), it doesn’t
        /// consume the token, it only looks at it.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool Check(TokenType type)
        {
            if (IsAtEnd())
            {
                return false;
            }
            return Peek().Type == type;
        }

        /// <summary>
        /// Returns true if the current token is of the
        /// given type. Unlike match(), it doesn’t
        /// consume the token, it only looks at it.
        /// </summary>
        /// <returns></returns>
        private Token Advance()
        {
            if (!IsAtEnd())
            {
                _current++;
            }
            return Previous();
        }

        /// <summary>
        /// Returns true if at the EOF token
        /// </summary>
        /// <returns></returns>
        private bool IsAtEnd() => Peek().Type == TokenType.EOF;

        /// <summary>
        /// Returns the current token we have yet to consume
        /// </summary>
        /// <returns></returns>
        private Token Peek() => _tokens[_current];


        /// <summary>
        /// Returns the most recently consumed token
        /// </summary>
        /// <returns></returns>
        private Token Previous() => _tokens[_current - 1];

        /// <summary>
        /// Reports the error message for the provided token
        /// and returns a <see cref="ParseException"/>
        /// </summary>
        /// <param name="token"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private ParseException Error(Token token, string errorMessage)
        {
            Program.Error(token, errorMessage);
            return new ParseException();
        }

        /// <summary>
        /// Discards tokens until it thinks it found a statement boundary
        /// </summary>
        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.SEMICOLON)
                {
                    return;
                }

                switch (Peek().Type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }
                Advance();
            }
        }

        /// <summary>
        /// Parses out left-associatvie Binary expression
        /// matching the provided <see cref="TokenType"/> list
        /// </summary>
        /// <param name="operandMethod">Expression operand method</param>
        /// <param name="types">One or more types for the operand</param>
        /// <returns></returns>
        private Expr ParseLeftAssociative(Func<Expr> operandMethod, params TokenType[] types)
        {
            var expr = operandMethod();
            while (Match(types))
            {
                var op = Previous();                // get the matched op token
                var right = operandMethod();        // Parse right as expr
                expr = new Binary(expr, op, right); // Return as Binary expr
            }
            return expr;
        }
    }
}
