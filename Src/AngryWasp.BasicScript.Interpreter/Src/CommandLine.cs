using System;
using AngryWasp.Cli.Config;
using AngryWasp.Serializer;

namespace AngryWasp.BasicScript.App
{
    public class CommandLine
    {
        [CommandLineArgument("entry", "Entry point to start the script")]
        public string Entry { get; set; } = null;
    }
}