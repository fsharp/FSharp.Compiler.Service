// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//----------------------------------------------------------------------------
// SourceCodeServices API to the compiler as an incremental service for parsing,
// type checking and intellisense-like environment-reporting.
//----------------------------------------------------------------------------

namespace Microsoft.FSharp.Compiler.SourceCodeServices
open System
open System.IO
open System.Collections.Generic

open Microsoft.FSharp.Compiler.AbstractIL.IL
open Microsoft.FSharp.Compiler 
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Driver
open Microsoft.FSharp.Compiler.ErrorLogger
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.TcGlobals
open Microsoft.FSharp.Compiler.NameResolution
open Microsoft.FSharp.Compiler.CompileOps
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library
open Microsoft.FSharp.Compiler 
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.TcGlobals 
open Microsoft.FSharp.Compiler.Infos
open Microsoft.FSharp.Compiler.NameResolution
open Microsoft.FSharp.Compiler.InfoReader
open Microsoft.FSharp.Compiler.Tast
open Microsoft.FSharp.Compiler.Tastops

/// Represents one parameter for one method (or other item) in a group. 
[<Sealed>]
type FSharpMethodGroupItemParameter = 

    /// The name of the parameter.
    member ParameterName: string

    /// A key that can be used for sorting the parameters, used to help sort overloads.
    member CanonicalTypeTextForSorting: string

    /// The structured representation for the parameter including its name, its type and visual indicators of other
    /// information such as whether it is optional.
    member StructuredDisplay: Layout

    /// The text to display for the parameter including its name, its type and visual indicators of other
    /// information such as whether it is optional.
    member Display: string

    /// The descriptive help text to display for the parameter.
    member Description: string

    /// Is the parameter optional
    member IsOptional: bool

/// Represents one method (or other item) in a method group. The item may represent either a method or 
/// a single, non-overloaded item such as union case or a named function value.
[<Sealed>]
type FSharpMethodGroupItem = 

    /// The documentation for the item
    member XmlDoc : FSharpXmlDoc

    /// The structured description representation for the method (or other item)
    member StructuredDescription : FSharpStructuredToolTipText

    /// The formatted description text for the method (or other item)
    member Description : FSharpToolTipText

    /// The The structured description representation for the method (or other item)
    member StructuredTypeText: Layout

    /// The formatted type text for the method (or other item)
    member TypeText: string

    /// The parameters of the method in the overload set
    member Parameters: FSharpMethodGroupItemParameter[]

    /// Does the method support an arguments list?  This is always true except for static type instantiations like TP<42,"foo">.
    member HasParameters: bool

    /// Does the method support a params list arg?
    member HasParamArrayArg: bool

    /// Does the type name or method support a static arguments list, like TP<42,"foo"> or conn.CreateCommand<42, "foo">(arg1, arg2)?
    member StaticParameters: FSharpMethodGroupItemParameter[]

/// Represents a group of methods (or other items) returned by GetMethods.  
[<Sealed>]
type FSharpMethodGroup = 
    /// The shared name of the methods (or other items) in the group
    member MethodName: string

    /// The methods (or other items) in the group
    member Methods: FSharpMethodGroupItem[] 

/// Represents the reason why the GetDeclarationLocation operation failed.
[<RequireQualifiedAccess>]
type FSharpFindDeclFailureReason = 

    /// Generic reason: no particular information about error
    | Unknown

    /// Source code file is not available
    | NoSourceCode

    /// Trying to find declaration of ProvidedType without TypeProviderDefinitionLocationAttribute
    | ProvidedType of string

    /// Trying to find declaration of ProvidedMember without TypeProviderDefinitionLocationAttribute
    | ProvidedMember of string

/// Represents the result of the GetDeclarationLocation operation.
[<RequireQualifiedAccess>]
type FSharpFindDeclResult = 
    /// Indicates a declaration location was not found, with an additional reason
    | DeclNotFound of FSharpFindDeclFailureReason
    /// Indicates a declaration location was found
    | DeclFound      of range
     
/// Represents the checking context implied by the ProjectOptions 
[<Sealed>]
type internal FSharpProjectContext =
    /// Get the resolution and full contents of the assemblies referenced by the project options
    member GetReferencedAssemblies : unit -> FSharpAssembly list

    /// Get the accessibility rights for this project context w.r.t. InternalsVisibleTo attributes granting access to other assemblies
    member AccessibilityRights : FSharpAccessibilityRights


