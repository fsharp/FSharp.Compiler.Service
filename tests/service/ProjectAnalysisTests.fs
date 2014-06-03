﻿#if INTERACTIVE
#r "../../bin/FSharp.Compiler.Service.dll"
#r "../../packages/NUnit.2.6.3/lib/nunit.framework.dll"
#load "FsUnit.fs"
#load "Common.fs"
#else
module FSharp.Compiler.Service.Tests.ProjectAnalysisTests
#endif


open NUnit.Framework
open FsUnit
open System
open System.IO
open System.Collections.Generic

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

open FSharp.Compiler.Service.Tests.Common

// Create an interactive checker instance 
let checker = InteractiveChecker.Create()

/// Extract range info 
let tups (m:Range.range) = (m.StartLine, m.StartColumn), (m.EndLine, m.EndColumn)

/// Extract range info  and convert to zero-based line  - please don't use this one any more
let tupsZ (m:Range.range) = (m.StartLine-1, m.StartColumn), (m.EndLine-1, m.EndColumn)

let attribsOfSymbolUse (s:FSharpSymbolUse) = 
    [ if s.IsFromDefinition then yield "defn" 
      if s.IsFromType then yield "type"
      if s.IsFromAttribute then yield "attribute"
      if s.IsFromDispatchSlotImplementation then yield "override"
      if s.IsFromPattern then yield "pattern" 
      if s.IsFromComputationExpression then yield "compexpr" ] 

let attribsOfSymbol (s:FSharpSymbol) = 
    [ match s with 
        | :? FSharpEntity as v -> 
            if v.IsNamespace then yield "namespace"
            if v.IsFSharpModule then yield "module"
            if v.IsByRef then yield "byref"
            if v.IsClass then yield "class"
            if v.IsDelegate then yield "delegate"
            if v.IsEnum then yield "enum"
            if v.IsFSharpAbbreviation then yield "abbrev"
            if v.IsFSharpExceptionDeclaration then yield "exn"
            if v.IsFSharpRecord then yield "record"
            if v.IsFSharpUnion then yield "union"
            if v.IsInterface then yield "interface"
            if v.IsMeasure then yield "measure"
            if v.IsProvided then yield "provided"
            if v.IsProvidedAndErased then yield "erased"
            if v.IsProvidedAndGenerated then yield "generated"
            if v.IsUnresolved then yield "unresolved"
            if v.IsValueType then yield "valuetype"

        | :? FSharpMemberFunctionOrValue as v -> 
            if v.IsActivePattern then yield "apat"
            if v.IsDispatchSlot then yield "slot"
            if v.IsModuleValueOrMember && not v.IsMember then yield "val"
            if v.IsMember then yield "member"
            if v.IsProperty then yield "prop"
            if v.IsExtensionMember then yield "extmem"
            if v.IsPropertyGetterMethod then yield "getter"
            if v.IsPropertySetterMethod then yield "setter"
            if v.IsEvent then yield "event"
            if v.IsEventAddMethod then yield "add"
            if v.IsEventRemoveMethod then yield "remove"
            if v.IsTypeFunction then yield "typefun"
            if v.IsCompilerGenerated then yield "compgen"
            if v.IsImplicitConstructor then yield "ctor"
            if v.IsMutable then yield "mutable" 
        | _ -> () ]

module Project1 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let fileName2 = Path.ChangeExtension(base2, ".fs")
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module M

type C() = 
    member x.P = 1

let xxx = 3 + 4
let fff () = xxx + xxx

type CAbbrev = C
    """
    File.WriteAllText(fileName1, fileSource1)

    let fileSource2 = """
module N

open M

type D1() = 
    member x.SomeProperty = M.xxx

type D2() = 
    member x.SomeProperty = M.fff() + D1().P

// Generate a warning
let y2 = match 1 with 1 -> M.xxx

// A class with some 'let' bindings
type D3(a:int) = 
    let b = a + 4

    [<DefaultValue(false)>]
    val mutable x : int

    member x.SomeProperty = a + b

let pair1,pair2 = (3 + 4 + int32 System.DateTime.Now.Ticks, 5 + 6)

// Check enum values
type SaveOptions = 
  | None = 0
  | DisableFormatting = 1

let enumValue = SaveOptions.DisableFormatting

let (++) x y = x + y
    
let c1 = 1 ++ 2

let c2 = 1 ++ 2

