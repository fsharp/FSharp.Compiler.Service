#### 37.0.1

This release is a packaging fix release that updates the .Net Framework TFM requirement from 4.6.1 to 4.7.2, to be
in line with the new dependencies on Microsoft.Build.Framework, Microsoft.Build.Tasks.Core and Microsoft.Build.Utilities.Core

#### 37.0.0

This release bring a number of new changes, including a massive enhancement to SemanticClassification types thanks to @cartermp.

From dotnet/fsharp:333eb82c8..d9e070a9d:

* now allows for multiple implementations of generic interfaces (preview feature) (thanks @0x53A!)
* the default assembly set for script resolution now includes System.Numerics (thanks @KevinRansom)
* #r nuget package resolution is now committed eagerly, instead of delayed (thanks @KevinRansom)
* reduced allocations for some strings in the compiler (thanks @kerams)
* better printing for SRTP constraints (thanks @abelbraaksma/@realvictorprm)
* more expressive DUs for semantic classification (thanks @cartermp)
* remove legacymsbuildreferenceresolver (thanks @KevinRandom)
* supply witnesses for op_UnaryPlus (thanks @TIHan)
* clean up CE classifications (thanks @cartermp)
* Fixed tostring/equality for StringText (thanks @deviousasti)
* Fixed error text for FS0874
* Disallow assignment to C# Const/Readonly properties (thanks @baronfel)
* Allow Int64.MinValue as a valid nativeint literal (thanks @abelbraaksma)
* Enhancements to the nameof feature (preview feature)
* String interpolation (preview feature)

#### 36.0.3

This is a small bugfix release that fixes a nuspec package dependency issue with Sourcelink

#### 36.0.2

This is a small bugfix release that I'm making primarily to publish a version
of FCS with sourcelink enabled, so that tooling users can make use of that information.

From dotnet/fsharp:079276b4b..37d0cccec:

* Fixes for `do!` handling in computation expressions (thanks @baronfel)
* Add missing versions in FCS' Interactive header (thanks @nightroman)
* Support `Source`-translation in `match!` expressions (thanks @baronfel)
* Ensure stack traces from uncaught exceptions in CEs are maintained (thanks @NinoFloris)
* Better handling of `inline` in witness-passing codepaths (thanks @dsyme)
* Enable publishing of FCS with sourcelink (thanks @baronfel)
* Extend `nameof` to support naming generic parameters (`nameof<'t>`) and instance members (`nameof(Unchecked.defaultof<C>.Property)`) (thanks @dsyme)

#### 36.0.1

From dotnet/fsharp:522dd906c..16bca5aef:

* Fixes to attribute range parsing (thanks @auduchinok)
* Added nested exception details to Type Provider load errors (thanks @dsyme)
* Improved error messages around unknown identifiers in patterns (thanks @jbeeko)
* Better dependency probing behavior with Facade assemblies (thanks @KevinRansom)
* APIs for programmatically adding and retrieving bindings in an FSI session (thanks @TIHan)
* Optional parameter on `FSharpChecker.FindBackgroundReferencesInFile` API to allow for stale results (thanks @TIHan)
* Better tooltips for function arguments (thanks @mcon)
* Many performance tweaks to various compiler function implementations (thanks @forki)
* Typo fixes in the AST (thanks @auduchinok)
* Better IL emitted for usages of static members as System.Action (thanks @MoFtZ)
* Allow for indexers to specify a type annotation (thanks @TIHan)
* Allow languages/scripts that have no notion of upper-case to use their characters for DU identifiers (thanks @KevinRansom) 
* more optimized comparison/equality for DateTime (thanks @cartermp)
* add support for `char` for the `GenericZero/GenericOne` mechanisms (thanks @Happypig375)
* enhancements for the dependencymanager's resolution for native scripts (thanks @KevinRansom)
* more consistent support for type-directed nullable parameters (thanks @dsyme)
* fix FSI's ordering of out-of-order dlls in nuget packages (thanks @KevinRansom)
* New SyntaxTree.Paren syntax node (thanks @auduchinok)
* add SRTP witness solutions (via the new `CallWithWitnesses` pattern) (thanks @dsyme)


#### 35.0.0

This version bumps the major due to API surface area changes in the AST and TAST. In addition, there's a new package for the
built-in Nuget dependency manager for FSI: FSharp.DependencyManager.Nuget

Members are now in SyntaxTree/SyntaxTreeOps and TypedTree/TypedTreeBasics/TypedTreeOps/TypedTreePickle.

From dotnet/fsharp:d1a3d0705..522dd906c:

