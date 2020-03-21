﻿#region Using Directives

using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System.Globalization;
using Menees.RpnCalc.Internal;

#endregion

namespace Menees.RpnCalc
{
	// Design note: Value-derived classes must implement immutable-value semantics.
	// Lots of things pass around references to Values and assume that a Value's "value"
	// will never change.  All Value operations should create and return new Value instances
	// when necessary (just like .NET's String class).
	public abstract class Value
	{
		#region Constructors

		protected Value()
		{
		}

		#endregion

		#region Public Properties

		public abstract ValueType ValueType { get; }

		#endregion

		#region Public Methods

		public virtual string ToString(Calculator calc)
		{
			return this.ToString();
		}

		public virtual string GetEntryValue(Calculator calc)
		{
			return this.ToString(calc);
		}

		public virtual IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			return new[] { new DisplayFormat(this.ToString(calc)) };
		}

		public static bool TryParse(ValueType type, string text, out Value value)
		{
			return TryParse(type, text, null, out value);
		}

		public static bool TryParse(ValueType type, string text, Calculator calc, out Value value)
		{
			// NOTE: calc can be null.

			bool result;

			switch (type)
			{
				case ValueType.Binary:
					BinaryValue binaryValue;
					result = BinaryValue.TryParse(text, calc, out binaryValue);
					value = binaryValue;
					break;
				case ValueType.Complex:
					ComplexValue complexValue;
					result = ComplexValue.TryParse(text, calc, out complexValue);
					value = complexValue;
					break;
				case ValueType.DateTime:
					DateTimeValue dateTimeValue;
					result = DateTimeValue.TryParse(text, out dateTimeValue);
					value = dateTimeValue;
					break;
				case ValueType.Double:
					DoubleValue doubleValue;
					result = DoubleValue.TryParse(text, out doubleValue);
					value = doubleValue;
					break;
				case ValueType.Fraction:
					FractionValue fractionValue;
					result = FractionValue.TryParse(text, out fractionValue);
					value = fractionValue;
					break;
				case ValueType.Integer:
					IntegerValue integerValue;
					result = IntegerValue.TryParse(text, out integerValue);
					value = integerValue;
					break;
				case ValueType.TimeSpan:
					TimeSpanValue timeSpanValue;
					result = TimeSpanValue.TryParse(text, out timeSpanValue);
					value = timeSpanValue;
					break;
				default:
					result = false;
					value = null;
					break;
			}

			return result;
		}

		public static Value Abs(Value value, Calculator calc)
		{
			Value result = value;

			switch (value.ValueType)
			{
				case ValueType.TimeSpan:
					TimeSpanValue timeSpan = (TimeSpanValue)value;
					if (timeSpan.AsTimeSpan < TimeSpan.Zero)
					{
						result = new TimeSpanValue(timeSpan.AsTimeSpan.Negate());
					}

					break;
				case ValueType.Complex:
					result = new DoubleValue(Complex.Abs(((ComplexValue)value).AsComplex));
					break;
				case ValueType.Double:
					result = new DoubleValue(Math.Abs(((DoubleValue)value).AsDouble));
					break;
				case ValueType.Fraction:
					FractionValue fraction = (FractionValue)value;
					if (fraction.Sign < 0)
					{
						result = FractionValue.Negate(fraction);
					}

					break;
				case ValueType.Integer:
					result = new IntegerValue(BigInteger.Abs(((IntegerValue)value).AsInteger));
					break;
				case ValueType.Binary:
					BinaryValue binary = (BinaryValue)value;
					if (binary.Sign(calc) < 0)
					{
						result = BinaryValue.Negate(binary, calc);
					}

					break;
				default:
					throw InvalidOp(Resources.Value_Abs, value);
			}

			return result;
		}