/// Represents the use of an F# symbol from F# source code
[<Sealed>]
type FSharpSymbolUse = 
    // For internal use only
    internal new : g:TcGlobals * denv: Tastops.DisplayEnv * symbol:FSharpSymbol * itemOcc:ItemOccurence * range: range -> FSharpSymbolUse

    /// The symbol referenced
    member Symbol : FSharpSymbol 

    /// The display context active at the point where the symbol is used. Can be passed to FSharpType.Format
    /// and other methods to format items in a way that is suitable for a specific source code location.
    member DisplayContext : FSharpDisplayContext

    /// Indicates if the reference is a definition for the symbol, either in a signature or implementation
    member IsFromDefinition : bool

    /// Indicates if the reference is in a pattern
    member IsFromPattern : bool

    /// Indicates if the reference is in a syntactic type
    member IsFromType : bool

    /// Indicates if the reference is in an attribute
    member IsFromAttribute : bool

    /// Indicates if the reference is via the member being implemented in a class or object expression
    member IsFromDispatchSlotImplementation : bool

    /// Indicates if the reference is either a builder or a custom operation in a compuation expression
    member IsFromComputationExpression : bool

    /// The file name the reference occurs in 
    member FileName: string 

    /// The range of text representing the reference to the symbol
    member RangeAlternate: range

[<RequireQualifiedAccess>]
type (*internal*) SemanticClassificationType =
    | ReferenceType
    | ValueType
    | UnionCase
    | Function
    | Property
    | MutableVar
    | Module
    | Printf
    | ComputationExpression
<<<<<<< HEAD
    | IntrinsicType
    | IntrinsicFunction
||||||| parent of bf0f7e8ac... desired changeset
    | IntrinsicType
=======
    | IntrinsicFunction
>>>>>>> bf0f7e8ac... desired changeset
    | Enumeration
    | Interface
    | TypeArgument
    | Operator
    | Disposable