let mmmm1 : M.C = new M.C()             // note, these don't count as uses of CAbbrev
let mmmm2 : M.CAbbrev = new M.CAbbrev() // note, these don't count as uses of C

    """
    File.WriteAllText(fileName2, fileSource2)

    let fileNames = [fileName1; fileName2]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)
    let cleanFileName a = if a = fileName1 then "file1" else if a = fileName2 then "file2" else "??"

let rec allSymbolsInEntities compGen (entities: IList<FSharpEntity>) = 
    [ for e in entities do 
          yield (e :> FSharpSymbol) 
          for gp in e.GenericParameters do 
            if compGen || not gp.IsCompilerGenerated then 
             yield (gp :> FSharpSymbol)
          for x in e.MembersFunctionsAndValues do
             if compGen || not x.IsCompilerGenerated then 
               yield (x :> FSharpSymbol)
             for gp in x.GenericParameters do 
              if compGen || not gp.IsCompilerGenerated then 
               yield (gp :> FSharpSymbol)
          for x in e.UnionCases do
             yield (x :> FSharpSymbol)
             for f in x.UnionCaseFields do
                 if compGen || not f.IsCompilerGenerated then 
                     yield (f :> FSharpSymbol)
          for x in e.FSharpFields do
             if compGen || not x.IsCompilerGenerated then 
                 yield (x :> FSharpSymbol)
          yield! allSymbolsInEntities compGen e.NestedEntities ]



[<Test>]
let ``Test project1 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    wholeProjectResults .Errors.Length |> shouldEqual 2
    wholeProjectResults.Errors.[1].Message.Contains("Incomplete pattern matches on this expression") |> shouldEqual true // yes it does

    wholeProjectResults.Errors.[0].StartLineAlternate |> shouldEqual 10
    wholeProjectResults.Errors.[0].EndLineAlternate |> shouldEqual 10
    wholeProjectResults.Errors.[0].StartColumn |> shouldEqual 43
    wholeProjectResults.Errors.[0].EndColumn |> shouldEqual 44

[<Test>]
let ``Test project1 basic`` () = 


    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously

    set [ for x in wholeProjectResults.AssemblySignature.Entities -> x.DisplayName ] |> shouldEqual (set ["N"; "M"])

    [ for x in wholeProjectResults.AssemblySignature.Entities.[0].NestedEntities -> x.DisplayName ] |> shouldEqual ["D1"; "D2"; "D3"; "SaveOptions" ]

    [ for x in wholeProjectResults.AssemblySignature.Entities.[1].NestedEntities -> x.DisplayName ] |> shouldEqual ["C"; "CAbbrev"]

    set [ for x in wholeProjectResults.AssemblySignature.Entities.[0].MembersFunctionsAndValues -> x.DisplayName ] 
        |> shouldEqual (set ["y2"; "pair2"; "pair1"; "( ++ )"; "c1"; "c2"; "mmmm1"; "mmmm2"; "enumValue" ])

[<Test>]
let ``Test project1 all symbols`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities true wholeProjectResults.AssemblySignature.Entities
    for s in allSymbols do 
        s.DeclarationLocation.IsSome |> shouldEqual true

    let allDeclarationLocations = 
        [ for s in allSymbols do 
             let m = s.DeclarationLocation.Value
             yield s.ToString(), Project1.cleanFileName  m.FileName, (m.StartLine, m.StartColumn), (m.EndLine, m.EndColumn ), attribsOfSymbol s
            ]

    allDeclarationLocations |> shouldEqual
          [("N", "file2", (2, 7), (2, 8), ["module"]);
           ("val y2", "file2", (13, 4), (13, 6), ["val"]);
           ("val pair2", "file2", (24, 10), (24, 15), ["val"]);
           ("val pair1", "file2", (24, 4), (24, 9), ["val"]);
           ("val enumValue", "file2", (31, 4), (31, 13), ["val"]);
           ("val op_PlusPlus", "file2", (33, 5), (33, 7), ["val"]);
           ("val c1", "file2", (35, 4), (35, 6), ["val"]);
           ("val c2", "file2", (37, 4), (37, 6), ["val"]);
           ("val mmmm1", "file2", (39, 4), (39, 9), ["val"]);
           ("val mmmm2", "file2", (40, 4), (40, 9), ["val"]);
           ("D1", "file2", (6, 5), (6, 7), ["class"]);
           ("member .ctor", "file2", (6, 5), (6, 7), ["member"; "ctor"]);
           ("member get_SomeProperty", "file2", (7, 13), (7, 25),
            ["member"; "getter"]);
           ("property SomeProperty", "file2", (7, 13), (7, 25),
            ["member"; "prop"]); ("D2", "file2", (9, 5), (9, 7), ["class"]);
           ("member .ctor", "file2", (9, 5), (9, 7), ["member"; "ctor"]);
           ("member get_SomeProperty", "file2", (10, 13), (10, 25),
            ["member"; "getter"]);
           ("property SomeProperty", "file2", (10, 13), (10, 25),
            ["member"; "prop"]); ("D3", "file2", (16, 5), (16, 7), ["class"]);
           ("member .ctor", "file2", (16, 5), (16, 7), ["member"; "ctor"]);
           ("member get_SomeProperty", "file2", (22, 13), (22, 25),
            ["member"; "getter"]);
           ("property SomeProperty", "file2", (22, 13), (22, 25),
            ["member"; "prop"]); ("field a", "file2", (16, 8), (16, 9), []);
           ("field b", "file2", (17, 8), (17, 9), []);
           ("field x", "file2", (20, 16), (20, 17), []);
           ("SaveOptions", "file2", (27, 5), (27, 16), ["enum"; "valuetype"]);
           ("field value__", "file2", (28, 2), (29, 25), []);
           ("field None", "file2", (28, 4), (28, 8), []);
           ("field DisableFormatting", "file2", (29, 4), (29, 21), []);
           ("M", "file1", (2, 7), (2, 8), ["module"]);
           ("val xxx", "file1", (7, 4), (7, 7), ["val"]);
           ("val fff", "file1", (8, 4), (8, 7), ["val"]);
           ("C", "file1", (4, 5), (4, 6), ["class"]);
           ("member .ctor", "file1", (4, 5), (4, 6), ["member"; "ctor"]);
           ("member get_P", "file1", (5, 13), (5, 14), ["member"; "getter"]);
           ("property P", "file1", (5, 13), (5, 14), ["member"; "prop"]);
           ("CAbbrev", "file1", (10, 5), (10, 12), ["abbrev"]);
           ("property P", "file1", (5, 13), (5, 14), ["member"; "prop"])]

    for s in allSymbols do 
        s.ImplementationLocation.IsSome |> shouldEqual true

    let allImplementationLocations = 
        [ for s in allSymbols do 
             let m = s.ImplementationLocation.Value
             yield s.ToString(), Project1.cleanFileName  m.FileName, (m.StartLine, m.StartColumn), (m.EndLine, m.EndColumn ), attribsOfSymbol s
            ]

    allImplementationLocations |> shouldEqual
          [("N", "file2", (2, 7), (2, 8), ["module"]);
           ("val y2", "file2", (13, 4), (13, 6), ["val"]);
           ("val pair2", "file2", (24, 10), (24, 15), ["val"]);
           ("val pair1", "file2", (24, 4), (24, 9), ["val"]);
           ("val enumValue", "file2", (31, 4), (31, 13), ["val"]);
           ("val op_PlusPlus", "file2", (33, 5), (33, 7), ["val"]);
           ("val c1", "file2", (35, 4), (35, 6), ["val"]);
           ("val c2", "file2", (37, 4), (37, 6), ["val"]);
           ("val mmmm1", "file2", (39, 4), (39, 9), ["val"]);
           ("val mmmm2", "file2", (40, 4), (40, 9), ["val"]);
           ("D1", "file2", (6, 5), (6, 7), ["class"]);
           ("member .ctor", "file2", (6, 5), (6, 7), ["member"; "ctor"]);
           ("member get_SomeProperty", "file2", (7, 13), (7, 25),
            ["member"; "getter"]);
           ("property SomeProperty", "file2", (7, 13), (7, 25),
            ["member"; "prop"]); ("D2", "file2", (9, 5), (9, 7), ["class"]);
           ("member .ctor", "file2", (9, 5), (9, 7), ["member"; "ctor"]);
           ("member get_SomeProperty", "file2", (10, 13), (10, 25),
            ["member"; "getter"]);
           ("property SomeProperty", "file2", (10, 13), (10, 25),
            ["member"; "prop"]); ("D3", "file2", (16, 5), (16, 7), ["class"]);
           ("member .ctor", "file2", (16, 5), (16, 7), ["member"; "ctor"]);
           ("member get_SomeProperty", "file2", (22, 13), (22, 25),
            ["member"; "getter"]);
           ("property SomeProperty", "file2", (22, 13), (22, 25),
            ["member"; "prop"]); ("field a", "file2", (16, 8), (16, 9), []);
           ("field b", "file2", (17, 8), (17, 9), []);
           ("field x", "file2", (20, 16), (20, 17), []);
           ("SaveOptions", "file2", (27, 5), (27, 16), ["enum"; "valuetype"]);
           ("field value__", "file2", (28, 2), (29, 25), []);
           ("field None", "file2", (28, 4), (28, 8), []);
           ("field DisableFormatting", "file2", (29, 4), (29, 21), []);
           ("M", "file1", (2, 7), (2, 8), ["module"]);
           ("val xxx", "file1", (7, 4), (7, 7), ["val"]);
           ("val fff", "file1", (8, 4), (8, 7), ["val"]);
           ("C", "file1", (4, 5), (4, 6), ["class"]);
           ("member .ctor", "file1", (4, 5), (4, 6), ["member"; "ctor"]);
           ("member get_P", "file1", (5, 13), (5, 14), ["member"; "getter"]);
           ("property P", "file1", (5, 13), (5, 14), ["member"; "prop"]);
           ("CAbbrev", "file1", (10, 5), (10, 12), ["abbrev"]);
           ("property P", "file1", (5, 13), (5, 14), ["member"; "prop"])]

    [ for x in allSymbols -> x.ToString() ] 
      |> shouldEqual 
              ["N"; "val y2"; "val pair2"; "val pair1"; "val enumValue"; "val op_PlusPlus";
               "val c1"; "val c2"; "val mmmm1"; "val mmmm2"; "D1"; "member .ctor";
               "member get_SomeProperty"; "property SomeProperty"; "D2"; "member .ctor";
               "member get_SomeProperty"; "property SomeProperty"; "D3"; "member .ctor";
               "member get_SomeProperty"; "property SomeProperty"; "field a"; "field b";
               "field x"; "SaveOptions"; "field value__"; "field None";
               "field DisableFormatting"; "M"; "val xxx"; "val fff"; "C"; "member .ctor";
               "member get_P"; "property P"; "CAbbrev"; "property P"]

[<Test>]
let ``Test project1 all symbols excluding compiler generated`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let allSymbolsNoCompGen = allSymbolsInEntities false wholeProjectResults.AssemblySignature.Entities
    [ for x in allSymbolsNoCompGen -> x.ToString() ] 
      |> shouldEqual 
              ["N"; "val y2"; "val pair2"; "val pair1"; "val enumValue"; "val op_PlusPlus";
               "val c1"; "val c2"; "val mmmm1"; "val mmmm2"; "D1"; "member .ctor";
               "member get_SomeProperty"; "property SomeProperty"; "D2"; "member .ctor";
               "member get_SomeProperty"; "property SomeProperty"; "D3"; "member .ctor";
               "member get_SomeProperty"; "property SomeProperty"; "field x";
               "SaveOptions"; "field None"; "field DisableFormatting"; "M"; "val xxx";
               "val fff"; "C"; "member .ctor"; "member get_P"; "property P"; "CAbbrev";
               "property P"]

[<Test>]
let ``Test project1 xxx symbols`` () = 


    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project1.fileName1, Project1.options) 
        |> Async.RunSynchronously

    let xSymbolUseOpt = backgroundTypedParse1.GetSymbolUseAtLocation(9,9,"",["xxx"]) |> Async.RunSynchronously
    let xSymbolUse = xSymbolUseOpt.Value
    let xSymbol = xSymbolUse.Symbol
    xSymbol.ToString() |> shouldEqual "val xxx"

    let usesOfXSymbol = 
        [ for su in wholeProjectResults.GetUsesOfSymbol(xSymbol) |> Async.RunSynchronously do
              yield Project1.cleanFileName su.FileName , tupsZ su.RangeAlternate, attribsOfSymbol su.Symbol ]

    usesOfXSymbol |> shouldEqual
       [("file1", ((6, 4), (6, 7)), ["val"]);
        ("file1", ((7, 13), (7, 16)), ["val"]);
        ("file1", ((7, 19), (7, 22)), ["val"]);
        ("file2", ((6, 28), (6, 33)), ["val"]);
        ("file2", ((12, 27), (12, 32)), ["val"])]

[<Test>]
let ``Test project1 all uses of all signature symbols`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities true wholeProjectResults.AssemblySignature.Entities
    let allUsesOfAllSymbols = 
        [ for s in allSymbols do 
             yield s.ToString(), 
                  [ for s in wholeProjectResults.GetUsesOfSymbol(s) |> Async.RunSynchronously -> 
                         (Project1.cleanFileName s.FileName, tupsZ s.RangeAlternate) ] ]
    let expected =      
          [("N", [("file2", ((1, 7), (1, 8)))]);
           ("val y2", [("file2", ((12, 4), (12, 6)))]);
           ("val pair2", [("file2", ((23, 10), (23, 15)))]);
           ("val pair1", [("file2", ((23, 4), (23, 9)))]);
           ("val enumValue", [("file2", ((30, 4), (30, 13)))]);
           ("val op_PlusPlus",
            [("file2", ((32, 5), (32, 7))); ("file2", ((34, 11), (34, 13)));
             ("file2", ((36, 11), (36, 13)))]);
           ("val c1", [("file2", ((34, 4), (34, 6)))]);
           ("val c2", [("file2", ((36, 4), (36, 6)))]);
           ("val mmmm1", [("file2", ((38, 4), (38, 9)))]);
           ("val mmmm2", [("file2", ((39, 4), (39, 9)))]);
           ("D1", [("file2", ((5, 5), (5, 7))); ("file2", ((9, 38), (9, 40)))]);
           ("member .ctor",
            [("file2", ((5, 5), (5, 7))); ("file2", ((9, 38), (9, 40)))]);
           ("member get_SomeProperty", [("file2", ((6, 13), (6, 25)))]);
           ("property SomeProperty", [("file2", ((6, 13), (6, 25)))]);
           ("D2", [("file2", ((8, 5), (8, 7)))]);
           ("member .ctor", [("file2", ((8, 5), (8, 7)))]);
           ("member get_SomeProperty", [("file2", ((9, 13), (9, 25)))]);
           ("property SomeProperty", [("file2", ((9, 13), (9, 25)))]);
           ("D3", [("file2", ((15, 5), (15, 7)))]);
           ("member .ctor", [("file2", ((15, 5), (15, 7)))]);
           ("member get_SomeProperty", [("file2", ((21, 13), (21, 25)))]);
           ("property SomeProperty", [("file2", ((21, 13), (21, 25)))]);
           ("field a", []); ("field b", []);
           ("field x", [("file2", ((19, 16), (19, 17)))]);
           ("SaveOptions",
            [("file2", ((26, 5), (26, 16))); ("file2", ((30, 16), (30, 27)))]);
           ("field value__", []); ("field None", [("file2", ((27, 4), (27, 8)))]);
           ("field DisableFormatting",
            [("file2", ((28, 4), (28, 21))); ("file2", ((30, 16), (30, 45)))]);
           ("M",
            [("file1", ((1, 7), (1, 8))); ("file2", ((6, 28), (6, 29)));
             ("file2", ((9, 28), (9, 29))); ("file2", ((12, 27), (12, 28)));
             ("file2", ((38, 12), (38, 13))); ("file2", ((38, 22), (38, 23)));
             ("file2", ((39, 12), (39, 13))); ("file2", ((39, 28), (39, 29)))]);
           ("val xxx",
            [("file1", ((6, 4), (6, 7))); ("file1", ((7, 13), (7, 16)));
             ("file1", ((7, 19), (7, 22))); ("file2", ((6, 28), (6, 33)));
             ("file2", ((12, 27), (12, 32)))]);
           ("val fff", [("file1", ((7, 4), (7, 7))); ("file2", ((9, 28), (9, 33)))]);
           ("C",
            [("file1", ((3, 5), (3, 6))); ("file1", ((9, 15), (9, 16)));
             ("file2", ((38, 12), (38, 15))); ("file2", ((38, 22), (38, 25)))]);
           ("member .ctor",
            [("file1", ((3, 5), (3, 6))); ("file1", ((9, 15), (9, 16)));
             ("file2", ((38, 12), (38, 15))); ("file2", ((38, 22), (38, 25)))]);
           ("member get_P", [("file1", ((4, 13), (4, 14)))]);
           ("property P", [("file1", ((4, 13), (4, 14)))]);
           ("CAbbrev",
            [("file1", ((9, 5), (9, 12))); ("file2", ((39, 12), (39, 21)));
             ("file2", ((39, 28), (39, 37)))]);
           ("property P", [("file1", ((4, 13), (4, 14)))])]
    set allUsesOfAllSymbols - set expected |> shouldEqual Set.empty
    set expected - set allUsesOfAllSymbols |> shouldEqual Set.empty
    (set expected = set allUsesOfAllSymbols) |> shouldEqual true

[<Test>]
let ``Test project1 all uses of all symbols`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let allUsesOfAllSymbols = 
        [ for s in wholeProjectResults.GetAllUsesOfAllSymbols() |> Async.RunSynchronously -> 
              s.Symbol.DisplayName, s.Symbol.FullName, Project1.cleanFileName s.FileName, tupsZ s.RangeAlternate, attribsOfSymbol s.Symbol ]
    let expected =      
              [("C", "M.C", "file1", ((3, 5), (3, 6)), ["class"]);
               ("( .ctor )", "M.C.( .ctor )", "file1", ((3, 5), (3, 6)),
                ["member"; "ctor"]);
               ("P", "M.C.P", "file1", ((4, 13), (4, 14)), ["member"]);
               ("x", "x", "file1", ((4, 11), (4, 12)), []);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file1",
                ((6, 12), (6, 13)), ["val"]);
               ("xxx", "M.xxx", "file1", ((6, 4), (6, 7)), ["val"]);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file1",
                ((7, 17), (7, 18)), ["val"]);
               ("xxx", "M.xxx", "file1", ((7, 13), (7, 16)), ["val"]);
               ("xxx", "M.xxx", "file1", ((7, 19), (7, 22)), ["val"]);
               ("fff", "M.fff", "file1", ((7, 4), (7, 7)), ["val"]);
               ("C", "M.C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("C", "M.C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("C", "M.C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("C", "M.C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("CAbbrev", "M.CAbbrev", "file1", ((9, 5), (9, 12)), ["abbrev"]);
               ("M", "M", "file1", ((1, 7), (1, 8)), ["module"]);
               ("D1", "N.D1", "file2", ((5, 5), (5, 7)), ["class"]);
               ("( .ctor )", "N.D1.( .ctor )", "file2", ((5, 5), (5, 7)),
                ["member"; "ctor"]);
               ("SomeProperty", "N.D1.SomeProperty", "file2", ((6, 13), (6, 25)),
                ["member"]); ("x", "x", "file2", ((6, 11), (6, 12)), []);
               ("M", "M", "file2", ((6, 28), (6, 29)), ["module"]);
               ("xxx", "M.xxx", "file2", ((6, 28), (6, 33)), ["val"]);
               ("D2", "N.D2", "file2", ((8, 5), (8, 7)), ["class"]);
               ("( .ctor )", "N.D2.( .ctor )", "file2", ((8, 5), (8, 7)),
                ["member"; "ctor"]);
               ("SomeProperty", "N.D2.SomeProperty", "file2", ((9, 13), (9, 25)),
                ["member"]); ("x", "x", "file2", ((9, 11), (9, 12)), []);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((9, 36), (9, 37)), ["val"]);
               ("M", "M", "file2", ((9, 28), (9, 29)), ["module"]);
               ("fff", "M.fff", "file2", ((9, 28), (9, 33)), ["val"]);
               ("D1", "N.D1", "file2", ((9, 38), (9, 40)), ["member"; "ctor"]);
               ("M", "M", "file2", ((12, 27), (12, 28)), ["module"]);
               ("xxx", "M.xxx", "file2", ((12, 27), (12, 32)), ["val"]);
               ("y2", "N.y2", "file2", ((12, 4), (12, 6)), ["val"]);
               ("DefaultValueAttribute", "Microsoft.FSharp.Core.DefaultValueAttribute",
                "file2", ((18, 6), (18, 18)), ["class"]);
               ("DefaultValueAttribute", "Microsoft.FSharp.Core.DefaultValueAttribute",
                "file2", ((18, 6), (18, 18)), ["class"]);
               ("DefaultValueAttribute", "Microsoft.FSharp.Core.DefaultValueAttribute",
                "file2", ((18, 6), (18, 18)), ["member"]);
               ("int", "Microsoft.FSharp.Core.int", "file2", ((19, 20), (19, 23)),
                ["abbrev"]);
               ("DefaultValueAttribute", "Microsoft.FSharp.Core.DefaultValueAttribute",
                "file2", ((18, 6), (18, 18)), ["class"]);
               ("DefaultValueAttribute", "Microsoft.FSharp.Core.DefaultValueAttribute",
                "file2", ((18, 6), (18, 18)), ["class"]);
               ("DefaultValueAttribute", "Microsoft.FSharp.Core.DefaultValueAttribute",
                "file2", ((18, 6), (18, 18)), ["member"]);
               ("x", "N.D3.x", "file2", ((19, 16), (19, 17)), []);
               ("D3", "N.D3", "file2", ((15, 5), (15, 7)), ["class"]);
               ("int", "Microsoft.FSharp.Core.int", "file2", ((15, 10), (15, 13)),
                ["abbrev"]); ("a", "a", "file2", ((15, 8), (15, 9)), []);
               ("( .ctor )", "N.D3.( .ctor )", "file2", ((15, 5), (15, 7)),
                ["member"; "ctor"]);
               ("SomeProperty", "N.D3.SomeProperty", "file2", ((21, 13), (21, 25)),
                ["member"]);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((16, 14), (16, 15)), ["val"]);
               ("a", "a", "file2", ((16, 12), (16, 13)), []);
               ("b", "b", "file2", ((16, 8), (16, 9)), []);
               ("x", "x", "file2", ((21, 11), (21, 12)), []);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((21, 30), (21, 31)), ["val"]);
               ("a", "a", "file2", ((21, 28), (21, 29)), []);
               ("b", "b", "file2", ((21, 32), (21, 33)), []);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((23, 25), (23, 26)), ["val"]);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((23, 21), (23, 22)), ["val"]);
               ("int32", "Microsoft.FSharp.Core.Operators.int32", "file2",
                ((23, 27), (23, 32)), ["val"]);
               ("DateTime", "System.DateTime", "file2", ((23, 40), (23, 48)),
                ["valuetype"]);
               ("System", "System", "file2", ((23, 33), (23, 39)), ["namespace"]);
               ("Now", "System.DateTime.Now", "file2", ((23, 33), (23, 52)),
                ["member"; "prop"]);
               ("Ticks", "System.DateTime.Ticks", "file2", ((23, 33), (23, 58)),
                ["member"; "prop"]);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((23, 62), (23, 63)), ["val"]);
               ("pair2", "N.pair2", "file2", ((23, 10), (23, 15)), ["val"]);
               ("pair1", "N.pair1", "file2", ((23, 4), (23, 9)), ["val"]);
               ("None", "N.SaveOptions.None", "file2", ((27, 4), (27, 8)), []);
               ("DisableFormatting", "N.SaveOptions.DisableFormatting", "file2",
                ((28, 4), (28, 21)), []);
               ("SaveOptions", "N.SaveOptions", "file2", ((26, 5), (26, 16)),
                ["enum"; "valuetype"]);
               ("SaveOptions", "N.SaveOptions", "file2", ((30, 16), (30, 27)),
                ["enum"; "valuetype"]);
               ("DisableFormatting", "N.SaveOptions.DisableFormatting", "file2",
                ((30, 16), (30, 45)), []);
               ("enumValue", "N.enumValue", "file2", ((30, 4), (30, 13)), ["val"]);
               ("x", "x", "file2", ((32, 9), (32, 10)), []);
               ("y", "y", "file2", ((32, 11), (32, 12)), []);
               ("( + )", "Microsoft.FSharp.Core.Operators.( + )", "file2",
                ((32, 17), (32, 18)), ["val"]);
               ("x", "x", "file2", ((32, 15), (32, 16)), []);
               ("y", "y", "file2", ((32, 19), (32, 20)), []);
               ("( ++ )", "N.( ++ )", "file2", ((32, 5), (32, 7)), ["val"]);
               ("( ++ )", "N.( ++ )", "file2", ((34, 11), (34, 13)), ["val"]);
               ("c1", "N.c1", "file2", ((34, 4), (34, 6)), ["val"]);
               ("( ++ )", "N.( ++ )", "file2", ((36, 11), (36, 13)), ["val"]);
               ("c2", "N.c2", "file2", ((36, 4), (36, 6)), ["val"]);
               ("M", "M", "file2", ((38, 12), (38, 13)), ["module"]);
               ("C", "M.C", "file2", ((38, 12), (38, 15)), ["class"]);
               ("M", "M", "file2", ((38, 22), (38, 23)), ["module"]);
               ("C", "M.C", "file2", ((38, 22), (38, 25)), ["class"]);
               ("C", "M.C", "file2", ((38, 22), (38, 25)), ["member"; "ctor"]);
               ("mmmm1", "N.mmmm1", "file2", ((38, 4), (38, 9)), ["val"]);
               ("M", "M", "file2", ((39, 12), (39, 13)), ["module"]);
               ("CAbbrev", "M.CAbbrev", "file2", ((39, 12), (39, 21)), ["abbrev"]);
               ("M", "M", "file2", ((39, 28), (39, 29)), ["module"]);
               ("CAbbrev", "M.CAbbrev", "file2", ((39, 28), (39, 37)), ["abbrev"]);
               ("C", "M.C", "file2", ((39, 28), (39, 37)), ["member"; "ctor"]);
               ("mmmm2", "N.mmmm2", "file2", ((39, 4), (39, 9)), ["val"]);
               ("N", "N", "file2", ((1, 7), (1, 8)), ["module"])]

    set allUsesOfAllSymbols - set expected |> shouldEqual Set.empty
    set expected - set allUsesOfAllSymbols |> shouldEqual Set.empty
    (set expected = set allUsesOfAllSymbols) |> shouldEqual true

[<Test>]
let ``Test file explicit parse symbols`` () = 


    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let parseResults1 = checker.ParseFileInProject(Project1.fileName1, Project1.fileSource1, Project1.options)  |> Async.RunSynchronously
    let parseResults2 = checker.ParseFileInProject(Project1.fileName2, Project1.fileSource2, Project1.options)  |> Async.RunSynchronously

    let checkResults1 = 
        checker.CheckFileInProject(parseResults1, Project1.fileName1, 0, Project1.fileSource1, Project1.options) 
        |> Async.RunSynchronously
        |> function CheckFileAnswer.Succeeded x ->  x | _ -> failwith "unexpected aborted"

    let checkResults2 = 
        checker.CheckFileInProject(parseResults2, Project1.fileName2, 0, Project1.fileSource2, Project1.options)
        |> Async.RunSynchronously
        |> function CheckFileAnswer.Succeeded x ->  x | _ -> failwith "unexpected aborted"

    let xSymbolUse2Opt = checkResults1.GetSymbolUseAtLocation(9,9,"",["xxx"]) |> Async.RunSynchronously
    let xSymbol2 = xSymbolUse2Opt.Value.Symbol
    let usesOfXSymbol2 = 
        [| for s in wholeProjectResults.GetUsesOfSymbol(xSymbol2) |> Async.RunSynchronously -> (Project1.cleanFileName s.FileName, tupsZ s.RangeAlternate) |] 

    let usesOfXSymbol21 = 
        [| for s in checkResults1.GetUsesOfSymbolInFile(xSymbol2) |> Async.RunSynchronously -> (Project1.cleanFileName s.FileName, tupsZ s.RangeAlternate) |] 

    let usesOfXSymbol22 = 
        [| for s in checkResults2.GetUsesOfSymbolInFile(xSymbol2) |> Async.RunSynchronously -> (Project1.cleanFileName s.FileName, tupsZ s.RangeAlternate) |] 

    usesOfXSymbol2
         |> shouldEqual [|("file1", ((6, 4), (6, 7)));
                          ("file1", ((7, 13), (7, 16)));
                          ("file1", ((7, 19), (7, 22)));
                          ("file2", ((6, 28), (6, 33)));
                          ("file2", ((12, 27), (12, 32)))|]

    usesOfXSymbol21
         |> shouldEqual [|("file1", ((6, 4), (6, 7)));
                          ("file1", ((7, 13), (7, 16)));
                          ("file1", ((7, 19), (7, 22)))|]

    usesOfXSymbol22
         |> shouldEqual [|("file2", ((6, 28), (6, 33)));
                          ("file2", ((12, 27), (12, 32)))|]


[<Test>]
let ``Test file explicit parse all symbols`` () = 


    let wholeProjectResults = checker.ParseAndCheckProject(Project1.options) |> Async.RunSynchronously
    let parseResults1 = checker.ParseFileInProject(Project1.fileName1, Project1.fileSource1, Project1.options) |> Async.RunSynchronously
    let parseResults2 = checker.ParseFileInProject(Project1.fileName2, Project1.fileSource2, Project1.options) |> Async.RunSynchronously

    let checkResults1 = 
        checker.CheckFileInProject(parseResults1, Project1.fileName1, 0, Project1.fileSource1, Project1.options) 
        |> Async.RunSynchronously
        |> function CheckFileAnswer.Succeeded x ->  x | _ -> failwith "unexpected aborted"

    let checkResults2 = 
        checker.CheckFileInProject(parseResults2, Project1.fileName2, 0, Project1.fileSource2, Project1.options)
        |> Async.RunSynchronously
        |> function CheckFileAnswer.Succeeded x ->  x | _ -> failwith "unexpected aborted"

    let usesOfSymbols = checkResults1.GetAllUsesOfAllSymbolsInFile() |> Async.RunSynchronously
    let cleanedUsesOfSymbols = 
         [ for s in usesOfSymbols -> s.Symbol.DisplayName, Project1.cleanFileName s.FileName, tupsZ s.RangeAlternate, attribsOfSymbol s.Symbol ]

    cleanedUsesOfSymbols 
       |> shouldEqual 
              [("C", "file1", ((3, 5), (3, 6)), ["class"]);
               ("( .ctor )", "file1", ((3, 5), (3, 6)), ["member"; "ctor"]);
               ("P", "file1", ((4, 13), (4, 14)), ["member"]);
               ("x", "file1", ((4, 11), (4, 12)), []);
               ("( + )", "file1", ((6, 12), (6, 13)), ["val"]);
               ("xxx", "file1", ((6, 4), (6, 7)), ["val"]);
               ("( + )", "file1", ((7, 17), (7, 18)), ["val"]);
               ("xxx", "file1", ((7, 13), (7, 16)), ["val"]);
               ("xxx", "file1", ((7, 19), (7, 22)), ["val"]);
               ("fff", "file1", ((7, 4), (7, 7)), ["val"]);
               ("C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("C", "file1", ((9, 15), (9, 16)), ["class"]);
               ("CAbbrev", "file1", ((9, 5), (9, 12)), ["abbrev"]);
               ("M", "file1", ((1, 7), (1, 8)), ["module"])]


//-----------------------------------------------------------------------------------------

module Project2 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module M

type DUWithNormalFields = 
    | DU1 of int * int
    | DU2 of int * int
    | D of int * int

let _ = DU1(1, 2)
let _ = DU2(1, 2)
let _ = D(1, 2)

type DUWithNamedFields = DU of x : int * y : int

let _ = DU(x=1, y=2)

type GenericClass<'T>() = 
    member x.GenericMethod<'U>(t: 'T, u: 'U) = 1

let c = GenericClass<int>()
let _ = c.GenericMethod<int>(3, 4)

let GenericFunction (x:'T, y: 'T) = (x,y) : ('T * 'T)

let _ = GenericFunction(3, 4)
    """
    File.WriteAllText(fileName1, fileSource1)

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)