		public static Value Add(Value x, Value y, Calculator calc)
		{
			Value result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case ValueType.Binary:
						result = BinaryValue.Add((BinaryValue)x, (BinaryValue)y, calc);
						break;
					case ValueType.Complex:
						result = (ComplexValue)x + (ComplexValue)y;
						break;
					case ValueType.Double:
						result = (DoubleValue)x + (DoubleValue)y;
						break;
					case ValueType.Fraction:
						result = (FractionValue)x + (FractionValue)y;
						break;
					case ValueType.Integer:
						result = (IntegerValue)x + (IntegerValue)y;
						break;
					case ValueType.TimeSpan:
						result = (TimeSpanValue)x + (TimeSpanValue)y;
						break;
				}
			}
			else // Handle special cases.
			{
				if (x.ValueType == ValueType.DateTime && y.ValueType == ValueType.TimeSpan)
				{
					result = (DateTimeValue)x + (TimeSpanValue)y;
				}
				else if (x.ValueType == ValueType.TimeSpan && y.ValueType == ValueType.DateTime)
				{
					result = (TimeSpanValue)x + (DateTimeValue)y;
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.Value_Add, x, y);
			}

			return result;
		}

		public static Value Subtract(Value x, Value y, Calculator calc)
		{
			Value result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case ValueType.Binary:
						result = BinaryValue.Subtract((BinaryValue)x, (BinaryValue)y, calc);
						break;
					case ValueType.Complex:
						result = (ComplexValue)x - (ComplexValue)y;
						break;
					case ValueType.DateTime:
						result = (DateTimeValue)x - (DateTimeValue)y;
						break;
					case ValueType.Double:
						result = (DoubleValue)x - (DoubleValue)y;
						break;
					case ValueType.Fraction:
						result = (FractionValue)x - (FractionValue)y;
						break;
					case ValueType.Integer:
						result = (IntegerValue)x - (IntegerValue)y;
						break;
					case ValueType.TimeSpan:
						result = (TimeSpanValue)x - (TimeSpanValue)y;
						break;
				}
			}
			else // Handle special cases.
			{
				if (x.ValueType == ValueType.DateTime && y.ValueType == ValueType.TimeSpan)
				{
					result = (DateTimeValue)x - (TimeSpanValue)y;
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.Value_Subtract, x, y);
			}

			return result;
		}

		public static Value Multiply(Value x, Value y, Calculator calc)
		{
			Value result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case ValueType.Binary:
						result = BinaryValue.Multiply((BinaryValue)x, (BinaryValue)y, calc);
						break;
					case ValueType.Complex:
						result = (ComplexValue)x * (ComplexValue)y;
						break;
					case ValueType.Double:
						result = (DoubleValue)x * (DoubleValue)y;
						break;
					case ValueType.Fraction:
						result = (FractionValue)x * (FractionValue)y;
						break;
					case ValueType.Integer:
						result = (IntegerValue)x * (IntegerValue)y;
						break;
				}
			}
			else // Handle special cases.
			{
				NumericValue numX = x as NumericValue;
				NumericValue numY = y as NumericValue;
				if (x.ValueType == ValueType.TimeSpan && numY != null) // TimeSpan * Numeric
				{
					result = (TimeSpanValue)x * new DoubleValue(numY.ToDouble());
				}
				else if (numX != null && y.ValueType == ValueType.TimeSpan) // Numeric * TimeSpan
				{
					result = (TimeSpanValue)y * new DoubleValue(numX.ToDouble());
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.Value_Multiply, x, y);
			}

			return result;
		}

		public static Value Divide(Value x, Value y, Calculator calc)
		{
			Value result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case ValueType.Binary:
						result = BinaryValue.Divide((BinaryValue)x, (BinaryValue)y, calc);
						break;
					case ValueType.Complex:
						result = (ComplexValue)x / (ComplexValue)y;
						break;
					case ValueType.Double:
						result = (DoubleValue)x / (DoubleValue)y;
						break;
					case ValueType.Fraction:
						result = (FractionValue)x / (FractionValue)y;
						break;
					case ValueType.Integer:
						result = (IntegerValue)x / (IntegerValue)y;
						break;
					case ValueType.TimeSpan:
						result = (TimeSpanValue)x / (TimeSpanValue)y;
						break;
				}
			}
			else // Handle special cases.
			{
				NumericValue numY = y as NumericValue;
				if (x.ValueType == ValueType.TimeSpan && numY != null) // TimeSpan / Numeric
				{
					result = (TimeSpanValue)x / new DoubleValue(numY.ToDouble());
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.Value_Divide, x, y);
			}

			return result;
		}

		public static Value Negate(Value value, Calculator calc)
		{
			Value result;
			switch (value.ValueType)
			{
				case ValueType.TimeSpan:
					result = -(TimeSpanValue)value;
					break;
				case ValueType.Complex:
					result = -(ComplexValue)value;
					break;
				case ValueType.Double:
					result = -(DoubleValue)value;
					break;
				case ValueType.Fraction:
					result = -(FractionValue)value;
					break;
				case ValueType.Integer:
					result = -(IntegerValue)value;
					break;
				case ValueType.Binary:
					result = BinaryValue.Negate((BinaryValue)value, calc);
					break;
				default:
					throw InvalidOp(Resources.Value_Negate, value);
			}

			return result;
		}

		public static NumericValue Sign(Value value, Calculator calc)
		{
			NumericValue result;

			switch (value.ValueType)
			{
				case ValueType.TimeSpan:
					TimeSpan timeSpan = ((TimeSpanValue)value).AsTimeSpan;
					result = new IntegerValue((timeSpan > TimeSpan.Zero) ? 1 : (timeSpan < TimeSpan.Zero ? -1 : 0));
					break;
				case ValueType.Complex:
					Complex complex = ((ComplexValue)value).AsComplex;
					if (complex == Complex.Zero)
					{
						result = new ComplexValue(Complex.Zero);
					}
					else
					{
						// Return the "unit vector" like the HP48 did.  This is the
						// complex number with magnitude 1 pointing in the same
						// direction (i.e., with the same phase) as the current value.
						double magnitude = complex.Magnitude;
						Complex unitVector = new Complex(complex.Real / magnitude, complex.Imaginary / magnitude);
						result = new ComplexValue(unitVector);
					}

					break;
				case ValueType.Double:
					result = new IntegerValue(Math.Sign(((DoubleValue)value).AsDouble));
					break;
				case ValueType.Fraction:
					result = new IntegerValue(((FractionValue)value).Sign);
					break;
				case ValueType.Integer:
					result = new IntegerValue(((IntegerValue)value).AsInteger.Sign);
					break;
				case ValueType.Binary:
					result = new IntegerValue(((BinaryValue)value).Sign(calc));
					break;
				default:
					throw InvalidOp(Resources.Value_Sign, value);
			}

			return result;
		}

		#endregion

		#region Internal Methods

		internal static Value Load(INode valueNode, Calculator calc)
		{
			Value result = null;

			if (valueNode != null)
			{
				string typeText = valueNode.GetValue("ValueType", null);
				string valueText = valueNode.GetValue("EntryValue", null);

				if (typeText != null && valueText != null)
				{
					ValueType type;
					if (Utility.TryParse(typeText, false, out type))
					{
						TryParse(type, valueText, calc, out result);
					}
				}
			}

			return result;
		}

		internal void Save(INode valueNode, Calculator calc)
		{
			valueNode.SetValue("ValueType", this.ValueType);
			valueNode.SetValue("EntryValue", this.GetEntryValue(calc));
		}

		/// <summary>
		/// This compares values after implicit type conversion, and complex values
		/// are never compared.  This method allows 3/2 to be compared to 3.75
		/// even though they're different data types.
		/// </summary>
		internal static int Compare(Value x, Value y, Calculator calc)
		{
			int? result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				// X and Y should be of the same type now.
				switch (x.ValueType)
				{
					case ValueType.Binary:
						result = ((BinaryValue)x).CompareTo((BinaryValue)y);
						break;
					case ValueType.DateTime:
						result = ((DateTimeValue)x).CompareTo((DateTimeValue)y);
						break;
					case ValueType.Double:
						result = ((DoubleValue)x).CompareTo((DoubleValue)y);
						break;
					case ValueType.Fraction:
						result = ((FractionValue)x).CompareTo((FractionValue)y);
						break;
					case ValueType.Integer:
						result = ((IntegerValue)x).CompareTo((IntegerValue)y);
						break;
					case ValueType.TimeSpan:
						result = ((TimeSpanValue)x).CompareTo((TimeSpanValue)y);
						break;
				}
			}

			if (!result.HasValue)
			{
				throw new ArgumentException(string.Format(
					CultureInfo.CurrentCulture,
					Resources.Value_UnableToCompare, x.ToString(calc), y.ToString(calc)));
			}

			return result.Value;
		}

		internal static Exception InvalidOp(string opName, Value value)
		{
			string message = string.Format(
				CultureInfo.CurrentCulture,
				Resources.Value_UnaryOperationNotSupported, opName, value.ValueType);
			throw new ArithmeticException(message);
		}

		internal static Exception InvalidOp(string opName, Value x, Value y)
		{
			string message = string.Format(
				CultureInfo.CurrentCulture,
				Resources.Value_BinaryOperationNotSupported, opName, x.ValueType, y.ValueType);
			throw new ArithmeticException(message);
		}

		internal static bool CompareWithNulls(Value x, Value y, out int nullComparisonResult)
		{
			nullComparisonResult = 0;
			bool atLeastOneNull = false;

			// Return -1, 0, or 1 using the null semantics required by IComparable<T>.CompareTo.
			// Any object compares greater than a null reference, and two null references compare
			// equal to each other. 
			if (x == null)
			{
				atLeastOneNull = true;
				if (y == null)
				{
					nullComparisonResult = 0;
				}
				else
				{
					nullComparisonResult = -1;
				}
			}
			else if (y == null)
			{
				atLeastOneNull = true;
				nullComparisonResult = 1;
			}

			return atLeastOneNull;
		}

		#endregion

		#region Private Methods

		private static bool HandleImplicitTypeConversion(ref Value x, ref Value y)
		{
			// We can only do implicit conversions between numeric types.
			NumericValue numX = x as NumericValue;
			NumericValue numY = y as NumericValue;
			if (numX != null && numY != null)
			{
				if (NumericValue.HandleImplicitTypeConversion(ref numX, ref numY))
				{
					x = numX;
					y = numY;
				}
			}

			bool result = x.ValueType == y.ValueType;
			return result;
		}

		#endregion
	}
}