* Improved error recovery from patterns (thanks @auduchinok)
* Smaller IL Emit for unsigned 64-bit constants (thanks @teo-tsirpanis)
* Improved ProvidedTypes Type generation (thanks @DedSec256)
* Improved CodeLenses provided (thanks @realvictorprm)
* Optimize internal member calculations in PrettyNaming and items lookup (thanks @auduchinok)
* More fixes to compiler internals, ranges, etc (thanks @auduchinok)
* Support for consumption of C# Default Interfaces
* Better encapsulation of ProvidedExpr members (thanks @DedSec256)

#### 34.1.1

From dotnet/fsharp:3777cd4d8..836da28c0:

* Slight tweaks to error messages around numeric literals (Thanks @Happypig375)
* Deny taking native address of an immutable local value (Thanks @TIHan)
* Fixes to reported ranges for wildcard self-identifiers, module abbreviations, nested modules, attributes, nested types, and fields (Thanks @auduchinok)
* Better compiler error recovery for errors in constructor expressions (Thanks @auduchinok)
* Fix handling of F# Options in C# members with regards to nullable type interop (Thanks @TIHan)
* Move dependency handling of native dlls to the DependencyManager (Thanks @KevinRansom)

#### 34.1.0

From dotnet/fsharp:3af8959b6..9d69b49b7:

* set default script TFM to netcoreapp3.1 if none found
* improve C#-nullable and optional interop (RFC FS-1075)
* Add type name to `undefined name error` if known
* improve printing via %A/fsi
* misc. improvements to DependencyManager
* improve type provider support for .netcoreapp3.1 target frameworks.
* New, optimized find-all-references API with reduced memory usage.
* better error messages for failed overload resolution

#### 34.0.1

Contains commits from 32b124966 to d7018737c from dotnet/fsharp. Notable changes include:

* lowered allocations for large strings and char arrays (notably source file texts)
* improved support for byref-like rules with regards to type abbreviations
* better support for scopes in recursive modules
* better location of .net core reference assemblies
* lowered allocations for several internal compiler structures
* better error messages for anonymous record mismatches
* FSharpChecker learned how to keep background symbol uses
* Project cracker/project cracker tool were removed
* Better support for consuming C# in-ref parameters
* new services around simplifying names and finding unused declarations
* package management in scripts (in preview)
* and-bang syntax support (in preview)

#### 33.0.1

Integrate dotnet/fsharp from 4f5f08320 to 7b25d7f82. Notable changes include:

* Addition of the FsharpUnionCase.HasFields property
* FCS builds entirely on .Net Core now
* Better debug information for ranges
* Support for Literal values in signature files
* Using memory-mapped files cross-platform to read IL.

#### 33.0.0

Integrate dotnet/fsharp from 48f932cf8 to 085985140. Notable changes include:

    allowing '_' as a self-identifier
    events for FSI evaluation lifecycle events
    enhancements to FSI return-values
    fixed parsing for langversion CLI arguments
    allow cancellation of FSI interactions
    ToString fixes for value options
    Fixes for code generation in autogenerated members for very large records
    make ranges of member declarations capture the entire member
    improve error recovery in the parser
    improve logic for auto-detecting framework assemblies for FSI

#### 32.0.0

* integrate dotnet/fsharp from e1b8537ee to 48f932cf8
* notable changes include:
  * (preview) nameof
  * (preview) open static classes
  * Fixed 64-bit integer literal parsing
  * Better exhaustiveness checking for byte and sbyte pattern matches
  * Better native resource handling
  * Script-host assembly load events

#### 31.0.0
  * Integrate dotnet/fsharp from 5a8f454a1 to 05c558a61
  * Notable changes include:
    * Removal of the `Microsoft.FSharp.Compiler.SourceCodeServices` namespace
    * A new API for determining if an identifier needs to be quoted is available: `FSharp.Compiler.LexHelp.Keywords.DoesIdentifierNeedQuotation`
    * Enhancements to the correctness of PDBs
    * Better string formatting of records and values
    * More stack overflow fixes in the compiler
    * Inline IL parsing error handling
    * `LeafExpressionConverter` handles struct tuples
    * `FSharpChecker` now can parse a file without caching: `ParseFileNoCache`

#### 30.0.0
  * Integrate dotnet/fsharp from 25560f477 to 5a8f454a1
  * Notable improvements include:
    * performance improvements
    * FCS APIs for FSComp and Implement Interface
    * line directive handling
    * improved performance when computing quick fix suggestions

#### 29.0.1
  * Fix versioning of the assembly

#### 29.0.0
  * Integrate visualfsharp master from 165b736b9 (2019-03-29) to 25560f477 (2019-05-24)
  * Notable improvements include:
    * Improved Portable PDB debugging
    * Misc IL generation fixes
    * Representing inlined mutable variables in the AST
    * Moving on from net46 targeting
    * Fixes for anonymous record generation
    * Dependency updates
    * Checking for constructors in FSharpMemberOrFunctionOrValue
    * Fix unused opens false positive for record fields