[<Test>]
let ``Test project2 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project2.options) |> Async.RunSynchronously
    wholeProjectResults .Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project2 basic`` () = 


    let wholeProjectResults = checker.ParseAndCheckProject(Project2.options) |> Async.RunSynchronously

    set [ for x in wholeProjectResults.AssemblySignature.Entities -> x.DisplayName ] |> shouldEqual (set ["M"])

    [ for x in wholeProjectResults.AssemblySignature.Entities.[0].NestedEntities -> x.DisplayName ] |> shouldEqual ["DUWithNormalFields"; "DUWithNamedFields"; "GenericClass" ]

    set [ for x in wholeProjectResults.AssemblySignature.Entities.[0].MembersFunctionsAndValues -> x.DisplayName ] 
        |> shouldEqual (set ["c"; "GenericFunction"])

[<Test>]
let ``Test project2 all symbols in signature`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project2.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities true wholeProjectResults.AssemblySignature.Entities
    [ for x in allSymbols -> x.ToString() ] 
       |> shouldEqual 
              ["M"; "val c"; "val GenericFunction"; "generic parameter T";
               "DUWithNormalFields"; "DU1"; "field Item1"; "field Item2"; "DU2";
               "field Item1"; "field Item2"; "D"; "field Item1"; "field Item2";
               "DUWithNamedFields"; "DU"; "field x"; "field y"; "GenericClass`1";
               "generic parameter T"; "member .ctor"; "member GenericMethod";
               "generic parameter U"]

[<Test>]
let ``Test project2 all uses of all signature symbols`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project2.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities true wholeProjectResults.AssemblySignature.Entities
    let allUsesOfAllSymbols = 
        [ for s in allSymbols do 
             let uses = [ for s in wholeProjectResults.GetUsesOfSymbol(s) |> Async.RunSynchronously -> (if s.FileName = Project2.fileName1 then "file1" else "??"), tupsZ s.RangeAlternate ]
             yield s.ToString(), uses ]
    let expected =      
              [("M", [("file1", ((1, 7), (1, 8)))]);
               ("val c", [("file1", ((19, 4), (19, 5))); ("file1", ((20, 8), (20, 9)))]);
               ("val GenericFunction",
                [("file1", ((22, 4), (22, 19))); ("file1", ((24, 8), (24, 23)))]);
               ("generic parameter T",
                [("file1", ((22, 23), (22, 25))); ("file1", ((22, 30), (22, 32)));
                 ("file1", ((22, 45), (22, 47))); ("file1", ((22, 50), (22, 52)))]);
               ("DUWithNormalFields", [("file1", ((3, 5), (3, 23)))]);
               ("DU1", [("file1", ((4, 6), (4, 9))); ("file1", ((8, 8), (8, 11)))]);
               ("field Item1", [("file1", ((4, 6), (4, 9))); ("file1", ((8, 8), (8, 11)))]);
               ("field Item2", [("file1", ((4, 6), (4, 9))); ("file1", ((8, 8), (8, 11)))]);
               ("DU2", [("file1", ((5, 6), (5, 9))); ("file1", ((9, 8), (9, 11)))]);
               ("field Item1", [("file1", ((5, 6), (5, 9))); ("file1", ((9, 8), (9, 11)))]);
               ("field Item2", [("file1", ((5, 6), (5, 9))); ("file1", ((9, 8), (9, 11)))]);
               ("D", [("file1", ((6, 6), (6, 7))); ("file1", ((10, 8), (10, 9)))]);
               ("field Item1",
                [("file1", ((6, 6), (6, 7))); ("file1", ((10, 8), (10, 9)))]);
               ("field Item2",
                [("file1", ((6, 6), (6, 7))); ("file1", ((10, 8), (10, 9)))]);
               ("DUWithNamedFields", [("file1", ((12, 5), (12, 22)))]);
               ("DU", [("file1", ((12, 25), (12, 27))); ("file1", ((14, 8), (14, 10)))]);
               ("field x",
                [("file1", ((12, 25), (12, 27))); ("file1", ((14, 8), (14, 10)))]);
               ("field y",
                [("file1", ((12, 25), (12, 27))); ("file1", ((14, 8), (14, 10)))]);
               ("GenericClass`1",
                [("file1", ((16, 5), (16, 17))); ("file1", ((19, 8), (19, 20)))]);
               ("generic parameter T",
                [("file1", ((16, 18), (16, 20))); ("file1", ((17, 34), (17, 36)))]);
               ("member .ctor",
                [("file1", ((16, 5), (16, 17))); ("file1", ((19, 8), (19, 20)))]);
               ("member GenericMethod",
                [("file1", ((17, 13), (17, 26))); ("file1", ((20, 8), (20, 23)))]);
               ("generic parameter U",
                [("file1", ((17, 27), (17, 29))); ("file1", ((17, 41), (17, 43)))])]
    set allUsesOfAllSymbols - set expected |> shouldEqual Set.empty
    set expected - set allUsesOfAllSymbols |> shouldEqual Set.empty
    (set expected = set allUsesOfAllSymbols) |> shouldEqual true

[<Test>]
let ``Test project2 all uses of all symbols`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project2.options) |> Async.RunSynchronously
    let allUsesOfAllSymbols = 
        [ for s in wholeProjectResults.GetAllUsesOfAllSymbols() |> Async.RunSynchronously -> 
            s.Symbol.DisplayName, (if s.FileName = Project2.fileName1 then "file1" else "???"), tupsZ s.RangeAlternate, attribsOfSymbol s.Symbol ]
    let expected =      
          [("int", "file1", ((4, 13), (4, 16)), ["abbrev"]);
           ("int", "file1", ((4, 19), (4, 22)), ["abbrev"]);
           ("int", "file1", ((5, 13), (5, 16)), ["abbrev"]);
           ("int", "file1", ((5, 19), (5, 22)), ["abbrev"]);
           ("int", "file1", ((6, 11), (6, 14)), ["abbrev"]);
           ("int", "file1", ((6, 17), (6, 20)), ["abbrev"]);
           ("int", "file1", ((4, 13), (4, 16)), ["abbrev"]);
           ("int", "file1", ((4, 19), (4, 22)), ["abbrev"]);
           ("int", "file1", ((5, 13), (5, 16)), ["abbrev"]);
           ("int", "file1", ((5, 19), (5, 22)), ["abbrev"]);
           ("int", "file1", ((6, 11), (6, 14)), ["abbrev"]);
           ("int", "file1", ((6, 17), (6, 20)), ["abbrev"]);
           ("DU1", "file1", ((4, 6), (4, 9)), []);
           ("DU2", "file1", ((5, 6), (5, 9)), []);
           ("D", "file1", ((6, 6), (6, 7)), []);
           ("DUWithNormalFields", "file1", ((3, 5), (3, 23)), ["union"]);
           ("DU1", "file1", ((8, 8), (8, 11)), []);
           ("DU2", "file1", ((9, 8), (9, 11)), []);
           ("D", "file1", ((10, 8), (10, 9)), []);
           ("int", "file1", ((12, 35), (12, 38)), ["abbrev"]);
           ("int", "file1", ((12, 45), (12, 48)), ["abbrev"]);
           ("int", "file1", ((12, 35), (12, 38)), ["abbrev"]);
           ("x", "file1", ((12, 31), (12, 32)), []);
           ("int", "file1", ((12, 45), (12, 48)), ["abbrev"]);
           ("y", "file1", ((12, 41), (12, 42)), []);
           ("DU", "file1", ((12, 25), (12, 27)), []);
           ("DUWithNamedFields", "file1", ((12, 5), (12, 22)), ["union"]);
           ("DU", "file1", ((14, 8), (14, 10)), []);
           ("x", "file1", ((14, 11), (14, 12)), []);
           ("y", "file1", ((14, 16), (14, 17)), []);
           ("T", "file1", ((16, 18), (16, 20)), []);
           ("GenericClass", "file1", ((16, 5), (16, 17)), ["class"]);
           ("( .ctor )", "file1", ((16, 5), (16, 17)), ["member"; "ctor"]);
           ("U", "file1", ((17, 27), (17, 29)), []);
           ("T", "file1", ((17, 34), (17, 36)), []);
           ("U", "file1", ((17, 41), (17, 43)), []);
           ("GenericMethod", "file1", ((17, 13), (17, 26)), ["member"]);
           ("x", "file1", ((17, 11), (17, 12)), []);
           ("T", "file1", ((17, 34), (17, 36)), []);
           ("U", "file1", ((17, 41), (17, 43)), []);
           ("u", "file1", ((17, 38), (17, 39)), []);
           ("t", "file1", ((17, 31), (17, 32)), []);
           ("GenericClass", "file1", ((19, 8), (19, 20)), ["member"; "ctor"]);
           ("int", "file1", ((19, 21), (19, 24)), ["abbrev"]);
           ("c", "file1", ((19, 4), (19, 5)), ["val"]);
           ("c", "file1", ((20, 8), (20, 9)), ["val"]);
           ("GenericMethod", "file1", ((20, 8), (20, 23)), ["member"]);
           ("int", "file1", ((20, 24), (20, 27)), ["abbrev"]);
           ("T", "file1", ((22, 23), (22, 25)), []);
           ("T", "file1", ((22, 30), (22, 32)), []);
           ("y", "file1", ((22, 27), (22, 28)), []);
           ("x", "file1", ((22, 21), (22, 22)), []);
           ("T", "file1", ((22, 45), (22, 47)), []);
           ("T", "file1", ((22, 50), (22, 52)), []);
           ("x", "file1", ((22, 37), (22, 38)), []);
           ("y", "file1", ((22, 39), (22, 40)), []);
           ("GenericFunction", "file1", ((22, 4), (22, 19)), ["val"]);
           ("GenericFunction", "file1", ((24, 8), (24, 23)), ["val"]);
           ("M", "file1", ((1, 7), (1, 8)), ["module"])]
    set allUsesOfAllSymbols - set expected |> shouldEqual Set.empty
    set expected - set allUsesOfAllSymbols |> shouldEqual Set.empty
    (set expected = set allUsesOfAllSymbols) |> shouldEqual true

//-----------------------------------------------------------------------------------------

module Project3 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module M

type IFoo =
    abstract InterfaceProperty: string
    abstract InterfacePropertySet: string with set
    abstract InterfaceMethod: methodArg:string -> string
    [<CLIEvent>]
    abstract InterfaceEvent: IEvent<int>

[<AbstractClass>]
type CFoo() =
    abstract AbstractClassProperty: string
    abstract AbstractClassPropertySet: string with set
    abstract AbstractClassMethod: methodArg:string -> string
    [<CLIEvent>]
    abstract AbstractClassEvent: IEvent<int>

type CBaseFoo() =
    let ev = Event<_>()
    abstract BaseClassProperty: string
    abstract BaseClassPropertySet: string with set
    abstract BaseClassMethod: methodArg:string -> string
    [<CLIEvent>]
    abstract BaseClassEvent: IEvent<int>
    default __.BaseClassProperty = "dflt"
    default __.BaseClassPropertySet with set (v:string) = ()
    default __.BaseClassMethod(m) = m
    [<CLIEvent>]
    default __.BaseClassEvent = ev.Publish

type IFooImpl() =
    let ev = Event<_>()
    interface IFoo with
        member this.InterfaceProperty = "v"
        member this.InterfacePropertySet with set (v:string) = ()
        member this.InterfaceMethod(x) = x
        [<CLIEvent>]
        member this.InterfaceEvent = ev.Publish

type CFooImpl() =
    inherit CFoo()
    let ev = Event<_>()
    override this.AbstractClassProperty = "v"
    override this.AbstractClassPropertySet with set (v:string) = ()
    override this.AbstractClassMethod(x) = x
    [<CLIEvent>]
    override this.AbstractClassEvent = ev.Publish

type CBaseFooImpl() =
    inherit CBaseFoo()
    let ev = Event<_>()
    override this.BaseClassProperty = "v"
    override this.BaseClassPropertySet with set (v:string)  = ()
    override this.BaseClassMethod(x) = x
    [<CLIEvent>]
    override this.BaseClassEvent = ev.Publish

let IFooImplObjectExpression() =
    let ev = Event<_>()
    { new IFoo with
        member this.InterfaceProperty = "v"
        member this.InterfacePropertySet with set (v:string) = ()
        member this.InterfaceMethod(x) = x
        [<CLIEvent>]
        member this.InterfaceEvent = ev.Publish }

let CFooImplObjectExpression() =
    let ev = Event<_>()
    { new CFoo() with
        override this.AbstractClassProperty = "v"
        override this.AbstractClassPropertySet with set (v:string) = ()
        override this.AbstractClassMethod(x) = x
        [<CLIEvent>]
        override this.AbstractClassEvent = ev.Publish }

let getP (foo: IFoo) = foo.InterfaceProperty
let setP (foo: IFoo) v = foo.InterfacePropertySet <- v
let getE (foo: IFoo) = foo.InterfaceEvent
let getM (foo: IFoo) = foo.InterfaceMethod("d")
    """
    File.WriteAllText(fileName1, fileSource1)

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)




[<Test>]
let ``Test project3 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project3.options) |> Async.RunSynchronously
    wholeProjectResults .Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project3 basic`` () = 


    let wholeProjectResults = checker.ParseAndCheckProject(Project3.options) |> Async.RunSynchronously

    set [ for x in wholeProjectResults.AssemblySignature.Entities -> x.DisplayName ] |> shouldEqual (set ["M"])

    [ for x in wholeProjectResults.AssemblySignature.Entities.[0].NestedEntities -> x.DisplayName ] 
        |> shouldEqual ["IFoo"; "CFoo"; "CBaseFoo"; "IFooImpl"; "CFooImpl"; "CBaseFooImpl"]

    [ for x in wholeProjectResults.AssemblySignature.Entities.[0].MembersFunctionsAndValues -> x.DisplayName ] 
        |> shouldEqual ["IFooImplObjectExpression"; "CFooImplObjectExpression"; "getP"; "setP"; "getE";"getM"]

[<Test>]
let ``Test project3 all symbols in signature`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project3.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities false wholeProjectResults.AssemblySignature.Entities
    [ for x in allSymbols -> x.ToString(), attribsOfSymbol x ] 
      |> shouldEqual 
              [("M", ["module"]); ("val IFooImplObjectExpression", ["val"]);
               ("val CFooImplObjectExpression", ["val"]); ("val getP", ["val"]);
               ("val setP", ["val"]); ("val getE", ["val"]); ("val getM", ["val"]);
               ("IFoo", ["interface"]);
               ("member InterfaceMethod", ["slot"; "member"]);
               ("member add_InterfaceEvent", ["slot"; "member"; "add" ]);
               ("member get_InterfaceEvent", ["slot"; "member"; "getter"]);
               ("member get_InterfaceProperty", ["slot"; "member"; "getter"]);
               ("member remove_InterfaceEvent", ["slot"; "member"; "remove" ]);
               ("member set_InterfacePropertySet", ["slot"; "member"; "setter"]);
               ("property InterfacePropertySet", ["slot"; "member"; "prop"]);
               ("property InterfaceProperty", ["slot"; "member"; "prop"]);
               ("property InterfaceEvent", ["slot"; "member"; "prop"]);
               ("CFoo", ["class"]); ("member .ctor", ["member"; "ctor"]);
               ("member AbstractClassMethod", ["slot"; "member"]);
               ("member add_AbstractClassEvent", ["slot"; "member"; "add" ]);
               ("member get_AbstractClassEvent", ["slot"; "member"; "getter"]);
               ("member get_AbstractClassProperty", ["slot"; "member"; "getter"]);
               ("member remove_AbstractClassEvent", ["slot"; "member"; "remove"]);
               ("member set_AbstractClassPropertySet", ["slot"; "member"; "setter"]);
               ("property AbstractClassPropertySet", ["slot"; "member"; "prop"]);
               ("property AbstractClassProperty", ["slot"; "member"; "prop"]);
               ("property AbstractClassEvent", ["slot"; "member"; "prop"]);
               ("CBaseFoo", ["class"]); ("member .ctor", ["member"; "ctor"]);
               ("member BaseClassMethod", ["slot"; "member"]);
               ("member add_BaseClassEvent", ["slot"; "member"; "add"]);
               ("member get_BaseClassEvent", ["slot"; "member"; "getter"]);
               ("member get_BaseClassProperty", ["slot"; "member"; "getter"]);
               ("member remove_BaseClassEvent", ["slot"; "member"; "remove"]);
               ("member set_BaseClassPropertySet", ["slot"; "member"; "setter"]);
               ("property BaseClassPropertySet", ["member"; "prop"]);
               ("property BaseClassPropertySet", ["slot"; "member"; "prop"]);
               ("property BaseClassProperty", ["member"; "prop"]);
               ("property BaseClassProperty", ["slot"; "member"; "prop"]);
               ("property BaseClassEvent", ["member"; "prop"]);
               ("property BaseClassEvent", ["slot"; "member"; "prop"]);
               ("IFooImpl", ["class"]); ("member .ctor", ["member"; "ctor"]);
               ("CFooImpl", ["class"]); ("member .ctor", ["member"; "ctor"]);
               ("property AbstractClassPropertySet", ["member"; "prop"]);
               ("property AbstractClassProperty", ["member"; "prop"]);
               ("property AbstractClassEvent", ["member"; "prop"]);
               ("CBaseFooImpl", ["class"]); ("member .ctor", ["member"; "ctor"]);
               ("property BaseClassPropertySet", ["member"; "prop"]);
               ("property BaseClassProperty", ["member"; "prop"]);
               ("property BaseClassEvent", ["member"; "prop"])]

