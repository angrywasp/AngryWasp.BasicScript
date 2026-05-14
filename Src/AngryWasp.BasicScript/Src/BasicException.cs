using System;

namespace AngryWasp.BasicScript
{
    public class BasicException : Exception
    {
        public int line;
        public int column;

        public BasicException(string message, int line, int column)
            : base(message)
        {
            this.line = line;
            this.column = column;
        }
    }
}
