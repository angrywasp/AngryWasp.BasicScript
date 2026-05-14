namespace AngryWasp.BasicScript
{
    public enum Token
    {
        Unknown,

        Identifier,
        Value,

        Include,

        Print,
        If,
        EndIf,
        Then,
        Else,
        Loop,
        To,
        Next,
        Goto,
        Input,
        Var,
        Const,
        Rem,
        End,
        Exit,
        Assert,

        Function,
        Local,
        Global,
        Ref,

        NewLine,
        Colon,
        Comma,

        Plus,
        Minus,
        Slash,
        Asterisk,
        DoubleAsterisk,
        ShiftLeft,
        ShiftRight,
        Caret,
        Equals,
        Less,
        More,
        ExactEqual,
        NotEqual,
        LessEqual,
        MoreEqual,
        Or,
        And,
        Not,

        LParen,
        RParen,
        LSParen,
        RSParen,

        EOF = -1
    }
}