/// A handle to the results of CheckFileInProject.
[<Sealed>]
type FSharpCheckFileResults =
    /// The errors returned by parsing a source file.
    member Errors : FSharpErrorInfo[]

    /// Get a view of the contents of the assembly up to and including the file just checked
    member PartialAssemblySignature : FSharpAssemblySignature

    /// Get the resolution of the ProjectOptions 
    member ProjectContext : FSharpProjectContext

    /// Indicates whether type checking successfully occured with some results returned. If false, indicates that 
    /// an unrecoverable error in earlier checking/parsing/resolution steps.
    member HasFullTypeCheckInfo: bool

    /// Indicates the set of files which must be watched to accurately track changes that affect these results,
    /// Clients interested in reacting to updates to these files should watch these files and take actions as described
    /// in the documentation for compiler service.
    member DependencyFiles : string list

    /// <summary>Get the items for a declaration list</summary>
    ///
    /// <param name="ParsedFileResultsOpt">
    ///    If this is present, it is used to filter declarations based on location in the
    ///    parse tree, specifically at 'open' declarations, 'inherit' of class or interface
    ///    'record field' locations and r.h.s. of 'range' operator a..b
    /// </param>
    /// <param name="line">The line number where the completion is happening</param>
    /// <param name="colAtEndOfNamesAndResidue">The column number at the end of the 'names' text </param>
    /// <param name="qualifyingNames">The long identifier to the left of the '.'</param>
    /// <param name="partialName">The residue of a partial long identifier to the right of the '.'</param>
    /// <param name="lineStr">The residue of a partial long identifier to the right of the '.'</param>
    /// <param name="lineText">
    ///    The text of the line where the completion is happening. This is only used to make a couple
    ///    of adhoc corrections to completion accuracy (e.g. checking for "..")
    /// </param>
    /// <param name="hasTextChangedSinceLastTypecheck">
    ///    If text has been used from a captured name resolution from the typecheck, then 
    ///    callback to the client to check if the text has changed. If it has, then give up
    ///    and assume that we're going to repeat the operation later on.
    /// </param>

    member GetDeclarationListInfo : ParsedFileResultsOpt:FSharpParseFileResults option * line: int * colAtEndOfPartialName: int * lineText:string * qualifyingNames: string list * partialName: string * ?hasTextChangedSinceLastTypecheck: (obj * range -> bool) -> Async<FSharpDeclarationListInfo>

    /// <summary>Get the items for a declaration list in FSharpSymbol format</summary>
    ///
    /// <param name="ParsedFileResultsOpt">
    ///    If this is present, it is used to filter declarations based on location in the
    ///    parse tree, specifically at 'open' declarations, 'inherit' of class or interface
    ///    'record field' locations and r.h.s. of 'range' operator a..b
    /// </param>
    /// <param name="line">The line number where the completion is happening</param>
    /// <param name="colAtEndOfNamesAndResidue">The column number (1-based) at the end of the 'names' text </param>
    /// <param name="qualifyingNames">The long identifier to the left of the '.'</param>
    /// <param name="partialName">The residue of a partial long identifier to the right of the '.'</param>
    /// <param name="lineStr">The residue of a partial long identifier to the right of the '.'</param>
    /// <param name="lineText">
    ///    The text of the line where the completion is happening. This is only used to make a couple
    ///    of adhoc corrections to completion accuracy (e.g. checking for "..")
    /// </param>
    /// <param name="hasTextChangedSinceLastTypecheck">
    ///    If text has been used from a captured name resolution from the typecheck, then 
    ///    callback to the client to check if the text has changed. If it has, then give up
    ///    and assume that we're going to repeat the operation later on.
    /// </param>
    member GetDeclarationListSymbols : ParsedFileResultsOpt:FSharpParseFileResults option * line: int * colAtEndOfPartialName: int * lineText:string * qualifyingNames: string list * partialName: string * ?hasTextChangedSinceLastTypecheck: (obj * range -> bool) -> Async<FSharpSymbolUse list list>


    /// <summary>Compute a formatted tooltip for the given location</summary>
    ///
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    /// <param name="tokenTag">Used to discriminate between 'identifiers', 'strings' and others. For strings, an attempt is made to give a tooltip for a #r "..." location. Use a value from FSharpTokenInfo.Tag, or FSharpTokenTag.Identifier, unless you have other information available.</param>
    member GetStructuredToolTipTextAlternate : line:int * colAtEndOfNames:int * lineText:string * names:string list * tokenTag:int -> Async<FSharpStructuredToolTipText>

    /// <summary>Compute a formatted tooltip for the given location</summary>
    ///
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    /// <param name="tokenTag">Used to discriminate between 'identifiers', 'strings' and others. For strings, an attempt is made to give a tooltip for a #r "..." location. Use a value from FSharpTokenInfo.Tag, or FSharpTokenTag.Identifier, unless you have other information available.</param>
    member GetToolTipTextAlternate : line:int * colAtEndOfNames:int * lineText:string * names:string list * tokenTag:int -> Async<FSharpToolTipText>

    /// <summary>Compute the Visual Studio F1-help key identifier for the given location, based on name resolution results</summary>
    ///
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    member GetF1KeywordAlternate                   : line:int * colAtEndOfNames:int * lineText:string * names:string list -> Async<string option>


    /// <summary>Compute a set of method overloads to show in a dialog relevant to the given code location.</summary>
    ///
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    member GetMethodsAlternate              : line:int * colAtEndOfNames:int * lineText:string * names:string list option -> Async<FSharpMethodGroup>

    /// <summary>Compute a set of method overloads to show in a dialog relevant to the given code location.  The resulting method overloads are returned as symbols.</summary>
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    member GetMethodsAsSymbols : line:int * colAtEndOfNames:int * lineText:string * names:string list -> Async<FSharpSymbolUse list option>

    /// <summary>Resolve the names at the given location to the declaration location of the corresponding construct.</summary>
    ///
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    /// <param name="preferFlag">If not given, then get the location of the symbol. If false, then prefer the location of the corresponding symbol in the implementation of the file (rather than the signature if present). If true, prefer the location of the corresponding symbol in the signature of the file (rather than the implementation).</param>
    member GetDeclarationLocationAlternate         : line:int * colAtEndOfNames:int * lineText:string * names:string list * ?preferFlag:bool -> Async<FSharpFindDeclResult>


    /// <summary>Resolve the names at the given location to a use of symbol.</summary>
    ///
    /// <param name="line">The line number where the information is being requested.</param>
    /// <param name="colAtEndOfNames">The column number at the end of the identifiers where the information is being requested.</param>
    /// <param name="lineText">The text of the line where the information is being requested.</param>
    /// <param name="names">The identifiers at the location where the information is being requested.</param>
    member GetSymbolUseAtLocation  : line:int * colAtEndOfNames:int * lineText:string * names:string list -> Async<FSharpSymbolUse option>

    /// <summary>Get any extra colorization info that is available after the typecheck</summary>
    member GetSemanticClassification : range option -> (range * SemanticClassificationType)[]

    /// <summary>Get the locations of format specifiers</summary>
    [<System.Obsolete("This member has been replaced by GetFormatSpecifierLocationsAndArity, which returns both range and arity of specifiers")>]
    member GetFormatSpecifierLocations : unit -> range[]

    /// <summary>Get the locations of and number of arguments associated with format specifiers</summary>
    member GetFormatSpecifierLocationsAndArity : unit -> (range*int)[]

    /// Get all textual usages of all symbols throughout the file
    member GetAllUsesOfAllSymbolsInFile : unit -> Async<FSharpSymbolUse[]>

    /// Get the textual usages that resolved to the given symbol throughout the file
    member GetUsesOfSymbolInFile : symbol:FSharpSymbol -> Async<FSharpSymbolUse[]>

    /// Determines if a long ident is resolvable at a specific point.
    member internal IsRelativeNameResolvable: cursorPos : pos * plid : string list * item: Item -> Async<bool>

