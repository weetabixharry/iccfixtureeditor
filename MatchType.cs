// <!--Copyright (c) 2016
//    Isura Edirisinghe

//        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.-->

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Fixtures
{
    public class MatchType : IComparable<MatchType>, IComparable
    {
        private static List<MatchType> _types;
        private static Dictionary<Byte, MatchType> _typesByCode;
        private String _name;
        private Byte _code;
        private Int32 _numDays;

        static MatchType()
        {
            _types = new List<MatchType>();
            _typesByCode = new Dictionary<byte, MatchType>();
        }

        private MatchType(String name, Byte code, Int32 numDays)
        {
            _name = name;
            _code = code;
            _numDays = numDays;
        }

        public static List<MatchType> Types
        {
            get { return _types; }
        }

        public Byte Code
        {
            get { return _code; }
        }

        public Int32 NumDays
        {
            get { return _numDays; }
        }

        public static void LoadTypes(Int32 version)
        {
            StringReader sr;
            if (version == 2012) 
            {
                sr = new StringReader(Fixtures.Properties.Resources.MatchTypes_2012);
            }
            else if (version == 2013)
            {
                sr = new StringReader(Fixtures.Properties.Resources.MatchTypes_2013);
            }
            else if (version == 2014)
            {
                sr = new StringReader(Fixtures.Properties.Resources.MatchTypes_2014);
            }
            else
            {
                sr = new StringReader(Fixtures.Properties.Resources.MatchTypes_2016);
            }

            _types.Clear();
            _typesByCode.Clear();

            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                if (line == "" || line.Substring(0, 1) == "#") continue;

                string[] row = line.Split(',');
                Byte code = Byte.Parse(row[1], System.Globalization.NumberStyles.HexNumber);
                Int32 numDays = Int32.Parse(row[2]);
                MatchType format = new MatchType(row[0], code, numDays);
                _types.Add(format);
                _typesByCode.Add(code, format);
            }
        }

        public static MatchType FindByCode(Byte code)
        {
            Debug.Assert(_typesByCode.ContainsKey(code));
            return _typesByCode[code];
        }

        public override String ToString()
        {
            return _name;
        }

        public int CompareTo(Object other)
        {
            return CompareTo((MatchType)other);
        }

        public int CompareTo(MatchType other)
        {
            return ToString().CompareTo(other.ToString());
        }
    }
}