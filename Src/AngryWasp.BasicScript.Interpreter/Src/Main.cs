using System;
using System.IO;

namespace AngryWasp.BasicScript.App
{
    internal class MainClass
    {
        private static void Main(string[] rawArgs)
        {
            if (rawArgs.Length == 0)
            {
                Console.WriteLine("No arguments");
                return;
            }

            if (!File.Exists(rawArgs[0]))
            {
                Console.WriteLine($"File '{rawArgs[0]}' does not exist");
                return;
            }

            Environment.SetEnvironmentVariable("BSI_SOURCE_DIR", Path.GetDirectoryName(Path.GetFullPath(rawArgs[0])));
            Environment.SetEnvironmentVariable("BSI_SOURCE", Path.GetFullPath(rawArgs[0]));

            if (rawArgs.Length > 1)
            {
                for (int i = 1; i < rawArgs.Length; i++)
                    Environment.SetEnvironmentVariable($"${i}", rawArgs[i]);
            }

            try
            {
                var interpreter = new Interpreter(File.ReadAllText(rawArgs[0]), new ExecutionContext());
                interpreter.printHandler += Console.WriteLine;
                interpreter.inputHandler += Console.ReadLine;
                interpreter.Exec();
            }
            catch (BasicException ex)
            {
                Console.WriteLine($"{ex.Message}: Line {ex.line}, Col {ex.column}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}