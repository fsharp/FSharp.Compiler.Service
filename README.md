F# Compiler Service

 * [F# Compiler Service documentation](http://fsharp.github.io/FSharp.Compiler.Service/)
 * [Developer notes explain the project structure](http://fsharp.github.io/FSharp.Compiler.Service/devnotes.html)

Build and Test
-----

.NET Framework:

   Install [.NET 4.5.1](http://www.microsoft.com/en-us/download/details.aspx?id=40779) and  [MSBuild 12.0](http://www.microsoft.com/en-us/download/details.aspx?id=40760)

    build.cmd All.NetFx 
    (unix: ./build.sh All.NetFx)

.NET Core

    build All.NetCore

Both:

    build All


Build Status
------------

Head (branch ``master``), Mono, Linux/OSX + unit tests (Travis) [![Build Status](https://travis-ci.org/fsharp/FSharp.Compiler.Service.png?branch=master)](https://travis-ci.org/fsharp/FSharp.Compiler.Service/branches)

Head (branch ``master``), Windows Server 2012 R2 + VS2015 + unit tests (AppVeyor)  [![Build status](https://ci.appveyor.com/api/projects/status/3yllu2qh19brk61d?svg=true)](https://ci.appveyor.com/project/fsgit/fsharp-compiler-service)

NuGet Feed  [![NuGet Badge](https://buildstats.info/nuget/FSharp.Compiler.Service)](https://www.nuget.org/packages/FSharp.Compiler.Service)

Stable builds are available in the NuGet Gallery:
[https://www.nuget.org/packages/FSharp.Compiler.Service](https://www.nuget.org/packages/FSharp.Compiler.Service)

All AppVeyor builds are available using the NuGet feed: https://ci.appveyor.com/nuget/fsgit-fsharp-compiler-service

If using Paket, add the source at the top of `paket.dependencies`.

Maintainers
-----------

The maintainers of this repository from the F# Core Engineering Group are:

 - [Don Syme](http://github.com/dsyme), [Dave Thomas](http://github.com/7sharp9), [Enrico Sada](http://github.com/enricosada)
 - with help and guidance from [Robin Neatherway](https://github.com/rneatherway), [Tomas Petricek](http://github.com/tpetricek), [Lincoln Atkinson](http://github.com/latkin), [Kevin Ransom](http://github.com/KevinRansom), [Vladimir Matveev](http://github.com/vladima) and others
