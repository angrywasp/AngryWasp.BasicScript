using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AngryWasp.BasicScript
{
    public class StackObjectArgument
    {
        public string Name { get; set; }
        public Value Value { get; set; }
        public bool IsArray { get; set; }
        public int Index { get; set; }

        public StackObjectArgument(string name, Value value, bool isArray, int index)
        {
            this.Name = name;
            this.Value = value;
            this.IsArray = isArray;
            this.Index = index;
        }
    }

    public class StackObject
    {
        public bool IsLocal { get; set; }
        public string FunctionName { get; set; }
        public Marker EntryMarker { get; set; }
        public Marker ReturnMarker { get; set; }
        public Dictionary<string, StackObjectArgument> Arguments { get; set; } = new Dictionary<string, StackObjectArgument>();
        public Dictionary<string, Variable> Variables { get; set; } = new Dictionary<string, Variable>();
    }

    public class CallStack
    {
        private Interpreter interpreter;
        private Lexer lexer;
        private List<StackObject> stackObjects = new List<StackObject>();

        public StackObject CurrentFunction => stackObjects.Count == 0 ? null : stackObjects.Last();

        public CallStack(Interpreter interpreter, Lexer lexer)
        {
            this.interpreter = interpreter;
            this.lexer = lexer;
        }

        public void Push(StackObject obj)
        {
            stackObjects.Add(obj);
            lexer.Jump(obj.EntryMarker);
        }

        public void Pop()
        {
            if (stackObjects.Count == 0)
                throw new System.Exception("Attempt to pop empty call stack");

            var obj = stackObjects.Last();
            stackObjects.RemoveAt(stackObjects.Count - 1);
            
            foreach (var a in obj.Arguments.Values)
            {
                if (a.Name == null)
                    continue;

                if (a.Index != 0)
                    Debugger.Break();

                if (a.IsArray)
                    Debugger.Break();

                interpreter.SetVar(false, new Variable(a.Name, a.IsArray, [ a.Value ]));
            }
            lexer.Jump(obj.ReturnMarker);
        }
    }
}