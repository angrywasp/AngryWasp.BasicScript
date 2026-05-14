using System;
using System.Diagnostics;

namespace AngryWasp.BasicScript.App
{
    public class ExternalTool
    {
        public static int Run(string command, string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                Arguments = arguments,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false,
                FileName = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };

            using (var process = new Process())
            {
                process.StartInfo = processInfo;
                process.EnableRaisingEvents = true;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.OutputDataReceived += (sender, args) => {
                    Console.WriteLine(args.Data);
                };
                
                process.ErrorDataReceived += (sender, args) => {
                    Console.WriteLine(args.Data);
                };

                process.StandardInput.Close();

                process.WaitForExit();

                return process.ExitCode;
            }
        }
    }
}
