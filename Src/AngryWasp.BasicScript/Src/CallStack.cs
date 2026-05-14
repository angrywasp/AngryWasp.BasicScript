using System.Collections.Generic;
using System.Linq;

namespace AngryWasp.BasicScript
{
    public class StackObjectArgument
    {
        public string VarName { get; set; }
        public Value Value { get; set; }
        public bool IsArray { get; set; }
        public int Index { get; set; }

        public StackObjectArgument(string varName, Value value, bool isArray, int index)
        {
            this.VarName = varName;
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
        public Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> Variables { get; set; } = new Dictionary<string, (bool IsArrayDeclaration, Value[] Values)>();
        public Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> Constants { get; set; } = new Dictionary<string, (bool IsArrayDeclaration, Value[] Values)>();
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
                if (a.VarName == null)
                    continue;

                interpreter.SetVar(false, a.VarName, a.Value, a.IsArray, a.Index);
            }
            lexer.Jump(obj.ReturnMarker);
        }
    }
}