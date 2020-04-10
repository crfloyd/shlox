using System.Collections.Generic;

namespace shlox
{
    public class Scanner
    {
        private static readonly Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>
        {
            ["and"] = TokenType.AND,
            ["class"] = TokenType.CLASS,
            ["else"] = TokenType.ELSE,
            ["false"] = TokenType.FALSE,
            ["for"] = TokenType.FOR,
            ["fun"] = TokenType.FUN,
            ["if"] = TokenType.IF,
            ["nil"] = TokenType.NIL,
            ["or"] = TokenType.OR,
            ["print"] = TokenType.PRINT,
            ["return"] = TokenType.RETURN,
            ["super"] = TokenType.SUPER,
            ["this"] = TokenType.THIS,
            ["true"] = TokenType.TRUE,
            ["var"] = TokenType.VAR,
            ["while"] = TokenType.WHILE
        };


		private readonly string _source;
		private readonly List<Token> _tokens;
		private int _start;
		private int _current;
		private int _line;
        
	    public Scanner(string source) 
        {
			_line = 1;
            _tokens = new List<Token>();
		    _source = source;
	    }

        public IEnumerable<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // We are at the beginning of the next lexeme.
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.EOF, "", null, _line));
            return _tokens;
        }

		private void ScanToken()
		{
			char c = Advance();
			switch (c)
			{
				case '(': AddToken(TokenType.LEFT_PAREN); break;
				case ')': AddToken(TokenType.RIGHT_PAREN); break;
				case '{': AddToken(TokenType.LEFT_BRACE); break;
				case '}': AddToken(TokenType.RIGHT_BRACE); break;
				case ',': AddToken(TokenType.COMMA); break;
				case '.': AddToken(TokenType.DOT); break;
				case '-': AddToken(TokenType.MINUS); break;
				case '+': AddToken(TokenType.PLUS); break;
				case ';': AddToken(TokenType.SEMICOLON); break;
				case '*': AddToken(TokenType.STAR); break;
				case '!': AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
				case '=': AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL); break;
				case '<': AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
				case '>': AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;
				case '/':
					if (Match('*'))
					{
						while (!IsAtEnd() && !Match('*') && !Match('/'))
						{
							if (Peek() == '\n') _line++;
							Advance();
						}
						Advance();
					}
					else if (Match('/'))
					{
						while (Peek() != '\n' && !IsAtEnd())
						{
							Advance();
						}
					}
					else
					{
						AddToken(TokenType.SLASH);
					}
					break;
				case ' ':
				case '\r':
				case '\t':
					// Ignore whitespace.                      
					break;
				case '\n':
					_line++;
					break;
				case '"':
                    Str();
                    break;
				default:
					if (IsDigit(c))
					{
						Number();
					}
					else if (IsAlpha(c))
					{
						Identifier();
					}
					else
					{

						Program.Error(_line, "Unexpected character.");
					}
					break;
			}
		}

		private void Identifier()
		{
			while (IsAlphaNumeric(Peek())) Advance();

			// check if identifier is a reserved word
			var text = _source.Substring(_start, _current - _start);
            if (!_keywords.TryGetValue(text, out var type))
            {
				type = TokenType.IDENTIFIER;
			}
			AddToken(type);
		}

		private void Number()
		{
			while (IsDigit(Peek())) Advance();

			// Look for fractional part.
			if (Peek() == '.' && IsDigit(PeekNext()))
			{
				Advance(); // consume the decimal
				while (IsDigit(Peek())) Advance();
			}

			AddToken(TokenType.NUMBER, double.Parse(_source.Substring(_start, _current - _start)));
		}

		private void Str()
        {
		    while (Peek() != '"' && !IsAtEnd())
            {
			    if (Peek() == '\n') _line++;
			    Advance();
	        }
		
		    // String wasn't terminated. Error
		    if (IsAtEnd()) {
			    Program.Error(_line, "Unterminated string.");
			    return;
		    }

            // Include inclosing "
            Advance();

			// Trim surrounding quotes from string
			var from = _start + 1;
			var cnt = _current - 1 - from;
            var value = _source.Substring(from, cnt);
            AddToken(TokenType.STRING, value);
	    }

        /// <summary>
        /// Returns true if the current value matches
        /// expected and consumes it.
        /// </summary>
        /// <param name="expected"></param>
        /// <returns></returns>
		private bool Match(char expected)
		{
			if (IsAtEnd()) return false;
			if (_source[_current] != expected) return false;
			_current++;
			return true;
		}

        /// <summary>
        /// Returns the current value without consuming
        /// it.
        /// </summary>
        /// <returns></returns>
		private char Peek()
		{
			if (IsAtEnd()) return '\0';
			return _source[_current];
		}

        /// <summary>
        /// Returns the next value without consuming it.
        /// </summary>
        /// <returns></returns>
		private char PeekNext()
		{
			if (_current + 1 >= _source.Length) return '\0';
			return _source[_current + 1];
		}

		private bool IsAlpha(char c)
		{
			return (c >= 'a' && c <= 'z')
					|| (c >= 'A' && c <= 'Z')
					|| c == '_';
		}

		private bool IsAlphaNumeric(char c)
		{
			return IsAlpha(c) || IsDigit(c);
		}

		private bool IsDigit(char c)
		{
			return c >= '0' && c <= '9';
		}

		private bool IsAtEnd()
		{
			return _current >= _source.Length;
		}

        /// <summary>
        /// Consumes the next token and returns it
        /// </summary>
        /// <returns></returns>
		private char Advance()
		{
			_current++;
			return _source[_current - 1];
		}

		private void AddToken(TokenType type)
		{
			AddToken(type, null);
		}

		private void AddToken(TokenType type, object literal)
		{
			var text = _source.Substring(_start, _current - _start);
			_tokens.Add(new Token(type, text, literal, _line));
		}

	}
}