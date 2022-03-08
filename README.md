## Overview

Emet.Filesystems was created to solve two filesystem API problems that came up for
me about the same time. Since Microsoft has not been interested in addressing the
shortcomings of their APIs, I went ahead and did so.

From this came the other Emet modules; each one solves a simple API problem in a
simple manner, and work with the same high reliability Emet.Filesystems provides.
Simple sometimes means different things to different people. Abstractions should
not leak in surprising ways. The Emet family:

* Emet.FileSystems: APIs for operating on files, symbolic links, and hard links.

* Emet.VB: Extra extensions for working with Visual Basic's limitations, for working
with legacy Visual Basic code. There's little to no use importing this into C# programs.

* Emet.MultiCall: Library for assisting in making amalgumated single-file binaries
with multiple programs inside them.

[License](LICENSE)

## Philosophy

Filesystem action functions throw when presented with something they cannot do.

Filesystem query functions do not throw when presented with files or directories
that do not exist; however they do throw when presented with disk or network IO
issues. I found this is behavior is the most conducive to writing reliable code
in the face of unreliable power.

Emet.FileSystems does not call .NET System.IO functions but rather P/Invokes
native functions. Consequently, there is no platform-neutral build of Emet.FileSystems;
when you run `dotnet publish` or its moral equivalent on the executable project,
the appropriate binary is selected automatically and copied into the build output directory.

## Getting Started

Add Emet.FileSystems to your project. The current release is on nuget, so you
don't have to worry about building it yourself.

The typical entry point is `Emet.FileSystems.FileSystem` where useful functions
`GetDirectoryContents()`, `CreateHardLink()`, `CreateSymbolicLink()`, `ReadLink()`,
and `RenameReplace()` are found. It is also possible to examine a path directly
by creating an `Emet.FileSystems.DirectoryEntry` object.

There is copious documentation. The best way to read the documentation is to
add Emet.FileSystems to a project in Visual Studio and use Object Browser to
browse the public API and XML comments.

Individual IO errors may be caught and handled independently by writing code that
looks like this:

````csharp
    catch (System.IOException ioex) when (ioex.HResult == Emet.FileSystems.IOErrors.NotADirectory)
````

You would most likely use `using` directives to import namespaces, but the example is
easier to understand this way.

The members of `Emet.FileSystems.IOErrors` are magic readonly variables, not constants.
You can reference them from an `any` RID dll, upload that dll to your private nuget server
(or a public one for that matter), reference that dll from another dll in another codebase,
reference this dll from an executable compiled for some platform, and the members of
`Emet.FileSystems.IOErrors` will take on the correct value for the target platform.

## MultiCall

To make a multi-call binary, make all your other projects first and debug then normally in
Visual Studio (or whatever). Then make the multi-call project that has `<OutputType>exe</OutputType>`
and uses package reference to reference all the others. This package, and only this package
should have a reference to `Emet.MultiCall`. Your `Main(string[] args)` function should call
`Emet.MultiCall.Dispatch()`, passing it the `args[]` array and the array of tuples of the other
`Main` functions and their names. For this to work, the other projects should all declare their
`Main` functions `public` rather than the default `internal`.