[<Test>]
let ``Test project3 all uses of all signature symbols`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project3.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities false wholeProjectResults.AssemblySignature.Entities

    let allUsesOfAllSymbols = 
        [ for s in allSymbols do 
             let uses = [ for s in wholeProjectResults.GetUsesOfSymbol(s) |> Async.RunSynchronously -> 
                            ((if s.FileName = Project3.fileName1 then "file1" else "??"), 
                             tupsZ s.RangeAlternate, attribsOfSymbolUse s, attribsOfSymbol s.Symbol) ]
             yield s.ToString(), uses ]
    let expected =      
              [("M", [("file1", ((1, 7), (1, 8)), ["defn"], ["module"])]);
               ("val IFooImplObjectExpression",
                [("file1", ((58, 4), (58, 28)), ["defn"], ["val"])]);
               ("val CFooImplObjectExpression",
                [("file1", ((67, 4), (67, 28)), ["defn"], ["val"])]);
               ("val getP", [("file1", ((76, 4), (76, 8)), ["defn"], ["val"])]);
               ("val setP", [("file1", ((77, 4), (77, 8)), ["defn"], ["val"])]);
               ("val getE", [("file1", ((78, 4), (78, 8)), ["defn"], ["val"])]);
               ("val getM", [("file1", ((79, 4), (79, 8)), ["defn"], ["val"])]);
               ("IFoo",
                [("file1", ((3, 5), (3, 9)), ["defn"], ["interface"]);
                 ("file1", ((33, 14), (33, 18)), ["type"], ["interface"]);
                 ("file1", ((60, 10), (60, 14)), ["type"], ["interface"]);
                 ("file1", ((76, 15), (76, 19)), ["type"], ["interface"]);
                 ("file1", ((77, 15), (77, 19)), ["type"], ["interface"]);
                 ("file1", ((78, 15), (78, 19)), ["type"], ["interface"]);
                 ("file1", ((79, 15), (79, 19)), ["type"], ["interface"])]);
               ("member InterfaceMethod",
                [("file1", ((6, 13), (6, 28)), ["defn"], ["slot"; "member"]);
                 ("file1", ((63, 20), (63, 35)), ["override"], ["slot"; "member"]);
                 ("file1", ((79, 23), (79, 42)), [], ["slot"; "member"]);
                 ("file1", ((36, 20), (36, 35)), ["override"], ["slot"; "member"])]);
               ("member add_InterfaceEvent",
                [("file1", ((8, 13), (8, 27)), ["defn"], ["slot"; "member"; "add" ]);
                 ("file1", ((65, 20), (65, 34)), ["override"], ["slot"; "member"; "add" ]);
                 ("file1", ((78, 23), (78, 41)), [], ["slot"; "member"; "add" ]);
                 ("file1", ((38, 20), (38, 34)), ["override"], ["slot"; "member"; "add" ])]);
               ("member get_InterfaceEvent",
                [("file1", ((8, 13), (8, 27)), ["defn"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((65, 20), (65, 34)), ["override"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((38, 20), (38, 34)), ["override"],
                  ["slot"; "member"; "getter"])]);
               ("member get_InterfaceProperty",
                [("file1", ((4, 13), (4, 30)), ["defn"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((61, 20), (61, 37)), ["override"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((76, 23), (76, 44)), [], ["slot"; "member"; "getter"]);
                 ("file1", ((34, 20), (34, 37)), ["override"],
                  ["slot"; "member"; "getter"])]);
               ("member remove_InterfaceEvent",
                [("file1", ((8, 13), (8, 27)), ["defn"], ["slot"; "member"; "remove"]);
                 ("file1", ((65, 20), (65, 34)), ["override"], ["slot"; "member"; "remove"]);
                 ("file1", ((38, 20), (38, 34)), ["override"], ["slot"; "member"; "remove"])]);
               ("member set_InterfacePropertySet",
                [("file1", ((5, 13), (5, 33)), ["defn"],
                  ["slot"; "member"; "setter"]);
                 ("file1", ((62, 20), (62, 40)), ["override"],
                  ["slot"; "member"; "setter"]);
                 ("file1", ((77, 25), (77, 49)), [], ["slot"; "member"; "setter"]);
                 ("file1", ((35, 46), (35, 49)), ["override"],
                  ["slot"; "member"; "setter"])]);
               ("property InterfacePropertySet",
                [("file1", ((5, 13), (5, 33)), ["defn"], ["slot"; "member"; "prop"]);
                 ("file1", ((62, 20), (62, 40)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((77, 25), (77, 49)), [], ["slot"; "member"; "prop"]);
                 ("file1", ((35, 46), (35, 49)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("property InterfaceProperty",
                [("file1", ((4, 13), (4, 30)), ["defn"], ["slot"; "member"; "prop"]);
                 ("file1", ((61, 20), (61, 37)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((76, 23), (76, 44)), [], ["slot"; "member"; "prop"]);
                 ("file1", ((34, 20), (34, 37)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("property InterfaceEvent",
                [("file1", ((8, 13), (8, 27)), ["defn"], ["slot"; "member"; "prop"]);
                 ("file1", ((65, 20), (65, 34)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((38, 20), (38, 34)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("CFoo",
                [("file1", ((11, 5), (11, 9)), ["defn"], ["class"]);
                 ("file1", ((41, 12), (41, 16)), ["type"], ["class"]);
                 ("file1", ((41, 12), (41, 16)), [], ["class"]);
                 ("file1", ((69, 10), (69, 14)), ["type"], ["class"]);
                 ("file1", ((69, 10), (69, 14)), [], ["class"])]);
               ("member .ctor",
                [("file1", ((11, 5), (11, 9)), ["defn"], ["member"; "ctor"]);
                 ("file1", ((41, 12), (41, 16)), ["type"], ["member"; "ctor"]);
                 ("file1", ((41, 12), (41, 16)), [], ["member"; "ctor"]);
                 ("file1", ((69, 10), (69, 14)), ["type"], ["member"; "ctor"]);
                 ("file1", ((69, 10), (69, 14)), [], ["member"; "ctor"])]);
               ("member AbstractClassMethod",
                [("file1", ((14, 13), (14, 32)), ["defn"], ["slot"; "member"]);
                 ("file1", ((72, 22), (72, 41)), ["override"], ["slot"; "member"]);
                 ("file1", ((45, 18), (45, 37)), ["override"], ["slot"; "member"])]);
               ("member add_AbstractClassEvent",
                [("file1", ((16, 13), (16, 31)), ["defn"], ["slot"; "member"; "add" ]);
                 ("file1", ((74, 22), (74, 40)), ["override"], ["slot"; "member"; "add" ]);
                 ("file1", ((47, 18), (47, 36)), ["override"], ["slot"; "member"; "add" ])]);
               ("member get_AbstractClassEvent",
                [("file1", ((16, 13), (16, 31)), ["defn"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((74, 22), (74, 40)), ["override"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((47, 18), (47, 36)), ["override"],
                  ["slot"; "member"; "getter"])]);
               ("member get_AbstractClassProperty",
                [("file1", ((12, 13), (12, 34)), ["defn"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((70, 22), (70, 43)), ["override"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((43, 18), (43, 39)), ["override"],
                  ["slot"; "member"; "getter"])]);
               ("member remove_AbstractClassEvent",
                [("file1", ((16, 13), (16, 31)), ["defn"], ["slot"; "member"; "remove"]);
                 ("file1", ((74, 22), (74, 40)), ["override"], ["slot"; "member"; "remove"]);
                 ("file1", ((47, 18), (47, 36)), ["override"], ["slot"; "member"; "remove"])]);
               ("member set_AbstractClassPropertySet",
                [("file1", ((13, 13), (13, 37)), ["defn"],
                  ["slot"; "member"; "setter"]);
                 ("file1", ((71, 22), (71, 46)), ["override"],
                  ["slot"; "member"; "setter"]);
                 ("file1", ((44, 48), (44, 51)), ["override"],
                  ["slot"; "member"; "setter"])]);
               ("property AbstractClassPropertySet",
                [("file1", ((13, 13), (13, 37)), ["defn"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((71, 22), (71, 46)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((44, 48), (44, 51)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("property AbstractClassProperty",
                [("file1", ((12, 13), (12, 34)), ["defn"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((70, 22), (70, 43)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((43, 18), (43, 39)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("property AbstractClassEvent",
                [("file1", ((16, 13), (16, 31)), ["defn"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((74, 22), (74, 40)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((47, 18), (47, 36)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("CBaseFoo",
                [("file1", ((18, 5), (18, 13)), ["defn"], ["class"]);
                 ("file1", ((50, 12), (50, 20)), ["type"], ["class"]);
                 ("file1", ((50, 12), (50, 20)), [], ["class"])]);
               ("member .ctor",
                [("file1", ((18, 5), (18, 13)), ["defn"], ["member"; "ctor"]);
                 ("file1", ((50, 12), (50, 20)), ["type"], ["member"; "ctor"]);
                 ("file1", ((50, 12), (50, 20)), [], ["member"; "ctor"])]);
               ("member BaseClassMethod",
                [("file1", ((22, 13), (22, 28)), ["defn"], ["slot"; "member"]);
                 ("file1", ((27, 15), (27, 30)), ["override"], ["slot"; "member"]);
                 ("file1", ((54, 18), (54, 33)), ["override"], ["slot"; "member"])]);
               ("member add_BaseClassEvent",
                [("file1", ((24, 13), (24, 27)), ["defn"], ["slot"; "member"; "add" ]);
                 ("file1", ((29, 15), (29, 29)), ["override"], ["slot"; "member"; "add" ]);
                 ("file1", ((56, 18), (56, 32)), ["override"], ["slot"; "member"; "add" ])]);
               ("member get_BaseClassEvent",
                [("file1", ((24, 13), (24, 27)), ["defn"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((29, 15), (29, 29)), ["override"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((56, 18), (56, 32)), ["override"],
                  ["slot"; "member"; "getter"])]);
               ("member get_BaseClassProperty",
                [("file1", ((20, 13), (20, 30)), ["defn"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((25, 15), (25, 32)), ["override"],
                  ["slot"; "member"; "getter"]);
                 ("file1", ((52, 18), (52, 35)), ["override"],
                  ["slot"; "member"; "getter"])]);
               ("member remove_BaseClassEvent",
                [("file1", ((24, 13), (24, 27)), ["defn"], ["slot"; "member"; "remove"]);
                 ("file1", ((29, 15), (29, 29)), ["override"], ["slot"; "member"; "remove"]);
                 ("file1", ((56, 18), (56, 32)), ["override"], ["slot"; "member"; "remove"])]);
               ("member set_BaseClassPropertySet",
                [("file1", ((21, 13), (21, 33)), ["defn"],
                  ["slot"; "member"; "setter"]);
                 ("file1", ((26, 41), (26, 44)), ["override"],
                  ["slot"; "member"; "setter"]);
                 ("file1", ((53, 44), (53, 47)), ["override"],
                  ["slot"; "member"; "setter"])]);
               ("property BaseClassPropertySet",
                [("file1", ((26, 41), (26, 44)), ["defn"], ["member"; "prop"])]);
               ("property BaseClassPropertySet",
                [("file1", ((21, 13), (21, 33)), ["defn"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((26, 41), (26, 44)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((53, 44), (53, 47)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("property BaseClassProperty",
                [("file1", ((25, 15), (25, 32)), ["defn"], ["member"; "prop"])]);
               ("property BaseClassProperty",
                [("file1", ((20, 13), (20, 30)), ["defn"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((25, 15), (25, 32)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((52, 18), (52, 35)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("property BaseClassEvent",
                [("file1", ((29, 15), (29, 29)), ["defn"], ["member"; "prop"])]);
               ("property BaseClassEvent",
                [("file1", ((24, 13), (24, 27)), ["defn"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((29, 15), (29, 29)), ["override"],
                  ["slot"; "member"; "prop"]);
                 ("file1", ((56, 18), (56, 32)), ["override"],
                  ["slot"; "member"; "prop"])]);
               ("IFooImpl", [("file1", ((31, 5), (31, 13)), ["defn"], ["class"])]);
               ("member .ctor",
                [("file1", ((31, 5), (31, 13)), ["defn"], ["member"; "ctor"])]);
               ("CFooImpl", [("file1", ((40, 5), (40, 13)), ["defn"], ["class"])]);
               ("member .ctor",
                [("file1", ((40, 5), (40, 13)), ["defn"], ["member"; "ctor"])]);
               ("property AbstractClassPropertySet",
                [("file1", ((44, 48), (44, 51)), ["defn"], ["member"; "prop"])]);
               ("property AbstractClassProperty",
                [("file1", ((43, 18), (43, 39)), ["defn"], ["member"; "prop"])]);
               ("property AbstractClassEvent",
                [("file1", ((47, 18), (47, 36)), ["defn"], ["member"; "prop"])]);
               ("CBaseFooImpl", [("file1", ((49, 5), (49, 17)), ["defn"], ["class"])]);
               ("member .ctor",
                [("file1", ((49, 5), (49, 17)), ["defn"], ["member"; "ctor"])]);
               ("property BaseClassPropertySet",
                [("file1", ((53, 44), (53, 47)), ["defn"], ["member"; "prop"])]);
               ("property BaseClassProperty",
                [("file1", ((52, 18), (52, 35)), ["defn"], ["member"; "prop"])]);
               ("property BaseClassEvent",
                [("file1", ((56, 18), (56, 32)), ["defn"], ["member"; "prop"])])]

    set allUsesOfAllSymbols - set expected |> shouldEqual Set.empty
    set expected - set allUsesOfAllSymbols |> shouldEqual Set.empty
    (set expected = set allUsesOfAllSymbols) |> shouldEqual true

//-----------------------------------------------------------------------------------------

module Project4 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module M

type Foo<'T>(x : 'T, y : Foo<'T>) = class end

let inline twice(x : ^U, y : ^U) = x + y
    """
    File.WriteAllText(fileName1, fileSource1)

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)




[<Test>]
let ``Test project4 whole project errors`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project4.options) |> Async.RunSynchronously
    wholeProjectResults .Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project4 basic`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project4.options) |> Async.RunSynchronously

    set [ for x in wholeProjectResults.AssemblySignature.Entities -> x.DisplayName ] |> shouldEqual (set ["M"])

    [ for x in wholeProjectResults.AssemblySignature.Entities.[0].NestedEntities -> x.DisplayName ] 
        |> shouldEqual ["Foo"]

    [ for x in wholeProjectResults.AssemblySignature.Entities.[0].MembersFunctionsAndValues -> x.DisplayName ] 
        |> shouldEqual ["twice"]

[<Test>]
let ``Test project4 all symbols in signature`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project4.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities false wholeProjectResults.AssemblySignature.Entities
    [ for x in allSymbols -> x.ToString() ] 
      |> shouldEqual 
              ["M"; "val twice"; "generic parameter U"; "Foo`1"; "generic parameter T";
               "member .ctor"]


