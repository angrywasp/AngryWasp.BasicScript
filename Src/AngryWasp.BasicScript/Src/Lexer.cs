using System;
using System.Diagnostics;
using AngryWasp.Helpers;
using Newtonsoft.Json.Serialization;

namespace AngryWasp.BasicScript
{
    public class Lexer
    {
        private string source;
        private Marker sourceMarker;
        private char lastChar;

        public string Source => source;

        public Marker TokenMarker { get; set; }

        public string Identifier { get; set; }
        public Value Value { get; set; }

        public Lexer(string input)
        {
            source = input;
            sourceMarker = new Marker(0, 1, 1);
            lastChar = source[0];
        }

        public void InsertSource(string input) => source = source.Insert(sourceMarker.Pointer + 1, input);
        public void AppendSource(string input) => source += input;

        public void Jump(Marker marker) {
            this.sourceMarker = marker;
            lastChar = GetChar();
        }

        public char PeekChar()
        {
            var m = sourceMarker;
            var lc = lastChar;

            return GetChar(ref m, ref lc);
        }

        public char GetChar() => GetChar(ref sourceMarker, ref lastChar);

        public char GetChar(ref Marker marker, ref char lastCharacter)
        {
            marker.Column++;
            marker.Pointer++;

            if (marker.Pointer >= source.Length)
                return lastCharacter = (char)0;

            if ((lastCharacter = source[marker.Pointer]) == '\n')
            {
                marker.Column = 1;
                marker.Line++;
            }
            return lastCharacter;
        }

        public Token GetToken() => GetToken(ref sourceMarker, ref lastChar);

        public Token PeekToken()
        {
            var m = sourceMarker;
            var lc = lastChar;

            return GetToken(ref m, ref lc);
        }

        public Token GetToken(ref Marker marker, ref char lastCharacter)
        {
            while (lastCharacter == ' ' || lastCharacter == '\t' || lastCharacter == '\r')
                GetChar(ref marker, ref lastCharacter);

            TokenMarker = marker;

            if (char.IsLetter(lastCharacter))
            {
                Identifier = lastCharacter.ToString();
                while (char.IsLetterOrDigit(GetChar(ref marker, ref lastCharacter)))
                    Identifier += lastCharacter;

                switch (Identifier.ToUpper())
                {
                    case "PRINT": return Token.Print;
                    case "INCLUDE": return Token.Include;
                    case "IF": return Token.If;
                    case "ENDIF": return Token.EndIf;
                    case "THEN": return Token.Then;
                    case "ELSE": return Token.Else;
                    case "LOOP": return Token.Loop;
                    case "TO": return Token.To;
                    case "NEXT": return Token.Next;
                    case "GOTO": return Token.Goto;
                    case "INPUT": return Token.Input;
                    case "VAR": return Token.Var;
                    case "CONST": return Token.Const;
                    case "END": return Token.End;
                    case "EXIT": return Token.Exit;
                    case "OR": return Token.Or;
                    case "AND": return Token.And;
                    case "NOT": return Token.Not;
                    case "ASSERT": return Token.Assert;
                    case "FUNC": return Token.Function;
                    case "LOCAL": return Token.Local;
                    case "GLOBAL": return Token.Global;
                    case "REF": return Token.Ref;
                    case "REM":
                        while (lastCharacter != '\n') GetChar(ref marker, ref lastCharacter);
                        GetChar(ref marker, ref lastCharacter);
                        return GetToken(ref marker, ref lastCharacter);
                    default:
                        return Token.Identifier;
                }
            }

            if (char.IsDigit(lastCharacter))
            {
                string num = "";
                do { num += lastCharacter; } while (char.IsDigit(GetChar(ref marker, ref lastCharacter)));

                if (num + lastCharacter == "0x")
                {
                    do { num += lastCharacter; } while (char.IsAsciiHexDigit(GetChar(ref marker, ref lastCharacter)));
                    Value = new Value(num.TrimHexPrefix().FromHex());
                    return Token.Value;
                }

                if (!num.ParseInt128(out Int128 real))
                    throw new Exception("ERROR while parsing number");

                Value = new Value(real);
                return Token.Value;
            }

            Token tok = Token.Unknown;
            switch (lastCharacter)
            {
                case '\n': tok = Token.NewLine; break;
                case ':': tok = Token.Colon; break;
                case ',': tok = Token.Comma; break;
                case '=':
                    GetChar(ref marker, ref lastCharacter);
                    if (lastCharacter == '=') tok = Token.ExactEqual;
                    else tok = Token.Equals;
                    break;
                case '+': tok = Token.Plus; break;
                case '-': tok = Token.Minus; break;
                case '/': tok = Token.Slash; break;
                case '*':
                    GetChar(ref marker, ref lastCharacter);
                    if (lastCharacter == '*') tok = Token.DoubleAsterisk;
                    else tok = Token.Asterisk;
                    break;
                case '^': tok = Token.Caret; break;
                case '(': tok = Token.LParen; break;
                case ')': tok = Token.RParen; break;
                case '[': tok = Token.LSParen; break;
                case ']': tok = Token.RSParen; break;
                case '\'':
                case '#':
                    // skip comment until new line
                    while (lastCharacter != '\n') GetChar(ref marker, ref lastCharacter);
                    GetChar(ref marker, ref lastCharacter);
                    return GetToken(ref marker, ref lastCharacter);
                case '<':
                    GetChar(ref marker, ref lastCharacter);
                    if (lastCharacter == '>') tok = Token.NotEqual;
                    else if (lastCharacter == '=') tok = Token.LessEqual;
                    else if (lastCharacter == '<') tok = Token.ShiftLeft;
                    else return Token.Less;
                    break;
                case '>':
                    GetChar(ref marker, ref lastCharacter);
                    if (lastCharacter == '=') tok = Token.MoreEqual;
                    else if (lastCharacter == '>') tok = Token.ShiftRight;
                    else return Token.More;
                    break;
                case '"':
                    string str = "";
                    while (GetChar(ref marker, ref lastCharacter) != '"')
                    {
                        if (lastCharacter == (char)0)
                            throw new Exception("Unexpected end of file while building string. Potential unclused quotes");

                        if (lastCharacter == '\\')
                        {
                            // parse \n, \t, \\, \"
                            switch (char.ToLower(GetChar(ref marker, ref lastCharacter)))
                            {
                                case 'n': str += '\n'; break;
                                case 't': str += '\t'; break;
                                case '\\': str += '\\'; break;
                                case '"': str += '"'; break;
                            }
                        }
                        else
                        {
                            str += lastCharacter;
                        }
                    }
                    Value = new Value(str);
                    tok = Token.Value;
                    break;
                case (char)0:
                    return Token.EOF;
            }

            GetChar(ref marker, ref lastCharacter);
            return tok;
        }
    }
}

