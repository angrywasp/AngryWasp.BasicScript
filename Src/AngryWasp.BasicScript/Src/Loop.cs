using System;
using System.Collections.Generic;
using AngryWasp.BasicScript;

namespace AngryWasp.BasicScript
{
    public class Loop
    {
        public string Name { get; set; }
        public Marker Marker { get; set; }
        public bool Increment { get; set; }
        public Value Start { get; set; }
        public Value End { get; set; }

        public Loop(string name, Marker marker, Value start, Value end)
        {
            Name = name;
            Marker = marker;
            Start = start;
            End = end;
        }

        public override string ToString() => Name;
    }
}