[<Test>]
let ``Test project4 all uses of all signature symbols`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project4.options) |> Async.RunSynchronously
    let allSymbols = allSymbolsInEntities false wholeProjectResults.AssemblySignature.Entities
    let allUsesOfAllSymbols = 
        [ for s in allSymbols do 
             let uses = [ for s in wholeProjectResults.GetUsesOfSymbol(s) |> Async.RunSynchronously -> (if s.FileName = Project4.fileName1 then "file1" else "??"), tupsZ s.RangeAlternate ]
             yield s.ToString(), uses ]
    let expected =      
      [("M", [("file1", ((1, 7), (1, 8)))]);
       ("val twice", [("file1", ((5, 11), (5, 16)))]);
       ("generic parameter U",
        [("file1", ((5, 21), (5, 23))); ("file1", ((5, 29), (5, 31)))]);
       ("Foo`1", [("file1", ((3, 5), (3, 8))); ("file1", ((3, 25), (3, 28)))]);
       ("generic parameter T",
        [("file1", ((3, 9), (3, 11))); ("file1", ((3, 17), (3, 19)));
         ("file1", ((3, 29), (3, 31)))]);
       ("member .ctor",
        [("file1", ((3, 5), (3, 8))); ("file1", ((3, 25), (3, 28)))])]
    
    set allUsesOfAllSymbols - set expected |> shouldEqual Set.empty
    set expected - set allUsesOfAllSymbols |> shouldEqual Set.empty
    (set expected = set allUsesOfAllSymbols) |> shouldEqual true

[<Test>]
let ``Test project4 T symbols`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project4.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project4.fileName1, Project4.options) 
        |> Async.RunSynchronously

    let tSymbolUse2 = backgroundTypedParse1.GetSymbolUseAtLocation(4,19,"",["T"]) |> Async.RunSynchronously
    tSymbolUse2.IsSome |> shouldEqual true
    let tSymbol2 = tSymbolUse2.Value.Symbol 
    tSymbol2.ToString() |> shouldEqual "generic parameter T"

    tSymbol2.ImplementationLocation.IsSome |> shouldEqual true

    let uses = backgroundTypedParse1.GetAllUsesOfAllSymbolsInFile() |> Async.RunSynchronously
    let allUsesOfAllSymbols = 
        [ for s in uses -> s.Symbol.ToString(), (if s.FileName = Project4.fileName1 then "file1" else "??"), tupsZ s.RangeAlternate ]
    allUsesOfAllSymbols |> shouldEqual
          [("generic parameter T", "file1", ((3, 9), (3, 11)));
           ("Foo`1", "file1", ((3, 5), (3, 8)));
           ("generic parameter T", "file1", ((3, 17), (3, 19)));
           ("Foo`1", "file1", ((3, 25), (3, 28)));
           ("generic parameter T", "file1", ((3, 29), (3, 31)));
           ("val y", "file1", ((3, 21), (3, 22)));
           ("val x", "file1", ((3, 13), (3, 14)));
           ("member .ctor", "file1", ((3, 5), (3, 8)));
           ("generic parameter U", "file1", ((5, 21), (5, 23)));
           ("generic parameter U", "file1", ((5, 29), (5, 31)));
           ("val y", "file1", ((5, 25), (5, 26)));
           ("val x", "file1", ((5, 17), (5, 18)));
           ("val op_Addition", "file1", ((5, 37), (5, 38)));
           ("val x", "file1", ((5, 35), (5, 36)));
           ("val y", "file1", ((5, 39), (5, 40)));
           ("val twice", "file1", ((5, 11), (5, 16)));
           ("M", "file1", ((1, 7), (1, 8)))]

    let tSymbolUse3 = backgroundTypedParse1.GetSymbolUseAtLocation(4,11,"",["T"]) |> Async.RunSynchronously
    tSymbolUse3.IsSome |> shouldEqual true
    let tSymbol3 = tSymbolUse3.Value.Symbol
    tSymbol3.ToString() |> shouldEqual "generic parameter T"

    tSymbol3.ImplementationLocation.IsSome |> shouldEqual true

    let usesOfTSymbol2 = 
        wholeProjectResults.GetUsesOfSymbol(tSymbol2) |> Async.RunSynchronously
        |> Array.map (fun su -> su.FileName , tupsZ su.RangeAlternate)
        |> Array.map (fun (a,b) -> (if a = Project4.fileName1 then "file1" else "??"), b)

    usesOfTSymbol2 |> shouldEqual 
          [|("file1", ((3, 9), (3, 11))); ("file1", ((3, 17), (3, 19)));
            ("file1", ((3, 29), (3, 31)))|]

    let usesOfTSymbol3 = 
        wholeProjectResults.GetUsesOfSymbol(tSymbol3) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.FileName , tupsZ su.RangeAlternate)
        |> Array.map (fun (a,b) -> (if a = Project4.fileName1 then "file1" else "??"), b)

    usesOfTSymbol3 |> shouldEqual usesOfTSymbol2

    let uSymbolUse2 = backgroundTypedParse1.GetSymbolUseAtLocation(6,23,"",["U"]) |> Async.RunSynchronously
    uSymbolUse2.IsSome |> shouldEqual true
    let uSymbol2 = uSymbolUse2.Value.Symbol
    uSymbol2.ToString() |> shouldEqual "generic parameter U"

    uSymbol2.ImplementationLocation.IsSome |> shouldEqual true

    let usesOfUSymbol2 = 
        wholeProjectResults.GetUsesOfSymbol(uSymbol2) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.FileName , tupsZ su.RangeAlternate)
        |> Array.map (fun (a,b) -> (if a = Project4.fileName1 then "file1" else "??"), b)

    usesOfUSymbol2 |> shouldEqual  [|("file1", ((5, 21), (5, 23))); ("file1", ((5, 29), (5, 31)))|]

//-----------------------------------------------------------------------------------------


module Project5 = 
    open System.IO


    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module ActivePatterns 


let (|Even|Odd|) input = if input % 2 = 0 then Even else Odd


let TestNumber input =
   match input with
   | Even -> printfn "%d is even" input
   | Odd -> printfn "%d is odd" input


let (|Float|_|) (str: string) =
   let mutable floatvalue = 0.0
   if System.Double.TryParse(str, &floatvalue) then Some(floatvalue)
   else None


let parseNumeric str =
   match str with
   | Float f -> printfn "%f : Floating point" f
   | _ -> printfn "%s : Not matched." str
    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test project5 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project5.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project 5 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project5.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.FullName, Project5.cleanFileName su.FileName, tupsZ su.RangeAlternate, attribsOfSymbolUse su)

    allUsesOfAllSymbols |> shouldEqual
          [|("symbol ", "Even", "file1", ((4, 6), (4, 10)), ["defn"]);
            ("symbol ", "Odd", "file1", ((4, 11), (4, 14)), ["defn"]);
            ("val input", "input", "file1", ((4, 17), (4, 22)), ["defn"]);
            ("val op_Equality", "Microsoft.FSharp.Core.Operators.( = )", "file1",
             ((4, 38), (4, 39)), []);
            ("val op_Modulus", "Microsoft.FSharp.Core.Operators.( % )", "file1",
             ((4, 34), (4, 35)), []);
            ("val input", "input", "file1", ((4, 28), (4, 33)), []);
            ("symbol ", "Even", "file1", ((4, 47), (4, 51)), []);
            ("symbol ", "Odd", "file1", ((4, 57), (4, 60)), []);
            ("val |Even|Odd|", "ActivePatterns.( |Even|Odd| )", "file1",
             ((4, 5), (4, 15)), ["defn"]);
            ("val input", "input", "file1", ((7, 15), (7, 20)), ["defn"]);
            ("val input", "input", "file1", ((8, 9), (8, 14)), []);
            ("symbol Even", "ActivePatterns.( |Even|Odd| ).Even", "file1",
             ((9, 5), (9, 9)), ["pattern"]);
            ("val printfn", "Microsoft.FSharp.Core.ExtraTopLevelOperators.printfn",
             "file1", ((9, 13), (9, 20)), []);
            ("val input", "input", "file1", ((9, 34), (9, 39)), []);
            ("symbol Odd", "ActivePatterns.( |Even|Odd| ).Odd", "file1",
             ((10, 5), (10, 8)), ["pattern"]);
            ("val printfn", "Microsoft.FSharp.Core.ExtraTopLevelOperators.printfn",
             "file1", ((10, 12), (10, 19)), []);
            ("val input", "input", "file1", ((10, 32), (10, 37)), []);
            ("val TestNumber", "ActivePatterns.TestNumber", "file1", ((7, 4), (7, 14)),
             ["defn"]); ("symbol ", "Float", "file1", ((13, 6), (13, 11)), ["defn"]);
            ("string", "Microsoft.FSharp.Core.string", "file1", ((13, 22), (13, 28)),
             ["type"]); ("val str", "str", "file1", ((13, 17), (13, 20)), ["defn"]);
            ("val floatvalue", "floatvalue", "file1", ((14, 15), (14, 25)), ["defn"]);
            ("Double", "System.Double", "file1", ((15, 13), (15, 19)), []);
            ("System", "System", "file1", ((15, 6), (15, 12)), []);
            ("val str", "str", "file1", ((15, 29), (15, 32)), []);
            ("val op_AddressOf",
             "Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators.( ~& )",
             "file1", ((15, 34), (15, 35)), []);
            ("val floatvalue", "floatvalue", "file1", ((15, 35), (15, 45)), []);
            ("member TryParse", "System.Double.TryParse", "file1", ((15, 6), (15, 28)),
             []);
            ("Some", "Microsoft.FSharp.Core.Option<_>.Some", "file1",
             ((15, 52), (15, 56)), []);
            ("val floatvalue", "floatvalue", "file1", ((15, 57), (15, 67)), []);
            ("None", "Microsoft.FSharp.Core.Option<_>.None", "file1",
             ((16, 8), (16, 12)), []);
            ("val |Float|_|", "ActivePatterns.( |Float|_| )", "file1",
             ((13, 5), (13, 14)), ["defn"]);
            ("val str", "str", "file1", ((19, 17), (19, 20)), ["defn"]);
            ("val str", "str", "file1", ((20, 9), (20, 12)), []);
            ("val f", "f", "file1", ((21, 11), (21, 12)), ["defn"]);
            ("symbol Float", "ActivePatterns.( |Float|_| ).Float", "file1",
             ((21, 5), (21, 10)), ["pattern"]);
            ("val printfn", "Microsoft.FSharp.Core.ExtraTopLevelOperators.printfn",
             "file1", ((21, 16), (21, 23)), []);
            ("val f", "f", "file1", ((21, 46), (21, 47)), []);
            ("val printfn", "Microsoft.FSharp.Core.ExtraTopLevelOperators.printfn",
             "file1", ((22, 10), (22, 17)), []);
            ("val str", "str", "file1", ((22, 38), (22, 41)), []);
            ("val parseNumeric", "ActivePatterns.parseNumeric", "file1",
             ((19, 4), (19, 16)), ["defn"]);
            ("ActivePatterns", "ActivePatterns", "file1", ((1, 7), (1, 21)), ["defn"])|]

[<Test>]
let ``Test complete active patterns's exact ranges from uses of symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project5.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project5.fileName1, Project5.options) 
        |> Async.RunSynchronously


    let oddSymbolUse = backgroundTypedParse1.GetSymbolUseAtLocation(11,8,"",["Odd"]) |> Async.RunSynchronously
    oddSymbolUse.IsSome |> shouldEqual true  
    let oddSymbol = oddSymbolUse.Value.Symbol
    oddSymbol.ToString() |> shouldEqual "symbol Odd"

    let evenSymbolUse = backgroundTypedParse1.GetSymbolUseAtLocation(10,9,"",["Even"]) |> Async.RunSynchronously
    evenSymbolUse.IsSome |> shouldEqual true  
    let evenSymbol = evenSymbolUse.Value.Symbol
    evenSymbol.ToString() |> shouldEqual "symbol Even"

    let usesOfEvenSymbol = 
        wholeProjectResults.GetUsesOfSymbol(evenSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), Project5.cleanFileName su.FileName, tupsZ su.RangeAlternate)

    let usesOfOddSymbol = 
        wholeProjectResults.GetUsesOfSymbol(oddSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), Project5.cleanFileName su.FileName, tupsZ su.RangeAlternate)

    usesOfEvenSymbol |> shouldEqual 
          [|("symbol Even", "file1", ((4, 6), (4, 10)));
            ("symbol Even", "file1", ((4, 47), (4, 51)));
            ("symbol Even", "file1", ((9, 5), (9, 9)))|]

    usesOfOddSymbol |> shouldEqual 
          [|("symbol Odd", "file1", ((4, 11), (4, 14)));
            ("symbol Odd", "file1", ((4, 57), (4, 60)));
            ("symbol Odd", "file1", ((10, 5), (10, 8)))|]


[<Test>]
let ``Test partial active patterns's exact ranges from uses of symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project5.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project5.fileName1, Project5.options) 
        |> Async.RunSynchronously    


    let floatSymbolUse = backgroundTypedParse1.GetSymbolUseAtLocation(22,10,"",["Float"]) |> Async.RunSynchronously
    floatSymbolUse.IsSome |> shouldEqual true  
    let floatSymbol = floatSymbolUse.Value.Symbol 
    floatSymbol.ToString() |> shouldEqual "symbol Float"


    let usesOfFloatSymbol = 
        wholeProjectResults.GetUsesOfSymbol(floatSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), Project5.cleanFileName su.FileName, tups su.RangeAlternate)

    usesOfFloatSymbol |> shouldEqual 
          [|("symbol Float", "file1", ((14, 6), (14, 11)));
            ("symbol Float", "file1", ((22, 5), (22, 10)))|]

    // Should also return its definition
    let floatSymUseOpt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(14,11,"",["Float"])
        |> Async.RunSynchronously

    floatSymUseOpt.IsSome |> shouldEqual true


