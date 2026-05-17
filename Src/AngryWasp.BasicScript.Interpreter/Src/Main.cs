using System;
using System.IO;
using System.Linq;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Cli.Config;

namespace AngryWasp.BasicScript.App
{
    internal class MainClass
    {
        private static void Main(string[] rawArgs)
        {
            string scriptFile = null;
            //script file always needs to be present and always needs to be first
            if (rawArgs.Length == 0)
            {
                Console.WriteLine("No script file");
                return;
            }
            else
            {
                scriptFile = rawArgs[0];

                if (string.IsNullOrEmpty(scriptFile))
                {
                    Console.WriteLine("No script file");
                    return;
                }

                if (!File.Exists(scriptFile))
                {
                    Console.WriteLine($"Script '{scriptFile}' does not exist");
                    return;
                }

                var trimmedList = rawArgs.ToList();
                trimmedList.RemoveAt(0);
                rawArgs = trimmedList.ToArray();
            }

            var parsedArgs = Arguments.Parse(rawArgs);
            CommandLine cl = new CommandLine();
            string[,] extras = {
                {"env", "Set environment variables for use by the script."}
            };

            if (!ConfigMapper<CommandLine>.Process(parsedArgs, cl, extras))
                return;

            Application.RegisterCommands();

            Environment.SetEnvironmentVariable("BSI_SOURCE_DIR", Path.GetDirectoryName(Path.GetFullPath(scriptFile)));
            Environment.SetEnvironmentVariable("BSI_SOURCE", Path.GetFullPath(scriptFile));

            for (int i = 0; i < rawArgs.Length; i++)
                if (rawArgs[i].ToLower() == "--env")
                {
                    var var = rawArgs[++i].Split('=');
                    Environment.SetEnvironmentVariable(var[0], var[1]);
                }

            try
            {
                var interpreter = new Interpreter(File.ReadAllText(scriptFile), new ExecutionContext());
                interpreter.printHandler += Console.WriteLine;
                interpreter.inputHandler += Console.ReadLine;
                interpreter.Exec();
                if (!string.IsNullOrEmpty(cl.Entry))
                {
                    interpreter.Lexer.AppendSource("\r\n" + cl.Entry + "\r\n");
                    interpreter.ResetToken();
                    interpreter.Exec();
                }
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