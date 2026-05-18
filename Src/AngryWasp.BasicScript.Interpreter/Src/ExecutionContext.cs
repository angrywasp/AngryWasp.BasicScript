using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using Newtonsoft.Json.Linq;

namespace AngryWasp.BasicScript.App
{
    public class ExecutionContext : IExecutionContext
    {
        public void Install(Interpreter interpreter)
        {
            interpreter.AddIntrinsic("str", Str);
            interpreter.AddIntrinsic("string", Str);
            interpreter.AddIntrinsic("int", Int);
            interpreter.AddIntrinsic("integer", Int);
            interpreter.AddIntrinsic("byte", Bytes);
            interpreter.AddIntrinsic("bytes", Bytes);
            interpreter.AddIntrinsic("abs", Abs);
            interpreter.AddIntrinsic("min", Min);
            interpreter.AddIntrinsic("max", Max);
            interpreter.AddIntrinsic("not", Not);
            interpreter.AddIntrinsic("count", Count);
            interpreter.AddIntrinsic("require", Require);
            
            //Convert an ASCII string to bytes
            interpreter.AddIntrinsic("encode", Encode);
            //Convert bytes to an ASCII string
            interpreter.AddIntrinsic("decode", Decode);
            interpreter.AddIntrinsic("hash", Hash);

            interpreter.AddIntrinsic("substr", SubString);
            interpreter.AddIntrinsic("substring", SubString);
            interpreter.AddIntrinsic("exec", Exec);
            interpreter.AddIntrinsic("pwd", Pwd);
            interpreter.AddIntrinsic("cd", Cd);
            interpreter.AddIntrinsic("dir", DirName);
            interpreter.AddIntrinsic("file", FileName);
            interpreter.AddIntrinsic("ext", ExtName);
            interpreter.AddIntrinsic("http", Http);
            interpreter.AddIntrinsic("jq", Jq);
            interpreter.AddIntrinsic("fmt", Fmt);
            interpreter.AddIntrinsic("trim", Trim);
            interpreter.AddIntrinsic("trimws", TrimWhitespace);
            interpreter.AddIntrinsic("split", Split);
            interpreter.AddIntrinsic("env", Env);
            interpreter.AddIntrinsic("files", Files);
            interpreter.AddIntrinsic("dirs", Dirs);
            interpreter.AddIntrinsic("read", Read);
            interpreter.AddIntrinsic("mk", MakeFile);
            interpreter.AddIntrinsic("rm", RemoveFile);
            interpreter.AddIntrinsic("mkdir", MakeDir);
            interpreter.AddIntrinsic("rmdir", RemoveDir);
            interpreter.AddIntrinsic("pause", Pause);
        }

        private void ValidateArgs(List<Value> args, int expectedCount)
        {
            if (args.Count != expectedCount) throw new ArgumentException($"Incorrect number of arguments. Expected {expectedCount}, got {args.Count}");
            for (int i = 0; i < args.Count; i++)
                if (args[i].Type != Value_Type.String)
                    throw new ArgumentException($"Argument {i} is incompatible type {args[i].Type}");
        }

        private Value MakeStringArrayRef(Interpreter interpreter, string[] entries, string varName)
        {
            var dict = interpreter.CallStack.CurrentFunction != null ? interpreter.CallStack.CurrentFunction.Variables : interpreter.Variables;

            if (dict.ContainsKey(varName))
                dict[varName] = new Variable(varName, true, entries.Select(x => new Value(x)).ToArray());
            else
                dict.Add(varName, new Variable(varName, true, entries.Select(x => new Value(x)).ToArray()));

            return new Value(dict[varName].Values.Length);
        }

                private Value Str(Interpreter interpreter, List<Value> args)
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

        private Value Int(Interpreter interpreter, List<Value> args)
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
        private Value Bytes(Interpreter interpreter, List<Value> args)
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

        private Value Abs(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.Integer)
                throw new ArgumentException($"abs function is not valid on value with type {a.Type}");

            return new Value(Int128.Abs(a.Integer));
        }

        private Value Min(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 2) throw new ArgumentException();

            var a = args[0];
            var b = args[1];

            if (a.Type != Value_Type.Integer || b.Type != Value_Type.Integer)
                throw new ArgumentException($"min function is not valid on values with types {a.Type} and {b.Type}");