/// A handle to the results of CheckFileInProject.
[<Sealed>]
type FSharpCheckProjectResults =
    /// The errors returned by processing the project
    member Errors : FSharpErrorInfo[]

    /// Get a view of the overall signature of the assembly. Only valid to use if HasCriticalErrors is false.
    member AssemblySignature : FSharpAssemblySignature

    /// Get a view of the overall contents of the assembly. Only valid to use if HasCriticalErrors is false.
    member AssemblyContents : FSharpAssemblyContents

    /// Get the resolution of the ProjectOptions 
    member ProjectContext : FSharpProjectContext

    /// Get the textual usages that resolved to the given symbol throughout the project
    member GetUsesOfSymbol : symbol:FSharpSymbol -> Async<FSharpSymbolUse[]>

    /// Get all textual usages of all symbols throughout the project
    member GetAllUsesOfAllSymbols : unit -> Async<FSharpSymbolUse[]>

    /// Indicates if critical errors existed in the project options
    member HasCriticalErrors : bool 

    /// Indicates the set of files which must be watched to accurately track changes that affect these results,
    /// Clients interested in reacting to updates to these files should watch these files and take actions as described
    /// in the documentation for compiler service.
    member DependencyFiles : string list

/// <summary>Unused in this API</summary>
type UnresolvedReferencesSet 

/// <summary>A set of information describing a project or script build configuration.</summary>
type FSharpProjectOptions = 
    { 
      // Note that this may not reduce to just the project directory, because there may be two projects in the same directory.
      ProjectFileName: string
      /// The files in the project
      ProjectFileNames: string[]
      /// Additional command line argument options for the project. These can include additional files and references.
      OtherOptions: string[]
      /// The command line arguments for the other projects referenced by this project, indexed by the
      /// exact text used in the "-r:" reference in FSharpProjectOptions.
      ReferencedProjects: (string * FSharpProjectOptions)[]
      /// When true, the typechecking environment is known a priori to be incomplete, for
      /// example when a .fs file is opened outside of a project. In this case, the number of error 
      /// messages reported is reduced.
      IsIncompleteTypeCheckEnvironment : bool
      /// When true, use the reference resolution rules for scripts rather than the rules for compiler.
      UseScriptResolutionRules : bool
      /// Timestamp of project/script load, used to differentiate between different instances of a project load.
      /// This ensures that a complete reload of the project or script type checking
      /// context occurs on project or script unload/reload.
      LoadTime : DateTime
      /// Unused in this API and should be 'None' when used as user-specified input
      UnresolvedReferences : UnresolvedReferencesSet option
      /// Unused in this API and should be '[]' when used as user-specified input
      OriginalLoadReferences: (range * string) list
      /// Extra information passed back on event trigger
      ExtraProjectInfo : obj option
    }
         
          
