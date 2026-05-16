using System.Collections.Generic;

namespace AngryWasp.BasicScript
{
    public class Variable
    {
        public string Name { get; set; }
        public bool IsArray { get; set; } = false;
        public Value[] Values { get; set; } = [];

        public Variable(string name, bool isArray, Value[] values)
        {
            Name = name;
            IsArray = isArray;
            Values = values;
        }

        public override string ToString() => Name;
    }
}