//-----------------------------------------------------------------------------------------

module Project6 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Exceptions

exception Fail of string

let f () =
   raise (Fail "unknown")
    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test project6 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project6.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project 6 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project6.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), Project6.cleanFileName su.FileName, tupsZ su.RangeAlternate, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("string", "file1", ((3, 18), (3, 24)), ["abbrev"]);
            ("Fail", "file1", ((3, 10), (3, 14)), ["exn"]);
            ("val raise", "file1", ((6, 3), (6, 8)), ["val"]);
            ("Fail", "file1", ((6, 10), (6, 14)), ["exn"]);
            ("val f", "file1", ((5, 4), (5, 5)), ["val"]);
            ("Exceptions", "file1", ((1, 7), (1, 17)), ["module"])|]


//-----------------------------------------------------------------------------------------

module Project7 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module NamedArgs

type C() = 
    static member M(arg1: int, arg2: int, ?arg3 : int) = arg1 + arg2 + defaultArg arg3 4

let x1 = C.M(arg1 = 3, arg2 = 4, arg3 = 5)

let x2 = C.M(arg1 = 3, arg2 = 4, ?arg3 = Some 5)

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test project7 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project7.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project 7 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project7.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project7.cleanFileName su.FileName, tups su.RangeAlternate)

    let arg1symbol = 
        wholeProjectResults.GetAllUsesOfAllSymbols() 
        |> Async.RunSynchronously
        |> Array.pick (fun x -> if x.Symbol.DisplayName = "arg1" then Some x.Symbol else None)
    let arg1uses = 
        wholeProjectResults.GetUsesOfSymbol(arg1symbol) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), Option.map tups su.Symbol.DeclarationLocation, Project7.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbol su.Symbol)
    arg1uses |> shouldEqual
          [|("val arg1", Some ((5, 20), (5, 24)), "file1", ((5, 20), (5, 24)), []);
            ("val arg1", Some ((5, 20), (5, 24)), "file1", ((5, 57), (5, 61)), []);
            ("val arg1", Some ((5, 20), (5, 24)), "file1", ((7, 13), (7, 17)), []);
            ("val arg1", Some ((5, 20), (5, 24)), "file1", ((9, 13), (9, 17)), [])|]


//-----------------------------------------------------------------------------------------
module Project8 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module NamedUnionFields

type A = B of xxx: int * yyy : int
let b = B(xxx=1, yyy=2)

let x = 
    match b with
    // does not find usage here
    | B (xxx = a; yyy = b) -> ()
    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test project8 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project8.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project 8 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project8.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project8.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols 
      |> shouldEqual
              [|("int", "int", "file1", ((4, 19), (4, 22)), ["type"], ["abbrev"]);
                ("int", "int", "file1", ((4, 31), (4, 34)), ["type"], ["abbrev"]);
                ("int", "int", "file1", ((4, 19), (4, 22)), ["type"], ["abbrev"]);
                ("parameter xxx", "xxx", "file1", ((4, 14), (4, 17)), ["defn"], []);
                ("int", "int", "file1", ((4, 31), (4, 34)), ["type"], ["abbrev"]);
                ("parameter yyy", "yyy", "file1", ((4, 25), (4, 28)), ["defn"], []);
                ("B", "B", "file1", ((4, 9), (4, 10)), ["defn"], []);
                ("A", "A", "file1", ((4, 5), (4, 6)), ["defn"], ["union"]);
                ("B", "B", "file1", ((5, 8), (5, 9)), [], []);
                ("parameter xxx", "xxx", "file1", ((5, 10), (5, 13)), [], []);
                ("parameter yyy", "yyy", "file1", ((5, 17), (5, 20)), [], []);
                ("val b", "b", "file1", ((5, 4), (5, 5)), ["defn"], ["val"]);
                ("val b", "b", "file1", ((8, 10), (8, 11)), [], ["val"]);
                ("parameter xxx", "xxx", "file1", ((10, 9), (10, 12)), ["pattern"], []);
                ("parameter yyy", "yyy", "file1", ((10, 18), (10, 21)), ["pattern"], []);
                ("val b", "b", "file1", ((10, 24), (10, 25)), ["defn"], []);
                ("val a", "a", "file1", ((10, 15), (10, 16)), ["defn"], []);
                ("B", "B", "file1", ((10, 6), (10, 7)), ["pattern"], []);
                ("val x", "x", "file1", ((7, 4), (7, 5)), ["defn"], ["val"]);
                ("NamedUnionFields", "NamedUnionFields", "file1", ((2, 7), (2, 23)),
                 ["defn"], ["module"])|]

    let arg1symbol = 
        wholeProjectResults.GetAllUsesOfAllSymbols() 
        |> Async.RunSynchronously
        |> Array.pick (fun x -> if x.Symbol.DisplayName = "xxx" then Some x.Symbol else None)
    let arg1uses = 
        wholeProjectResults.GetUsesOfSymbol(arg1symbol) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> Option.map tups su.Symbol.DeclarationLocation, Project8.cleanFileName su.FileName, tups su.RangeAlternate)

    arg1uses |> shouldEqual
     [|(Some ((4, 14), (4, 17)), "file1", ((4, 14), (4, 17)));
       (Some ((4, 14), (4, 17)), "file1", ((5, 10), (5, 13)));
       (Some ((4, 14), (4, 17)), "file1", ((10, 9), (10, 12)))|]

//-----------------------------------------------------------------------------------------
module Project9 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Constraints

let inline check< ^T when ^T : (static member IsInfinity : ^T -> bool)> (num: ^T) : ^T option =
    if (^T : (static member IsInfinity: ^T -> bool) (num)) then None
    else Some num
    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test project9 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project9.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test project 9 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project9.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project9.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("generic parameter T", "T", "file1", ((4, 18), (4, 20)), []);
            ("generic parameter T", "T", "file1", ((4, 26), (4, 28)), []);
            ("generic parameter T", "T", "file1", ((4, 59), (4, 61)), []);
            ("bool", "bool", "file1", ((4, 65), (4, 69)), ["abbrev"]);
            ("parameter IsInfinity", "IsInfinity", "file1", ((4, 46), (4, 56)), []);
            ("generic parameter T", "T", "file1", ((4, 78), (4, 80)), []);
            ("val num", "num", "file1", ((4, 73), (4, 76)), []);
            ("option`1", "option", "file1", ((4, 87), (4, 93)), ["abbrev"]);
            ("generic parameter T", "T", "file1", ((4, 84), (4, 86)), []);
            ("generic parameter T", "T", "file1", ((5, 8), (5, 10)), []);
            ("generic parameter T", "T", "file1", ((5, 40), (5, 42)), []);
            ("bool", "bool", "file1", ((5, 46), (5, 50)), ["abbrev"]);
            ("parameter IsInfinity", "IsInfinity", "file1", ((5, 28), (5, 38)), []);
            ("val num", "num", "file1", ((5, 53), (5, 56)), []);
            ("None", "None", "file1", ((5, 64), (5, 68)), []);
            ("Some", "Some", "file1", ((6, 9), (6, 13)), []);
            ("val num", "num", "file1", ((6, 14), (6, 17)), []);
            ("val check", "check", "file1", ((4, 11), (4, 16)), ["val"]);
            ("Constraints", "Constraints", "file1", ((2, 7), (2, 18)), ["module"])|]

    let arg1symbol = 
        wholeProjectResults.GetAllUsesOfAllSymbols() 
        |> Async.RunSynchronously
        |> Array.pick (fun x -> if x.Symbol.DisplayName = "IsInfinity" then Some x.Symbol else None)
    let arg1uses = 
        wholeProjectResults.GetUsesOfSymbol(arg1symbol) 
        |> Async.RunSynchronously
        |> Array.map (fun su -> Option.map tups su.Symbol.DeclarationLocation, Project9.cleanFileName su.FileName, tups su.RangeAlternate)

    arg1uses |> shouldEqual
     [|(Some ((4, 46), (4, 56)), "file1", ((4, 46), (4, 56)))|]

//-----------------------------------------------------------------------------------------
// see https://github.com/fsharp/FSharp.Compiler.Service/issues/95

module Project10 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module NamedArgs

type C() = 
    static member M(url: string, query: int)  = ()

C.M("http://goo", query = 1)

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project10 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project10.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project10 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project10.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project10.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("C", "C", "file1", ((4, 5), (4, 6)), ["class"]);
            ("member .ctor", "( .ctor )", "file1", ((4, 5), (4, 6)),
             ["member"; "ctor"]);
            ("string", "string", "file1", ((5, 25), (5, 31)), ["abbrev"]);
            ("int", "int", "file1", ((5, 40), (5, 43)), ["abbrev"]);
            ("member M", "M", "file1", ((5, 18), (5, 19)), ["member"]);
            ("string", "string", "file1", ((5, 25), (5, 31)), ["abbrev"]);
            ("int", "int", "file1", ((5, 40), (5, 43)), ["abbrev"]);
            ("val url", "url", "file1", ((5, 20), (5, 23)), []);
            ("val query", "query", "file1", ((5, 33), (5, 38)), []);
            ("C", "C", "file1", ((7, 0), (7, 1)), ["class"]);
            ("member M", "M", "file1", ((7, 0), (7, 3)), ["member"]);
            ("parameter query", "query", "file1", ((7, 18), (7, 23)), []);
            ("NamedArgs", "NamedArgs", "file1", ((2, 7), (2, 16)), ["module"])|]

    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project10.fileName1, Project10.options) 
        |> Async.RunSynchronously

    let querySymbolUseOpt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(7,23,"",["query"]) 
        |> Async.RunSynchronously

    let querySymbolUse = querySymbolUseOpt.Value
    let querySymbol = querySymbolUse.Symbol
    querySymbol.ToString() |> shouldEqual "parameter query"

    let querySymbolUse2Opt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(7,22,"",["query"])
        |> Async.RunSynchronously

    let querySymbolUse2 = querySymbolUse2Opt.Value
    let querySymbol2 = querySymbolUse2.Symbol
    querySymbol2.ToString() |> shouldEqual "val query" // This is perhaps the wrong result, but not that the input location was wrong - was not the "column at end of names"

//-----------------------------------------------------------------------------------------
// see https://github.com/fsharp/FSharp.Compiler.Service/issues/92

module Project11 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module NestedTypes

let enum = new System.Collections.Generic.Dictionary<int,int>.Enumerator()
let fff (x:System.Collections.Generic.Dictionary<int,int>.Enumerator) = ()

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project11 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project11.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project11 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project11.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project11.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("Generic", "Generic", "file1", ((4, 34), (4, 41)), ["type"],
             ["namespace"]);
            ("Collections", "Collections", "file1", ((4, 22), (4, 33)), ["type"],
             ["namespace"]);
            ("System", "System", "file1", ((4, 15), (4, 21)), ["type"], ["namespace"]);
            ("Dictionary`2", "Dictionary", "file1", ((4, 15), (4, 52)), ["type"],
             ["class"]); ("int", "int", "file1", ((4, 53), (4, 56)), [], ["abbrev"]);
            ("int", "int", "file1", ((4, 57), (4, 60)), [], ["abbrev"]);
            ("Enumerator", "Enumerator", "file1", ((4, 62), (4, 72)), ["type"],
             ["valuetype"]);
            ("member .ctor", "Enumerator", "file1", ((4, 15), (4, 72)), [], ["member"]);
            ("val enum", "enum", "file1", ((4, 4), (4, 8)), ["defn"], ["val"]);
            ("Generic", "Generic", "file1", ((5, 30), (5, 37)), ["type"],
             ["namespace"]);
            ("Collections", "Collections", "file1", ((5, 18), (5, 29)), ["type"],
             ["namespace"]);
            ("System", "System", "file1", ((5, 11), (5, 17)), ["type"], ["namespace"]);
            ("Dictionary`2", "Dictionary", "file1", ((5, 11), (5, 48)), ["type"],
             ["class"]);
            ("int", "int", "file1", ((5, 49), (5, 52)), ["type"], ["abbrev"]);
            ("int", "int", "file1", ((5, 53), (5, 56)), ["type"], ["abbrev"]);
            ("Enumerator", "Enumerator", "file1", ((5, 58), (5, 68)), ["type"],
             ["valuetype"]); ("val x", "x", "file1", ((5, 9), (5, 10)), ["defn"], []);
            ("val fff", "fff", "file1", ((5, 4), (5, 7)), ["defn"], ["val"]);
            ("NestedTypes", "NestedTypes", "file1", ((2, 7), (2, 18)), ["defn"],
             ["module"])|]

//-----------------------------------------------------------------------------------------
// see https://github.com/fsharp/FSharp.Compiler.Service/issues/92

module Project12 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module ComputationExpressions

let x1 = seq { for i in 0 .. 100 -> i }
let x2 = query { for i in 0 .. 100 do
                 where (i = 0)
                 select (i,i) }

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project12 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project12.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project12 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project12.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project12.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("val seq", "seq", "file1", ((4, 9), (4, 12)), ["compexpr"], ["val"]);
            ("val op_Range", "( .. )", "file1", ((4, 26), (4, 28)), [], ["val"]);
            ("val i", "i", "file1", ((4, 19), (4, 20)), ["defn"], []);
            ("val i", "i", "file1", ((4, 36), (4, 37)), [], []);
            ("val x1", "x1", "file1", ((4, 4), (4, 6)), ["defn"], ["val"]);
            ("val query", "query", "file1", ((5, 9), (5, 14)), [], ["val"]);
            ("val query", "query", "file1", ((5, 9), (5, 14)), ["compexpr"], ["val"]);
            ("member Where", "where", "file1", ((6, 17), (6, 22)), ["compexpr"],
             ["member"]);
            ("member Select", "select", "file1", ((7, 17), (7, 23)), ["compexpr"],
             ["member"]);
            ("val op_Range", "( .. )", "file1", ((5, 28), (5, 30)), [], ["val"]);
            ("val i", "i", "file1", ((5, 21), (5, 22)), ["defn"], []);
            ("val op_Equality", "( = )", "file1", ((6, 26), (6, 27)), [], ["val"]);
            ("val i", "i", "file1", ((6, 24), (6, 25)), [], []);
            ("val i", "i", "file1", ((7, 25), (7, 26)), [], []);
            ("val i", "i", "file1", ((7, 27), (7, 28)), [], []);
            ("val x2", "x2", "file1", ((5, 4), (5, 6)), ["defn"], ["val"]);
            ("ComputationExpressions", "ComputationExpressions", "file1",
             ((2, 7), (2, 29)), ["defn"], ["module"])|]

//-----------------------------------------------------------------------------------------
// Test fetching information about some external types (e.g. System.Object, System.DateTime)

module Project13 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module ExternalTypes