#### 28.0.0
  * Integrate visualfsharp master from efb57cf56 to 8dfc02feb
  * Notable improvements include:
    * XmlDoc fixes for overloads
    * Fixes for deterministic compilation
    * Improved tail-recursion when processing large expressions
    * Better tooltip detection for operators with constraints
    * FSharp.Compiler.Service nuget now uses net461, netstandard2.0 and FSharp.Core 4.6.2
    * updated lexer and parser implementations to reduce stackoverflow likelihood on .net core

#### 27.0.1
  * Integrate visualfsharp master from 5a5ca976ec296d02551e79c3eb8e8db809e4304d to 2c8497bb097d5c5d3ef12f355594873838a48494
  * Notable improvements include:
    * Anonymous Record support for expressions
    * Union Case Naming fixes
    * Trimming of the nuget package dependencies from 26.0.1

#### 26.0.1
  * Integrate visualfsharp master to 99e307f3a3ef2109ba6542ffc58affe76fc0e2a0

#### 25.0.1
  * Integrate visualfsharp master to 15d9391e78c554f91824d2be2e69938cd811df68

#### 24.0.1
  * Integrate visualfsharp master to 59156db2d0a744233d1baffee7088ca2d9f959c7

#### 23.0.3
  * Clarify package authors

#### 23.0.1
  * Integrate visualfsharp master to ee938a7a5cfdf4849b091087efbf64605110541f

