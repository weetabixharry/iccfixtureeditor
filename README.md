iccfixtureeditor
================

Fixture editor for International Cricket Captain. Written in C# and WPF. 

If you simply want to use the editor download from https://iccfixtureeditor.codeplex.com/.

Features
========
* Edit fixtures in all career modes.
* Create custom tours and tournaments
* Add matches
* Remove matches
* Change match dates
* Change match type
* Change series length
* Change teams
* Supports International Cricket Captain 2012 and 2013

Editing Details
===============
* Fixture files for International Cricket Captain 2012 are stored in fxt folder in game directory (e.g. C:/Program Files (x86)/Childish Things/International Cricket Captain 2012/fxt)
* Fixture file is loaded at the beginning of a new season
* The file numbering determines the season (e.g. fix13.fxt is full career 2013-14 season and int15.fxt is international only 2015-16 season)
* You can rename the files in order to load a schedule from a different season (e.g. Rename int17.fxt to int12.txt and a new game will use the 2017-18 schedule)
