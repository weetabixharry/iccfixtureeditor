// <!--Copyright (c) 2013
//    Isura Edirisinghe

//        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.-->
 
using System;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Fixtures
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ObservableCollection<Match> gMatches;
        private FixtureFileManager _fixtureFileMgr;

        public MainWindow()
        {
            InitializeComponent();
            gMatches = (Matches)Resources["matches"];
        }

        //private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Hack to remove the overflow grip on right side of toolbar.
        //    ToolBar toolBar = sender as ToolBar;
        //    var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
        //    if (overflowGrid != null)
        //    {
        //        overflowGrid.Visibility = Visibility.Collapsed;
        //    }
        //}

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "fxt";
            dlg.Filter = "Fixture Files (*.fxt)|*.fxt|All Files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                FilePath.Text = dlg.FileName;
                _fixtureFileMgr = new FixtureFileManager(dlg.FileName);
                PopulateMatches();
            }
        }

        private void PopulateMatches()
        {
            Matches matches = (Matches)Resources["matches"];
            matches.Clear();
            var matchesList = _fixtureFileMgr.CreateMatchesFromFile().ToList();
            matchesList.ForEach(m => matches.Add(m));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fixtureFileMgr != null)
            {
                _fixtureFileMgr.SaveChanges();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Matches matches = (Matches)Resources["matches"];
            Match selectedMatch = (Match)matchGrid.SelectedItem;
            if (_fixtureFileMgr == null || selectedMatch == null || matches.Count < 2)
                return;            
            // If first match is selected insert above the second match. Don't use the first match
            // because duplicating the first match has issues (first match has special header info).
            if (selectedMatch == matches[0])
            {
                selectedMatch = matches[1];
            }
            Match newMatch = _fixtureFileMgr.CreateAndAddNewMatch(selectedMatch.Date, selectedMatch.Type);
            matches.Insert(matches.IndexOf(selectedMatch), newMatch);
            _fixtureFileMgr.UpdateIds(matches);
            matchGrid.SelectedItem = matchGrid.Items[matches.IndexOf(newMatch)];            
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Matches matches = (Matches)Resources["matches"];
            Match selectedMatch = (Match)matchGrid.SelectedItem;
            // Don't support removing the first match in the file for now because it has special header info.
            if (_fixtureFileMgr == null || selectedMatch == null || matches.Count < 2 || selectedMatch == matches[0])
                return;            
            _fixtureFileMgr.RemoveMatch(selectedMatch);
            matches.Remove(selectedMatch);
            _fixtureFileMgr.UpdateIds(matches);            
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            // Uses a constructor that takes a parent window for the AboutBox.
            WPFAboutBox1 about = new WPFAboutBox1(this);
            about.ShowDialog();
        }
    }

    public class Matches : ObservableCollection<Match>
    {
        // Wrapper class so we can bind data in Xmal. 
    }

    public class Match : INotifyPropertyChanged
    {
        private readonly FixtureFileManager _manager;
        private  Int32 _id;
        private DateTime _date;
        private MatchType _type;
        private Byte _matchInSeries;
        private Byte _seriesLength;
        private Team _hostTeam;
        private Team _visitorTeam;

        public Match(MatchParameter parameters, FixtureFileManager manager)
        {
            _manager = manager;
            _id = parameters.Id;
            _date = parameters.Date;
            _type = parameters.Type;
            _matchInSeries = parameters.MatchInSeries;
            _seriesLength = parameters.SeriesLength;
            _hostTeam = parameters.HostTeam;
            _visitorTeam = parameters.VisitorTeam;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Int32 Id
        {
            get 
            { 
                return _id; 
            }
            set 
            { 
                _id = value;
                NotifyPropertyChanged("Id");
            }
        }

        public DateTime Date
        {
            get 
            { 
                return _date; 
            }
            set
            {
                // TODO: validation of date
                DateTime oldDate = _date;
                DateTime newDate = value;
                _date = newDate;
                _manager.UpdateMatchDate(this, oldDate, newDate);
                NotifyPropertyChanged("Date");
            }
        }

        public MatchType Type
        {
            get 
            { 
                return _type;
            }
            set
            {
                MatchType oldType = _type;
                _type = value;
                _manager.UpdateMatchType(this, oldType);
                NotifyPropertyChanged("Type");
            }
        }

        public Byte MatchInSeries
        {
            get 
            { 
                return _matchInSeries;
            }
            set
            {
                _matchInSeries = value;
                _manager.UpdateMatch(this, MatchProperty.MatchInSeries);
                NotifyPropertyChanged("MatchInSeries");
            }
        }

        public Byte SeriesLength
        {
            get 
            { 
                return _seriesLength;
            }
            set
            {
                _seriesLength = value;
                _manager.UpdateMatch(this, MatchProperty.SeriesLength);
                NotifyPropertyChanged("SeriesLength");
            }
        }

        public Team HostTeam
        {
            get 
            { 
                return _hostTeam;
            }
            set
            {
                _hostTeam = value;
                _manager.UpdateMatch(this, MatchProperty.HostTeam);
                NotifyPropertyChanged("HostTeam");
            }
        }

        public Team VisitorTeam
        {
            get { return _visitorTeam; }
            set
            {
                _visitorTeam = value;
                _manager.UpdateMatch(this, MatchProperty.VisitorTeam);
                NotifyPropertyChanged("VisitorTeam");
            }
        }

        public void SetDate(DateTime date)
        {
            // This function is needed because FixtureFileManager cannot set the match date through
            // the property because it leads to recursion.
            _date = date;
        }

        private void NotifyPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class FixtureFileManager
    {
        private const Int32 StartOffset = 6;
        private const Int32 DayDataLength = 6;
        private const Int32 MatchDataLength = 42;
        private const Int32 MatchRefLength = 2;
        private const Int32 FirstMatchDataOffset = 13;
        private const Int32 StartMatchId = 2;

        private readonly string _filePath;
        private readonly DateTime _dayZero;
        private List<Byte> _fileContents;
        private List<DayHeader> _dayHeaders;
        private List<MatchHeader> _matchHeaders;
        private readonly List<Byte> _matchDataTemplate;
        private List<MatchRef> _matchRefs;
        private Int32 _nextMatchId;

        public FixtureFileManager(String filePath) 
        {
            _filePath = filePath;
            _dayZero = new DateTime(ParseStartYear(filePath), 3, 31);
            _fileContents = new List<byte>(File.ReadAllBytes(filePath)); // To do: Handle IO exceptions
            _dayHeaders = new List<DayHeader>();
            _matchHeaders = new List<MatchHeader>();
            _matchDataTemplate = new List<Byte>(MatchDataLength);
            _matchRefs = new List<MatchRef>();
            _nextMatchId = StartMatchId;
            InitializeMatchDataTemplate();
            InitializeHeadersAndRefs();
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
            return matches;
        }

        public Match CreateAndAddNewMatch(DateTime date, MatchType type)
        {
            Match newMatch = CreateNewMatch(date, type);

            DayHeader startDayHeader = GetDayHeader(date);
            IncrementNumMatches(startDayHeader);

            Int32 dataInsertAddress = startDayHeader.StartAddress + DayDataLength;
            AdjustAddressesAfter(dataInsertAddress, MatchDataLength);
            CreateAndAddMatchHeader(newMatch, dataInsertAddress);
            InsertMatchDataTemplate(dataInsertAddress);
            UpdateMatchData(newMatch);

            Int32 startDay = ConvertToDay(date);
            Int32 endDay = startDay + type.NumDays - 1;
            for (Int32 currDay = startDay + 1; currDay <= endDay; currDay++)
            {
                DayHeader currDayHeader = GetDayHeader(ConvertToDate(currDay));
                IncrementNumMatches(currDayHeader);

                Int32 refInsertAddress = currDayHeader.StartAddress + DayDataLength;
                AdjustAddressesAfter(refInsertAddress, MatchRefLength);
                CreateAndAddMatchRef(newMatch, refInsertAddress);
                InsertMatchRefData(refInsertAddress, newMatch.Id);
            }

            return newMatch;
        }

        public void AddMatch(Match match)
        {
            DayHeader startDayHeader = GetDayHeader(match.Date);
            IncrementNumMatches(startDayHeader);

            Int32 dataInsertAddress = startDayHeader.StartAddress + DayDataLength;
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
                AdjustAddressesAfter(refInsertAddress, MatchRefLength);
                CreateAndAddMatchRef(match, refInsertAddress);
                InsertMatchRefData(refInsertAddress, match.Id);
            }
        }

        public void RemoveMatch(Match match)
        {
            DayHeader startDayHeader = GetDayHeader(match.Date);
            DecrementNumMatches(startDayHeader);

            MatchHeader removeHeader = _matchHeaders.Where(h => h.Match == match).First();
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

        public void UpdateIds(IEnumerable<Match> matches)
        {
            // UI must call this function after adding match because it's necessary to renumber the
            // matches. This is because the match ids must be ordered by the ordre of match definition
            // in the fxt file.
            _matchHeaders.Sort((x, y) => x.StartAddress - y.StartAddress);
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
                Int32 endDay =  ConvertToDay(match.Date) + match.Type.NumDays - 1;
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

        public void UpdateMatch(Match match, MatchProperty property)
        {
            // Use First instead of FirstOrDefault because it really is exceptional if we are in here
            // but there are no match headers. Function should never be called if there is no match
            // trying to update.
            MatchHeader header = _matchHeaders.Where(h => h.Id == match.Id).First();
            switch (property)
            {
                case MatchProperty.Date:
                    SetNextTwoBytes(header.AddressOfDay, ConvertToDay(match.Date));
                    break;
                case MatchProperty.Type:
                    _fileContents[header.AddressOfType] = match.Type.Code;
                    break;
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

        private Int32 ParseStartYear(String filePath)
        {
            // Last 2 characters of fixture file should be 2 digit year (i.e 2012 -> 12).
            Int32 startYear = 2012;
            String fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            if (fileName.Length >= 2)
            {
                try
                {
                    String fileSuffix = fileName.Substring(fileName.Length - 2);
                    startYear = 2000 + Int32.Parse(fileSuffix);
                }
                catch (FormatException)
                {
                    startYear = 2012;
                }
            }
            return startYear;
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
            // 42 bytes long and encodes the date, format, series length, match in series, home team,
            // and visiting team. The match ref is 2 bytes encoding the (id + 1) of the match. The
            // match ref appears for days 2+ for multi day matches.
            bool firstMatch = true;
            Int32 currentAddress = StartOffset;
            while (currentAddress < _fileContents.Count)
            {
                if (!HasMatchesAfter(currentAddress))
                {
                    break;
                }
                DayHeader currDayHeader = new DayHeader(currentAddress);
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
                        currentAddress += FirstMatchDataOffset;
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
            // The string of 14 consecutive CD bytes is the way to flag new match definitions.
            for (Int32 currentAddress = address; currentAddress < _fileContents.Count; currentAddress++)
            {
                // We are lazy so should be safe to check only the first 4 bytes. TODO: Check all 14 bytes.
                if (_fileContents[currentAddress] == 0xCD && _fileContents[currentAddress + 1] == 0xCD &&
                    _fileContents[currentAddress + 2] == 0xCD && _fileContents[currentAddress + 3] == 0xCD)
                {
                    return true;
                }
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
    }

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
        private const Int32 HostTeamOffset = 31;
        private const Int32 VisitorTeamOffset = 33;
        private const Int32 DayOffset = 37;

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

    // Object used to pass parameters to Match class.
    public class MatchParameter
    {
        public Int32 Id { get; set; }
        public DateTime Date { get; set; }
        public MatchType Type { get; set; }
        public Byte MatchInSeries { get; set; }
        public Byte SeriesLength { get; set; }
        public Team HostTeam { get; set; }
        public Team VisitorTeam { get; set; }
    }

    public enum MatchProperty
    {
        Date,
        Type,
        MatchInSeries,
        SeriesLength,
        HostTeam,
        VisitorTeam
    }

    //public class MatchData
    //{
    //    public Int32 Index { get; set; }
    //    public Int32 StartAddress { get; set; }
    //    public Byte Format { get; set; }
    //    public Byte MatchInSeries { get; set; }
    //    public Byte SeriesLength { get; set; }
    //    public Byte HostLSB { get; set; }
    //    public Byte HostMSB { get; set; }
    //    public Byte VisitorLSB { get; set; }
    //    public Byte VisitorMSB { get; set; }
    //    public Byte DayLSB { get; set; }
    //    public Byte DayMSB { get; set; }
    //    public Boolean ChangedFCFToTest { get; set; }
    //}

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
            _typesByCode = new Dictionary<byte,MatchType>();
            using (StringReader sr = new StringReader(Fixtures.Properties.Resources.MatchTypes))
            {
                while (sr.Peek() >= 0)
                {
                    string line = sr.ReadLine();
                    if (line.Substring(0, 1) == "#") continue;

                    string[] row = line.Split(',');
                    Byte code = Byte.Parse(row[1], System.Globalization.NumberStyles.HexNumber);
                    Int32 numDays = Int32.Parse(row[2]);
                    MatchType format = new MatchType(row[0], code, numDays);
                    _types.Add(format);
                    _typesByCode.Add(code, format);
                }
            }
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

    public class Team : IComparable<Team>, IComparable
    {
        public static Team TBD;
        private static List<Team> _teams;
        private String _name;
        private Int32 _code;

        static Team()
        {
            TBD = new Team("T.B.D.", 0);
            _teams = new List<Team>();
            using (StringReader sr = new StringReader(Fixtures.Properties.Resources.Teams))
            {
                while (sr.Peek() >= 0)
                {
                    string[] row = sr.ReadLine().Split(',');
                    String name = row[0];
                    Int32 code = Int32.Parse(row[1], System.Globalization.NumberStyles.HexNumber);
                    _teams.Add(new Team(name, code));
                }
            }
            _teams.Add(TBD);
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

        public static Team FindByCode(Int32 code)
        {
            Team result = _teams.Where(t => t._code == code).FirstOrDefault();
            if (result == null)
            {
                result = TBD;
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
