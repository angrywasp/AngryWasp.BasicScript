using System;
using AngryWasp.Cli.Config;
using AngryWasp.Serializer;

namespace AngryWasp.BasicScript.App
{
    public class CommandLine
    {
        [CommandLineArgument("run", "Run a line of code against the script")]
        public string Run { get; set; } = null;
    }
}