            return new Value(Int128.Min(a.Integer, b.Integer));
        }

        private Value Max(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 2) throw new ArgumentException();

            var a = args[0];
            var b = args[1];

            if (a.Type != Value_Type.Integer || b.Type != Value_Type.Integer)
                throw new ArgumentException($"max function is not valid on values with types {a.Type} and {b.Type}");

            return new Value(Int128.Max(a.Integer, b.Integer));
        }

        private Value Not(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.Integer)
                throw new ArgumentException($"not function is not valid on value with type {a.Type}");

            return new Value(args[0].Integer == 0 ? 1 : 0);
        }

        private Value Count(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.String)
                throw new ArgumentException($"count function is not valid on value with type {a.Type}");

            Variable var = null;

            if (interpreter.CallStack.CurrentFunction != null)
            {
                if (interpreter.CallStack.CurrentFunction.Variables.ContainsKey(a.String))
                    var = interpreter.CallStack.CurrentFunction.Variables[a.String];
            }
            else
            {
                if (interpreter.Variables.ContainsKey(a.String))
                    var = interpreter.Variables[a.String];     
            }

            if (var == default)
                throw new Exception($"Variable {a.String} not found");
                
            return new Value(var.IsArray ? var.Values.Length : 1);
        }

        private Value Require(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 2)
                throw new ArgumentException($"Incorrect number of arguments. Expected 2, got {args.Count}");

            if (args[0].Type != args[1].Type)
                throw new ArgumentException($"Argument types do not match");

            if (args[0].BinOp(args[1], Token.ExactEqual).Integer != 1)
                throw new ArgumentException($"Argument values do not match");

            return new Value(1);
        }

        private Value Encode(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.String)
                throw new ArgumentException($"encode function is not valid on value with type {a.Type}");

            return new Value(Encoding.UTF8.GetBytes(a.String));
        }

        private Value Decode(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1) throw new ArgumentException();

            var a = args[0];

            if (a.Type != Value_Type.Bytes)
                throw new ArgumentException($"decode function is not valid on value with type {a.Type}");

            return new Value(Encoding.UTF8.GetString(a.Bytes));
        }

        private Value Hash(Interpreter interpreter, List<Value> args)
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

        public Value Pwd(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 0)
                throw new ArgumentException($"Incorrect number of arguments. Expected 0, got {args.Count}");

            return new Value(Environment.CurrentDirectory);
        }

        public Value Cd(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                    throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            Environment.CurrentDirectory = args[0].String;

            return new Value(Environment.CurrentDirectory);
        }

        public Value DirName(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                    throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            return new Value(Path.GetDirectoryName(args[0].String));
        }

        public Value Files(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 3);

            var path = args[0].String;
            var pattern = args[1].String;
            var newName = args[2].String;

            return MakeStringArrayRef(interpreter, Directory.GetFiles(path, pattern), newName);
        }

        public Value Dirs(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 3);

            var path = args[0].String;
            var pattern = args[1].String;
            var newName = args[2].String;

            return MakeStringArrayRef(interpreter, Directory.GetDirectories(path, pattern), newName);
        }

        public Value FileName(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 1 || args.Count > 2)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1 or 2, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                    throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            if (args.Count == 1)
            {
                if (args[0].Type != Value_Type.String)
                    throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

                return new Value(Path.GetFileName(args[0].String));
            }
            else if (args.Count == 2)
            {
                if (args[0].Type != Value_Type.String)
                    throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

                if (args[1].Type != Value_Type.Integer)
                    throw new ArgumentException($"Argument 1 is incompatible type {args[0].Type}");

                if (args[1].Integer == 0)
                    return new Value(Path.GetFileNameWithoutExtension(args[0].String));
                else
                    return new Value(Path.GetFileName(args[0].String));
            }

            return new Value(Path.GetFileName(args[0].String));
        }

        public Value ExtName(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                    throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            return new Value(Path.GetExtension(args[0].String));
        }

        public Value Exec(Interpreter interpreter, List<Value> args)
        {
            if (args.Count > 2)
                throw new ArgumentException($"Incorrect number of arguments. Expected 2 max, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            if (args.Count > 1 && args[1].Type != Value_Type.String)
                throw new ArgumentException($"Argument 1 is incompatible type {args[1].Type}");

            if (args.Count > 2 && args[2].Type != Value_Type.String)
                throw new ArgumentException($"Argument 2 is incompatible type {args[2].Type}");

            var cmd = args[0].String;
            var arguments = args.Count == 1 ? string.Empty : args[1].String;
            string envVar = args.Count > 2 ? args[2].String : null;

            return new Value(ExternalTool.Run(cmd, arguments, envVar));
        }

        public Value Http(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 1);

            var url = args[0].String;

            var request = AngryWasp.Net.Http.HttpRequestAsync(url, 5).GetAwaiter().GetResult();
            if (request.HasError)
                return new Value(request.StatusCode);
            else
                return new Value(request.Data);
        }

        public Value Jq(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 2);

            var json = args[0].String;
            var query = args[1].String;

            JObject o = JObject.Parse(json);
            var t = o.SelectToken(query);

            return new Value(t.HasValues ? t.ToString() : string.Empty);
        }

        public Value Fmt(Interpreter interpreter, List<Value> args)
        {
            var fmt = args[0].String;

            return new Value(string.Format(fmt, args.Skip(1).Select(x => x.ToString()).ToArray()));
        }

        public Value Trim(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 2);

            var txt = args[0].String;
            var trimmed = args[1].String;

            return new Value(txt.Trim(trimmed.ToCharArray()));
        }

        public Value TrimWhitespace(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 1);

            var txt = args[0].String;

            return new Value(txt.Trim());
        }
        
        public Value Split(Interpreter interpreter, List<Value> args)
        {
            ValidateArgs(args, 3);

            var value = args[0].String;
            var delimiter = args[1].String;
            var newName = args[2].String;

            return MakeStringArrayRef(interpreter, value.Split(delimiter, StringSplitOptions.RemoveEmptyEntries), newName);
        }

        public Value Env(Interpreter interpreter, List<Value> args)
        {
            if (args.Count == 1)
            {
                ValidateArgs(args, 1);

                var key = args[0].String;

                var env = Environment.GetEnvironmentVariable(key);
                if (env == null)
                    throw new Exception($"Environment variable {key} not found");
                    
                return new Value(env);
            }
            else if (args.Count == 2)
            {
                ValidateArgs(args, 2);

                var key = args[0].String;
                var value = args[1].String;

                Environment.SetEnvironmentVariable(key, value);
                
                var env = Environment.GetEnvironmentVariable(key);
                if (env == null)
                    throw new Exception($"Environment variable {key} not found");
                    
                return new Value(env);
            }
            else
                throw new ArgumentException($"Incorrect number of arguments. Expected 1 or 2, got {args.Count}");
        }

        public Value Read(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            var path = Path.Combine(Environment.GetEnvironmentVariable("BSI_SOURCE_DIR"), args[0].String);

            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found {path}");

            var text = File.ReadAllText(path);

            return new Value(text);
        }

        public Value MakeDir(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            string path = args[0].String;

            if (!Path.IsPathFullyQualified(path))
                path = Path.Combine(Environment.GetEnvironmentVariable("BSI_SOURCE_DIR"), path);

            if (Directory.Exists(path))
                throw new ArgumentException("Cannot create directory. Already exists");

            Directory.CreateDirectory(path);

            return new Value(path);
        }

        public Value RemoveDir(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            string path = args[0].String;

            if (!Path.IsPathFullyQualified(path))
                path = Path.Combine(Environment.GetEnvironmentVariable("BSI_SOURCE_DIR"), path);

            if (!Directory.Exists(path))
                return new Value(path);

            Directory.Delete(path, true);

            return new Value(path);
        }

        public Value MakeFile(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            string path = args[0].String;

            if (!Path.IsPathFullyQualified(path))
                path = Path.Combine(Environment.GetEnvironmentVariable("BSI_SOURCE_DIR"), path);

            if (File.Exists(path))
                throw new ArgumentException("Cannot create file. Already exists");

            File.Create(path);

            return new Value(path);
        }

        public Value RemoveFile(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            string path = args[0].String;

            if (!Path.IsPathFullyQualified(path))
                path = Path.Combine(Environment.GetEnvironmentVariable("BSI_SOURCE_DIR"), path);

            if (!File.Exists(path))
                return new Value(path);

            File.Delete(path);

            return new Value(path);
        }

        public Value Pause(Interpreter interpreter, List<Value> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"Incorrect number of arguments. Expected 1, got {args.Count}");

            if (args[0].Type != Value_Type.Integer)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            Thread.Sleep((int)args[0].Integer);

            return new Value(1);
        }

        public Value SubString(Interpreter interpreter, List<Value> args)
        {
            if (args.Count < 2 || args.Count > 3)
                throw new ArgumentException($"Incorrect number of arguments. Expected 2 - 3, got {args.Count}");

            if (args[0].Type != Value_Type.String)
                throw new ArgumentException($"Argument 0 is incompatible type {args[0].Type}");

            if (args.Count == 2)
            {
                if (args[1].Type != Value_Type.Integer)
                    throw new ArgumentException($"Argument 1 is incompatible type {args[0].Type}");

                return new Value(args[0].String.Substring((int)args[1].Integer));
            }
            else if (args.Count == 3)
            {
                if (args[1].Type != Value_Type.Integer)
                    throw new ArgumentException($"Argument 1 is incompatible type {args[0].Type}");

                if (args[2].Type != Value_Type.Integer)
                    throw new ArgumentException($"Argument 2 is incompatible type {args[0].Type}");

                return new Value(args[0].String.Substring((int)args[1].Integer, (int)args[2].Integer));
            }

            return new Value();
        }
    }
}
