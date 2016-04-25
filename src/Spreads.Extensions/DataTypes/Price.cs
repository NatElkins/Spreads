﻿/*
    Copyright(c) 2014-2016 Victor Baybekov.

    This file is a part of Spreads library.

    Spreads library is free software; you can redistribute it and/or modify it under
    the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    Spreads library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using System.Runtime.InteropServices;

namespace Spreads.DataTypes {

    /// <summary>
    /// A blittable structure to store positive price values with decimal precision up to 15 digits.
    /// Could be qualified to trade/buy/sell to add additional checks and logic to trading applications.
    /// Qualification is optional and doesn't affect eqiality and comparison.
    /// </summary>
    /// <remarks>
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |Q T S B|  -exp |        Int56 mantissa                         |
    /// +-------------------------------+-+-+---------------------------+
    /// |               Int56 mantissa                                  |
    /// +-------------------------------+-+-+---------------------------+
    /// R - reserved
    /// Q - is qualified, when set to 1 then T,S,B could be set to 1, otherwise T,S,B must be zero and do not have a special meaning
    /// T (trade) - is trade, when 1 then the price is of some actual trade
    /// S (sell)  - is Sell (Ask). When T = 0, S = 1 indicates an Ask order.
    /// B (buy)   - is Buy (Bid). When T = 0, B = 1 indicates an Bid order.
    /// When T = 1, S/B flags could have a special meaning depending on context.
    /// S and B are mutually exlusive.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct Price : IComparable<Price>, IEquatable<Price> {


        public override int GetHashCode() {
            return _value.GetHashCode();
        }

        private const ulong MantissaMask = ((1L << 56) - 1L);
        private const ulong UnqualifiedMask = ((1L << 56) - 1L);
        private readonly ulong _value;

        private static decimal[] DecimalFractions10 = new decimal[] {
            1M,
            0.1M,
            0.01M,
            0.001M,
            0.0001M,
            0.00001M,
            0.000001M,
            0.0000001M,
            0.00000001M,
            0.000000001M,
            0.0000000001M,
            0.00000000001M,
            0.000000000001M,
            0.0000000000001M,
            0.00000000000001M,
            0.000000000000001M,
        };


        private static double[] DoubleFractions10 = new double[] {
            1,
            0.1,
            0.01,
            0.001,
            0.0001,
            0.00001,
            0.000001,
            0.0000001,
            0.00000001,
            0.000000001,
            0.0000000001,
            0.00000000001,
            0.000000000001,
            0.0000000000001,
            0.00000000000001,
            0.000000000000001,
        };

        private static ulong[] Powers10 = new ulong[] {
            1,
            10,
            100,
            1000,
            10000,
            100000,
            1000000,
            10000000,
            100000000,
            1000000000,
            10000000000,
            100000000000,
            1000000000000,
            10000000000000,
            100000000000000,
            1000000000000000,
        };

        public ulong Exponent => (_value >> 56) & 15UL;
        public ulong Mantissa => _value & MantissaMask;
        public decimal AsDecimal => (this);
        public double AsDouble => (this);
        public Price AsUnqualified => IsQualified ? new Price(_value) : this;

        public bool IsQualified => _value >> 63 == 1UL;

        public bool? IsTrade
        {
            get
            {
                var firstTwo = _value >> 62;
                // 11
                if (firstTwo == 3UL) {
                    return true;
                }
                // 10
                if (firstTwo == 2UL) {
                    return false;
                }
                // 00
                if (firstTwo == 0UL) {
                    return null;
                }
                // 01
                throw new ApplicationException("IsTrade bit could only be set together with IsQualified bit");
            }
        }

        public bool? IsBuy
        {
            get
            {
                if (!IsQualified) return null;
                var thirdAndForth = (_value >> 60) & 3UL;
                // 1_11
                if (thirdAndForth == 3UL) {
                    throw new ApplicationException("IsSell and IsBuy bits must be mutually exclusive");
                }
                // 1_01
                if (thirdAndForth == 1UL) {
                    return true;
                }
                // 1_00 or 1_10
                return false;
            }
        }

        public bool? IsSell
        {
            get
            {
                if (!IsQualified) return null;
                var thirdAndForth = (_value >> 60) & 3UL;
                // 1_11
                if (thirdAndForth == 3UL) {
                    throw new ApplicationException("IsSell and IsBuy bits must be mutually exclusive");
                }
                // 1_10
                if (thirdAndForth == 2UL) {
                    return true;
                }
                // 1_00 or 1_01
                return false;
            }
        }

        public Side? Side
        {
            get
            {
                if (!IsQualified) return null;
                // ReSharper disable once PossibleInvalidOperationException
                if (IsBuy.Value) {
                    return DataTypes.Side.Buy;
                }
                // ReSharper disable once PossibleInvalidOperationException
                if (IsSell.Value) {
                    return DataTypes.Side.Sell;
                }
                return DataTypes.Side.None;
            }
        }

        
        public Price(int exponent, long mantissa) {
            if ((ulong)exponent > 15) throw new ArgumentOutOfRangeException(nameof(exponent));
            if ((ulong)mantissa > MantissaMask) throw new ArgumentOutOfRangeException(nameof(mantissa));
            _value = ((ulong)exponent << 56) | ((ulong)mantissa);
        }

        private Price(ulong value, Side? side = null, bool? isTrade = null)
        {
            _value = value & UnqualifiedMask;
            if (side != null && side.Value != DataTypes.Side.None) {
                if (side == DataTypes.Side.Buy) {
                    _value = _value | (9UL << 60); // 9 ~ 1001
                } else if (side == DataTypes.Side.Sell) { // 10 ~ 1010
                    _value = _value | (10UL << 60);
                }
            }
            if (isTrade != null && isTrade.Value) {
                _value = _value | (12UL << 60); // 12 ~ 1100
            }
        }

        public Price(decimal value, int precision = 5) : this(value, precision, null, null) {}

        public Price(decimal value, int precision = 5, Side? side = null, bool? isTrade = null) {
            if ((ulong)precision > 15) throw new ArgumentOutOfRangeException(nameof(precision));
            if (value > MantissaMask * DecimalFractions10[precision]) throw new ArgumentOutOfRangeException(nameof(value));
            var mantissa = decimal.ToUInt64(value * Powers10[precision]);
            _value = ((ulong)precision << 56) | mantissa;
            if (side != null && side.Value != DataTypes.Side.None) {
                if (side == DataTypes.Side.Buy) {
                    _value = _value | (9UL << 60); // 9 ~ 1001
                } else if (side == DataTypes.Side.Sell) { // 10 ~ 1010
                    _value = _value | (10UL << 60);
                }
            }
            if (isTrade != null && isTrade.Value) {
                _value = _value | (12UL << 60); // 12 ~ 1100
            }
        }

        public Price(Price price, Side? side = null, bool? isTrade = null) : this(price._value, side, isTrade)
        {
        }

        public Price(double value, int precision = 5) {
            if ((ulong)precision > 15) throw new ArgumentOutOfRangeException(nameof(precision));
            if (value > MantissaMask * DoubleFractions10[precision]) throw new ArgumentOutOfRangeException(nameof(value));
            var mantissa = (ulong)(value * Powers10[precision]);
            _value = ((ulong)precision << 56) | mantissa;
        }

        public Price(int value) {
            _value = (ulong)value;
        }

        public static implicit operator double(Price price) {
            return price.Mantissa * DoubleFractions10[price.Exponent];
        }

        public static implicit operator float(Price price) {
            return (float)(price.Mantissa * DoubleFractions10[price.Exponent]);
        }

        public static implicit operator decimal(Price price) {
            return price.Mantissa * DecimalFractions10[price.Exponent];
        }

        public int CompareTo(Price other) {
            var c = (int)this.Exponent - (int)other.Exponent;
            if (c == 0) {
                return this.Mantissa.CompareTo(other.Mantissa);
            }
            if (c > 0) {
                return (this.Mantissa * Powers10[c]).CompareTo(other.Mantissa);
            } else {
                return this.Mantissa.CompareTo(other.Mantissa * Powers10[-c]);
            }
        }

        public bool Equals(Price other) {
            return this.CompareTo(other) == 0;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Price && Equals((Price)obj);
        }

        public static bool operator ==(Price x, Price y) {
            return x.Equals(y);
        }
        public static bool operator !=(Price x, Price y) {
            return !x.Equals(y);
        }
    }
}