let x1  = new System.Object()
let x2  = new System.DateTime(1,1,1)
let x3 = new System.DateTime()

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project13 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project13.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project13 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project13.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project13.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("System", "System", "file1", ((4, 14), (4, 20)), ["type"], ["namespace"]);
            ("Object", "Object", "file1", ((4, 14), (4, 27)), [], ["class"]);
            ("member .ctor", "Object", "file1", ((4, 14), (4, 27)), [], ["member"]);
            ("val x1", "x1", "file1", ((4, 4), (4, 6)), ["defn"], ["val"]);
            ("System", "System", "file1", ((5, 14), (5, 20)), ["type"], ["namespace"]);
            ("DateTime", "DateTime", "file1", ((5, 14), (5, 29)), [], ["valuetype"]);
            ("member .ctor", "DateTime", "file1", ((5, 14), (5, 29)), [], ["member"]);
            ("val x2", "x2", "file1", ((5, 4), (5, 6)), ["defn"], ["val"]);
            ("System", "System", "file1", ((6, 13), (6, 19)), ["type"], ["namespace"]);
            ("DateTime", "DateTime", "file1", ((6, 13), (6, 28)), [], ["valuetype"]);
            ("member .ctor", "DateTime", "file1", ((6, 13), (6, 28)), [], ["member"]);
            ("val x3", "x3", "file1", ((6, 4), (6, 6)), ["defn"], ["val"]);
            ("ExternalTypes", "ExternalTypes", "file1", ((2, 7), (2, 20)), ["defn"],
             ["module"])|]
    

    let objSymbol = wholeProjectResults.GetAllUsesOfAllSymbols() |> Async.RunSynchronously |> Array.find (fun su -> su.Symbol.DisplayName = "Object")
    let objEntity = objSymbol.Symbol :?> FSharpEntity
    let objMemberNames = [ for x in objEntity.MembersFunctionsAndValues -> x.DisplayName ]
    set objMemberNames |> shouldEqual (set [".ctor"; "ToString"; "Equals"; "Equals"; "ReferenceEquals"; "GetHashCode"; "GetType"; "Finalize"; "MemberwiseClone"])
       
    let dtSymbol = wholeProjectResults.GetAllUsesOfAllSymbols() |> Async.RunSynchronously |> Array.find (fun su -> su.Symbol.DisplayName = "DateTime")
    let dtEntity = dtSymbol.Symbol :?> FSharpEntity
    let dtPropNames = [ for x in dtEntity.MembersFunctionsAndValues do if x.IsProperty then yield x.DisplayName ]

    let dtType = dtSymbol.Symbol:?> FSharpEntity

    set [ for i in dtType.DeclaredInterfaces -> i.ToString() ] |> shouldEqual
        (set
          ["type System.IComparable"; 
           "type System.IFormattable";
           "type System.IConvertible";
           "type System.Runtime.Serialization.ISerializable";
           "type System.IComparable<System.DateTime>";
           "type System.IEquatable<System.DateTime>"])

    dtType.BaseType.ToString() |> shouldEqual "Some(type System.ValueType)"
    
    set ["Date"; "Day"; "DayOfWeek"; "DayOfYear"; "Hour"; "Kind"; "Millisecond"; "Minute"; "Month"; "Now"; "Second"; "Ticks"; "TimeOfDay"; "Today"; "Year"]  
    - set dtPropNames  
      |> shouldEqual (set [])

    let objDispatchSlotNames = [ for x in objEntity.MembersFunctionsAndValues do if x.IsDispatchSlot then yield x.DisplayName ]
    
    set objDispatchSlotNames |> shouldEqual (set ["ToString"; "Equals"; "GetHashCode"; "Finalize"])

    // check we can get the CurriedParameterGroups
    let objMethodsCurriedParameterGroups = 
        [ for x in objEntity.MembersFunctionsAndValues do 
             for pg in x.CurriedParameterGroups do 
                 for p in pg do 
                     yield x.CompiledName, p.Name,  p.Type.ToString(), p.Type.Format(dtSymbol.DisplayContext) ]

    objMethodsCurriedParameterGroups |> shouldEqual 
          [("Equals", Some "obj", "type Microsoft.FSharp.Core.obj", "obj");
           ("Equals", Some "objA", "type Microsoft.FSharp.Core.obj", "obj");
           ("Equals", Some "objB", "type Microsoft.FSharp.Core.obj", "obj");
           ("ReferenceEquals", Some "objA", "type Microsoft.FSharp.Core.obj", "obj");
           ("ReferenceEquals", Some "objB", "type Microsoft.FSharp.Core.obj", "obj")]

    // check we can get the ReturnParameter
    let objMethodsReturnParameter = 
        [ for x in objEntity.MembersFunctionsAndValues do 
             let p = x.ReturnParameter 
             yield x.DisplayName, p.Name,  p.Type.ToString(), p.Type.Format(dtSymbol.DisplayContext) ]
    set objMethodsReturnParameter |> shouldEqual
       (set
           [(".ctor", None, "type Microsoft.FSharp.Core.unit", "unit");
            ("ToString", None, "type Microsoft.FSharp.Core.string", "string");
            ("Equals", None, "type Microsoft.FSharp.Core.bool", "bool");
            ("Equals", None, "type Microsoft.FSharp.Core.bool", "bool");
            ("ReferenceEquals", None, "type Microsoft.FSharp.Core.bool", "bool");
            ("GetHashCode", None, "type Microsoft.FSharp.Core.int", "int");
            ("GetType", None, "type System.Type", "System.Type");
            ("Finalize", None, "type Microsoft.FSharp.Core.unit", "unit");
            ("MemberwiseClone", None, "type Microsoft.FSharp.Core.obj", "obj")])

    // check we can get the CurriedParameterGroups
    let dtMethodsCurriedParameterGroups = 
        [ for x in dtEntity.MembersFunctionsAndValues do 
           if x.CompiledName = "FromFileTime" || x.CompiledName = "AddMilliseconds"  then 
             for pg in x.CurriedParameterGroups do 
                 for p in pg do 
                     yield x.CompiledName, p.Name,  p.Type.ToString(), p.Type.Format(dtSymbol.DisplayContext) ]

    dtMethodsCurriedParameterGroups |> shouldEqual 
          [("AddMilliseconds", Some "value", "type Microsoft.FSharp.Core.float","float");
           ("FromFileTime", Some "fileTime", "type Microsoft.FSharp.Core.int64","int64")]


    let _test1 = [ for x in objEntity.MembersFunctionsAndValues -> x.FullType ]
    for x in objEntity.MembersFunctionsAndValues do 
       x.IsCompilerGenerated |> shouldEqual false
       x.IsExtensionMember |> shouldEqual false
       x.IsEvent |> shouldEqual false
       x.IsProperty |> shouldEqual false
       x.IsPropertySetterMethod |> shouldEqual false
       x.IsPropertyGetterMethod |> shouldEqual false
       x.IsImplicitConstructor |> shouldEqual false
       x.IsTypeFunction |> shouldEqual false
       x.IsUnresolved |> shouldEqual false
    ()

//-----------------------------------------------------------------------------------------
// Misc - structs

module Project14 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Structs

[<Struct>]
type S(p:int) = 
   member x.P = p

let x1  = S()
let x2  = S(3)

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project14 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project14.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project14 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project14.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project14.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su)

    allUsesOfAllSymbols |> shouldEqual
          [|("StructAttribute", "StructAttribute", "file1", ((4, 2), (4, 8)),
             ["attribute"]);
            ("StructAttribute", "StructAttribute", "file1", ((4, 2), (4, 8)), ["type"]);
            ("member .ctor", "StructAttribute", "file1", ((4, 2), (4, 8)), []);
            ("int", "int", "file1", ((5, 9), (5, 12)), ["type"]);
            ("int", "int", "file1", ((5, 9), (5, 12)), ["type"]);
            ("S", "S", "file1", ((5, 5), (5, 6)), ["defn"]);
            ("int", "int", "file1", ((5, 9), (5, 12)), ["type"]);
            ("val p", "p", "file1", ((5, 7), (5, 8)), ["defn"]);
            ("member .ctor", "( .ctor )", "file1", ((5, 5), (5, 6)), ["defn"]);
            ("member get_P", "P", "file1", ((6, 12), (6, 13)), ["defn"]);
            ("val x", "x", "file1", ((6, 10), (6, 11)), ["defn"]);
            ("val p", "p", "file1", ((6, 16), (6, 17)), []);
            ("member .ctor", ".ctor", "file1", ((8, 10), (8, 11)), []);
            ("val x1", "x1", "file1", ((8, 4), (8, 6)), ["defn"]);
            ("member .ctor", ".ctor", "file1", ((9, 10), (9, 11)), []);
            ("val x2", "x2", "file1", ((9, 4), (9, 6)), ["defn"]);
            ("Structs", "Structs", "file1", ((2, 7), (2, 14)), ["defn"])|]

//-----------------------------------------------------------------------------------------
// Misc - union patterns

module Project15 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module UnionPatterns

let f x = 
    match x with 
    | [h] 
    | [_; h] 
    | [_; _; h] -> h 
    | _ -> 0

    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project15 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project15.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project15 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project15.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project15.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su)

    allUsesOfAllSymbols |> shouldEqual
          [|("val x", "x", "file1", ((4, 6), (4, 7)), ["defn"]);
            ("val x", "x", "file1", ((5, 10), (5, 11)), []);
            ("val h", "h", "file1", ((6, 7), (6, 8)), ["defn"]);
            ("val h", "h", "file1", ((7, 10), (7, 11)), ["defn"]);
            ("val h", "h", "file1", ((8, 13), (8, 14)), ["defn"]);
            ("val h", "h", "file1", ((8, 19), (8, 20)), []);
            ("val f", "f", "file1", ((4, 4), (4, 5)), ["defn"]);
            ("UnionPatterns", "UnionPatterns", "file1", ((2, 7), (2, 20)), ["defn"])|]


//-----------------------------------------------------------------------------------------
// Misc - signature files

module Project16 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let sigFileName1 = Path.ChangeExtension(fileName1, ".fsi")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

type C() = 
    member x.PC = 1

and D() = 
    member x.PD = 1

and E() = 
    member x.PE = 1
    """
    File.WriteAllText(fileName1, fileSource1)

    let sigFileSource1 = """
module Impl

type C = 
    new : unit -> C
    member PC : int

and [<Class>] D = 
    new : unit -> D
    member PD : int

and [<Class>] E = 
    new : unit -> E
    member PE : int
    """
    File.WriteAllText(sigFileName1, sigFileSource1)
    let cleanFileName a = if a = fileName1 then "file1" elif a = sigFileName1 then "sig1"  else "??"

    let fileNames = [sigFileName1; fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project16 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project16.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project16 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project16.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project16.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("ClassAttribute", "ClassAttribute", "sig1", ((8, 6), (8, 11)),
             ["attribute"], ["class"]);
            ("ClassAttribute", "ClassAttribute", "sig1", ((8, 6), (8, 11)), ["type"],
             ["class"]);
            ("member .ctor", "ClassAttribute", "sig1", ((8, 6), (8, 11)), [],
             ["member"]);
            ("ClassAttribute", "ClassAttribute", "sig1", ((12, 6), (12, 11)),
             ["attribute"], ["class"]);
            ("ClassAttribute", "ClassAttribute", "sig1", ((12, 6), (12, 11)), ["type"],
             ["class"]);
            ("member .ctor", "ClassAttribute", "sig1", ((12, 6), (12, 11)), [],
             ["member"]); ("C", "C", "sig1", ((4, 5), (4, 6)), ["defn"], ["class"]);
            ("unit", "unit", "sig1", ((5, 10), (5, 14)), ["type"], ["abbrev"]);
            ("C", "C", "sig1", ((5, 18), (5, 19)), ["type"], ["class"]);
            ("member .ctor", "( .ctor )", "sig1", ((5, 4), (5, 7)), ["defn"],
             ["member"]);
            ("int", "int", "sig1", ((6, 16), (6, 19)), ["type"], ["abbrev"]);
            ("member get_PC", "PC", "sig1", ((6, 11), (6, 13)), ["defn"], ["member"]);
            ("D", "D", "sig1", ((8, 14), (8, 15)), ["defn"], ["class"]);
            ("unit", "unit", "sig1", ((9, 10), (9, 14)), ["type"], ["abbrev"]);
            ("D", "D", "sig1", ((9, 18), (9, 19)), ["type"], ["class"]);
            ("member .ctor", "( .ctor )", "sig1", ((9, 4), (9, 7)), ["defn"],
             ["member"]);
            ("int", "int", "sig1", ((10, 16), (10, 19)), ["type"], ["abbrev"]);
            ("member get_PD", "PD", "sig1", ((10, 11), (10, 13)), ["defn"], ["member"]);
            ("E", "E", "sig1", ((12, 14), (12, 15)), ["defn"], ["class"]);
            ("unit", "unit", "sig1", ((13, 10), (13, 14)), ["type"], ["abbrev"]);
            ("E", "E", "sig1", ((13, 18), (13, 19)), ["type"], ["class"]);
            ("member .ctor", "( .ctor )", "sig1", ((13, 4), (13, 7)), ["defn"],
             ["member"]);
            ("int", "int", "sig1", ((14, 16), (14, 19)), ["type"], ["abbrev"]);
            ("member get_PE", "PE", "sig1", ((14, 11), (14, 13)), ["defn"], ["member"]);
            ("Impl", "Impl", "sig1", ((2, 7), (2, 11)), ["defn"], ["module"]);
            ("C", "C", "file1", ((4, 5), (4, 6)), ["defn"], ["class"]);
            ("D", "D", "file1", ((7, 4), (7, 5)), ["defn"], ["class"]);
            ("E", "E", "file1", ((10, 4), (10, 5)), ["defn"], ["class"]);
            ("member .ctor", "( .ctor )", "file1", ((4, 5), (4, 6)), ["defn"],
             ["member"; "ctor"]);
            ("member get_PC", "PC", "file1", ((5, 13), (5, 15)), ["defn"], ["member"]);
            ("member .ctor", "( .ctor )", "file1", ((7, 4), (7, 5)), ["defn"],
             ["member"; "ctor"]);
            ("member get_PD", "PD", "file1", ((8, 13), (8, 15)), ["defn"], ["member"]);
            ("member .ctor", "( .ctor )", "file1", ((10, 4), (10, 5)), ["defn"],
             ["member"; "ctor"]);
            ("member get_PE", "PE", "file1", ((11, 13), (11, 15)), ["defn"],
             ["member"]); ("val x", "x", "file1", ((5, 11), (5, 12)), ["defn"], []);
            ("val x", "x", "file1", ((8, 11), (8, 12)), ["defn"], []);
            ("val x", "x", "file1", ((11, 11), (11, 12)), ["defn"], []);
            ("Impl", "Impl", "file1", ((2, 7), (2, 11)), ["defn"], ["module"])|]



//-----------------------------------------------------------------------------------------
// Misc - namespace symbols

module Project17 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

let _ = Microsoft.FSharp.Collections.List<int>.Empty // check use of getter property using long namespace

let f1 (x: System.Collections.Generic.IList<'T>) = x.Item(3), x.[3], x.Count  // check use of getter properties and indexer

let f2 (x: System.Collections.Generic.IList<int>) = x.[3] <- 4  // check use of .NET setter indexer

let f3 (x: System.Exception) = x.HelpLink <- "" // check use of .NET setter property
    """
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project17 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project17.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project17 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project17.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project17.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols 
      |> shouldEqual
              [|("Collections", "Collections", "file1", ((4, 25), (4, 36)), [],
                 ["namespace"]);
                ("FSharp", "FSharp", "file1", ((4, 18), (4, 24)), [], ["namespace"]);
                ("Microsoft", "Microsoft", "file1", ((4, 8), (4, 17)), [], ["namespace"]);
                ("FSharpList`1", "List", "file1", ((4, 8), (4, 41)), [], ["union"]);
                ("int", "int", "file1", ((4, 42), (4, 45)), ["type"], ["abbrev"]);
                ("FSharpList`1", "List", "file1", ((4, 8), (4, 46)), [], ["union"]);
                ("property Empty", "Empty", "file1", ((4, 8), (4, 52)), [],
                 ["member"; "prop"]);
                ("Generic", "Generic", "file1", ((6, 30), (6, 37)), ["type"],
                 ["namespace"]);
                ("Collections", "Collections", "file1", ((6, 18), (6, 29)), ["type"],
                 ["namespace"]);
                ("System", "System", "file1", ((6, 11), (6, 17)), ["type"], ["namespace"]);
                ("IList`1", "IList", "file1", ((6, 11), (6, 43)), ["type"], ["interface"]);
                ("generic parameter T", "T", "file1", ((6, 44), (6, 46)), ["type"], []);
                ("val x", "x", "file1", ((6, 8), (6, 9)), ["defn"], []);
                ("val x", "x", "file1", ((6, 51), (6, 52)), [], []);
                ("property Item", "Item", "file1", ((6, 51), (6, 57)), [],
                 ["slot"; "member"; "prop"]);
                ("val x", "x", "file1", ((6, 62), (6, 63)), [], []);
                ("property Item", "Item", "file1", ((6, 62), (6, 67)), [],
                 ["slot"; "member"; "prop"]);
                ("val x", "x", "file1", ((6, 69), (6, 70)), [], []);
                ("property Count", "Count", "file1", ((6, 69), (6, 76)), [],
                 ["slot"; "member"; "prop"]);
                ("val f1", "f1", "file1", ((6, 4), (6, 6)), ["defn"], ["val"]);
                ("Generic", "Generic", "file1", ((8, 30), (8, 37)), ["type"],
                 ["namespace"]);
                ("Collections", "Collections", "file1", ((8, 18), (8, 29)), ["type"],
                 ["namespace"]);
                ("System", "System", "file1", ((8, 11), (8, 17)), ["type"], ["namespace"]);
                ("IList`1", "IList", "file1", ((8, 11), (8, 43)), ["type"], ["interface"]);
                ("int", "int", "file1", ((8, 44), (8, 47)), ["type"], ["abbrev"]);
                ("val x", "x", "file1", ((8, 8), (8, 9)), ["defn"], []);
                ("val x", "x", "file1", ((8, 52), (8, 53)), [], []);
                ("property Item", "Item", "file1", ((8, 52), (8, 57)), [],
                 ["slot"; "member"; "prop"]);
                ("val f2", "f2", "file1", ((8, 4), (8, 6)), ["defn"], ["val"]);
                ("System", "System", "file1", ((10, 11), (10, 17)), ["type"],
                 ["namespace"]);
                ("Exception", "Exception", "file1", ((10, 11), (10, 27)), ["type"],
                 ["class"]); ("val x", "x", "file1", ((10, 8), (10, 9)), ["defn"], []);
                ("val x", "x", "file1", ((10, 31), (10, 32)), [], []);
                ("property HelpLink", "HelpLink", "file1", ((10, 31), (10, 41)), [],
                 ["slot"; "member"; "prop"]);
                ("val f3", "f3", "file1", ((10, 4), (10, 6)), ["defn"], ["val"]);
                ("Impl", "Impl", "file1", ((2, 7), (2, 11)), ["defn"], ["module"])|]


