// <!--Copyright (c) 2014
//    Isura Edirisinghe

//        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.-->

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private const int DEFAULT_VERSION = 2016;
        private FixtureFileManager _fixtureFileMgr;

        public MainWindow()
        {
            InitializeComponent();
            showDebugColumns();
            gMatches = (Matches)Resources["matches"];
            MatchType.LoadTypes(DEFAULT_VERSION);
            Team.LoadTeams(DEFAULT_VERSION);
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

            Match newMatch = _fixtureFileMgr.AddNewMatch(selectedMatch.Date, selectedMatch.Type);
            matches.Insert(matches.IndexOf(selectedMatch), newMatch);
            _fixtureFileMgr.UpdateIds(matches);
            matchGrid.SelectedItem = matchGrid.Items[matches.IndexOf(newMatch)];            
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Matches matches = (Matches)Resources["matches"];
            Match selectedMatch = (Match)matchGrid.SelectedItem;
            if (_fixtureFileMgr == null || selectedMatch == null || matches.Count < 2)
                return;            
            _fixtureFileMgr.RemoveExistingMatch(selectedMatch);
            matches.Remove(selectedMatch);
            _fixtureFileMgr.UpdateIds(matches);            
        }

        private void Version_Selected(object sender, RoutedEventArgs e)
        {
            var selectedItem = (versionComboBox.SelectedItem as ComboBoxItem);
            // Hack: Since we want to set a default version this event is fired at startup but the
            // content is null for some reason.
            if (selectedItem.Content != null)
            {
                var version = Int32.Parse(selectedItem.Content.ToString());
                MatchType.LoadTypes(version);
                Team.LoadTeams(version);
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            // Uses a constructor that takes a parent window for the AboutBox.
            WPFAboutBox1 about = new WPFAboutBox1(this);
            about.ShowDialog();
        }

        private void showDebugColumns()
        {
#if DEBUG
            typeCode.Visibility = Visibility.Visible;
            hostCode.Visibility = Visibility.Visible;
            visitorCode.Visibility = Visibility.Visible;
#endif
        }
    }

    public class Matches : ObservableCollection<Match>
    {
        // Wrapper class so we can bind data in Xmal. 
    }    
}
