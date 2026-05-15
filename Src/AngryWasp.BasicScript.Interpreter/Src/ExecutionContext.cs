using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace AngryWasp.BasicScript.App
{
    public class ExecutionContext : IExecutionContext
    {
        public void Install(Interpreter interpreter)
        {
            interpreter.AddFunction("exec", Exec);
            interpreter.AddFunction("pwd", Pwd);
            interpreter.AddFunction("cd", Cd);
            interpreter.AddFunction("dir", DirName);
            interpreter.AddFunction("file", FileName);
            interpreter.AddFunction("ext", ExtName);
            interpreter.AddFunction("http", Http);
            interpreter.AddFunction("jq", Jq);
            interpreter.AddFunction("fmt", Fmt);
            interpreter.AddFunction("trim", Trim);
            interpreter.AddFunction("trimws", TrimWhitespace);
            interpreter.AddFunction("split", Split);
            interpreter.AddFunction("env", Env);
            interpreter.AddFunction("files", Files);
            interpreter.AddFunction("dirs", Dirs);
            interpreter.AddFunction("read", Read);
            interpreter.AddFunction("mk", MakeFile);
            interpreter.AddFunction("rm", RemoveFile);
            interpreter.AddFunction("mkdir", MakeDir);
            interpreter.AddFunction("rmdir", RemoveDir);
            interpreter.AddFunction("pause", Pause);
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
                dict[varName] = (true, entries.Select(x => new Value(x)).ToArray());
            else
                dict.Add(varName, (true, entries.Select(x => new Value(x)).ToArray()));

            return new Value(dict[varName].Values.Length);
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

            var cmd = args[0].String;
            var arguments = args.Count == 1 ? string.Empty : args[1].String;

            return new Value(ExternalTool.Run(cmd, arguments));
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
                throw new ArgumentException("Cannot remove what doesn't exist");

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

            if (!Directory.Exists(path))
                throw new ArgumentException("Cannot remove what doesn't exist");

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
    }
}
