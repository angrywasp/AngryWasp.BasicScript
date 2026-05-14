using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace AngryWasp.BasicScript
{
    public class Interpreter
    {
        public delegate void PrintFunction(string text);
        public delegate string InputFunction();

        public PrintFunction printHandler;
        public InputFunction inputHandler;

        private Lexer lex;
        private CallStack callStack;
        private Token prevToken;
        private Token lastToken;

        private Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> vars;
        private Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> consts;
        private Dictionary<string, Marker> labels;
        private Dictionary<string, (Marker Marker, bool Increment, Value V1, Value V2)> loops;
        private Dictionary<string, (bool IsLocal, Marker Marker, List<ScriptArgument> Arguments)> functions;
        
        private IExecutionContext executionContext;

        private Dictionary<string, Func<Interpreter, List<Value>, Value>> funcs;

        private int ifcounter;

        private Marker lineMarker;

        private bool exit;

        public CallStack CallStack => callStack;

        public Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> Variables => vars;

        public Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> Constants => consts;

        public Interpreter(string input, IExecutionContext executionContext)
        {
            this.executionContext = executionContext;
            this.lex = new Lexer(input);
            this.callStack = new CallStack(this, this.lex);
            this.vars = new Dictionary<string, (bool IsArrayDeclaration, Value[] Values)>();
            this.consts = new Dictionary<string, (bool IsArrayDeclaration, Value[] Values)>();
            this.labels = new Dictionary<string, Marker>();
            this.loops = new Dictionary<string, (Marker, bool, Value, Value)>();
            this.funcs = new Dictionary<string, Func<Interpreter, List<Value>, Value>>();
            this.functions = new Dictionary<string, (bool IsPublic, Marker Marker, List<ScriptArgument> Arguments)>();
            this.ifcounter = 0;
            Intrinsics.InstallAll(this);
            this.executionContext.Install(this);
        }

        public Value GetVar(string name, int index)
        {
            var func = callStack.CurrentFunction;

            if (func != null)
            {
                if (func.Arguments.ContainsKey(name))
                    return func.Arguments[name].Value;
                else if (func.Variables.ContainsKey(name))
                {
                    if (index >= func.Variables[name].Values.Length)
                        Error($"Index {index} is out of range on array {name}");

                    return func.Variables[name].Values[index];
                }
            }

            if (!vars.ContainsKey(name))
                Error($"Variable with name {name} does not exist");

            if (index >= vars[name].Values.Length)
                Error($"Index {index} is out of range on array {name}");

            return vars[name].Values[index];
        }

        public void SetConst(Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> constList, string name, Value[] val, bool isArray)
        {
            if (constList.ContainsKey(name))
                Error("Reassignment of constant value");

            constList.Add(name, (isArray, val));
        }

        public void SetConst(string name, Value[] val, bool isArray)
        {
            var func = callStack.CurrentFunction;

            if (func != null)
                SetConst(func.Constants, name, val, isArray);
            else
                SetConst(consts, name, val, isArray);
        }

        public void SetVar(Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> varList, string name, Value val, bool isArray, int index)
        {
            if (varList.ContainsKey(name))
            {
                if (varList[name].Values.Length <= index)
                {
                    var d = varList[name].IsArrayDeclaration;
                    var a = varList[name].Values;
                    Array.Resize(ref a, index + 1);
                    varList[name] = (d, a);
                }

                varList[name].Values[index] = val;
            }
            else
            {
                var valArray = new Value[index + 1];
                valArray[index] = val;
                varList.Add(name, (isArray, valArray));
            }
        }

        public void SetVar(bool declaration, string name, Value val, bool isArray, int index)
        {
            var func = callStack.CurrentFunction;

            if (func != null)
            {
                if (func.Constants.ContainsKey(name))
                    Debugger.Break();

                if (declaration)
                    SetVar(func.Variables, name, val, isArray, index);
                else
                {
                    if (func.Arguments.ContainsKey(name))
                        func.Arguments[name].Value = val;
                    else if (func.Variables.ContainsKey(name))
                        SetVar(func.Variables, name, val, isArray, index);
                    else
                    {
                        if (func.IsLocal)
                            Error("Local functions may not set global variables. Use a ref function parameter");

                        SetVar(vars, name, val, isArray, index);
                    }
                }
            }
            else
            {
                if (consts.ContainsKey(name))
                    Debugger.Break();

                SetVar(vars, name, val, isArray, index);
            }
        }

        public string GetLine() => lex.GetLine(lineMarker);

        public void AddFunction(string name, Func<Interpreter, List<Value>, Value> function)
        {
            if (!funcs.ContainsKey(name))
                funcs.Add(name, function);
            else Error($"Function {name} already defined");
        }

        void Error(string text) => throw new BasicException(text, lineMarker.Line, lineMarker.Column);

        void Warning(string text)
        {
            var lastColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{text}: Line {lineMarker.Line}");
            Console.ForegroundColor = lastColor;
        }

        void Match(Token tok)
        {
            if (lastToken != tok)
                Error($"Expected {tok}, got {lastToken}");
        }

        public void Exec()
        {
            try
            {
                exit = false;
                GetNextToken();
                while (!exit) Line();
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        Token GetNextToken()
        {
            prevToken = lastToken;
            lastToken = lex.GetToken();

            if (lastToken == Token.EOF && prevToken == Token.EOF)
                Error("Unexpected end of file");

            return lastToken;
        }

        void Line()
        {
            while (lastToken == Token.NewLine) GetNextToken();

            if (lastToken == Token.EOF)
            {
                exit = true;
                return;
            }

            lineMarker = lex.TokenMarker;
            Statment();

            if (lastToken != Token.NewLine && lastToken != Token.EOF)
                Error("Expected new line got " + lastToken.ToString());
        }

        void Statment()
        {
            Token keyword = lastToken;
            GetNextToken();
            switch (keyword)
            {
                case Token.Print: Print(); break;
                case Token.Include: Include(); break;
                case Token.Input: Input(); break;
                case Token.Goto: Goto(); break;
                case Token.If: If(); break;
                case Token.Else: Else(); break;
                case Token.EndIf: break;
                case Token.Loop: Loop(); break;
                case Token.Next: Next(); break;
                case Token.Var: Var(true); break;
                case Token.Const: Const(); break;
                case Token.Exit: Exit(); break;
                case Token.Assert: Assert(); break;
                case Token.Function: Function(); break;
                case Token.End: End(); break;
                case Token.Identifier:
                    if (lastToken == Token.Equals) Var(false);
                    else if (lastToken == Token.Colon) Label();
                    else if (functions.ContainsKey(lex.Identifier)) Call();
                    else if (funcs.ContainsKey(lex.Identifier)) CallIntrinsic();
                    else goto default;
                    break;
                case Token.EOF:
                    exit = true;
                    break;
                default:
                    Console.Write(lex.Source);
                    Error($"Expect keyword got {keyword}");
                    
                    break;
            }
        }

        void Print()
        {
            var printExpr = Expr().ToString();
            printHandler?.Invoke(printExpr);
        }

        void Include()
        {
            var incExpr = Expr().ToString();
            var includeFileContents = File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("BSI_SOURCE_DIR"), incExpr));
            lex.InsertSource(includeFileContents);
        }

        void Input()
        {
            while (true)
            {
                Match(Token.Identifier);

                if (!vars.ContainsKey(lex.Identifier)) vars.Add(lex.Identifier, (false, new Value[] { new Value() }));

                string input = inputHandler?.Invoke();

                if (input.ParseInt128(out Int128 i))
                    vars[lex.Identifier] = (false, new Value[] { new Value(i) });
                else
                    vars[lex.Identifier] = (false, new Value[] { new Value(input) });

                GetNextToken();
                if (lastToken != Token.Comma) break;
                GetNextToken();
            }
        }

        void Goto()
        {
            Match(Token.Identifier);
            string name = lex.Identifier;

            if (!labels.ContainsKey(name))
            {
                // if we didn't encaunter required label yet, start to search for it
                while (true)
                {
                    if (GetNextToken() == Token.Colon && prevToken == Token.Identifier)
                    {
                        if (!labels.ContainsKey(lex.Identifier))
                            labels.Add(lex.Identifier, lex.TokenMarker);
                        if (lex.Identifier == name)
                            break;
                    }
                    if (lastToken == Token.EOF)
                    {
                        Error("Cannot find label named " + name);
                    }
                }
            }
            lex.GoTo(labels[name]);
            lastToken = Token.NewLine;
        }

        void Function()
        {
            Match(Token.Identifier);
            string name = lex.Identifier;

            var funcVars = new Dictionary<string, ScriptArgument>();

            GetNextToken();
            Match(Token.LParen);

            while (true)
            {
                GetNextToken();
                if (lastToken == Token.RParen)
                    break;

                Value_Type expectedType = Value_Type.Undefined;
                switch (lex.Identifier.ToUpper())
                {
                    case "INT":
                    case "INTEGER":
                        expectedType = Value_Type.Integer;
                        break;
                    case "STR":
                    case "STRING":
                        expectedType = Value_Type.String;
                        break;
                    case "BYTE":
                    case "BYTES":
                        expectedType = Value_Type.Bytes;
                        break;
                }

                if (expectedType == Value_Type.Undefined)
                {
                    Error($"Invalid argument type {lex.Identifier}");
                    return;
                }

                GetNextToken();

                bool isRef = lastToken == Token.Ref;
                if (isRef) GetNextToken();

                Match(Token.Identifier);

                if (funcVars.ContainsKey(lex.Identifier))
                    Error($"Function {name} already contains an argument {lex.Identifier}");

                funcVars.Add(lex.Identifier, new ScriptArgument
                {
                    Name = lex.Identifier,
                    Type = expectedType,
                    Ref = isRef
                });
                GetNextToken();
                if (lastToken == Token.RParen)
                    break;
            }

            GetNextToken();

            bool isGlobal = lastToken == Token.Global;
            if (lastToken == Token.Local || lastToken == Token.Global)
                GetNextToken();

            Match(Token.NewLine);

            functions.Add(name, (!isGlobal, lex.TokenMarker, new List<ScriptArgument>(funcVars.Values)));

            while (true)
            {
                if (lastToken == Token.End)
                {
                    GetNextToken();
                    Match(Token.NewLine);
                    return;
                }
                GetNextToken();
            }
        }

        void End() => callStack.Pop();

        void CallIntrinsic()
        {
            string name = lex.Identifier;

            if (!funcs.ContainsKey(name))
                Error($"Intrinsic {name} does not exist");

            var argList = new List<Value>();

            Match(Token.LParen);
            GetNextToken();

            while (true)
            {
                switch (lastToken)
                {
                    case Token.NewLine: goto exit;
                    case Token.Comma:
                        GetNextToken();
                        continue;
                    case Token.RParen:
                        GetNextToken();
                        Match(Token.NewLine);
                        goto exit;
                    default:
                        argList.Add(Expr());
                        break;
                }
            }

        exit:

            funcs[name](this, argList);
        }

        private void Call()
        {
            var name = lex.Identifier;

            if (!functions.ContainsKey(name))
                Error($"Function {name} does not exist");

            var argList = new List<StackObjectArgument>();
            var argDict = new Dictionary<string, StackObjectArgument>();

            Match(Token.LParen);
            GetNextToken();

            int ReadArrayIndexer(string ident, Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> list)
            {
                int index = 0;
                if (list[ident].IsArrayDeclaration)
                {
                    GetNextToken();
                    while (lastToken == Token.NewLine)
                        GetNextToken();
                    Match(Token.LSParen);
                    GetNextToken();
                    while (lastToken == Token.NewLine)
                        GetNextToken();
                    var val = Expr();
                    if (val.Type != Value_Type.Integer)
                        Error($"Invalid indexer for array item {ident}");
                    Match(Token.RSParen);
                    index = (int)val.Integer;
                }

                return index;
            }

            while (true)
            {
                switch (lastToken)
                {
                    case Token.NewLine: goto exit;
                    case Token.Comma:
                        GetNextToken();
                        continue;
                    case Token.RParen:
                        GetNextToken();
                        Match(Token.NewLine);
                        goto exit;
                    case Token.Identifier:
                        {
                            var cf = callStack.CurrentFunction;
                            var ident = lex.Identifier;

                            if (cf != null)
                            {
                                if (functions[name].Arguments[argList.Count].Ref && cf.Constants.ContainsKey(ident))
                                    Warning("Using a constant as a ref argument will not modify the value of the constant. The script will have unexpected results");

                                if (cf.Arguments.ContainsKey(ident))
                                {
                                    argList.Add(new StackObjectArgument(ident, GetVar(ident, 0), false, 0));
                                    GetNextToken();
                                }
                                else if (cf.Constants.ContainsKey(ident))
                                {
                                    var index = ReadArrayIndexer(ident, cf.Constants);

                                    argList.Add(new StackObjectArgument(null, cf.Constants[ident].Values[index], cf.Constants[ident].IsArrayDeclaration, index));
                                    GetNextToken();
                                }
                                else if (cf.Variables.ContainsKey(ident))
                                {
                                    var index = ReadArrayIndexer(ident, cf.Variables);

                                    argList.Add(new StackObjectArgument(ident, GetVar(ident, index), cf.Variables[ident].IsArrayDeclaration, index));
                                    GetNextToken();
                                }
                                else if (consts.ContainsKey(ident))
                                {
                                    var index = ReadArrayIndexer(ident, consts);

                                    argList.Add(new StackObjectArgument(null, consts[ident].Values[index], consts[ident].IsArrayDeclaration, index));
                                    GetNextToken();
                                }
                                else if (vars.ContainsKey(ident))
                                {
                                    var index = ReadArrayIndexer(ident, vars);

                                    argList.Add(new StackObjectArgument(ident, GetVar(ident, index), vars[ident].IsArrayDeclaration, index));
                                    GetNextToken();
                                }
                                else
                                    argList.Add(new StackObjectArgument(null, Expr(), false, 0));
                            }
                            else
                            {
                                if (functions[name].Arguments[argList.Count].Ref && consts.ContainsKey(ident))
                                    Warning("Using a constant as a ref argument will not modify the value of the constant. The script will have unexpected results");
                                
                                if (consts.ContainsKey(ident))
                                {
                                    var index = ReadArrayIndexer(ident, consts);

                                    argList.Add(new StackObjectArgument(null, consts[ident].Values[index], consts[ident].IsArrayDeclaration, index));
                                    GetNextToken();
                                }
                                else if (vars.ContainsKey(ident))
                                {
                                    var index = ReadArrayIndexer(ident, vars);

                                    argList.Add(new StackObjectArgument(ident, GetVar(ident, index), vars[ident].IsArrayDeclaration, index));
                                    GetNextToken();
                                }
                                else
                                    argList.Add(new StackObjectArgument(null, Expr(), false, 0));
                            }
                        }
                        break;
                    case Token.Value:
                        argList.Add(new StackObjectArgument(null, Expr(), false, 0));
                        break;
                    case Token.LSParen:
                        Debugger.Break();
                        break;
                    default:
                        Error($"Unexpected token {lastToken}");
                        break;
                }
            }

        exit:

            var func = functions[name];

            if (func.Arguments.Count != argList.Count)
                Error($"Incorrect number of arguments to function {name}");

            for (int i = 0; i < argList.Count; i++)
            {
                var fa = func.Arguments[i];
                var a = argList[i];

                var argName = fa.Name;
                var argType = fa.Type;
                var isRef = fa.Ref;

                //when the function call exits, values are mapped back to variables in the higher order scope by their variable name
                //by wiping out names where ref is false, we are explicitly only modifying variables in higher order scopes if we request that action through use of the ref keyword
                //without this, every variable gets passed to the function by reference
                if (!isRef)
                    a.VarName = null;

                if (argType != a.Value.Type)
                    Error($"Expected argument type {argType}. Got {a.Value.Type}");

                argDict.Add(func.Arguments[i].Name, a);
            }

            callStack.Push(new StackObject
            {
                IsLocal = func.IsLocal,
                FunctionName = name,
                EntryMarker = func.Marker,
                ReturnMarker = lex.TokenMarker,
                Arguments = argDict
            });
        }

        void If()
        {
            // check if argument is equal to 0
            bool result = Expr().BinOp(new Value(0), Token.ExactEqual).Integer == 1;

            Match(Token.Then);
            GetNextToken();

            if (result)
            {
                // in case "if" evaulate to zero skip to matching else or endif
                int i = ifcounter;
                while (true)
                {
                    if (lastToken == Token.If)
                        i++;
                    else if (lastToken == Token.Else)
                    {
                        if (i == ifcounter)
                        {
                            GetNextToken();
                            return;
                        }
                    }
                    else if (lastToken == Token.EndIf)
                    {
                        if (i == ifcounter)
                        {
                            GetNextToken();
                            return;
                        }
                        i--;
                    }
                    GetNextToken();
                }
            }
        }

        void Else()
        {
            // skip to matching endif
            int i = ifcounter;
            while (true)
            {
                if (lastToken == Token.If)
                    i++;
                else if (lastToken == Token.EndIf)
                {
                    if (i == ifcounter)
                    {
                        GetNextToken();
                        return;
                    }
                    i--;
                }
                GetNextToken();
            }
        }

        void Label()
        {
            string name = lex.Identifier;
            if (!labels.ContainsKey(name)) labels.Add(name, lex.TokenMarker);

            GetNextToken();
            Match(Token.NewLine);
        }

        void Exit() => exit = true;

        void Const()
        {
            if (lastToken != Token.Equals)
            {
                Match(Token.Identifier);
                GetNextToken();
                Match(Token.Equals);
            }

            string id = lex.Identifier;

            var cf = callStack.CurrentFunction;

            if (cf != null)
            {
                if (cf.Variables.ContainsKey(id))
                    Error($"Redefined variable {id} in function {cf.FunctionName}");

                if (cf.Constants.ContainsKey(id))
                    Error($"Redefined constant {id} in function {cf.FunctionName}");
            }
            else
            {
                if (vars.ContainsKey(id))
                    Error($"Redefined variable {id} in global scope");

                if (consts.ContainsKey(id))
                    Error($"Redefined constant {id} in global scope");
            }

            GetNextToken();
            if (lastToken == Token.LSParen)
            {
                GetNextToken();
                List<Value> values = new List<Value>();
                while (true)
                {
                    values.Add(Expr());

                    if (lastToken == Token.NewLine)
                        Error("Expected ], got NewLine");

                    if (lastToken == Token.RSParen)
                    {
                        GetNextToken();
                        break;
                    }

                    GetNextToken();
                }

                SetConst(id, values.ToArray(), true);
            }
            else
                SetConst(id, new Value[] { Expr() }, false);
        }

        void Var(bool declaration)
        {
            if (lastToken != Token.Equals)
            {
                Match(Token.Identifier);
                GetNextToken();
                Match(Token.Equals);
            }

            string id = lex.Identifier;

            var cf = callStack.CurrentFunction;

            if (declaration)
            {
                if (cf != null)
                {
                    if (cf.Arguments.ContainsKey(id))
                        Error($"Variable {id} redefines argument with same name in function {cf.FunctionName}");
                    else if (cf.Variables.ContainsKey(id))
                        Error($"Redefined variable {id} in function {cf.FunctionName}");
                    else if (cf.Constants.ContainsKey(id))
                        Error($"Redefined constant {id} in function {cf.FunctionName}");
                }
                else
                {
                    if (vars.ContainsKey(id))
                        Error($"Redefined variable {id} in global scope");
                    else if (consts.ContainsKey(id))
                        Error($"Redefined constant {id} in global scope");
                }
            }
            else
            {
                if (cf != null)
                {
                    if (!cf.Arguments.ContainsKey(id) && !cf.Variables.ContainsKey(id) && !vars.ContainsKey(id))
                        Error($"Variable {id} is not defined");
                }
                else
                {
                    if (!vars.ContainsKey(id))
                        Error($"Variable {id} is not defined");
                }
            }

            GetNextToken();
            if (lastToken == Token.LSParen)
            {
                GetNextToken();
                int offset = 0;
                while (true)
                {
                    while (lastToken == Token.NewLine)
                        GetNextToken();
                        
                    var val = Expr();

                    SetVar(declaration, id, val, true, offset++);

                    while (lastToken == Token.NewLine)
                        GetNextToken();

                    if (lastToken == Token.NewLine)
                        Error("Expected ], got NewLine");

                    if (lastToken == Token.RSParen)
                    {
                        GetNextToken();
                        break;
                    }

                    GetNextToken();
                }
            }
            else
                SetVar(declaration, id, Expr(), false, 0);
        }

        void Loop()
        {
            Match(Token.Identifier);
            string var = lex.Identifier;

            GetNextToken();
            Match(Token.Equals);

            GetNextToken();
            Value v1 = Expr();
            Match(Token.To);

            GetNextToken();
            Value v2 = Expr();

            // save for loop marker
            if (loops.ContainsKey(var))
                loops[var] = (lineMarker, loops[var].Increment, v1, v2);
            else
            {
                SetVar(true, var, v1, false, 0);
                loops.Add(var, (lineMarker, v1.BinOp(v2, Token.Less).Integer == 1 ? true : false, v1, v2));
            }

            if (GetVar(var, 0).BinOp(v2, loops[var].Increment ? Token.More : Token.Less).Integer == 1)
            {
                while (true)
                {
                    while (!(GetNextToken() == Token.Identifier && prevToken == Token.Next)) ;
                    if (lex.Identifier == var)
                    {
                        loops.Remove(var);
                        while (lastToken != Token.NewLine)
                            GetNextToken();
                        Match(Token.NewLine);
                        break;
                    }
                }
            }
        }

        void Next()
        {
            Match(Token.Identifier);
            string var = lex.Identifier;
            var nextToken = lex.PeekToken();

            if (nextToken == Token.NewLine)
            {
                var newValue = GetVar(var, 0).BinOp(new Value(1), loops[var].Increment ? Token.Plus : Token.Minus);
                SetVar(false, var, newValue, false, 0);
                lex.GoTo(new Marker(loops[var].Marker.Pointer - 1, loops[var].Marker.Line, loops[var].Marker.Column - 1));
                //hack: set to NewLine to get past an error elsewhere as this shorthand is not a fully evaluated expression
                lastToken = Token.NewLine;
            }
            else
            {
                GetNextToken();
                if (lastToken != nextToken)
                    Error("Unexpected system error. peeked token != fetched token");

                if (loops[var].Increment && lastToken != Token.Plus)
                    Error($"Expected expression to increment loop counter, got {lastToken}");

                if (!loops[var].Increment && lastToken != Token.Minus)
                    Error($"Expected expression to decrement loop counter, got {lastToken}");

                var tok = lastToken;
                var expr = Expr();
                var newValue = GetVar(var, 0).BinOp(expr, tok);
                SetVar(false, var, newValue, false, 0);
                lex.GoTo(new Marker(loops[var].Marker.Pointer - 1, loops[var].Marker.Line, loops[var].Marker.Column - 1));
                lastToken = Token.NewLine;
            }
        }

        void Assert()
        {
            var expr = Expr();
            bool result = expr.BinOp(new Value(0), Token.ExactEqual).Integer == 1;

            if (result)
                Error("Assertion fault");
        }

        Value Expr(int min = 0)
        {
            Dictionary<Token, int> precedens = new Dictionary<Token, int>()
            {
                { Token.Or, 0 }, { Token.And, 0 },
                { Token.ExactEqual, 1 }, { Token.NotEqual, 1 },
                { Token.Less, 1 }, { Token.More, 1 },
                { Token.LessEqual, 1 },  { Token.MoreEqual, 1 },
                { Token.Plus, 2 }, { Token.Minus, 2 },
                { Token.Asterisk, 3 }, {Token.Slash, 3 }, { Token.DoubleAsterisk, 3 },
                { Token.Caret, 4 }, { Token.ShiftLeft, 4 }, { Token.ShiftRight, 4 }
            };

            var lhs = Primary();

            while (true)
            {
                if (lastToken < Token.Plus || lastToken > Token.And || precedens[lastToken] < min)
                    break;

                Token op = lastToken;
                int prec = precedens[lastToken]; // Operator Precedence
                int assoc = 0; // 0 left, 1 right; Operator associativity
                int nextmin = assoc == 0 ? prec : prec + 1;
                GetNextToken();
                Value rhs = Expr(nextmin);
                lhs = lhs.BinOp(rhs, op);
            }

            return lhs;
        }

        public Value Primary(Dictionary<string, (bool IsArrayDeclaration, Value[] Values)> list, string name)
        {
            Value prim = default;

            bool GetArrayIndex(out int index)
            {
                GetNextToken();
                if (lastToken != Token.LSParen)
                {
                    index = -1;
                    return false;
                }

                Match(Token.LSParen);
                GetNextToken();
                var e = Expr();
                Match(Token.RSParen);
                index = (int)e.Integer;
                return true;
            };

            var ident = lex.Identifier;
            if (list[ident].IsArrayDeclaration)
            {
                if (!GetArrayIndex(out var index))
                    Debugger.Break();
                else
                    prim = list[ident].Values[index];
            }
            else
                prim = list[ident].Values[0];

            return prim;
        }

        Value Primary()
        {
            Value prim = default;

            if (lastToken == Token.Value)
            {
                // number | string
                prim = lex.Value;
                GetNextToken();
            }
            else if (lastToken == Token.Identifier)
            {
                // ident | ident '(' args ')'
                var func = callStack.CurrentFunction;

                if (func != null)
                {
                    if (func.Arguments.ContainsKey(lex.Identifier))
                        prim = func.Arguments[lex.Identifier].Value;
                    else if (func.Constants.ContainsKey(lex.Identifier))
                        prim = Primary(func.Constants, lex.Identifier);
                    else if (func.Variables.ContainsKey(lex.Identifier))
                        prim = Primary(func.Variables, lex.Identifier);
                    else if (consts.ContainsKey(lex.Identifier))
                        prim = Primary(consts, lex.Identifier);
                    else if (vars.ContainsKey(lex.Identifier))
                    {
                        if (func.IsLocal)
                            Error("Local functions may not read variables outside the function scope.\nPass variable as function argument");
                        else
                            prim = Primary(vars, lex.Identifier);
                    }
                    else if (funcs.ContainsKey(lex.Identifier))
                    {
                        string name = lex.Identifier;
                        List<Value> args = new List<Value>();
                        GetNextToken();
                        Match(Token.LParen);

                    start:
                        if (GetNextToken() != Token.RParen)
                        {
                            args.Add(Expr());
                            if (lastToken == Token.Comma)
                                goto start;
                        }

                        prim = funcs[name](this, args);
                    }
                    else
                    {
                        Error("Undeclared variable " + lex.Identifier);
                    }

                }
                else if (consts.ContainsKey(lex.Identifier))
                    prim = Primary(consts, lex.Identifier);
                else if (vars.ContainsKey(lex.Identifier))
                    prim = Primary(vars, lex.Identifier);
                else if (funcs.ContainsKey(lex.Identifier))
                {
                    string name = lex.Identifier;
                    List<Value> args = new List<Value>();
                    GetNextToken();
                    Match(Token.LParen);

                start:
                    if (GetNextToken() != Token.RParen)
                    {
                        args.Add(Expr());
                        if (lastToken == Token.Comma)
                            goto start;
                    }

                    prim = funcs[name](this, args);
                }
                else
                {
                    Error("Undeclared variable " + lex.Identifier);
                }
                GetNextToken();
            }
            else if (lastToken == Token.LParen)
            {
                // '(' expr ')'
                GetNextToken();

                while(lastToken == Token.NewLine)
                    GetNextToken();

                prim = Expr();

                while(lastToken == Token.NewLine)
                    GetNextToken();

                Match(Token.RParen);
                GetNextToken();
            }
            else if (lastToken == Token.Plus || lastToken == Token.Minus || lastToken == Token.Not)
            {
                // unary operator
                // '-' | '+' primary
                Token op = lastToken;
                GetNextToken();
                prim = Primary().UnaryOp(op);
            }
            else if (lastToken == Token.NewLine)
                GetNextToken();
            else
            {
                Error($"Unexpected token in primary! {lastToken}");
            }

            return prim;
        }
    }
}
