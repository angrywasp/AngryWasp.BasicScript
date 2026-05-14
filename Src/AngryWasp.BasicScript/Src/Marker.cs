namespace AngryWasp.BasicScript
{
    public struct Marker
    {
        public int Pointer { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public Marker(int pointer, int line, int column)
        {
            Pointer = pointer;
            Line = line;
            Column = Column;
        }

        public override string ToString() => $"Marker: Pointer {Pointer}, Line {Line}, Column {Column}";
    }
}