module internal CompileHelpers =
    val mkCompilationErorHandlers : unit -> List<FSharpErrorInfo> * ErrorLogger * ErrorLoggerProvider
    val tryCompile : errorLogger:ErrorLogger -> f:(Exiter -> unit) -> int
    val compileFromArgs : argv:string [] * referenceResolver: ReferenceResolver.Resolver * tcImportsCapture:(TcImports -> unit) option * dynamicAssemblyCreator:(TcGlobals * string * ILModuleDef -> unit) option -> FSharpErrorInfo [] * int
    val compileFromAsts : referenceResolver: ReferenceResolver.Resolver * asts:ParsedInput list * assemblyName:string * outFile:string * dependencies:string list * noframework:bool * pdbFile:string option * executable:bool option * tcImportsCapture:(TcImports -> unit) option * dynamicAssemblyCreator:(TcGlobals * string * ILModuleDef -> unit) option -> FSharpErrorInfo [] * int
    val createDynamicAssembly : debugInfo:bool * tcImportsRef:TcImports option ref * execute:bool * assemblyBuilderRef:Reflection.Emit.AssemblyBuilder option ref -> ilGlobals:TcGlobals * outfile:string * ilxMainModule:ILModuleDef -> unit
    val setOutputStreams : execute:(#TextWriter * #TextWriter) option -> unit


/// The result of calling TypeCheckResult including the possibility of abort and background compiler not caught up.
[<RequireQualifiedAccess>]
type FSharpCheckFileAnswer =
    | Aborted // because cancellation caused an abandonment of the operation
    | Succeeded of FSharpCheckFileResults    

[<Sealed; AutoSerializable(false)>]      
/// Used to parse and check F# source code.
type FSharpChecker =
    /// <summary>
    /// Create an instance of an FSharpChecker.  
    /// </summary>
    ///
    /// <param name="projectCacheSize">The optional size of the project checking cache.</param>
    /// <param name="keepAssemblyContents">Keep the checked contents of projects.</param>
    /// <param name="keepAllBackgroundResolutions">If false, do not keep full intermediate checking results from background checking suitable for returning from GetBackgroundCheckResultsForFileInProject. This reduces memory usage.</param>
    /// <param name="msbuildEnabled">If false, no dependency on MSBuild v12 is assumed. If true, at attempt is made to load MSBuild for reference resolution in scripts</param>
    static member Create : ?projectCacheSize: int * ?keepAssemblyContents: bool * ?keepAllBackgroundResolutions: bool * ?msbuildEnabled: bool -> FSharpChecker

    /// <summary>
    ///   Parse a source code file, returning information about brace matching in the file.
    ///   Return an enumeration of the matching parenthetical tokens in the file.
    /// </summary>
    ///
    /// <param name="filename">The filename for the file, used to help caching of results.</param>
    /// <param name="source">The full source for the file.</param>
    /// <param name="options">The options for the project or script, used to determine active --define conditionals and other options relevant to parsing.</param>
    member MatchBracesAlternate : filename : string * source: string * options: FSharpProjectOptions -> Async<(range * range)[]>

    /// <summary>
    /// <para>Parse a source code file, returning a handle that can be used for obtaining navigation bar information
    /// To get the full information, call 'CheckFileInProject' method on the result</para>
    /// <para>All files except the one being checked are read from the FileSystem API</para>
    /// </summary>
    ///
    /// <param name="filename">The filename for the file.</param>
    /// <param name="source">The full source for the file.</param>
    /// <param name="options">The options for the project or script, used to determine active --define conditionals and other options relevant to parsing.</param>
    member ParseFileInProject : filename: string * source: string * options: FSharpProjectOptions -> Async<FSharpParseFileResults>

    /// <summary>
    /// <para>Check a source code file, returning a handle to the results of the parse including
    /// the reconstructed types in the file.</para>
    ///
    /// <para>All files except the one being checked are read from the FileSystem API</para>
    /// <para>Note: returns NoAntecedent if the background builder is not yet done preparing the type check context for the 
    /// file (e.g. loading references and parsing/checking files in the project that this file depends upon). 
    /// In this case, the caller can either retry, or wait for FileTypeCheckStateIsDirty to be raised for this file.
    /// </para>
    /// </summary>
    ///
    /// <param name="parsed">The results of ParseFileInProject for this file.</param>
    /// <param name="filename">The name of the file in the project whose source is being checked.</param>
    /// <param name="fileversion">An integer that can be used to indicate the version of the file. This will be returned by TryGetRecentCheckResultsForFile when looking up the file.</param>
    /// <param name="source">The full source for the file.</param>
    /// <param name="options">The options for the project or script.</param>
    /// <param name="textSnapshotInfo">
    ///     An item passed back to 'hasTextChangedSinceLastTypecheck' (from some calls made on 'FSharpCheckFileResults') to help determine if 
    ///     an approximate intellisense resolution is inaccurate because a range of text has changed. This 
    ///     can be used to marginally increase accuracy of intellisense results in some situations.
    /// </param>
    ///
    member CheckFileInProjectIfReady : parsed: FSharpParseFileResults * filename: string * fileversion: int * source: string * options: FSharpProjectOptions * ?textSnapshotInfo: obj -> Async<FSharpCheckFileAnswer option>

    /// <summary>
    /// <para>
    ///   Check a source code file, returning a handle to the results
    /// </para>
    /// <para>
    ///    Note: all files except the one being checked are read from the FileSystem API
    /// </para>
    /// <para>
    ///   Return FSharpCheckFileAnswer.Aborted if a parse tree was not available or if the check
    ////  was abandoned due to some checkpoint during type checking.
    /// </para>
    /// </summary>
    ///
    /// <param name="parsed">The results of ParseFileInProject for this file.</param>
    /// <param name="filename">The name of the file in the project whose source is being checked.</param>
    /// <param name="fileversion">An integer that can be used to indicate the version of the file. This will be returned by TryGetRecentCheckResultsForFile when looking up the file.</param>
    /// <param name="source">The full source for the file.</param>
    /// <param name="options">The options for the project or script.</param>
    /// <param name="textSnapshotInfo">
    ///     An item passed back to 'hasTextChangedSinceLastTypecheck' (from some calls made on 'FSharpCheckFileResults') to help determine if 
    ///     an approximate intellisense resolution is inaccurate because a range of text has changed. This 
    ///     can be used to marginally increase accuracy of intellisense results in some situations.
    /// </param>
    ///
    member CheckFileInProject : parsed: FSharpParseFileResults * filename: string * fileversion: int * source: string * options: FSharpProjectOptions * ?textSnapshotInfo: obj -> Async<FSharpCheckFileAnswer>

    /// <summary>
    /// <para>
    ///   Parse and check a source code file, returning a handle to the results 
    /// </para>
    /// <para>
    ///    Note: all files except the one being checked are read from the FileSystem API
    /// </para>
    /// <para>
    ///   Return FSharpCheckFileAnswer.Aborted if a parse tree was not available or if the check
    ////  was abandoned due to some checkpoint during type checking.
    /// </para>
    /// </summary>
    ///
    /// <param name="filename">The name of the file in the project whose source is being checked.</param>
    /// <param name="fileversion">An integer that can be used to indicate the version of the file. This will be returned by TryGetRecentCheckResultsForFile when looking up the file.</param>
    /// <param name="source">The full source for the file.</param>
    /// <param name="options">The options for the project or script.</param>
    /// <param name="textSnapshotInfo">
    ///     An item passed back to 'hasTextChangedSinceLastTypecheck' (from some calls made on 'FSharpCheckFileResults') to help determine if 
    ///     an approximate intellisense resolution is inaccurate because a range of text has changed. This 
    ///     can be used to marginally increase accuracy of intellisense results in some situations.
    /// </param>
    ///
    member ParseAndCheckFileInProject : filename: string * fileversion: int * source: string * options: FSharpProjectOptions * ?textSnapshotInfo: obj -> Async<FSharpParseFileResults * FSharpCheckFileAnswer>

    /// <summary>
    /// <para>Parse and typecheck all files in a project.</para>
    /// <para>All files are read from the FileSystem API</para>
    /// </summary>
    ///
    /// <param name="options">The options for the project or script.</param>
    member ParseAndCheckProject : options: FSharpProjectOptions -> Async<FSharpCheckProjectResults>

    /// <summary>
    /// <para>Create resources for the project and keep the project alive until the returned object is disposed.</para>
    /// </summary>
    ///
    /// <param name="options">The options for the project or script.</param>
    member KeepProjectAlive : options: FSharpProjectOptions -> Async<IDisposable>

    /// <summary>
    /// <para>For a given script file, get the FSharpProjectOptions implied by the #load closure.</para>
    /// <para>All files are read from the FileSystem API, except the file being checked.</para>
    /// </summary>
    ///
    /// <param name="filename">Used to differentiate between scripts, to consider each script a separate project.
    /// Also used in formatted error messages.</param>
    ///
    /// <param name="loadedTimeStamp">Indicates when the script was loaded into the editing environment,
    /// so that an 'unload' and 'reload' action will cause the script to be considered as a new project,
    /// so that references are re-resolved.</param>
<<<<<<< HEAD
    member GetProjectOptionsFromScript : filename: string * source: string * ?loadedTimeStamp: DateTime * ?otherFlags: string[] * ?useFsiAuxLib: bool * ?extraProjectInfo: obj -> Async<FSharpProjectOptions * FSharpErrorInfo list>
||||||| parent of bf0f7e8ac... desired changeset
    member GetProjectOptionsFromScript : filename: string * source: string * ?loadedTimeStamp: DateTime * ?otherFlags: string[] * ?useFsiAuxLib: bool * ?assumeDotNetFramework: bool * ?extraProjectInfo: obj -> Async<FSharpProjectOptions>
=======
<<<<<<< HEAD
    member GetProjectOptionsFromScript : filename: string * source: string * ?loadedTimeStamp: DateTime * ?otherFlags: string[] * ?useFsiAuxLib: bool * ?assumeDotNetFramework: bool * ?extraProjectInfo: obj -> Async<FSharpProjectOptions>
||||||| merged common ancestors
    member GetProjectOptionsFromScript : filename: string * source: string * ?loadedTimeStamp: DateTime * ?otherFlags: string[] * ?useFsiAuxLib: bool * ?extraProjectInfo: obj -> Async<FSharpProjectOptions>
=======
    member GetProjectOptionsFromScript : filename: string * source: string * ?loadedTimeStamp: DateTime * ?otherFlags: string[] * ?useFsiAuxLib: bool * ?extraProjectInfo: obj -> Async<FSharpProjectOptions * FSharpErrorInfo list>
>>>>>>> msfsharp/master
>>>>>>> bf0f7e8ac... desired changeset

    /// <summary>
    /// <para>Get the FSharpProjectOptions implied by a set of command line arguments.</para>
    /// </summary>
    ///
    /// <param name="projectFileName">Used to differentiate between projects and for the base directory of the project.</param>
    /// <param name="argv">The command line arguments for the project build.</param>
    /// <param name="loadedTimeStamp">Indicates when the script was loaded into the editing environment,
    /// so that an 'unload' and 'reload' action will cause the script to be considered as a new project,
    /// so that references are re-resolved.</param>
    member GetProjectOptionsFromCommandLineArgs : projectFileName: string * argv: string[] * ?loadedTimeStamp: DateTime * ?extraProjectInfo: obj -> FSharpProjectOptions
           
#if FX_ATLEAST_45
    /// <summary>
    /// <para>Get the project options implied by a standard F# project file in the xbuild/msbuild format.</para>
    /// </summary>
    ///
    /// <param name="projectFileName">Used to differentiate between projects and for the base directory of the project.</param>
    /// <param name="properties">The build properties such as Configuration=Debug etc.</param>
    /// <param name="loadedTimeStamp">Indicates when the project was loaded into the editing environment,
    /// so that an 'unload' and 'reload' action will cause the project to be considered as a new project.</param>
    [<Obsolete("This functionality has been moved to the new NuGet package 'FSharp.Compiler.Service.ProjectCracker'", true)>]
    member GetProjectOptionsFromProjectFile : projectFileName: string * ?properties : (string * string) list * ?loadedTimeStamp: DateTime -> FSharpProjectOptions
#endif

    /// <summary>
    /// <para>Like ParseFileInProject, but uses results from the background builder.</para>
    /// <para>All files are read from the FileSystem API, including the file being checked.</para>
    /// </summary>
    ///
    /// <param name="filename">The filename for the file.</param>
    /// <param name="options">The options for the project or script, used to determine active --define conditionals and other options relevant to parsing.</param>
    member GetBackgroundParseResultsForFileInProject : filename : string * options : FSharpProjectOptions -> Async<FSharpParseFileResults>

    /// <summary>
    /// <para>Like ParseFileInProject, but uses the existing results from the background builder.</para>
    /// <para>All files are read from the FileSystem API, including the file being checked.</para>
    /// </summary>
    ///
    /// <param name="filename">The filename for the file.</param>
    /// <param name="options">The options for the project or script, used to determine active --define conditionals and other options relevant to parsing.</param>
    member GetBackgroundCheckResultsForFileInProject : filename : string * options : FSharpProjectOptions -> Async<FSharpParseFileResults * FSharpCheckFileResults>

    /// Compile using the given flags.  Source files names are resolved via the FileSystem API. 
    /// The output file must be given by a -o flag. 
    /// The first argument is ignored and can just be "fsc.exe".
    member Compile: argv:string [] -> FSharpErrorInfo [] * int
    
    /// TypeCheck and compile provided AST
    member Compile: ast:ParsedInput list * assemblyName:string * outFile:string * dependencies:string list * ?pdbFile:string * ?executable:bool * ?noframework:bool -> FSharpErrorInfo [] * int

    /// Compiles to a dynamic assembly using the given flags.  
    ///
    /// The first argument is ignored and can just be "fsc.exe".
    ///
    /// Any source files names are resolved via the FileSystem API. An output file name must be given by a -o flag, but this will not
    /// be written - instead a dynamic assembly will be created and loaded.
    ///
    /// If the 'execute' parameter is given the entry points for the code are executed and 
    /// the given TextWriters are used for the stdout and stderr streams respectively. In this 
    /// case, a global setting is modified during the execution.
    member CompileToDynamicAssembly: otherFlags:string [] * execute:(TextWriter * TextWriter) option -> FSharpErrorInfo [] * int * System.Reflection.Assembly option

    /// TypeCheck and compile provided AST
    member CompileToDynamicAssembly: ast:ParsedInput list * assemblyName:string * dependencies:string list * execute:(TextWriter * TextWriter) option * ?debug:bool * ?noframework:bool -> FSharpErrorInfo [] * int * System.Reflection.Assembly option
       
    /// <summary>
    /// Try to get type check results for a file. This looks up the results of recent type checks of the
    /// same file, regardless of contents. The version tag specified in the original check of the file is returned.
    /// If the source of the file has changed the results returned by this function may be out of date, though may
    /// still be usable for generating intellisense menus and information.
    /// </summary>
    /// <param name="filename">The filename for the file.</param>
    /// <param name="options">The options for the project or script, used to determine active --define conditionals and other options relevant to parsing.</param>
    /// <param name="source">Optionally, specify source that must match the previous parse precisely.</param>
    member TryGetRecentCheckResultsForFile : filename: string * options:FSharpProjectOptions * ?source: string -> (FSharpParseFileResults * FSharpCheckFileResults * (*version*)int) option

    /// This function is called when the entire environment is known to have changed for reasons not encoded in the ProjectOptions of any project/compilation.
    /// For example, the type provider approvals file may have changed.
    member InvalidateAll : unit -> unit    
        
    /// This function is called when the configuration is known to have changed for reasons not encoded in the ProjectOptions.
    /// For example, dependent references may have been deleted or created.
    member InvalidateConfiguration: options: FSharpProjectOptions -> unit    

    /// Begin background parsing the given project.
    member StartBackgroundCompile: options: FSharpProjectOptions -> unit

    /// Set the project to be checked in the background.  Overrides any previous call to <c>CheckProjectInBackground</c>
    member CheckProjectInBackground: options: FSharpProjectOptions -> unit

    /// Stop the background compile.
    //[<Obsolete("Explicitly stopping background compilation is not recommended and the functionality to allow this may be rearchitected in future release.  If you use this functionality please add an issue on http://github.com/fsharp/FSharp.Compiler.Service describing how you use it and ignore this warning.")>]
    member StopBackgroundCompile : unit -> unit

    /// Block until the background compile finishes.
    //[<Obsolete("Explicitly waiting for background compilation is not recommended and the functionality to allow this may be rearchitected in future release.  If you use this functionality please add an issue on http://github.com/fsharp/FSharp.Compiler.Service describing how you use it and ignore this warning.")>]
    member WaitForBackgroundCompile : unit -> unit
    
    /// Report a statistic for testability
    static member GlobalForegroundParseCountStatistic : int

    /// Report a statistic for testability
    static member GlobalForegroundTypeCheckCountStatistic : int

    /// Flush all caches and garbage collect
    member ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients : unit -> unit

    /// Current queue length of the service, for debug purposes. 
    /// In addition, a single async operation or a step of a background build 
    /// may be in progress - such an operation is not counted in the queue length.
    member CurrentQueueLength : int

    /// This function is called when a project has been cleaned/rebuilt, and thus any live type providers should be refreshed.
    member NotifyProjectCleaned: options: FSharpProjectOptions -> Async<unit>
    
    /// Notify the host that the logical type checking context for a file has now been updated internally
    /// and that the file has become eligible to be re-typechecked for errors.
    ///
    /// The event will be raised on a background thread.
    member BeforeBackgroundFileCheck : IEvent<string * obj option>

    /// Raised after a parse of a file in the background analysis.
    ///
    /// The event will be raised on a background thread.
    member FileParsed : IEvent<string * obj option>

    /// Raised after a check of a file in the background analysis.
    ///
    /// The event will be raised on a background thread.
    member FileChecked : IEvent<string * obj option>
    
    /// Raised after the maxMB memory threshold limit is reached
    member MaxMemoryReached : IEvent<unit>

    /// A maximum number of megabytes of allocated memory. If the figure reported by <c>System.GC.GetTotalMemory(false)</c> goes over this limit, the FSharpChecker object will attempt to free memory and reduce cache sizes to a minimum.</param>
    member MaxMemory : int with get, set
    
    /// Get or set a flag which controls if background work is started implicitly. 
    ///
    /// If true, calls to CheckFileInProject implicitly start a background check of that project, replacing
    /// any other background checks in progress. This is useful in IDE applications with spare CPU cycles as 
    /// it prepares the project analysis results for use.  The default is 'true'.
    member ImplicitlyStartBackgroundWork: bool with get, set
    
    /// Get or set the pause time in milliseconds before background work is started.
    member PauseBeforeBackgroundWork: int with get, set
    
    /// Notify the host that a project has been fully checked in the background (using file contents provided by the file system API)
    ///
    /// The event may be raised on a background thread.
    member ProjectChecked : IEvent<string * obj option>

    // For internal use only 
    member internal ReactorOps : IReactorOperations

    [<Obsolete("Please create an instance of FSharpChecker using FSharpChecker.Create")>]
    static member Instance : FSharpChecker
    member internal FrameworkImportsCache : FrameworkImportsCache
    member internal ReferenceResolver : ReferenceResolver.Resolver



// An object to typecheck source in a given typechecking environment.
// Used internally to provide intellisense over F# Interactive.
type internal FsiInteractiveChecker =
    internal new : ReferenceResolver.Resolver * ops: IReactorOperations * tcConfig: TcConfig * tcGlobals: TcGlobals * tcImports: TcImports * tcState: TcState ->  FsiInteractiveChecker 
    member internal ParseAndCheckInteraction : CompilationThreadToken * source:string -> Async<FSharpParseFileResults * FSharpCheckFileResults * FSharpCheckProjectResults>
    static member internal CreateErrorInfos : tcConfig: TcConfig * allErrors:bool * mainInputFileName : string * seq<ErrorLogger.PhasedDiagnostic * FSharpErrorSeverity> -> FSharpErrorInfo[]

/// Information about the compilation environment
type [<Class>] CompilerEnvironment =
    /// The default location of FSharp.Core.dll and fsc.exe based on the version of fsc.exe that is running
    static member BinFolderOfDefaultFSharpCompiler : string option -> string option

/// Information about the compilation environment 
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]   
module CompilerEnvironment =
    /// These are the names of assemblies that should be referenced for .fs or .fsi files that
    /// are not associated with a project.
    val DefaultReferencesForOrphanSources : assumeDotNetFramework: bool -> string list
    /// Return the compilation defines that should be used when editing the given file.
    val GetCompilationDefinesForEditing : filename : string * compilerFlags : string list -> string list
    /// Return true if this is a subcategory of error or warning message that the language service can emit
    val IsCheckerSupportedSubcategory : string -> bool

/// Information about the debugging environment
module DebuggerEnvironment =
    /// Return the language ID, which is the expression evaluator id that the
    /// debugger will use.
    val GetLanguageID : unit -> Guid
    

/// A set of helpers related to naming of identifiers
module PrettyNaming =
    val IsIdentifierPartCharacter     : char -> bool
    val IsLongIdentifierPartCharacter : char -> bool
    val GetLongNameFromString         : string -> string list

    val FormatAndOtherOverloadsString : int -> string

    /// A utility to help determine if an identifier needs to be quoted 
    val QuoteIdentifierIfNeeded : string -> string

    /// All the keywords in the F# langauge 
    val KeywordNames : string list

