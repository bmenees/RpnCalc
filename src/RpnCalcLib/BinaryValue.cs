﻿#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Menees.RpnCalc.Internal;

#endregion

namespace Menees.RpnCalc
{
	public sealed class BinaryValue : NumericValue, IComparable<BinaryValue>
	{
		#region Constructors

		// This overload is provided for CLS compliance.
		public BinaryValue(long value)
		{
			m_value = unchecked((ulong)value);
		}

		[CLSCompliant(false)]
		public BinaryValue(ulong value)
		{
			m_value = value;
		}

		#endregion

		#region Public Properties

		public override ValueType ValueType
		{
			get
			{
				return ValueType.Binary;
			}
		}

		#endregion

		#region Public Methods

		public override string ToString()
		{
			return GetDecimalFormat(m_value);
		}

		public override string ToString(Calculator calc)
		{
			string result;

			ulong maskedValue = GetMaskedWordSizeValue(calc);

			switch (calc.BinaryFormat)
			{
				case BinaryFormat.Binary:
					result = GetBinaryFormat(maskedValue);
					break;
				case BinaryFormat.Octal:
					result = GetOctalFormat(maskedValue);
					break;
				case BinaryFormat.Hexadecimal:
					result = GetHexadecimalFormat(maskedValue);
					break;
				default:
					result = GetDecimalFormat(maskedValue);
					break;
			}

			return result;
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new List<DisplayFormat>(4);

			ulong maskedValue = GetMaskedWordSizeValue(calc);

			result.Add(new DisplayFormat(Resources.DisplayFormat_Binary, GetBinaryFormat(maskedValue)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Octal, GetOctalFormat(maskedValue)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Decimal, GetDecimalFormat(maskedValue)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Hexadecimal, GetHexadecimalFormat(maskedValue)));

			return result;
		}

		public static bool TryParse(string text, out BinaryValue value)
		{
			return TryParse(text, null, out value);
		}

		public static bool TryParse(string text, Calculator calc, out BinaryValue value)
		{
			bool result = false;
			value = null;

			if (!Utility.IsNullOrWhiteSpace(text))
			{
				text = text.Trim();
				if (text.Length > 0 && text[0] == Prefix)
				{
					// Format: # digits [b|o|d|h| ]
					text = text.Substring(1).TrimStart();
					result = TryParseSuffixedText(text, calc, out value);
				}
				else if (text.Length > 2 && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				{
					// Format: 0xHexDigits
					text = text.Substring(2);
					ulong hexValue;
					if (ulong.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out hexValue))
					{
						value = new BinaryValue(hexValue);
						result = true;
					}
				}
				else
				{
					// See if it's just decimal digits.
					ulong decimalValue;
					if (ulong.TryParse(text, out decimalValue))
					{
						value = new BinaryValue(decimalValue);
						result = true;
					}
				}
			}

			return result;
		}

		public override double ToDouble()
		{
			return (double)m_value;
		}

		public override BigInteger ToInteger()
		{
			return (BigInteger)m_value;
		}

		public static BinaryValue And(BinaryValue x, BinaryValue y)
		{
			return new BinaryValue(x.m_value & y.m_value);
		}

		public static BinaryValue Not(BinaryValue x, Calculator calc)
		{
			return new BinaryValue(GetMaskedWordSizeValue(calc, ~x.m_value));
		}

		public static BinaryValue Or(BinaryValue x, BinaryValue y)
		{
			return new BinaryValue(x.m_value | y.m_value);
		}

		public static BinaryValue ShiftLeft(BinaryValue x, int numBits, Calculator calc)
		{
			ulong value = GetMaskedWordSizeValue(calc, x.m_value << numBits);
			return new BinaryValue(value);
		}

		public static BinaryValue ShiftRight(BinaryValue x, int numBits, Calculator calc)
		{
			ulong value = GetMaskedWordSizeValue(calc, x.m_value >> numBits);
			return new BinaryValue(value);
		}