#### 22.0.3
  * [Add entity.DeclaringEntity](https://github.com/Microsoft/visualfsharp/pull/4633), [FCS feature request](https://github.com/fsharp/FSharp.Compiler.Service/issues/830)

#### 22.0.2
  * Use correct version number in DLLs (needed until https://github.com/Microsoft/visualfsharp/issues/3113 is fixed)

#### 22.0.1
  * Integrate visualfsharp master
  * Includes recent memory usage reduction work for ByteFile and ILAttributes

#### 21.0.1
  * Use new .NET SDK project files
  * FSharp.Compiler.Service nuget now uses net461 and netstandard2.0
  * FSharp.Compiler.Service netstandard2.0 now supports type providers
  
#### 20.0.1
  * Integrate visualfsharp master

#### 19.0.1
  * Rename ``LogicalEnclosingEntity`` to ``ApparentEnclosingEntity`` for consistency int he F# codebase terminology.
  * Rename ``EnclosingEntity`` to ``DeclaringEntity``.  In the case of extension properties, ``EnclosingEntity`` was incorrectly returning the logical enclosing entity (i.e. the type the property appears to extend), and in this case ``ApparentEnclosingEntity`` should be used instead.

#### 18.0.1
  * Integrate visualfsharp master

#### 17.0.2
  * Integrate visualfsharp master

#### 16.0.3
  * [File name deduplication not working with ParseAndCheckFileInProject](https://github.com/fsharp/FSharp.Compiler.Service/issues/819)

#### 16.0.2
  * [ProjectCracker returns *.fsi files in FSharpProjectOptions.SourceFiles array](https://github.com/fsharp/FSharp.Compiler.Service/pull/812)

  * [Fix line endings in the Nuget packages descriptions](https://github.com/fsharp/FSharp.Compiler.Service/pull/811)

#### 16.0.1
  * FSharpChecker provides non-reactor ParseFile instead of ParseFileInProject
  * Add FSharpParsingOptions, GetParsingOptionsFromProjectOptions, GetParsingOptionsFromCommandLine

#### 15.0.1
  * Integrate latest changes from visualfsharp
  * Add implementation file contents to CheckFileResults
  * Fix non-public API in .NET Standard 1.6 version

#### 14.0.1
  * Integrate latest changes from visualfsharp
  * Trial release for new build in fcs\...

#### 13.0.1
  * Move docs --> docssrc

#### 13.0.0
  * Move FSharp.Compiler.Service.MSBuild.v12.dll to a separate nuget package

#### 12.0.8
  * Set bit on output executables correctly

#### 12.0.7
  * Integrate visualfsharp master

#### 12.0.6
  * [758: Fix project cracker when invalid path given](https://github.com/fsharp/FSharp.Compiler.Service/pull/758)

#### 12.0.5
  * Remove dependency on System.ValueTuple

#### 12.0.3
  * [De-duplicate module names again](https://github.com/fsharp/FSharp.Compiler.Service/pull/749)

#### 12.0.2
  * De-duplicate module names

#### 12.0.1
  * [Integrate visualfsharp and fsharp](https://github.com/fsharp/fsharp/pull/696)

#### 11.0.10
  * [Fix F# Interactive on Mono 4.0.9+](https://github.com/fsharp/fsharp/pull/696)

#### 11.0.9
  * [Make incremental builder counter atomic](https://github.com/fsharp/FSharp.Compiler.Service/pull/724)
  * [Add IsValCompiledAsMethod to FSharpMemberOrFunctionOrValue](https://github.com/fsharp/FSharp.Compiler.Service/pull/727)
  * [Check before ILTypeInfo.FromType](https://github.com/fsharp/FSharp.Compiler.Service/issues/734)
  * [Transition over to dotnet cli Fsproj](https://github.com/fsharp/FSharp.Compiler.Service/issues/700)

#### 11.0.8
  * Depend on FSharp.Core package

#### 11.0.6
  * Fix [stack overflow exception](https://github.com/fsharp/FSharp.Compiler.Service/issues/672)

#### 11.0.4
  * Fix [out of range exception](https://github.com/fsharp/FSharp.Compiler.Service/issues/709)

#### 11.0.2
  * Integrate fsharp\fsharp and Microsoft\visualfsharp to 262deb017cfcd0f0d4138779ff42ede7dbf44c46

#### 11.0.1
  * Integrate fsharp\fsharp and Microsoft\visualfsharp to d0cc249b951374257d5a806939e42714d8a2f4c6

#### 10.0.3
  * [Expose assumeDotNetFramework in FSharpChecker.GetProjectOptionsFromScript](https://github.com/fsharp/FSharp.Compiler.Service/pull/699)
  * [SemanticClassificationType should not be internal](https://github.com/fsharp/FSharp.Compiler.Service/pull/696)

#### 10.0.1
  * [Adds FormatValue to FsiEvaluationSession, using the fsi object values for formatting](https://github.com/fsharp/FSharp.Compiler.Service/pull/686)

#### 10.0.0
  * Integrate fsharp\fsharp and Microsoft\visualfsharp to c3e55bf0b10bf08790235dc585b8cdc75f71618e
  * Integrate fsharp\fsharp and Microsoft\visualfsharp to 11c0a085c96a91102cc881145ce281271ac159fe
  * Some API changes for structured text provision for tagged structured text

#### 9.0.0
  * Update names of union fields in AST API
  * Fix load closure for ParseAndCheckInteraction
  * [Fix #631 compiler dependency on MSBuild](https://github.com/fsharp/FSharp.Compiler.Service/pull/657)
  * Fixed netcore codegen on Linux
  * Explicit error when cracker exe is missing

#### 8.0.0
  * Integrate fsharp\fsharp and Microsoft\visualfsharp to c494a9cab525dbd89585f7b733ea5310471a8001
  * Then integrate to 	2002675f8aba5b3576a924a2e1e47b18e4e9a83d
  * [Add module values to navigable items](https://github.com/fsharp/FSharp.Compiler.Service/pull/650)
  * Optionally remove dependency on MSBuild reference resolution https://github.com/fsharp/FSharp.Compiler.Service/pull/649
  * [Compiler api harmonise](https://github.com/fsharp/FSharp.Compiler.Service/pull/639)
  * Various bits of work on .NET Core version (buildable from source but not in nuget package)

#### 7.0.0
  * Integrate fsharp\fsharp and Microsoft\visualfsharp to 835b79c041f9032fceeceb39f680e0662cba92ec

#### 6.0.2
  * [Fix #568: recognize provided expressions](https://github.com/fsharp/FSharp.Compiler.Service/pull/568)

#### 6.0.1
  * [Fix ProjectFileNames order when getting project options from script](https://github.com/fsharp/FSharp.Compiler.Service/pull/594)

#### 6.0.0
  * Switch to new major version on assumption integrated F# compiler changes induce API change

#### 5.0.2
  * Integrate Microsoft\visualfsharp to 688c26bdbbfc766326fc45e4d918f87fcba1e7ba. F# 4.1 work

#### 5.0.1
* [Fixed dependencies in nuget package](https://github.com/fsharp/FSharp.Compiler.Service/pull/608)

#### 5.0.0
* Fixed empty symbol declared pdb #564 from kekyo/fix-empty-pdb
* .NET Core ProjectCracker - updated version and dependencies
* Properly embed 'FSIstrings' resource, fixes #591
* make build.sh work on windows (git bash).
* Added default script references for .NET Core
* Store useMonoResolution flag
* Updated MSBuild version
* Assume FSharp.Core 4.4.0.0

#### 4.0.1
* Integrate Microsoft\visualfsharp and fsharp\fsharp to master (including portable PDB)
* Remove .NET Framework 4.0 support (now needs .NET Framework 4.5)

#### 4.0.0
* Integrate Microsoft\visualfsharp and fsharp\fsharp to master

#### 3.0.0.0
* #538 - BackgroundCompiler takes a very long time on a big solution with a very connected project dependency graph
* #544 - Losing operator call when one of operands is application of a partially applied function
* #534 - Function valued property erasing calls
* #495 - Detupling missing when calling a module function value
* #543 - Tuple not being destructured in AST
* #541 - Results of multiple calls to active pattern are always bound to variable with same name
* #539 - BasicPatterns.NewDelegate shows same value for different arguments

#### 2.0.0.6
* #530 - Adjust ProjectCracker NuGet for VS/NuGet

#### 2.0.0.5
* #527 - Provide API that includes printf specifier arities along with ranges

#### 2.0.0.4
* #519 - Change nuget layout for ProjectCracker package
* #523 - Project cracking: spaces in file paths

#### 2.0.0.3
* #508 - Integrate visualfsharp/master removal of Silverlight #if
* #513 - Make CrackerTool `internal` to prevent accidental usage
* #515 - Add simple Visual Studio version detection for project cracker

#### 2.0.0.2
* Integrate visualfsharp/master and fsharp/master --> master
* Expose QualifiedName and FileName of FSharpImplementationFileContents
* Add FSharpErrorInfo.ErrorNumber

#### 2.0.0.1-beta
* Fix 452 - FSharpField.IsMutable = true for BCL enum cases
* Fix 414 - Add IsInstanceMemberInCompiledCode

#### 2.0.0.0-beta
* Feature #470, #478, #479 - Move ProjectCracker to separate nuget package and DLL, used ProjectCrackerTool.exe to run
* Feature #463 - Expose slot signatures of members in object expressions
* Feature #469, #475 - Add EvalExpressionNonThrowing, EvalInteractionNonThrowing, EvalScriptNonThrowing
* Fix #456 - FCS makes calls to kernel32.dll when running on OSX
* Fix #473 - stack overflow in resolution logic
* Fix #460 - Failure getting expression for a provided method call

#### 1.4.2.3 -
* Fix bug in loop optimization, apply https://github.com/Microsoft/visualfsharp/pull/756/

#### 1.4.2.2 -
* #488 - Performance problems with project references

#### 1.4.2.1 -
* #450 - Correct generation of ReferencedProjects

#### 1.4.2.0 -
* Fix bug in double lookup of cache, see https://github.com/fsharp/FSharp.Compiler.Service/pull/447

#### 1.4.1 -
* Add pause before backgrounnd work starts. The FCS request queue must be empty for 1 second before work will start
* Write trace information about the reactor queue to the event log
* Rewrite reactor to consistently prioritize queued work
* Implement cancellation for queued work if it is cancelled prior to being executed
* Adjust caching to check cache correctly if there is a gap before the request is executed

#### 1.4.0.9 -
* FSharpType.Format fix
* Disable maximum-memory trigger by default until use case ironed out

#### 1.4.0.8 -
* FSharpType.Format now prettifies type variables.  If necessary, FSharpType.Prettify can also be called
* Add maximum-memory trigger to downsize FCS caches. Defaults to 1.7GB of allocaed memory in the system
  process for a 32-bit process, and 2x this for a 64-bit process

#### 1.4.0.7 -
* fix 427 - Make event information available for properties which represent first-class uses of F#-declared events
* fix 410 - Symbols for C# fields (and especially enum fields)
* Expose implemented abstract slots
* Fix problem with obscure filenames caught by Microsoft\visualfsharp tests
* Integrate with visualfsharp master

#### 1.4.0.6 -
* fix 423 - Symbols for non-standard C# events
* fix 235 - XmlDocSigs for references assemblies
* fix 177 - GetAllUsesOfAllSymbolsInFile returns nothing for C# nested enum
* make Internal.Utilities.Text.Lexing.Position a struct
* Exposing assembly attributes on FSharpAssemblySignature
* clean up IncrementalFSharpBuild.frameworkTcImportsCache

#### 1.4.0.5 -
* add more entries to FSharpTokenTag

#### 1.4.0.4 -
* add more entries to FSharpTokenTag
* add PrettyNaming.QuoteIdentifierIfNeeded and PrettyNaming.KeywordNames

#### 1.4.0.3 -
* integrate Microsoft/visualfsharp OOB cleanup via fsharp/fsharp
* Make Parser and Lexer private

#### 1.4.0.2 -
* #387 - types and arrays in F# attribute contructor arguments

#### 1.4.0.1 - F# 4.0 support
* Use FSharp.Core 4.4.0.0 by default for scripting scenarios if not FSharp.Core referenced by host process

#### 1.4.0.0-beta - F# 4.0 support
* Integrate F# 4.0 support into FSharp.Compiler.Service

#### 1.3.1.0 -
* simplified source indexing with new SourceLink
* Add noframework option in AST compiler methods

#### 0.0.90 -
* Add fix for #343 Use ResolveReferences task
* Expose BinFolderOfDefaultFSharpCompiler to editors
* Fix the registry checking on mono to avoid unnecessary exceptions being thrown

#### 0.0.89 -
* Fix output location of referenced projects

#### 0.0.88 -
* Added Fix to allow implicit PCL references to be retrieved

#### 0.0.87 -
* Don't report fake symbols in indexing #325
* Add EnclosingEntity for an active pattern group #327
* Add ImmediateSubExpressions #284
* integrate fsharp/fsharp master into master

#### 0.0.85 -
* Fix for FSharpSymbolUse for single case union type #301
* Added supprt for ReturnParameter in nested functions

#### 0.0.84 -
* Added curried parameter groups for nested functions

#### 0.0.83 -
* Add Overloads to the symbols signature so it is publicly visible
* Update OnEvaluation event to have FSharpSymbolUse information available

#### 0.0.82 -
* Better support for Metadata of C# (and other) Assemblies.
* Expose the DefaultFileSystem as a type instead of anonymous

#### 0.0.81 -
* Update GetDeclarationListSymbols to expose FSharpSymbolUse
* Improve reporting of format specifiers

#### 0.0.80 -
* Update to latest F# 3.1.3 (inclunding updated FsLex/FsYacc used in build of FCS)
* Report printf specifiers from Service API
* Improve Accessibility of non-F# symbols

#### 0.0.79 -
* Do not use memory mapped files when cracking a DLL to get an assembly reference
* Fix for multilanguage projects in project cracker

#### 0.0.78 -
* Reduce background checker memory usage
* add docs on FSharp.Core
* docs on caches and queues

#### 0.0.77 -
* Update to github.com/fsharp/fsharp 05f426cee85609f2fe51b71473b07d7928bb01c8

#### 0.0.76 -
* Fix #249 - Fix TryFullName when used on namespaces of provided erased type definitions
* Add OnEvaluation event to FCS to allow detailed information to be exposed

#### 0.0.75 -
* Do not use shared cursor for IL binaries (https://github.com/fsprojects/VisualFSharpPowerTools/issues/822)

#### 0.0.74 -
* Extension members are returned as members of current modules
* Fix exceptions while cross-reference a type provider project

#### 0.0.73 -
* Add AssemblyContents and FSharpExpr to allow access to resolved, checked expression trees
* Populate ReferencedProjects using ProjectFileInfo
* Fix finding symbols declared in signature files
* Add logging to project cracking facility

#### 0.0.72 -
* Allow project parser to be used on project file with relative paths
* Expose attributes for non-F# symbols

#### 0.0.71 -
* More renamings in SourceCodeServices API for more consistent use of 'FSharp' prefix

#### 0.0.70 -
* Make FSharpProjectFileParser public
* Fixes to project parser for Mono (.NET 4.0 component)
* Renamings in SourceCodeServices API for more consistent use of 'FSharp' prefix

#### 0.0.67 -
* Fixes to project parser for Mono

#### 0.0.66 -
* Fixes to project parser for Mono
* Use MSBuild v12.0 for reference resolution on .NET 4.5+

#### 0.0.65 -
* Fixes to project parser

#### 0.0.64 -
* Add project parser, particularly GetProjectOptionsFromProjectFile

#### 0.0.63 -
* #221 - Normalize return types of .NET events

#### 0.0.62 -
* Integrate to latest https://github.com/fsharp/fsharp (#80f9221f811217bd890b3a670d717ebc510aeeaf)

#### 0.0.61 -
* #216 - Return associated getters/setters from F# properties
* #214 - Added missing XmlDocSig for FSharpMemberOrFunctionOrValue's Events, Methods and Properties
* #213 - Retrieve information for all active pattern cases
* #188 - Fix leak in file handles when using multiple instances of FsiEvaluationSession, and add optionally collectible assemblies

#### 0.0.60 -
* #207 - Add IsLiteral/LiteralValue to FSharpField
* #205 - Add IsOptionalArg and related properties to FSharpParameter
* #210 - Check default/override members via 'IsOverrideOrExplicitMember'
* #209 - Add TryFullName to FSharpEntity

#### 0.0.59 -
* Fix for #184 - Fix EvalScript by using verbatim string for #Load
* Fix for #183 - The line no. reporting is still using 0-based indexes in errors. This is confusing.

#### 0.0.58 -
* Fix for #156 - The FSharp.Core should be retrieved from the hosting environment

#### 0.0.57 -
* Second fix for #160 - Nuget package now contains .NET 4.0 and 4.5

#### 0.0.56 -
* Fix for #160 - Nuget package contains .NET 4.0 and 4.5

#### 0.0.55 -
* Integrate changes for F# 3.1.x, Fix #166

#### 0.0.54 -
* Fix for #159 - Unsubscribe from TP Invalidate events when disposing builders

#### 0.0.53 -
* Add queue length to InteractiveChecker

#### 0.0.52 -
* Fix caches keeping hold of stale entries

#### 0.0.51 -
* Add IsAccessible to FSharpSymbol, and ProjectContext.AccessibilityRights to give the context of an access

#### 0.0.50 -
* Fix #79 - FindUsesOfSymbol returns None at definition of properties with explicit getters and setters

#### 0.0.49 -
* Fix #138 - Fix symbol equality for provided type members
* Fix #150 - Return IsGetterMethod = true for declarations of F# properties (no separate 'property' symbol is yet returned, see #79)
* Fix #132 - Add IsStaticInstantiation on FSharpEntity to allow clients to detect fake symbols arising from application of static parameters
* Fix #154 - Add IsArrayType on FSharpEntity to allow clients to detect the symbols for array types
* Fix #96 - Return resolutions of 'Module' and 'Type' in "Module.field" and "Type.field"

#### 0.0.48 -
* Allow own fsi object without referencing FSharp.Compiler.Interactive.Settings.dll (#127)

#### 0.0.47 -
* Adjust fix for #143 for F# types with abstract+default events

#### 0.0.46 -
* Fix multi-project analysis when referenced projects have changed (#141)
* Fix process exit on bad arguments to FsiEvaluationSession (#126)
* Deprecate FsiEvaluationSession constructor and add FsiEvaluationSession.Create static method to allow for future API that can return errors
* Return additional 'property' and 'event' methods for F#-defined types to regularize symbols (#108, #143)
* Add IsPropertySetterMethod and IsPropertyGetterMethod which only return true for getter/setter methods, not properties. Deprecate IsSetterMethod and IsGetterMethod in favour of these.
* Add IsEventAddMethod and IsEventRemoveMethod which return true for add/remove methods with an associated event
* Change IsProperty and IsEvent to only return true for the symbols for properties and events, rather than the methods associated with these
* Fix value of Assembly for some symbols (e.g. property symbols)

#### 0.0.45 -
* Add optional project cache size parameter to InteractiveChecker
* Switch to openBinariesInMemory for SimpleSourceCodeServices
* Cleanup SimpleSourceCodeServices to avoid code duplication

#### 0.0.44 -
* Integrate latest changes from visualfsharp.codeplex.com via github.com/fsharp/fsharp
* Fix problem with task that generates description text of declaration
* Add AllInterfaceTypes to FSharpEntity and FSharpType
* Add BaseType to FSharpType to propagate instantiation
* Add Instantiate to FSharpType

#### 0.0.43 -
* Fix #109 - Duplicates in GetUsesOfSymbolInFile

#### 0.0.42 -
* Fix #105 - Register enum symbols in patterns
* Fix #107 - Return correct results for inheritance chain of .NET types
* Fix #101 - Add DeclaringEntity property

#### 0.0.41 -
* Fixed #104 - Make all operations that may utilize the FCS reactor async
* Add FSharpDisplayContext and FSharpType.Format
* Replace GetSymbolAtLocationAlternate by GetSymbolUseAtLocation

#### 0.0.40 -
* Fixed #86 - Expose Microsoft.FSharp.Compiler.Interactive.Shell.Settings.fsi
* Fixed #99 - Add IsNamespace property to FSharpEntity

#### 0.0.39 -
* Fixed #79 - Usage points for symbols in union patterns

#### 0.0.38 -
* Fixed #94 and #89 by addition of new properties to the FSharpSymbolUse type
* Fixed #93 by addition of IsOpaque to FSharpEntity type
* Fixed #92 - Issue with nested classes
* Fixed #87 - Allow analysis of members from external assemblies

#### 0.0.37 -
* Obsolete HasDefaultValue - see https://github.com/fsharp/FSharp.Compiler.Service/issues/77

#### 0.0.36 -
* Fix #71 - Expose static parameters and xml docs of type providers
* Fix #63 - SourceCodeServices: #r ignores include paths passed as command-line flags

#### 0.0.35 -
* Fix #38 - FSharp.Compiler.Services should tolerate an FSharp.Core without siginfo/optdata in the search path


#### 0.0.34 -
* Add StaticParameters property to entities, plus FSharpStaticParameter symbol
* Fix #65

#### 0.0.33 -
* Add FullName and Assembly properties for symbols
* Fix #76
* Add Japanese documentation

#### 0.0.32 -
* Make ParseFileInProject asynchronous
* Add ParseAndCheckFileInProject
* Use cached results in ParseAndCheckFileInProject if available

#### 0.0.31 -
* Fix performance problem with CheckFileInProject

#### 0.0.30 -
* Add initial prototype version of multi-project support, through optional ProjectReferences in ProjectOptions. Leave this empty
  to use DLL/file-based references to results from other projects.

#### 0.0.29 -
* Fix symbols for named union fields in patterns

#### 0.0.28 -
* Fix symbols for named union fields
* Add FSharpActivePatternCase to refine FSharpSymbol

#### 0.0.27 -
* Fix exception tag symbol reporting

#### 0.0.26 -
* Fix off-by-one in reporting of range for active pattern name

#### 0.0.25 -
* Add optional source argument to TryGetRecentTypeCheckResultsForFile to specify that source must match exactly

#### 0.0.24 -
* Update version number as nuget package may not have published properly

#### 0.0.23 -
* Move to one-based line numbering everywhere
* Provide better symbol information for active patterns

#### 0.0.22 -
* Provide symbol location for type parameters

#### 0.0.21 -
* Add GetUsesOfSymbolInFile
* Better symbol resolution results for type parameter symbols

#### 0.0.20 -
* Update version number as nuget package may not have published properly

#### 0.0.19 -
* Change return type of GetAllUsesOfSymbol, GetAllUsesOfAllSymbols and GetAllUsesOfAllSymbolsInFile to FSharpSymbolUse
* Add symbol uses when an abstract member is implemented.

#### 0.0.18 -
* Add GetAllUsesOfAllSymbols and GetAllUsesOfAllSymbolsInFile

#### 0.0.17 -
* Improvements to symbol accuracy w.r.t. type abbreviations

#### 0.0.16 -
* Make FSharpEntity.BaseType return an option
* FsiSesion got a new "EvalScript" method which allows to evaluate .fsx files

#### 0.0.15 -
* Update version number as nuget package may not have published properly

#### 0.0.14 -
* Update version number as nuget package may not have published properly

#### 0.0.13-alpha -
* Fix #39 - Constructor parameters are mistaken for record fields in classes

#### 0.0.12-alpha -
* Make the parts of the lexer/parser used by 'XmlDoc' tools in F# VS Power tools public

#### 0.0.11-alpha -
* Add 'IsUnresolved'

#### 0.0.10-alpha -
* Fix bug where 'multiple references to FSharp.Core' was given as error for scripts

#### 0.0.9-alpha -
* Fix fsc corrupting assemblies when generating pdb files (really)
* Give better error messages for missing assemblies
* Report more information about symbols returned by GetSymbolAtLocation (through subtypes)
* Fix typos in docs
* Return full project results from ParseAndCheckInteraction
* Be more robust to missing assembly references by default.

#### 0.0.8-alpha -
* Fix fsc corrupting assemblies when generating pdb files

#### 0.0.7-alpha -
* Fix docs
* Make symbols more robust to missing assemblies
* Be robust to failures on IncrementalBuilder creation
* Allow use of MSBuild resolution by IncrementalBuilder

#### 0.0.6-alpha -
* Fix version number

#### 0.0.5-alpha -
* Added GetUsesOfSymbol(), FSharpSymbol type, GetSymbolAtLocation(...)

#### 0.0.4-alpha -
* Added documentation of file system API
* Reporte errors correctly from ParseAndCheckProject


#### 0.0.3-alpha -
* Integrate FSharp.PowerPack.Metadata as the FSharp* symbol API
* Renamed Param --> MethodGroupItemParameter and hid record from view, made into an object
* Renamed Method --> MethodGroupItem and hid record from view, made into an object
* Renamed Methods --> MethodGroup and hid record from view, made into an object
* Renamed MethodGroup.Name --> MethodGroup.MethodName
* Renamed DataTip --> ToolTip consistently across all text
* Renamed CheckOptions --> ProjectOptions
* Renamed TypeCheckAnswer --> CheckFileAnswer
* Renamed UntypedParseInfo --> ParseFileResults
* Removed GetCheckOptionsFromScriptRoot member overload in favour of optional argument
* Renamed GetCheckOptionsFromScriptRoot --> GetProjectOptionsFromScript
* Renamed UntypedParse --> ParseFileInProject
* Renamed TypeCheckSource --> CheckFileInProjectIfReady
* Added numerous methods to API including CheckFileInProject
* Added experimental GetBackgroundCheckResultsForFileInProject, GetBackgroundParseResultsForFileInProject
* Added PartialAssemblySignature to TypeCheckResults/CheckFileResults
* Added CurrentPartialAssemblySignature to FsiEvaluationSession
* Added ParseAndCheckInteraction to FsiEvaluationSession to support intellisense implementation against a script fragment
* Added initial testing in tests/service
* Added ParseAndCheckProject to SourceCodeServices API. This will eventually return "whole project" information such as symbol tables.
* Added GetDefaultConfiguration to simplify process of configuring FsiEvaluationSession
* Added PartialAssemblySignatureUpdated event to FsiEvaluationSession
* Added travis build

#### 0.0.2-alpha -
* Integrate hosted FSI configuration, SimpleSourceCodeServices, cleanup to SourceCodeServices API


