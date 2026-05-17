using System;
using System.IO;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Cli.Config;

namespace AngryWasp.BasicScript.App
{
    internal class MainClass
    {
        private static void Main(string[] rawArgs)
        {
            var parsedArgs = Arguments.Parse(rawArgs);
            CommandLine cl = new CommandLine();
            string[,] extras = {
                {"env", "Set environment variables for use by the script."}
            };

            if (!ConfigMapper<CommandLine>.Process(parsedArgs, cl, extras))
                return;

            Application.RegisterCommands();
                
            if (string.IsNullOrEmpty(cl.Script))
            {
                Console.WriteLine("No script file");
                return;
            }

            if (!File.Exists(cl.Script))
            {
                Console.WriteLine($"Script '{cl.Script}' does not exist");
                return;
            }

            Environment.SetEnvironmentVariable("BSI_SOURCE_DIR", Path.GetDirectoryName(Path.GetFullPath(rawArgs[0])));
            Environment.SetEnvironmentVariable("BSI_SOURCE", Path.GetFullPath(rawArgs[0]));

            var x = 0;
            for (int i = 0; i < rawArgs.Length; i++)
                if (rawArgs[i].ToLower() == "--env")
                    Environment.SetEnvironmentVariable($"${++x}", rawArgs[++i]);

            try
            {
                var interpreter = new Interpreter(File.ReadAllText(cl.Script), new ExecutionContext());
                interpreter.printHandler += Console.WriteLine;
                interpreter.inputHandler += Console.ReadLine;
                interpreter.Exec();
                interpreter.Lexer.AppendSource("\r\n" + cl.Entry + "\r\n");
                interpreter.ResetToken();
                Console.WriteLine(interpreter.Lexer.Source);
                interpreter.Exec();
            }
            catch (BasicException ex)
            {
                Console.WriteLine($"{ex.Message}: Line {ex.line}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}