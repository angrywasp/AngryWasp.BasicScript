using System;
using System.Linq;
using AngryWasp.Helpers;

namespace AngryWasp.BasicScript
{
    public enum Value_Type
    {
        Undefined,
        String,
        Bytes,
        Integer
    }

    public struct Value
    {
        public Value_Type Type { get; set; }

        public string String { get; set; }
        public byte[] Bytes { get; set; }
        public Int128 Integer { get; set; }

        public Value(string value)
        {
            this.Type = Value_Type.String;
            this.String = value;
        }

        public Value(byte[] value)
        {
            this.Type = Value_Type.Bytes;
            this.Bytes = value;
        }

        public Value(Int128 value)
        {
            this.Type = Value_Type.Integer;
            this.Integer = value;
        }

        public Value UnaryOp(Token tok)
        {
            if (Type != Value_Type.Integer)
                throw new Exception("Can only do unary operations on numbers");

            switch (tok)
            {
                case Token.Plus: return this;
                case Token.Minus: return new Value(-Integer);
                case Token.Not: return new Value(Integer == 0 ? 1 : 0);
            }

            throw new Exception("Unknown unary operator");
        }

        public Value BinOp(Value b, Token tok)
        {
            Value a = this;
            if (a.Type != b.Type)
                throw new Exception("Invalid cast");

            if (tok == Token.Plus)
            {
                switch (a.Type)
                {
                    case Value_Type.String:
                        return new Value(a.String + b.String);
                    case Value_Type.Integer:
                        return new Value(a.Integer.Add(b.Integer));
                    case Value_Type.Bytes:
                        return new Value(a.Bytes.Concat(b.Bytes).ToArray());
                }
            }
            else if (tok == Token.ExactEqual)
            {
                switch (a.Type)
                {
                    case Value_Type.String:
                        return new Value(a.String == b.String ? 1 : 0);
                    case Value_Type.Integer:
                        return new Value(a.Integer == b.Integer ? 1 : 0);
                    case Value_Type.Bytes:
                        return new Value(a.Bytes.SequenceEqual(b.Bytes) ? 1 : 0);
                }
            }
            else if (tok == Token.NotEqual)
            {
                switch (a.Type)
                {
                    case Value_Type.String:
                        return new Value(a.String == b.String ? 0 : 1);
                    case Value_Type.Integer:
                        return new Value(a.Integer == b.Integer ? 0 : 1);
                    case Value_Type.Bytes:
                        return new Value(a.Bytes.SequenceEqual(b.Bytes) ? 0 : 1);
                }
            }
            else
            {
                if (a.Type == Value_Type.String)
                    throw new Exception("Cannot do binop on strings(except +).");

                if (a.Type == Value_Type.Bytes)
                    throw new Exception("Cannot do binop on bytes(except +).");

                switch (tok)
                {
                    case Token.Minus: return new Value(a.Integer.Subtract(b.Integer));
                    case Token.Asterisk: return new Value(a.Integer.Multiply(b.Integer));
                    case Token.DoubleAsterisk: return new Value(a.Integer.Pow(b.Integer));
                    case Token.Slash: return new Value(a.Integer.Divide(b.Integer));
                    case Token.Less: return new Value(a.Integer < b.Integer ? 1 : 0);
                    case Token.More: return new Value(a.Integer > b.Integer ? 1 : 0);
                    case Token.LessEqual: return new Value(a.Integer <= b.Integer ? 1 : 0);
                    case Token.MoreEqual: return new Value(a.Integer >= b.Integer ? 1 : 0);
                    case Token.And: return new Value((a.Integer != 0) && (b.Integer != 0) ? 1 : 0);
                    case Token.Or: return new Value((a.Integer != 0) || (b.Integer != 0) ? 1 : 0);
                    case Token.Caret: return new Value(a.Integer.Xor(b.Integer));
                    case Token.ShiftLeft: return new Value(a.Integer.ShiftLeft(b.Integer));
                    case Token.ShiftRight: return new Value(a.Integer.ShiftRight(b.Integer));
                }
            }

            throw new Exception("Unknown binary operator.");
        }

        public override string ToString()
        {
            switch (this.Type)
            {
                case Value_Type.String:
                    return this.String;
                case Value_Type.Integer:
                    return this.Integer.ToString();
                case Value_Type.Bytes:
                    return this.Bytes.ToPrefixedHex();
                default: throw new Exception($"Unsupported type {this.Type.ToString()}");
            }
        }
    }
}