//-----------------------------------------------------------------------------------------
// Misc - generic type definnitions

module Project18 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

let _ = list<_>.Empty
    """
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project18 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project18.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project18 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project18.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project18.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, 
                                (match su.Symbol with :? FSharpEntity as e -> e.IsNamespace | _ -> false))

    allUsesOfAllSymbols |> shouldEqual
      [|("list`1", "list", "file1", ((4, 8), (4, 12)), [], false);
        ("list`1", "list", "file1", ((4, 8), (4, 15)), [], false);
        ("property Empty", "Empty", "file1", ((4, 8), (4, 21)), [], false);
        ("Impl", "Impl", "file1", ((2, 7), (2, 11)), ["defn"], false)|]



//-----------------------------------------------------------------------------------------
// Misc - enums

module Project19 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

type Enum = | EnumCase1 = 1 | EnumCase2 = 2

let _ = Enum.EnumCase1
let _ = Enum.EnumCase2
let f x = match x with Enum.EnumCase1 -> 1 | Enum.EnumCase2 -> 2 | _ -> 3

let s = System.DayOfWeek.Monday
    """
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project19 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project19.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project19 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project19.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project19.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("field EnumCase1", "EnumCase1", "file1", ((4, 14), (4, 23)), ["defn"], []);
            ("field EnumCase2", "EnumCase2", "file1", ((4, 30), (4, 39)), ["defn"], []);
            ("Enum", "Enum", "file1", ((4, 5), (4, 9)), ["defn"],
             ["enum"; "valuetype"]);
            ("Enum", "Enum", "file1", ((6, 8), (6, 12)), [], ["enum"; "valuetype"]);
            ("field EnumCase1", "EnumCase1", "file1", ((6, 8), (6, 22)), [], []);
            ("Enum", "Enum", "file1", ((7, 8), (7, 12)), [], ["enum"; "valuetype"]);
            ("field EnumCase2", "EnumCase2", "file1", ((7, 8), (7, 22)), [], []);
            ("val x", "x", "file1", ((8, 6), (8, 7)), ["defn"], []);
            ("val x", "x", "file1", ((8, 16), (8, 17)), [], []);
            ("Enum", "Enum", "file1", ((8, 23), (8, 27)), [], ["enum"; "valuetype"]);
            ("field EnumCase1", "EnumCase1", "file1", ((8, 23), (8, 37)), ["pattern"],
             []);
            ("Enum", "Enum", "file1", ((8, 45), (8, 49)), [], ["enum"; "valuetype"]);
            ("field EnumCase2", "EnumCase2", "file1", ((8, 45), (8, 59)), ["pattern"],
             []); ("val f", "f", "file1", ((8, 4), (8, 5)), ["defn"], ["val"]);
            ("DayOfWeek", "DayOfWeek", "file1", ((10, 15), (10, 24)), [],
             ["enum"; "valuetype"]);
            ("System", "System", "file1", ((10, 8), (10, 14)), [], ["namespace"]);
            ("symbol Monday", "Monday", "file1", ((10, 8), (10, 31)), [], []);
            ("val s", "s", "file1", ((10, 4), (10, 5)), ["defn"], ["val"]);
            ("Impl", "Impl", "file1", ((2, 7), (2, 11)), ["defn"], ["module"])|]




//-----------------------------------------------------------------------------------------
// Misc - https://github.com/fsharp/FSharp.Compiler.Service/issues/109

module Project20 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

type A<'T>() = 
    member x.M() : 'T = failwith ""

    """
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project20 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project20.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project20 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project20.options) |> Async.RunSynchronously

    let tSymbolUse = wholeProjectResults.GetAllUsesOfAllSymbols() |> Async.RunSynchronously |> Array.find (fun su -> su.RangeAlternate.StartLine = 5 && su.Symbol.ToString() = "generic parameter T")
    let tSymbol = tSymbolUse.Symbol



    let allUsesOfTSymbol = 
        wholeProjectResults.GetUsesOfSymbol(tSymbol)
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project20.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfTSymbol |> shouldEqual
          [|("generic parameter T", "T", "file1", ((4, 7), (4, 9)), ["type"], []);
            ("generic parameter T", "T", "file1", ((5, 19), (5, 21)), ["type"], [])|]

//-----------------------------------------------------------------------------------------
// Misc - https://github.com/fsharp/FSharp.Compiler.Service/issues/137

module Project21 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

type IMyInterface<'a> = 
    abstract Method1: 'a -> unit
    abstract Method2: 'a -> unit

let _ = { new IMyInterface<int> with
              member x.Method1(arg1: string): unit = 
                  raise (System.NotImplementedException())

              member x.Method2(arg1: int): unit = 
                  raise (System.NotImplementedException())
               }

    """
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


[<Test>]
let ``Test Project21 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project21.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 2


[<Test>]
let ``Test Project21 all symbols`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project21.options) |> Async.RunSynchronously

    let allUsesOfAllSymbols = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.map (fun su -> su.Symbol.ToString(), su.Symbol.DisplayName, Project21.cleanFileName su.FileName, tups su.RangeAlternate, attribsOfSymbolUse su, attribsOfSymbol su.Symbol)

    allUsesOfAllSymbols |> shouldEqual
          [|("generic parameter a", "a", "file1", ((4, 18), (4, 20)), ["type"], []);
            ("generic parameter a", "a", "file1", ((5, 22), (5, 24)), ["type"], []);
            ("unit", "unit", "file1", ((5, 28), (5, 32)), ["type"], ["abbrev"]);
            ("member Method1", "Method1", "file1", ((5, 13), (5, 20)), ["defn"],
             ["slot"; "member"]);
            ("generic parameter a", "a", "file1", ((6, 22), (6, 24)), ["type"], []);
            ("unit", "unit", "file1", ((6, 28), (6, 32)), ["type"], ["abbrev"]);
            ("member Method2", "Method2", "file1", ((6, 13), (6, 20)), ["defn"],
             ["slot"; "member"]);
            ("IMyInterface`1", "IMyInterface", "file1", ((4, 5), (4, 17)), ["defn"],
             ["interface"]);
            ("IMyInterface`1", "IMyInterface", "file1", ((8, 14), (8, 26)), ["type"],
             ["interface"]);
            ("int", "int", "file1", ((8, 27), (8, 30)), ["type"], ["abbrev"]);
            ("val x", "x", "file1", ((9, 21), (9, 22)), ["defn"], []);
            ("string", "string", "file1", ((9, 37), (9, 43)), ["type"], ["abbrev"]);
            ("val x", "x", "file1", ((12, 21), (12, 22)), ["defn"], []);
            ("int", "int", "file1", ((12, 37), (12, 40)), ["type"], ["abbrev"]);
            ("val arg1", "arg1", "file1", ((12, 31), (12, 35)), ["defn"], []);
            ("unit", "unit", "file1", ((12, 43), (12, 47)), ["type"], ["abbrev"]);
            ("val raise", "raise", "file1", ((13, 18), (13, 23)), [], ["val"]);
            ("System", "System", "file1", ((13, 25), (13, 31)), [], ["namespace"]);
            ("member .ctor", ".ctor", "file1", ((13, 25), (13, 55)), [], ["member"]);
            ("Impl", "Impl", "file1", ((2, 7), (2, 11)), ["defn"], ["module"])|]

//-----------------------------------------------------------------------------------------
// Misc - namespace symbols

module Project22 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module Impl

type AnotherMutableList() = 
    member x.Item with get() = 3 and set (v:int) = ()

let f1 (x: System.Collections.Generic.IList<'T>) = () // grab the IList symbol and look into it
let f2 (x: AnotherMutableList) = () // grab the AnotherMutableList symbol and look into it
let f3 (x: System.Collections.ObjectModel.ObservableCollection<'T>) = () // grab the ObservableCollection symbol and look into it
    """
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)



[<Test>]
let ``Test Project22 whole project errors`` () = 

    let wholeProjectResults = checker.ParseAndCheckProject(Project22.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0


[<Test>]
let ``Test Project22 IList contents`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project22.options) |> Async.RunSynchronously

    let ilistTypeUse = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.find (fun su -> su.Symbol.DisplayName = "IList")

    let ocTypeUse = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.find (fun su -> su.Symbol.DisplayName = "ObservableCollection")

    let alistTypeUse = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.find (fun su -> su.Symbol.DisplayName = "AnotherMutableList")


    let ilistTypeDefn = ilistTypeUse.Symbol :?> FSharpEntity
    let ocTypeDefn = ocTypeUse.Symbol :?> FSharpEntity
    let alistTypeDefn = alistTypeUse.Symbol :?> FSharpEntity

    [ for x in ilistTypeDefn.MembersFunctionsAndValues -> x.LogicalName, attribsOfSymbol x ]
      |> shouldEqual
              [("get_Item", ["slot"; "member"; "getter"]);
               ("set_Item", ["slot"; "member"; "setter"]); 
               ("IndexOf", ["slot"; "member"]);
               ("Insert", ["slot"; "member"]); 
               ("RemoveAt", ["slot"; "member"]);
               ("Item", ["slot"; "member"; "prop"])]

    set [ for x in ocTypeDefn.MembersFunctionsAndValues -> x.LogicalName, attribsOfSymbol x ]
      |> shouldEqual
         (set [(".ctor", ["member"]); 
               (".ctor", ["member"]); 
               (".ctor", ["member"]);
               ("Move", ["member"]); 
               ("add_CollectionChanged", ["slot"; "member"; "add"]);
               ("remove_CollectionChanged", ["slot"; "member"; "remove"]);
               ("ClearItems", ["slot"; "member"]); 
               ("RemoveItem", ["slot"; "member"]);
               ("InsertItem", ["slot"; "member"]); 
               ("SetItem", ["slot"; "member"]);
               ("MoveItem", ["slot"; "member"]); 
               ("OnPropertyChanged", ["slot"; "member"]);
               ("add_PropertyChanged", ["slot"; "member"; "add"]);
               ("remove_PropertyChanged", ["slot"; "member"; "remove"]);
               ("OnCollectionChanged", ["slot"; "member"]);
               ("BlockReentrancy", ["member"]); 
               ("CheckReentrancy", ["member"]);
               ("CollectionChanged", ["slot"; "member"; "event"]);
               ("PropertyChanged", ["slot"; "member"; "event"])])

    [ for x in alistTypeDefn.MembersFunctionsAndValues -> x.LogicalName, attribsOfSymbol x ]
      |> shouldEqual
            [(".ctor", ["member"; "ctor"]); 
             ("get_Item", ["member"; "getter"]);
             ("set_Item", ["member"; "setter"]); 
             ("Item", ["member"; "prop"])]

    [ for x in ilistTypeDefn.AllInterfaces -> x.TypeDefinition.DisplayName, attribsOfSymbol x.TypeDefinition ]
       |> shouldEqual
              [("IList", ["interface"]); ("ICollection", ["interface"]);
               ("IEnumerable", ["interface"]); ("IEnumerable", ["interface"])]

[<Test>]
let ``Test Project22 IList properties`` () =

    let wholeProjectResults = checker.ParseAndCheckProject(Project22.options) |> Async.RunSynchronously

    let ilistTypeUse = 
        wholeProjectResults.GetAllUsesOfAllSymbols()
        |> Async.RunSynchronously
        |> Array.find (fun su -> su.Symbol.DisplayName = "IList")

    let ilistTypeDefn = ilistTypeUse.Symbol :?> FSharpEntity

    attribsOfSymbol ilistTypeDefn |> shouldEqual ["interface"]

    ilistTypeDefn.Assembly.SimpleName |> shouldEqual "mscorlib"

//-----------------------------------------------------------------------------------------
// Misc - type provider symbols

module Project23 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let base2 = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(base2, ".dll")
    let projFileName = Path.ChangeExtension(base2, ".fsproj")
    let fileSource1 = """
module TypeProviderTests
open FSharp.Data
type Project = XmlProvider<"<root><value>1</value><value>3</value></root>">
let _ = Project.GetSample()

type Record = { Field: int }
let r = { Record.Field = 1 }

type TypeWithProperties() =
    member x.Name
        with get() = 0
        and set (v: int) = ()
"""
    File.WriteAllText(fileName1, fileSource1)
    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = 
        [| yield! mkProjectCommandLineArgs (dllName, fileNames) 
           yield "-r:" + Path.Combine(__SOURCE_DIRECTORY__, "FSharp.Data.dll")
           yield @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Xml.Linq.dll" |]
    let options = checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)

[<Test>]
let ``Test Project23 whole project errors`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project23.options) |> Async.RunSynchronously
    wholeProjectResults.Errors.Length |> shouldEqual 0

[<Test; Ignore "FCS should return symbol uses for type-provided members">]
let ``Test symbol uses of type-provided members`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project23.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project23.fileName1, Project23.options) 
        |> Async.RunSynchronously   

    let getSampleSymbolUseOpt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(5,25,"",["GetSample"]) 
        |> Async.RunSynchronously

    let getSampleSymbol = getSampleSymbolUseOpt.Value.Symbol
    
    let usesOfGetSampleSymbol = 
        backgroundTypedParse1.GetUsesOfSymbolInFile(getSampleSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun s -> (Project23.cleanFileName s.FileName, tupsZ s.RangeAlternate))

    usesOfGetSampleSymbol |> shouldEqual [|("file1", ((4, 8), (4, 25)))|]

[<Test>]
let ``Test symbol uses of type-provided types`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project23.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project23.fileName1, Project23.options) 
        |> Async.RunSynchronously   

    let getSampleSymbolUseOpt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(4,26,"",["XmlProvider"]) 
        |> Async.RunSynchronously

    let getSampleSymbol = getSampleSymbolUseOpt.Value.Symbol
    
    let usesOfGetSampleSymbol = 
        backgroundTypedParse1.GetUsesOfSymbolInFile(getSampleSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun s -> (Project23.cleanFileName s.FileName, tupsZ s.RangeAlternate))

    usesOfGetSampleSymbol |> shouldEqual [|("file1", ((3, 15), (3, 26)))|]

[<Test; Ignore "FCS should return symbols for fully-qualified records">]
let ``Test symbol uses of fully-qualified records`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project23.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project23.fileName1, Project23.options) 
        |> Async.RunSynchronously   

    let getSampleSymbolUseOpt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(7,11,"",["Record"]) 
        |> Async.RunSynchronously

    let getSampleSymbol = getSampleSymbolUseOpt.Value.Symbol
    
    let usesOfGetSampleSymbol = 
        backgroundTypedParse1.GetUsesOfSymbolInFile(getSampleSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun s -> (Project23.cleanFileName s.FileName, tupsZ s.RangeAlternate))

    usesOfGetSampleSymbol |> shouldEqual [|("file1", ((6, 5), (6, 11))); ("file1", ((7, 10), (7, 16)))|]

[<Test; Ignore "FCS should return symbols for properties with explicit getters and setters">]
let ``Test symbol uses of properties with both getters and setters`` () = 
    let wholeProjectResults = checker.ParseAndCheckProject(Project23.options) |> Async.RunSynchronously
    let backgroundParseResults1, backgroundTypedParse1 = 
        checker.GetBackgroundCheckResultsForFileInProject(Project23.fileName1, Project23.options) 
        |> Async.RunSynchronously   

    let getSampleSymbolUseOpt = 
        backgroundTypedParse1.GetSymbolUseAtLocation(11,17,"",["Name"]) 
        |> Async.RunSynchronously

    let getSampleSymbol = getSampleSymbolUseOpt.Value.Symbol
    
    let usesOfGetSampleSymbol = 
        backgroundTypedParse1.GetUsesOfSymbolInFile(getSampleSymbol) 
        |> Async.RunSynchronously
        |> Array.map (fun s -> (Project23.cleanFileName s.FileName, tupsZ s.RangeAlternate))

    usesOfGetSampleSymbol |> shouldEqual [|("file1", ((10, 13), (10, 17)))|]
