using System.Collections.Generic;

namespace AngryWasp.BasicScript
{
    public class Variable
    {
        public string Name { get; set; }
        public bool IsArray { get; set; }
        public Value[] Values { get; set; }
        public string LoopID { get; set; }
        public bool IsConstant { get; set; }

        public Variable(string name, bool isArray, Value[] values, bool isConstant = false, string loopId = null)
        {
            Name = name;
            IsArray = isArray;
            Values = values;
            IsConstant = isConstant;
            LoopID = loopId;
        }

        public override string ToString() => Name;
    }
}