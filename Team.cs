// <!--Copyright (c) 2016
//    Isura Edirisinghe

//        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.-->

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fixtures
{
    public class Team : IComparable<Team>, IComparable
    {
        public static Team TBD;
        private static List<Team> _teams;
        private const bool DISCOVER_TEAMS = false;
        private String _name;
        private Int32 _code;

        static Team()
        {
            TBD = new Team("T.B.D.", 0);
            _teams = new List<Team>();
        }

        private Team(String name, Int32 code)
        {
            _name = name;
            _code = code;
        }

        public static List<Team> Teams
        {
            get { return _teams; }
        }

        public Int32 Code
        {
            get { return _code; }
        }

        public static void LoadTeams(Int32 version)
        {
            StringReader sr;
            if (version == 2012)
            {
                sr = new StringReader(Fixtures.Properties.Resources.Teams_2012);
            }
            else if (version == 2013)
            {
                sr = new StringReader(Fixtures.Properties.Resources.Teams_2013);
            }
            else if (version == 2014)
            {
                sr = new StringReader(Fixtures.Properties.Resources.Teams_2014);
            }
            else
            {
                Debug.Assert(version == 2016);
                sr = new StringReader(Fixtures.Properties.Resources.Teams_2016);
            }

            _teams.Clear();
            _teams.Add(TBD);
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                if (line == "" || line.Substring(0, 1) == "#") continue;

                string[] row = line.Split(',');
                String name = row[0];
                Int32 code = Int32.Parse(row[1], System.Globalization.NumberStyles.HexNumber);
                _teams.Add(new Team(name, code));
            }
        }

        public static Team FindByCode(Int32 code)
        {
            Team result = _teams.Where(t => t._code == code).FirstOrDefault();
            if (result == null)
            {
                result = DISCOVER_TEAMS ? new Team("Unknown", code) : TBD;
            }
            return result;
        }

        public override String ToString()
        {
            return _name;
        }

        public int CompareTo(Team other)
        {
            return ToString().CompareTo(other.ToString());
        }

        public int CompareTo(Object other)
        {
            return CompareTo((Team)other);
        }
    }
}