		public static BinaryValue RotateLeft(BinaryValue x, int numBits, Calculator calc)
		{
			ulong mostSignificantBits = x.m_value >> (calc.BinaryWordSize - numBits);
			ulong leastSignificantBits = x.m_value << numBits;
			ulong result = GetMaskedWordSizeValue(calc, unchecked(leastSignificantBits + mostSignificantBits));
			return new BinaryValue(result);
		}

		public static BinaryValue RotateRight(BinaryValue x, int numBits, Calculator calc)
		{
			ulong leastSignificantBits = x.m_value << (calc.BinaryWordSize - numBits);
			ulong mostSignificantBits = x.m_value >> numBits;
			ulong result = GetMaskedWordSizeValue(calc, unchecked(leastSignificantBits + mostSignificantBits));
			return new BinaryValue(result);
		}

		public static BinaryValue Xor(BinaryValue x, BinaryValue y)
		{
			return new BinaryValue(x.m_value ^ y.m_value);
		}

		public static BinaryValue Add(BinaryValue x, BinaryValue y, Calculator calc)
		{
			return new BinaryValue(GetMaskedWordSizeValue(calc, unchecked(x.m_value + y.m_value)));
		}

		public static BinaryValue Subtract(BinaryValue x, BinaryValue y, Calculator calc)
		{
			// Add the negation of y, so this is correct relative to the current word size.
			BinaryValue negativeY = Negate(y, calc);
			return Add(x, negativeY, calc);
		}

		public static BinaryValue Multiply(BinaryValue x, BinaryValue y, Calculator calc)
		{
			return new BinaryValue(GetMaskedWordSizeValue(calc, unchecked(x.m_value * y.m_value)));
		}

		public static BinaryValue Divide(BinaryValue x, BinaryValue y, Calculator calc)
		{
			return new BinaryValue(GetMaskedWordSizeValue(calc, unchecked(x.m_value / y.m_value)));
		}

		public static BinaryValue Negate(BinaryValue x, Calculator calc)
		{
			// Do two's complement with the current word size
			// (flip all the bits (one's complement), then add 1).
			BinaryValue onesComplement = Not(x, calc);
			return new BinaryValue(GetMaskedWordSizeValue(calc, unchecked(onesComplement.m_value + 1)));
		}

		public static NumericValue Power(BinaryValue x, BinaryValue exponent)
		{
			NumericValue result = IntegerValue.Power(new IntegerValue(x.m_value), new IntegerValue(exponent.m_value));

			// If we got an integer result in ulong's range, then convert it to binary.
			IntegerValue intResult = result as IntegerValue;
			if (intResult != null && intResult.AsInteger >= ulong.MinValue && intResult.AsInteger <= ulong.MaxValue)
			{
				result = new BinaryValue((ulong)intResult.AsInteger);
			}

			return result;
		}

		public static FractionValue Invert(BinaryValue x)
		{
			return new FractionValue(BigInteger.One, x.m_value);
		}

		public static BinaryValue Modulus(BinaryValue x, BinaryValue y, Calculator calc)
		{
			return new BinaryValue(GetMaskedWordSizeValue(calc, unchecked(x.m_value % y.m_value)));
		}

