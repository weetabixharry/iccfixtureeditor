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
    public class FixtureFileManager
    {
        private const Int32 StartForwardOffset = 6;
        private const Int32 YearBackwardOffset = 97;
        private const Int32 BaseYear = 2013;
        private const Byte BaseYearByte = 220;
        private const Int32 DayDataLength = 6;
        private const Int32 MatchDataLength = 50;
        private const Int32 MatchRefLength = 2;
        private const Int32 FirstMatchSpecialHeaderLength = 13;
        private const Int32 StartMatchId = 2;

        private readonly string _filePath;
        private Int32 _year;
        private DateTime _dayZero;
        private DateTime _firstDate;
        private DateTime _lastDate;
        private List<Byte> _fileContents;
        private List<DayHeader> _dayHeaders;
        private readonly List<Byte> _firstMatchSpecialHeader;
        private List<MatchHeader> _matchHeaders;
        private readonly List<Byte> _matchDataTemplate;
        private List<MatchRef> _matchRefs;
        private Match _firstMatch;
        private Int32 _nextMatchId;

        public FixtureFileManager(String filePath)
        {
            _filePath = filePath;

            _fileContents = new List<byte>(File.ReadAllBytes(filePath)); // To do: Handle IO exceptions
            _dayHeaders = new List<DayHeader>();
            _firstMatchSpecialHeader = new List<Byte>();
            _matchHeaders = new List<MatchHeader>();
            _matchDataTemplate = new List<Byte>(MatchDataLength);
            _matchRefs = new List<MatchRef>();
            _nextMatchId = StartMatchId;

            InitializeDates();
            InitializeFirstMatchHeader();
            InitializeMatchDataTemplate();
            InitializeHeadersAndRefs();
#if(DEBUG)
            LogMatches();
#endif
        }

        public DateTime FirstDate
        {
            get { return _firstDate; }
        }

        public DateTime LastDate
        {
            get { return _lastDate; }
        }

        public IEnumerable<Match> CreateMatchesFromFile()
        {
            List<Match> matches = new List<Match>();
            foreach (MatchHeader header in _matchHeaders)
            {
                Match match = CreateMatchFromHeader(header);
                SetMatchForHeaderAndRefs(header, match);
                matches.Add(match);
            }

            _firstMatch = matches[0];
            return matches;
        }

        public Match AddNewMatch(DateTime date, MatchType type)
        {
            Match newMatch = CreateNewMatch(date, type);
            AddMatch(newMatch);
            return newMatch;
        }

        public void RemoveExistingMatch(Match match)
        {
            RemoveMatch(match);
        }

        public void UpdateYear(Int32 year)
        {
            Byte yearOffset = Convert.ToByte(year - BaseYear);
            _fileContents[_fileContents.Count - 1 - YearBackwardOffset] = Convert.ToByte(BaseYearByte + yearOffset);

            Int32 yearDiff = year - _year;
            _year = year;
            _dayZero = _dayZero.AddYears(yearDiff);
            _firstDate = _firstDate.AddYears(yearDiff);
            _lastDate = _lastDate.AddYears(yearDiff);
        }

        public Int32 GetYear()
        {
            return _year;
        }

        public void UpdateIds(IEnumerable<Match> matches)
        {
            // UI must call this function after adding match because it's necessary to renumber the
            // matches. This is because the match ids must be ordered by the ordre of match definition
            // in the fxt file.
            SortMatchHeaders();
            Int32 currentId = 2;
            foreach (var header in _matchHeaders)
            {
                header.Id = currentId;
                var refs = _matchRefs.Where(r => r.Match == header.Match).ToList();
                refs.ForEach(r => { r.Id = currentId; SetNextTwoBytes(r.StartAddress, currentId); });
                Match match = matches.Where(m => m == header.Match).First();
                match.Id = currentId;
                currentId++;
            }
        }

        public void UpdateMatchDate(Match match, DateTime oldDate, DateTime newDate)
        {
            // It's simplest to implement date change by using the existing functions to remove
            // and re add the match with the new date.
            match.SetDate(oldDate);
            RemoveMatch(match);
            match.SetDate(newDate);
            AddMatch(match);
            UpdateIds(MainWindow.gMatches);
        }

        public void UpdateMatchType(Match match, MatchType oldType)
        {
            MatchHeader header = GetMatchHeader(match);
            SetMatchType(header, match.Type);
            if (match.Type.NumDays > oldType.NumDays)
            {
                // Match length extended so add refs for the additional days.
                Int32 extendStartDay = ConvertToDay(match.Date) + oldType.NumDays;
                Int32 endDay = ConvertToDay(match.Date) + match.Type.NumDays - 1;
                for (Int32 currDay = extendStartDay; currDay <= endDay; currDay++)
                {
                    DayHeader currDayHeader = GetDayHeader(ConvertToDate(currDay));
                    IncrementNumMatches(currDayHeader);
                    Int32 insertAddress = FindInsertAddressForNewRef(match, currDay);
                    AdjustAddressesAfter(insertAddress, MatchRefLength);
                    CreateAndAddMatchRef(match, insertAddress);
                    InsertMatchRefData(insertAddress, match.Id);
                }
            }
            else if (match.Type.NumDays < oldType.NumDays)
            {
                // Match length reduced so remove refs for the extra days.
                Int32 shortenStartDay = ConvertToDay(match.Date) + match.Type.NumDays;
                Int32 endDay = ConvertToDay(match.Date) + oldType.NumDays - 1;
                for (Int32 currDay = shortenStartDay; currDay <= endDay; currDay++)
                {
                    DayHeader currDayHeader = GetDayHeader(ConvertToDate(currDay));
                    DecrementNumMatches(currDayHeader);
                    MatchRef removeRef = FindMatchRefOnDay(match, currDay);
                    RemoveMatchRefData(removeRef.StartAddress);
                    _matchRefs.Remove(removeRef);
                    AdjustAddressesAfter(removeRef.StartAddress, -MatchRefLength);
                }
            }
        }

        public void UpdateMatchProperty(Match match, MatchProperty property)
        {
            // Use First instead of FirstOrDefault because it really is exceptional if we are in here
            // but there are no match headers. Function should never be called if there is no match
            // trying to update.
            MatchHeader header = GetMatchHeader(match);
            switch (property)
            {
                case MatchProperty.MatchInSeries:
                    SetMatchInSeries(header, match.MatchInSeries);
                    break;
                case MatchProperty.SeriesLength:
                    SetSeriesLength(header, match.SeriesLength);
                    break;
                case MatchProperty.HostTeam:
                    SetHostTeam(header, match.HostTeam);
                    break;
                case MatchProperty.VisitorTeam:
                    SetVisitorTeam(header, match.VisitorTeam);
                    break;
                default:
                    Debug.Assert(false, "Did not specify a valid property");
                    break;
            }
        }

        public void SaveChanges()
        {
            File.WriteAllBytes(_filePath, _fileContents.ToArray());
        }

        private void InitializeDates()
        {
            Byte yearByte = _fileContents[_fileContents.Count - 1 - YearBackwardOffset];
            Int32 year = BaseYear + (yearByte - BaseYearByte);
            _year = year;
            _dayZero = new DateTime(year, 3, 31);
            _firstDate = new DateTime(year, 4, 1);
            _lastDate = new DateTime(year + 1, 3, 30);
        }

        private void InitializeFirstMatchHeader()
        {
            using (StringReader sr = new StringReader(Fixtures.Properties.Resources.FirstMatchHeader))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Byte val = Byte.Parse(line, System.Globalization.NumberStyles.HexNumber);
                    _firstMatchSpecialHeader.Add(val);
                }
            }
            Debug.Assert(_firstMatchSpecialHeader.Count == FirstMatchSpecialHeaderLength);
        }

        private void InitializeMatchDataTemplate()
        {
            using (StringReader sr = new StringReader(Fixtures.Properties.Resources.MatchTemplate))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Byte val = Byte.Parse(line, System.Globalization.NumberStyles.HexNumber);
                    _matchDataTemplate.Add(val);
                }
            }
            Debug.Assert(_matchDataTemplate.Count == MatchDataLength);
        }

        private void InitializeHeadersAndRefs()
        {
            // Create the day headers, match headers, and match refs. Day data is 6 bytes long and
            // encodes the day number (starts at 1) and the number of matches played on that day. Match data is
            // 50 bytes long and encodes the date, format, series length, match in series, home team,
            // and visiting team. The match ref is 2 bytes encoding the (id + 1) of the match. The
            // match ref appears for days 2+ for multi day matches.
            bool firstMatch = true;
            Int32 currentAddress = StartForwardOffset;
            while (currentAddress < _fileContents.Count)
            {
                if (!HasMatchesAfter(currentAddress))
                {
                    break;
                }
                DayHeader currDayHeader = new DayHeader(currentAddress);
                
                // WEETABIXHARRY: Print out some debug info
                //System.Windows.MessageBox.Show("New day at 0x" + currentAddress.ToString("X") + "(" + currentAddress.ToString() + ") = " +
                //    _fileContents[currentAddress].ToString("X2") + " " + _fileContents[currentAddress+1].ToString("X2") + " " +
                //    _fileContents[currentAddress + 2].ToString("X2") + " " + _fileContents[currentAddress + 3].ToString("X2"));
                
                _dayHeaders.Add(currDayHeader);
                if (GetNumberMatches(currDayHeader) == 0)
                {
                    // No matches today so move to the next day.
                    currentAddress += DayDataLength;
                    continue;
                }
                // Go to start of the match data for this day.
                currentAddress += DayDataLength;
                // Process today's matches.
                for (int n = 0; n < GetNumberMatches(currDayHeader); n++)
                {
                    if (firstMatch)
                    {
                        // First match is special. There is 13 leading filler bytes before the match data.
                        currentAddress += FirstMatchSpecialHeaderLength;
                        firstMatch = false;
                        // Header bytes are different for first match.
                        Debug.Assert(_fileContents[currentAddress] == 0x72 && _fileContents[currentAddress + 1] == 0x65);
                        _matchHeaders.Add(new MatchHeader(GetNextMatchId(), currentAddress));
                        currentAddress += MatchDataLength;
                    }
                    else if (_fileContents[currentAddress] == 0x01 && _fileContents[currentAddress + 1] == 0x80)
                    {
                        // New match definition. 
                        _matchHeaders.Add(new MatchHeader(GetNextMatchId(), currentAddress));
                        currentAddress += MatchDataLength;
                    }
                    else
                    {
                        // Day 2+ of a match that was defined earlier in the file. The match id is here.
                        Int32 foundId = GetNextTwoBytes(currentAddress);
                        MatchHeader foundHeader = _matchHeaders.Where(h => h.Id == foundId).First();
                        _matchRefs.Add(new MatchRef(foundId, currentAddress));
                        currentAddress += MatchRefLength;
                    }
                }
            }
        }

        //private Int32 GetStartAddress()
        //{
        //    // The first occurrence of FF FF indicates where the first day data is stored.
        //    // Since the day data is 6 bytes long, 6 bytes before FF FF is where the day data
        //    // is stored.
        //    for (Int32 currAddress = 0; currAddress < _fileContents.Count; currAddress++)
        //    {
        //        if (_fileContents[currAddress] == 0xFF && _fileContents[currAddress + 1] == 0xFF)
        //        {
        //            return currAddress - 6;
        //        }
        //    }
        //    Debug.Assert(false, "Start address not found");
        //    return 0;
        //}

        private bool HasMatchesAfter(Int32 address)
        {
            // Return true if there are new match definitions after the specified address.
            // Up until CC 2020, the string of 14 consecutive CD bytes was the way to detect new
            // match definitions. However, in CC 2021, this has changed. Ideally, we should try to
            // preserve backwards compatibility, but this field is also defined in the template
            // resource file. TODO: Look into this.
            byte[] constPattern = { 0xff, 0x34, 0x77, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01};
            for (Int32 currentAddress = address; currentAddress < _fileContents.Count - constPattern.Length + 1; currentAddress++)
            {
                // Search for pattern at this offset.
                bool patternFound = true;
                for (Int32 patternIndex = 0; patternIndex < constPattern.Length; patternIndex++)
                    if (_fileContents[currentAddress + patternIndex] != constPattern[patternIndex])
                    {
                        patternFound = false;
                        break;
                    }
                // Return if pattern found.
                if (patternFound)
                    return true;
            }
            return false;
        }

        private Match CreateMatchFromHeader(MatchHeader header)
        {
            MatchParameter parameters = new MatchParameter();
            parameters.Id = header.Id;
            parameters.Type = GetMatchType(header);
            parameters.MatchInSeries = GetMatchInSeries(header);
            parameters.SeriesLength = GetSeriesLength(header);
            parameters.HostTeam = GetHostTeam(header);
            parameters.VisitorTeam = GetVisitorTeam(header);
            parameters.Date = GetMatchStartDate(header);
            return new Match(parameters, this);
        }

        private void SetMatchForHeaderAndRefs(MatchHeader header, Match match)
        {
            header.Match = match;
            var refs = _matchRefs.Where(r => r.Id == header.Id).ToList();
            refs.ForEach(r => r.Match = match);
        }

        private Int32 FindInsertAddressForNewRef(Match match, Int32 day)
        {
            // Used when the match length is extended. Given the current day, this function finds
            // the address where to insert the match ref for the extended day. Since match headers and
            // refs must be sorted by match id in the fxt file we have to construct the list of markers
            // that allow us to find the correct insert address.
            List<MatchMarker> markers = new List<MatchMarker>();
            DayHeader currDayHeader = GetDayHeader(ConvertToDate(day));
            DayHeader nextDayHeader = GetDayHeader(ConvertToDate(day + 1));

            var currHeaders = _matchHeaders
                .Where(h => h.StartAddress > currDayHeader.StartAddress && h.StartAddress < nextDayHeader.StartAddress)
                .ToList();

            var currRefs = _matchRefs
                .Where(r => r.StartAddress > currDayHeader.StartAddress && r.StartAddress < nextDayHeader.StartAddress)
                .ToList();

            currHeaders.ForEach(h => markers.Add(new MatchMarker(h.Id, h.StartAddress)));
            currRefs.ForEach(r => markers.Add(new MatchMarker(r.Id, r.StartAddress)));
            markers.Sort((a, b) => a.Id - b.Id);

            Int32 result = -1;
            foreach (MatchMarker mm in markers)
            {
                if (match.Id < mm.Id)
                {
                    result = mm.Address;
                    break;
                }
            }
            if (result == -1)
            {
                result = nextDayHeader.StartAddress;
            }

            return result;
        }

        private MatchRef FindMatchRefOnDay(Match match, Int32 day)
        {
            DayHeader currDayHeader = GetDayHeader(ConvertToDate(day));
            DayHeader nextDayHeader = GetDayHeader(ConvertToDate(day + 1));
            var result =
                from r in _matchRefs
                where r.Match == match && r.StartAddress > currDayHeader.StartAddress && r.StartAddress < nextDayHeader.StartAddress
                select r;
            return result.First();
        }

        private MatchHeader GetMatchHeader(Match match)
        {
            return _matchHeaders.Where(m => m.Match == match).First();
        }

        private void CreateAndAddMatchHeader(Match newMatch, Int32 address)
        {
            MatchHeader header = new MatchHeader(newMatch.Id, address);
            header.Match = newMatch;
            _matchHeaders.Add(header);
        }

        private void InsertMatchDataTemplate(Int32 insertAddress)
        {
            _fileContents.InsertRange(insertAddress, _matchDataTemplate);
        }

        private void RemoveMatchData(Int32 removeAddress)
        {
            _fileContents.RemoveRange(removeAddress, MatchDataLength);
        }

        private void InsertFirstMatchSpecialHeader(Int32 insertAddress)
        {
            _fileContents.InsertRange(insertAddress, _firstMatchSpecialHeader);
        }

        private void RemoveFirstMatchSpecialHeader(Int32 removeAddress)
        {
            _fileContents.RemoveRange(removeAddress, FirstMatchSpecialHeaderLength);
        }

        private void CreateAndAddMatchRef(Match match, Int32 address)
        {
            MatchRef matchRef = new MatchRef(match.Id, address);
            matchRef.Match = match;
            _matchRefs.Add(matchRef);
        }

        private void InsertMatchRefData(Int32 insertAddress, Int32 matchId)
        {
            _fileContents.InsertRange(insertAddress, new byte[] { 0, 0 });
            SetNextTwoBytes(insertAddress, matchId);
        }

        private void RemoveMatchRefData(Int32 removeAddress)
        {
            _fileContents.RemoveRange(removeAddress, MatchRefLength);
        }

        private Match CreateNewMatch(DateTime date, MatchType type)
        {
            MatchParameter parameters = new MatchParameter();
            parameters.Id = GetNextMatchId();
            parameters.Date = date;
            parameters.Type = type;
            parameters.MatchInSeries = 1;
            parameters.SeriesLength = 1;
            parameters.HostTeam = Team.TBD;
            parameters.VisitorTeam = Team.TBD;
            return new Match(parameters, this);
        }

        // TODO: Break up this function into several pieces
        private void AddMatch(Match match)
        {
            DayHeader startDayHeader = GetDayHeader(match.Date);
            IncrementNumMatches(startDayHeader);

            Int32 dataInsertAddress = startDayHeader.StartAddress + DayDataLength;
            if (match.Date == _firstMatch.Date)
            {
                dataInsertAddress += FirstMatchSpecialHeaderLength;
            }

            AdjustAddressesAfter(dataInsertAddress, MatchDataLength);
            CreateAndAddMatchHeader(match, dataInsertAddress);
            InsertMatchDataTemplate(dataInsertAddress);
            UpdateMatchData(match);

            Int32 startDay = ConvertToDay(match.Date);
            Int32 endDay = startDay + match.Type.NumDays - 1;
            for (Int32 currDay = startDay + 1; currDay <= endDay; currDay++)
            {
                DayHeader currDayHeader = GetDayHeader(ConvertToDate(currDay));
                IncrementNumMatches(currDayHeader);

                Int32 refInsertAddress = currDayHeader.StartAddress + DayDataLength;
                if (ConvertToDate(currDay) == _firstMatch.Date)
                {
                    refInsertAddress += FirstMatchSpecialHeaderLength;
                }

                AdjustAddressesAfter(refInsertAddress, MatchRefLength);
                CreateAndAddMatchRef(match, refInsertAddress);
                InsertMatchRefData(refInsertAddress, match.Id);
            }

            if (match.Date == _firstMatch.Date)
            {
                MatchHeader newMatchHeader = GetMatchHeader(match);
                SetFirstMatch(newMatchHeader);
            }
            else if (match.Date < _firstMatch.Date)
            {
                DayHeader oldFirstDayHeader = GetDayHeader(_firstMatch.Date);
                Int32 oldSpecialHeaderAddress = oldFirstDayHeader.StartAddress + DayDataLength;
                RemoveFirstMatchSpecialHeader(oldSpecialHeaderAddress);
                AdjustAddressesAfter(oldSpecialHeaderAddress, -FirstMatchSpecialHeaderLength);

                MatchHeader newMatchHeader = GetMatchHeader(match);
                InsertFirstMatchSpecialHeader(newMatchHeader.StartAddress);
                AdjustAddressesAfter(newMatchHeader.StartAddress, FirstMatchSpecialHeaderLength);
                SetFirstMatch(newMatchHeader);
            }
        }

        // TODO: Break up this function into several pieces
        private void RemoveMatch(Match match)
        {
            DayHeader startDayHeader = GetDayHeader(match.Date);
            DecrementNumMatches(startDayHeader);

            MatchHeader removeHeader = GetMatchHeader(match);

            if (IsFirstMatch(match))
            {
                Int32 oldSpecialHeaderAddress = startDayHeader.StartAddress + DayDataLength;
                RemoveFirstMatchSpecialHeader(oldSpecialHeaderAddress);
                AdjustAddressesAfter(oldSpecialHeaderAddress, -FirstMatchSpecialHeaderLength);

                // The second match in the file becomes the first match.
                SortMatchHeaders();
                MatchHeader newFirstMatchHeader = _matchHeaders[1];
                SetFirstMatch(newFirstMatchHeader);
                Int32 newSpecialHeaderAddress = newFirstMatchHeader.StartAddress;
                InsertFirstMatchSpecialHeader(newSpecialHeaderAddress);
                AdjustAddressesAfter(newSpecialHeaderAddress, FirstMatchSpecialHeaderLength);
            }

            RemoveMatchData(removeHeader.StartAddress);
            _matchHeaders.Remove(removeHeader);
            AdjustAddressesAfter(removeHeader.StartAddress, -MatchDataLength);

            Int32 startDay = ConvertToDay(match.Date);
            Int32 endDay = startDay + match.Type.NumDays - 1;
            for (Int32 currDay = startDay + 1; currDay <= endDay; currDay++)
            {
                DayHeader currDayHeader = GetDayHeader(ConvertToDate(currDay));
                DecrementNumMatches(currDayHeader);

                MatchRef currRef = _matchRefs.Where(r => r.Match == match).First(); //TODO: this is not that clean/clear. It works but rewrite
                RemoveMatchRefData(currRef.StartAddress);
                _matchRefs.Remove(currRef);
                AdjustAddressesAfter(currRef.StartAddress, -MatchRefLength);
            }
        }

        private void AdjustAddressesAfter(Int32 address, Int32 adjustment)
        {
            // Adjust all the headers and refs that are on or after the specified address. This is
            // needed when matches are inserted or deleted.
            _dayHeaders
                .Where(d => d.StartAddress >= address)
                .ToList()
                .ForEach(d => d.StartAddress += adjustment);

            _matchHeaders
                .Where(m => m.StartAddress >= address)
                .ToList()
                .ForEach(m => m.StartAddress += adjustment);

            _matchRefs
                .Where(r => r.StartAddress >= address)
                .ToList()
                .ForEach(r => r.StartAddress += adjustment);
        }

        private void SortMatchHeaders()
        {
            _matchHeaders.Sort((x, y) => x.StartAddress - y.StartAddress);
        }

        private void UpdateMatchData(Match match)
        {
            MatchHeader header = _matchHeaders.Where(h => h.Id == match.Id).First();
            SetNextTwoBytes(header.AddressOfDay, ConvertToDay(match.Date));
            _fileContents[header.AddressOfType] = match.Type.Code;
            _fileContents[header.AddressOfMatchInSeries] = match.MatchInSeries;
            _fileContents[header.AddressOfSeriesLength] = match.SeriesLength;
            SetNextTwoBytes(header.AddressOfHostTeam, match.HostTeam.Code);
            SetNextTwoBytes(header.AddressOfVisitorTeam, match.VisitorTeam.Code);
        }

        private Int32 GetNextMatchId()
        {
            return _nextMatchId++;
        }

        private DayHeader GetDayHeader(DateTime date)
        {
            Int32 day = ConvertToDay(date);
            return _dayHeaders.Where(h => GetNextTwoBytes(h.AddressOfDay) == day).First();
        }

        private Int32 GetNumberMatches(DayHeader dayHeader)
        {
            return GetNextTwoBytes(dayHeader.AddressOfNumMatches);
        }

        private void IncrementNumMatches(DayHeader dayHeader)
        {
            Int32 oldNumMatches = GetNextTwoBytes(dayHeader.AddressOfNumMatches);
            SetNextTwoBytes(dayHeader.AddressOfNumMatches, oldNumMatches + 1);
        }

        private void DecrementNumMatches(DayHeader dayHeader)
        {
            Int32 oldNumMatches = GetNextTwoBytes(dayHeader.AddressOfNumMatches);
            Debug.Assert(oldNumMatches >= 1);
            SetNextTwoBytes(dayHeader.AddressOfNumMatches, oldNumMatches - 1);
        }

        private bool IsFirstMatch(Match match)
        {
            return match == _firstMatch;
        }

        private void SetFirstMatch(MatchHeader newHeader)
        {
            // Unmark the old first match and mark the new first match
            MatchHeader oldHeader = _matchHeaders.Where(h => h.Match == _firstMatch).First();
            _fileContents[oldHeader.StartAddress] = 0x01;
            _fileContents[oldHeader.StartAddress + 1] = 0x80;
            _fileContents[newHeader.StartAddress] = 0x72;
            _fileContents[newHeader.StartAddress + 1] = 0x65;
            _firstMatch = newHeader.Match;
        }

        private DateTime GetMatchStartDate(MatchHeader header)
        {
            Int32 matchStartDay = GetNextTwoBytes(header.AddressOfDay);
            return ConvertToDate(matchStartDay);
        }

        private MatchType GetMatchType(MatchHeader header)
        {
            return MatchType.FindByCode(_fileContents[header.AddressOfType]);
        }

        private void SetMatchType(MatchHeader header, MatchType type)
        {
            _fileContents[header.AddressOfType] = type.Code;
        }

        private Byte GetMatchInSeries(MatchHeader header)
        {
            return _fileContents[header.AddressOfMatchInSeries];
        }

        private void SetMatchInSeries(MatchHeader header, Byte matchInSeries)
        {
            _fileContents[header.AddressOfMatchInSeries] = matchInSeries;
        }

        private Byte GetSeriesLength(MatchHeader header)
        {
            return _fileContents[header.AddressOfSeriesLength];
        }

        private void SetSeriesLength(MatchHeader header, Byte seriesLength)
        {
            _fileContents[header.AddressOfSeriesLength] = seriesLength;
        }

        private Team GetHostTeam(MatchHeader header)
        {
            Int32 hostTeamCode = GetNextTwoBytes(header.AddressOfHostTeam);
            return Team.FindByCode(hostTeamCode);
        }

        private void SetHostTeam(MatchHeader header, Team hostTeam)
        {
            SetNextTwoBytes(header.AddressOfHostTeam, hostTeam.Code);
        }

        private Team GetVisitorTeam(MatchHeader header)
        {
            Int32 visitorTeamCode = GetNextTwoBytes(header.AddressOfVisitorTeam);
            return Team.FindByCode(visitorTeamCode);
        }

        private void SetVisitorTeam(MatchHeader header, Team visitorTeam)
        {
            SetNextTwoBytes(header.AddressOfVisitorTeam, visitorTeam.Code);
        }

        private DateTime ConvertToDate(Int32 day)
        {
            DateTime result = _dayZero.AddDays(day);
            // Fixture file format doesn't assume leap years. so March dates are 1 day behind on leap years.
            if (DateTime.IsLeapYear(result.Year) && result.Month == 3)
            {
                result = result.AddDays(1);
            }
            return result;
        }

        private Int32 ConvertToDay(DateTime date)
        {
            return date.Subtract(_dayZero).Days;
        }

        private Int32 GetNextTwoBytes(Int32 startAddress)
        {
            // 2 byte values in the fixture file are ordered least significant byte folllowed by
            // most significant byte.
            return _fileContents[startAddress] + _fileContents[startAddress + 1] * 256;
        }

        private void SetNextTwoBytes(Int32 startAddress, Int32 value)
        {
            _fileContents[startAddress] = (Byte)(value % 256);
            _fileContents[startAddress + 1] = (Byte)(value / 256);
        }

        public void LogMatches()
        {
            String fileName = Path.GetFileName(_filePath);
            Int32 index = fileName.IndexOf(".");
            if (index > 0)
                fileName = fileName.Substring(0, index);

            using (StreamWriter sw = new StreamWriter("..\\..\\..\\tmp\\" + fileName + ".csv"))
            {
                foreach (MatchHeader header in _matchHeaders)
                {
                    Int32 id = header.Id;
                    Byte type = _fileContents[header.AddressOfType];
                    Int32 host = GetNextTwoBytes(header.AddressOfHostTeam);
                    Int32 visitor = GetNextTwoBytes(header.AddressOfVisitorTeam);
                    Int32 day = GetNextTwoBytes(header.AddressOfDay);
                    DateTime date = ConvertToDate(day);

                    sw.WriteLine("{0},{1},{2:X},{3:X},{4:X}", id, date.ToShortDateString(), type, host, visitor);
                }
            }
        }
    }
}