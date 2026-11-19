# Contributing to Classroom Toolkit

Thanks for helping. Classroom Toolkit is a small project, so every bug report,
idea from a classroom and pull request makes a real difference.

## Reporting bugs

[Open an issue](https://github.com/LukeHarrison1990/classroom-tools/issues/new/choose) with:

- the tool, and what you did, expected and saw;
- your Windows version and display scaling (for example *Windows 10 LTSC 2019, 125%*);
- the Classroom Toolkit version (right-click the icon by the clock and choose **About**);
- whether it runs from the computer, a network share or a portable `Data` folder;
- a screenshot, if it's about how something looks.

## Suggesting a tool

Tell us about the lesson moment it would help with, not just the feature.
New tools should be quick to pick up mid-lesson, readable from the back of
the room and useful in more than one subject.

## Building and testing

Everything builds with the C# compiler that ships with Windows, so there is
nothing to install:

```
build.bat           builds ClassroomToolkit.exe
build.bat test      builds and runs the tests in tests\
```

The tests run in portable mode (a `tests\Data` folder), so they never touch
your own settings or lists. Run them before sending a pull request.

You can also open `ClassroomToolkit.csproj` in Visual Studio 2022 or build it
with `dotnet build`.

`ClassroomToolkit.exe --selftest <folder>` opens every tool once and saves a
screenshot of each, which is a quick way to check nothing has broken visually.

## Code

- **C# 5 and .NET Framework 4.5 APIs only.** The program must build with the
  `csc.exe` that is part of Windows and run on Windows 10 1809 (LTSC 2019)
  without installing anything. Don't use newer language features (string
  interpolation, `?.`, expression-bodied members and so on) or NuGet packages.
- **One exe, no dependencies.** No installers, no extra DLLs, no network access.
- **Classroom first.** Big, high-contrast text; controls that work on an
  interactive whiteboard; keyboard shortcuts for the teacher at the desk.
  Use the shared controls in `src/Controls.cs` and the colours in `Theme`
  (`src/Ui.cs`) so every tool looks and behaves the same.
- **Settings are only ever added**, never renamed, so people's saved choices
  carry over between versions.
- Keep the existing formatting (`.editorconfig` is in the root) and the
  CRLF line endings in `.cs` files.
- New files start with the copyright and `SPDX-License-Identifier: MIT` lines
  used elsewhere.

## Adding a tool

1. Add a form class (usually a `ToolForm` subclass) in the `src/` file for its
   group: `Classroom.cs`, `Overlays.cs`, `Games.cs`, `Teacher.cs` or `Learner.cs`.
2. Register it in `Tools.Register()` in `src/Program.cs`, and give it an icon
   in the `Icons` table there.
3. Add a test in `tests/Tests.cs` for any logic that can be checked without
   looking at the screen.
4. Add it to the tool tables in `README.md` and `docs/package-readme.txt`, and
   a line under *Unreleased* in `CHANGELOG.md`.

## Releasing (maintainers)

1. Move the *Unreleased* notes in `CHANGELOG.md` under a new version heading,
   such as `## 1.2.0 (2026-12-01)`.
2. Set the same version in the three `Assembly...Version` lines at the top of
   `src/Program.cs`.
3. Commit, then tag and push: `git tag v1.2.0` and `git push origin v1.2.0`.

GitHub Actions builds and tests it, then publishes the release with the zip
attached and that version's changelog section as the notes. The build stops if
the tag and the version in `src/Program.cs` don't match.

## Original work only

Classroom Toolkit is an independent project. Don't contribute code, artwork,
sounds or text copied from other products, or anything made by taking their
programs apart. Published ideas, open-licensed material (with credit) and
your own work are all welcome.

## Licence

By contributing, you agree that your contribution is released under the
[MIT licence](LICENSE), like the rest of the project.
