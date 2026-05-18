using System;
using System.Diagnostics;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;

namespace AngryWasp.BasicScript.App
{
    public class ExternalTool
    {
        public static int Run(string command, string arguments, string envVar = null)
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

                var output = string.Empty;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.OutputDataReceived += (sender, args) => {
                    if (string.IsNullOrEmpty(args.Data))
                        return;

                    if (args.Data.Length == 0)
                        return;

                    Console.WriteLine(args.Data);
                };
                
                if (envVar == null)
                {
                    process.ErrorDataReceived += (sender, args) => {
                        if (string.IsNullOrEmpty(args.Data))
                            return;

                        if (args.Data.Length == 0)
                            return;

                        Console.WriteLine(args.Data);
                    };
                }
                else
                {
                    process.ErrorDataReceived += (sender, args) => {
                        if (string.IsNullOrEmpty(args.Data))
                            return;

                        if (args.Data.Length == 0)
                            return;

                        output += args.Data;
                    };
                }

                process.StandardInput.Close();

                process.WaitForExit();

                if (envVar != null)
                    Environment.SetEnvironmentVariable(envVar, output);

                return process.ExitCode;
            }
        }
    }
}
