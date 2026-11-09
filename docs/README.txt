CLASSROOM TOOLKIT 1.0
=====================

A free, open-source set of classroom tools for Windows: timers, traffic
lights, random pickers, quiz games, on-screen drawing and more. It is one
small program with nothing to install.

Copyright (c) 2026 Luke Harrison. Released under the MIT licence (see
LICENSE.txt): you may use, copy, change and share it freely, including in
schools and other organisations.


RUNNING IT
----------
Double-click ClassroomToolkit.exe.

Needs Windows 10 (version 1809 / LTSC 2019) or later, or Windows 11.
It uses .NET Framework 4, which is already part of those versions of
Windows, so there is nothing else to install and no administrator rights
are needed.

It starts quietly in the notification area beside the clock. Click its
icon (or double-click ClassroomToolkit.exe again) to open the launcher,
which lists every tool. Right-click the icon for the tools, "Start with
Windows", "Show window when starting" and Exit. Closing the launcher
leaves the program running in the notification area.

If you can't see the icon, click the small ^ arrow beside the clock;
you can drag the icon out of there so it is always visible.

The first time you copy the program to another computer, Windows may show
a "Windows protected your PC" message because the file is not signed.
Choose "More info" then "Run anyway", or ask IT to allow the file.


THE TOOLS
---------
Classroom
  Countdown Timer         presets or any time; Space starts and pauses
  Interval Timer          repeating work and rest periods
  Stopwatch               with lap times (L) that can be copied
  Exam Clock              clock, start, finish and extra-time finish
  Traffic Lights          click a light, or press R, A or G
  Random Student Picker   picks from a class list; can also make groups
  Dice                    one to six dice
  Random Number           any range, with or without repeats
  Scoreboard              two to six teams; click a name to rename it
  Plenary Prompts         question prompts at six levels of thinking
  Learning Outcomes       large-text note that stays on top
  Key Words               the same, for key words
  News Ticker             scrolling message; double-click it to edit

On-screen (these sit over whatever else is on the screen)
  Draw on Screen          freezes the screen so you can write over it
  Spotlight               dims everything except around the pointer
  Reading Guide           coloured band that follows the pointer
  Cover-up                movable panel to hide part of the screen

Quiz games (all use your own question sets)
  Flash Cards
  Noughts and Crosses Quiz
  Four in a Row Quiz
  Grid Quiz               categories and points, for one to four teams

Teacher
  Sticky Notes            stay on the desktop between sessions
  Register Reminders      a banner at set times of day
  Exam Countdown          days to go until exams and deadlines
  Learn Student Names     practise names from a folder of photos
  Report Statement Builder
  Special Characters      maths, Greek, accents, symbols
  Text Tidy               change case, sort, remove duplicates, totals

Learner
  Text to Speech          uses the voices built into Windows
  Speed Reader            one word at a time at a chosen speed
  Breathing Exercise
  Quotes and Goals

In most tools F11 switches full screen on and off, and "On top" keeps
the window above other programs.


YOUR LISTS
----------
Class lists, question sets, statement banks and quote lists are plain
text files. Each tool that uses them has Edit, New and Import buttons,
so you rarely need to touch the files directly.

They are kept in  Documents\Classroom Toolkit  in these folders:

  Classes      one student per line. "Sam Jones=3" gives Sam three
               tickets in the picker; "=0" leaves a student out.
  Questions    one question per line as   Question | Answer
               A line such as [Science] starts a category (Grid Quiz).
               Files that use Q=... and A=... lines also work, so class
               lists and question files from older tools can be imported.
  Statements   lines starting with # are section headings; every other
               line is a statement. Placeholders: {name} {subject} {he}
               {him} {his} {himself}, with {He} or {His} to start a
               sentence, and {is|are} style verb choices.
  Quotes       one per line, optionally followed by  | Author

Portable use (USB stick or shared drive): create an empty folder called
Data beside ClassroomToolkit.exe. If that folder exists and can be
written to, everything is kept there instead of in Documents.


FOR IT STAFF
------------
Deployment is a file copy. ClassroomToolkit.exe has no installer, no
dependencies beyond .NET Framework 4.x, writes only to the user's
Documents folder (or the Data folder), and never uses the network.

  ClassroomToolkit.exe            starts in the notification area
  ClassroomToolkit.exe --show     also opens the launcher window
  ClassroomToolkit.exe --tray     never opens the launcher at start-up

"Start with Windows" in the icon's menu adds a per-user Run entry
(HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ClassroomToolkit).


SOURCE CODE
-----------
The full source code, build instructions and issue tracker are on GitHub
in the classroom-tools repository. The program is written in plain C#
(Windows Forms) and can be rebuilt on any Windows PC with the build.bat
script there, using the compiler that is already part of Windows.

  ClassroomToolkit.exe --selftest C:\some\folder
      opens every tool once and saves a picture of each, with a log.
