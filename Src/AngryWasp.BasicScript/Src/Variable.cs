using System.Collections.Generic;

namespace AngryWasp.BasicScript
{
    public class Variable
    {
        private string name;
        private string loopId;
        private bool isConstant;

        public string Name => name;
        public string LoopID => loopId;
        public bool IsConstant => isConstant;

        public bool IsArray { get; set; }
        public Value[] Values { get; set; }

        public Variable(string name, bool isArray, Value[] values, bool isConstant = false, string loopId = null)
        {
            this.name = name;
            this.isConstant = isConstant;
            this.loopId = loopId;

            IsArray = isArray;
            Values = values;
        }

        public override string ToString() => Name;
    }
}