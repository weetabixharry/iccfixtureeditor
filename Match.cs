// <!--Copyright (c) 2013
//    Isura Edirisinghe

//        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.-->
 
using System;
using System.ComponentModel;

namespace Fixtures
{
    public class Match : INotifyPropertyChanged
    {
        private readonly FixtureFileManager _manager;
        private Int32 _id;
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
                DateTime oldDate = _date;
                DateTime newDate = GetValidDate(value);
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
                _manager.UpdateMatchProperty(this, MatchProperty.MatchInSeries);
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
                _manager.UpdateMatchProperty(this, MatchProperty.SeriesLength);
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
                _manager.UpdateMatchProperty(this, MatchProperty.HostTeam);
                NotifyPropertyChanged("HostTeam");
            }
        }

        public Team VisitorTeam
        {
            get { return _visitorTeam; }
            set
            {
                _visitorTeam = value;
                _manager.UpdateMatchProperty(this, MatchProperty.VisitorTeam);
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

        private DateTime GetValidDate(DateTime date)
        {
            DateTime result = date;
            DateTime latestDate = _manager.LastDate.Subtract(new TimeSpan(_type.NumDays - 1, 0, 0, 0));
            if (result < _manager.FirstDate)
            {
                result = _manager.FirstDate;
            }
            else if (result > latestDate)
            {
                result = latestDate;
            }
            return result;
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
        MatchInSeries,
        SeriesLength,
        HostTeam,
        VisitorTeam
    }
}