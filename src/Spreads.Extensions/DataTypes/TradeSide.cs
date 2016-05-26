/*
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


namespace Spreads.DataTypes
{
    public enum TradeSide : byte
    {
        /// <summary>
        /// By default, this should be always zero, so if there is an error and we forget to specify the Side, we must fail fast.
        /// </summary>
        None = 0 ,
        Buy = 1,
        Sell = 255, // -1 for signed byte
    }
}