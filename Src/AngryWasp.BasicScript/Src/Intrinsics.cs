using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using Org.BouncyCastle.Bcpg;

namespace AngryWasp.BasicScript
{
    class Intrinsics
    {
        public static void InstallAll(Interpreter interpreter)
        {
            interpreter.AddFunction("str", Str);
            interpreter.AddFunction("string", Str);
            interpreter.AddFunction("int", Int);
            interpreter.AddFunction("integer", Int);
            interpreter.AddFunction("byte", Bytes);
            interpreter.AddFunction("bytes", Bytes);
            interpreter.AddFunction("abs", Abs);
            interpreter.AddFunction("min", Min);
            interpreter.AddFunction("max", Max);
            interpreter.AddFunction("not", Not);
            interpreter.AddFunction("count", Count);
            interpreter.AddFunction("require", Require);
            
            //Convert an ASCII string to bytes
            interpreter.AddFunction("encode", Encode);
            //Convert bytes to an ASCII string
            interpreter.AddFunction("decode", Decode);
            interpreter.AddFunction("hash", Hash);
        }

        public static Value Str(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            switch (a.Type)
            {
                case Value_Type.String:
                    return new Value(a.String);
                case Value_Type.Integer:
                    return new Value(a.Integer.ToString());
                case Value_Type.Bytes:
                    return new Value(a.Bytes.ToPrefixedHex());
                default: throw new ArgumentException();
            }
        }

        public static Value Int(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            switch (a.Type)
            {
                case Value_Type.String:
                    var match = Regex.Match(a.String, @"\b0x[\dA-Fa-f]+\b");
                    if (match.Success)
                        return new Value(Int128.CreateChecked(new BigInteger(a.String.TrimHexPrefix().FromHex())));
                    else if (a.String.ParseInt128(out var i))
                        return new Value(i);
                    else
                        throw new Exception($"Unable to parse value {a.String} as integer");
                case Value_Type.Integer:
                    return new Value(a.Integer);
                case Value_Type.Bytes:
                    if (a.Bytes.Length > 16)
                        throw new ArgumentException("Attempt to convert bytes to 128-bit integer out of range");

                    return new Value(Int128.CreateChecked(new BigInteger(a.Bytes)));
                default: throw new ArgumentException();
            }
        }

        //converts int or bytes to a hex string or a hex string to bytes 
        public static Value Bytes(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            switch (a.Type)
            {
                case Value_Type.String:
                    var match = Regex.Match(a.String, @"\b0x[\dA-Fa-f]+\b");
                    if (!match.Success)
                        throw new ArgumentException("Attempt to convert non-hex string to bytes. Perhaps you wanted to use encode/decode");

                    try {
                        return new Value(a.String.TrimHexPrefix().FromHex());
                    } catch {
                        throw new ArgumentException("Failed to convert string to bytes");
                    }
                case Value_Type.Integer:
                    return new Value(BigInteger.CreateChecked(a.Integer).ToByteArray());
                case Value_Type.Bytes:
                    return new Value(a.Bytes);
                default: throw new ArgumentException();
            }
        }

        public static Value Abs(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.Integer)
                throw new ArgumentException($"abs function is not valid on value with type {a.Type}");

            return new Value(Int128.Abs(a.Integer));
        }

        public static Value Min(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 2) throw new ArgumentException();

            var a = args[0];
            var b = args[1];

            if (a.Type != Value_Type.Integer || b.Type != Value_Type.Integer)
                throw new ArgumentException($"min function is not valid on values with types {a.Type} and {b.Type}");

            return new Value(Int128.Min(a.Integer, b.Integer));
        }

        public static Value Max(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 2) throw new ArgumentException();

            var a = args[0];
            var b = args[1];

            if (a.Type != Value_Type.Integer || b.Type != Value_Type.Integer)
                throw new ArgumentException($"max function is not valid on values with types {a.Type} and {b.Type}");

            return new Value(Int128.Max(a.Integer, b.Integer));
        }

        public static Value Not(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.Integer)
                throw new ArgumentException($"not function is not valid on value with type {a.Type}");

            return new Value(args[0].Integer == 0 ? 1 : 0);
        }

        public static Value Count(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.String)
                throw new ArgumentException($"count function is not valid on value with type {a.Type}");

            Variable var = null;

            if (interpreter.CallStack.CurrentFunction != null)
            {
                if (interpreter.CallStack.CurrentFunction.Constants.ContainsKey(a.String))
                    var = interpreter.CallStack.CurrentFunction.Constants[a.String];
                else if (interpreter.CallStack.CurrentFunction.Variables.ContainsKey(a.String))
                    var = interpreter.CallStack.CurrentFunction.Variables[a.String];
            }
            else
            {
                if (interpreter.Constants.ContainsKey(a.String))
                    var = interpreter.Constants[a.String];
                else  if (interpreter.Variables.ContainsKey(a.String))
                    var = interpreter.Variables[a.String];     
            }

            if (var == default)
                throw new Exception($"Variable {a.String} not found");
                
            return new Value(var.IsArray ? var.Values.Length : 1);
        }

        public static Value Require(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 2)
                throw new ArgumentException($"Incorrect number of arguments. Expected 2, got {args.Count}");

            if (args[0].Type != args[1].Type)
                throw new ArgumentException($"Argument types do not match");

            if (args[0].BinOp(args[1], Token.ExactEqual).Integer != 1)
                throw new ArgumentException($"Argument values do not match");

            return new Value(1);
        }

        public static Value Encode(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.String)
                throw new ArgumentException($"encode function is not valid on value with type {a.Type}");

            return new Value(Encoding.UTF8.GetBytes(a.String));
        }

        public static Value Decode(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.Bytes)
                throw new ArgumentException($"decode function is not valid on value with type {a.Type}");

            return new Value(Encoding.UTF8.GetString(a.Bytes));
        }

        public static Value Hash(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];
            byte[] bytes;
            
            switch (a.Type)
            {
                case Value_Type.String:
                    var match = Regex.Match(a.String, @"\b0x[\dA-Fa-f]+\b");
                    bytes = match.Success ? a.String.TrimHexPrefix().FromHex() : Encoding.ASCII.GetBytes(a.String);
                    break;
                case Value_Type.Integer:
                    bytes = BigInteger.CreateChecked(a.Integer).ToByteArray();
                    break;
                case Value_Type.Bytes:
                    bytes = a.Bytes;
                    break;
                default: throw new ArgumentException();
            }
            
            return new Value(Keccak.Hash256(bytes));
        }
    }
}
