// <!--Copyright (c) 2016
//    Isura Edirisinghe

//        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.-->
 
using System;

namespace Fixtures
{
    // The DayHeader encapsulates data related to a single day in the fixture file. Each day header 
    // is followed by the match information for the day. If it is the first day of the match
    // the data is the actual match header data. Otherwise the data is simply MatchId + 1 (2 bytes).
    public class DayHeader
    {
        private const Int32 DayOffset = 0;
        private const Int32 NumMatchesOffset = 2;
        private Int32 _address;

        public DayHeader(Int32 address)
        {
            _address = address;
        }

        public Int32 StartAddress
        {
            get { return _address; }
            set { _address = value; }
        }

        public Int32 AddressOfDay
        {
            get { return _address + DayOffset; }
        }

        public Int32 AddressOfNumMatches
        {
            get { return _address + NumMatchesOffset; }
        }
    }

    // The MatchHeader encapsulates data related to a single match in the fixture file. The FixtureFileManager
    // uses the header information to store and retrieve data in the binary fixture file.
    public class MatchHeader
    {
        // Offset from start address for the match data.
        private const Int32 TypeOffset = 2;
        private const Int32 MatchInSeriesOffset = 3;
        private const Int32 SeriesLengthOffset = 4;
        private const Int32 HostTeamOffset = 39;
        private const Int32 VisitorTeamOffset = 41;
        private const Int32 DayOffset = 45;

        private Match _match;
        private Int32 _id;      // match id should be unique
        private Int32 _address; // start address of this match header

        public MatchHeader(Int32 id, Int32 address)
        {
            _id = id;
            _address = address;
        }

        public Match Match
        {
            get { return _match; }
            set { _match = value; }
        }

        public Int32 Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Int32 StartAddress
        {
            get { return _address; }
            set { _address = value; }
        }

        public Int32 AddressOfType
        {
            get { return _address + TypeOffset; }
        }

        public Int32 AddressOfMatchInSeries
        {
            get { return _address + MatchInSeriesOffset; }
        }

        public Int32 AddressOfSeriesLength
        {
            get { return _address + SeriesLengthOffset; }
        }

        public Int32 AddressOfHostTeam
        {
            get { return _address + HostTeamOffset; }
        }

        public Int32 AddressOfVisitorTeam
        {
            get { return _address + VisitorTeamOffset; }
        }

        public Int32 AddressOfDay
        {
            get { return _address + DayOffset; }
        }
    }

    public class MatchRef
    {
        private Match _match;
        private Int32 _id;
        private Int32 _address;

        public MatchRef(Int32 id, Int32 address)
        {
            _id = id;
            _address = address;
        }

        public Match Match
        {
            get { return _match; }
            set { _match = value; }
        }

        public Int32 Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Int32 StartAddress
        {
            get { return _address; }
            set { _address = value; }
        }
    }

    public class MatchMarker
    {
        private Int32 _id;
        private Int32 _address;

        public MatchMarker(Int32 id, Int32 address)
        {
            _id = id;
            _address = address;
        }

        public Int32 Id
        {
            get { return _id; }
        }

        public Int32 Address
        {
            get { return _address; }
        }
    }
}