# Changelog

All notable changes to Classroom Toolkit. Versions follow
[Semantic Versioning](https://semver.org/). Your settings and lists always
carry over from one version to the next.

## Unreleased

### Changed
- Sharper icon at small sizes: the notification-area and taskbar icons use a
  simplified drawing with bigger lights at 16 to 32 pixels.

### Project
- A website, published to GitHub Pages from `site/`.
- New README, contributing guide, code of conduct, issue and pull request
  templates, branding (logo, icon sources) and screenshots.
- `build.bat test` builds and runs the tests; they now run in portable mode,
  so they never touch your own settings or lists.
- GitHub Actions runs the tests on every push, and release notes come from
  this changelog.

## 1.1.5 (2026-11-18)

### Fixed
- The notification-area and window icons always show the coloured traffic
  light. Before, running the program from a network share (or anywhere Windows
  wouldn't hand over the exe's icon) showed a plain stand-in of three white dots.
- The notification-area icon uses the small size from the icon file, so it
  stays sharp at any display scaling.

## 1.1.4 (2026-11-17)

### Changed
- Register Reminders are off until you switch on **Reminders on** in the tool.
  If you had already turned them on or off, your choice is kept.

## 1.1.3 (2026-11-12)

### Fixed
- Cover-up: double-clicking the cover closes it, as the on-screen hint says
  (the drag-to-move handling was swallowing the second click). Esc also
  closes it.

## 1.1.2 (2026-11-12)

### Changed
- Launcher redrawn: tiles share the full width of the window evenly, so there
  is no empty strip when maximised, and narrow windows drop to fewer columns.
- Category headings show how many tools they hold, with no large gap above the
  first row.
- Slim dark scrollbar (drag it, or click above or below it) and smooth
  mouse-wheel scrolling.
- Clearer message when a search finds nothing; the search box narrows instead
  of wrapping under the title in small windows.

### Added
- Keyboard: arrow keys move a highlight between tools, Enter opens it, Page Up
  and Page Down scroll, and Esc clears the search. Typing a search highlights
  the first match.

### Fixed
- Names and descriptions use the same text drawing, so letter spacing is even
  and the left edges line up.

## 1.1.1 (2026-11-12)

### Fixed
- Launcher: tool names line up exactly with their descriptions (on Windows the
  names were drawn a few pixels further right).

## 1.1.0 (2026-11-12)

A visual refresh of every tool, keeping them clear and legible from the back of
a classroom.

### Changed
- New shared controls: "- 5 +" steppers (click to type, mouse wheel to change),
  on/off switches, themed drop-downs and icon buttons with tooltips, replacing
  the old Windows spinners, tick boxes and menus.
- Keep-on-top and full screen are icons at the right of every toolbar. In full
  screen the toolbar hides itself and comes back when the mouse goes to the top
  edge.
- Dark title bars and right-click menus to match the tools.
- Large on-screen buttons (Start, Pick, Roll, Correct, Wrong and so on) that
  suit interactive whiteboards. In full screen, timer buttons fade while running.
- Countdown Timer: one time box (type 7:30), turns amber in the final minute and
  red for the last ten seconds, shows the finish time, and pulses gently instead
  of flashing; + and - add or remove a minute.
- Interval Timer: phase badge, skip-to-next button (N), time boxes for work and
  rest.
- Stopwatch: lap list with the fastest and slowest splits highlighted; R resets.
- Exam Clock: clock-style start time, and a progress line with the normal
  finish marked.
- Traffic Lights: lit lamps glow, and turn sideways automatically in a wide
  window.
- Random Student Picker, Dice and Random Number: on-screen Pick and Roll, recent
  picks shown as chips, groups shown as cards.
- Plenary Prompts: levels as coloured tabs on the display; Another and Any level
  buttons.
- Scoreboard: team-coloured cards and buttons and a Leading badge; keys 1 to 6
  add a point, Shift+1 to 6 take one off.
- Quiz games: redesigned question window (Esc cancels), turn banner, winning
  line or four highlighted; Four in a Row counters match the team colours, and
  Grid Quiz shows who took each square.
- Learning Outcomes and Key Words: themed toolbar instead of the old menu bar.
- Draw on Screen: round colour swatches, pen sizes and icon buttons. Sticky
  Notes, News Ticker, the reminder banner and the overlay controls restyled.
- Report Statement Builder: statements shown as cards previewing the finished
  sentence, numbered in the order picked.
- Special Characters: category buttons and large tiles that confirm "Copied".
- Text Tidy: grouped actions and Undo.
- Launcher: an icon for every tool, and a search box.

### Fixed
- Wrapped text is sized more carefully, so it never spills out of its card.

## 1.0.0 (2026-11-09)

First public release.

- 32 tools across five groups: Classroom, On-screen, Quiz games, Teacher and
  Learner.
- Starts in the notification area; optional **Start with Windows** and **Show
  window when starting**.
- Plain-text class lists, question sets, statement banks and quote lists, with
  Edit, New and Import in each tool.
- Portable mode using a `Data` folder beside the program.
- Builds with the C# compiler included in Windows (`build.bat`), or with the
  included `.csproj`.
- Released under the MIT licence.
