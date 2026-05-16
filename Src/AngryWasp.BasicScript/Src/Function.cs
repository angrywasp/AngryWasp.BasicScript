using System.Collections.Generic;

namespace AngryWasp.BasicScript
{
    public class Function
    {
        public string Name { get; set; }
        public bool IsLocal { get; set; }
        public Marker Marker { get; set; }
        public ScriptArgument[] Arguments { get; set; }

        public Function(string name, bool isLocal, Marker marker, ScriptArgument[] arguments)
        {
            Name = name;
            IsLocal = isLocal;
            Marker = marker;
            Arguments = arguments;
        }

        public override string ToString() => Name;
    }
}