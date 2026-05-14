using System;
using System.Globalization;
using System.Numerics;
using AngryWasp.Math;

namespace AngryWasp.BasicScript
{
    public static class Arithmetic
    {
        public static Int128 Add(this Int128 a, Int128 b) => checked(a + b);

        public static Int128 Subtract(this Int128 a, Int128 b) => checked(a - b);

        public static Int128 Multiply(this Int128 a, Int128 b) => checked(a * b);

        public static Int128 Divide(this Int128 a, Int128 b) => checked(a / b);

        public static Int128 Pow(this Int128 a, Int128 b) {
            checked {
                Int128 result = 1;
                while (b > 0)
                {
                    if ((b & 1) == 1)
                    {
                        result = result * a;
                    }

                    a = a * a;
                    b >>= 1;
                }

                return result;
                
            }
        }

        public static Int128 ShiftLeft(this Int128 a, Int128 b) {
            if (b > 128) throw new ArithmeticException("Bitwise shift amount overflow");
            var amt = (int)b;
            return checked(a << amt);
        }

        public static Int128 ShiftRight(this Int128 a, Int128 b) {
            if (b > 128) throw new ArithmeticException("Bitwise shift amount overflow");
            var amt = (int)b;
            return checked(a >> amt);
        }

        public static Int128 Xor(this Int128 a, Int128 b) => checked(a ^ b);

        public static bool ParseInt128(this string input, out Int128 output)
        {
            checked {
                return Int128.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out output);
            }
        }

        public static BigInteger ToBigInteger(this Int128 input) => BigInteger.CreateChecked(input);
        public static BigDecimal ToBigDecimal(this Int128 input) => BigDecimal.Create(BigInteger.CreateChecked(input), 18);
    }
}