		public int Sign(Calculator calc)
		{
			int result = 0;
			if (m_value != 0)
			{
				// Negate does two's complement, so we'll say we're negative if the
				// most significant bit is set.  However, this is inconsistent with the
				// way ToInteger(), Invert(), Power(), and implicit type conversion
				// works.  They always treat binary values as non-negative.
				//
				// The HP48 has pretty much the same inconsistency.  It will negate
				// a binary value using two's complement, but it's other commands
				// always treat a binary value as non-negative.  And it doesn't
				// support the SIGN command for binary values.
				bool msbSet = (m_value & unchecked((ulong)(1L << (calc.BinaryWordSize - 1)))) != 0;
				result = msbSet ? -1 : 1;
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			BinaryValue value = obj as BinaryValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return m_value.GetHashCode();
		}

		public static int Compare(BinaryValue x, BinaryValue y)
		{
			int result;
			if (!CompareWithNulls(x, y, out result))
			{
				result = x.m_value.CompareTo(y.m_value);
			}
			return result;
		}

		public int CompareTo(BinaryValue other)
		{
			return Compare(this, other);
		}

		#endregion

		#region Public Operators

		public static bool operator ==(BinaryValue x, BinaryValue y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(BinaryValue x, BinaryValue y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(BinaryValue x, BinaryValue y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(BinaryValue x, BinaryValue y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(BinaryValue x, BinaryValue y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(BinaryValue x, BinaryValue y)
		{
			return Compare(x, y) >= 0;
		}

		#endregion

		#region Internal Constants

		internal const char Prefix = '#';
		internal const char BinarySuffix = 'b';
		internal const char OctalSuffix = 'o';
		internal const char DecimalSuffix = 'd';
		internal const char HexadecimalSuffix = 'h';

		#endregion

		#region Private Methods

		private ulong GetMaskedWordSizeValue(Calculator calc)
		{
			return GetMaskedWordSizeValue(calc, m_value);
		}

		private static ulong GetMaskedWordSizeValue(Calculator calc, ulong value)
		{
			int wordSize = calc.BinaryWordSize;
			int shiftSize = c_maxWordSize - wordSize;

			ulong result = ((value << shiftSize) >> shiftSize);
			return result;
		}

		private static string GetBinaryFormat(ulong value)
		{
			string result = GetFormat(value, BinaryFormat.Binary, BinarySuffix);
			return result;
		}

		private static string GetOctalFormat(ulong value)
		{
			string result = GetFormat(value, BinaryFormat.Octal, OctalSuffix);
			return result;
		}

		private static string GetDecimalFormat(ulong value)
		{
			string result = GetFormat(value, BinaryFormat.Decimal, DecimalSuffix);
			return result;
		}

		private static string GetHexadecimalFormat(ulong value)
		{
			string result = GetFormat(value, BinaryFormat.Hexadecimal, HexadecimalSuffix);
			return result;
		}

		private static string GetFormat(ulong value, BinaryFormat toBase, char suffix)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Prefix).Append(' ');

			string digits;
			if (toBase == BinaryFormat.Decimal)
			{
				// If the base is decimal, then continue to treat the value as unsigned,
				// so we don't have a negative sign show up if the high bit is set.
				digits = value.ToString(CultureInfo.CurrentCulture);
			}
			else
			{
				digits = Convert.ToString(unchecked((long)value), (int)toBase).ToUpper(CultureInfo.CurrentCulture);
			}
			sb.Append(digits);

			sb.Append(suffix);
			return sb.ToString();
		}

		private static bool TryParseSuffixedText(string text, Calculator calc, out BinaryValue value)
		{
			bool result = false;
			value = null;

			if (text.Length > 0)
			{
				// On the HP48, suffix characters are case-sensitive,
				// and they have precedence over digit characters.
				// So in Hex mode, #123d will parse as "decimal 123",
				// but "#123D" will parse as "hex 123D".  Similarly,
				// in Hex mode, #ABCD will parse as "hex ABCD", but
				// #abcd will fail to parse because "decimal abc" is
				// invalid.
				BinaryFormat format = calc != null ? calc.BinaryFormat : BinaryFormat.Decimal;
				int length = text.Length;
				char lastChar = text[length - 1];
				switch (lastChar)
				{
					case BinarySuffix:
						format = BinaryFormat.Binary;
						length--;
						break;
					case OctalSuffix:
						format = BinaryFormat.Octal;
						length--;
						break;
					case DecimalSuffix:
						format = BinaryFormat.Decimal;
						length--;
						break;
					case HexadecimalSuffix:
						format = BinaryFormat.Hexadecimal;
						length--;
						break;
				}

				// Remove the suffix character if necessary.
				text = text.Substring(0, length);

				BigInteger bigIntValue;
				if (Utility.TryParseDigits(text, (int)format, out bigIntValue) &&
					bigIntValue <= ulong.MaxValue && bigIntValue >= ulong.MinValue)
				{
					ulong ulongValue = (ulong)bigIntValue;
					value = new BinaryValue(ulongValue);
					result = true;
				}
			}

			return result;
		}

		#endregion

		#region Private Data Members

		private ulong m_value;

		private const int c_maxWordSize = 8 * sizeof(ulong); // 64

		#endregion
	}
}
