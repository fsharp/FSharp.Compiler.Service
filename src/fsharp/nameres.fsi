//----------------------------------------------------------------------------
//
// Copyright (c) 2002-2012 Microsoft Corporation. 
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
//----------------------------------------------------------------------------


module internal Microsoft.FSharp.Compiler.Nameres

open Microsoft.FSharp.Compiler 
open Microsoft.FSharp.Compiler.Lib
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.ErrorLogger
open Microsoft.FSharp.Compiler.Infos
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.Import
open Microsoft.FSharp.Compiler.Outcome
open Microsoft.FSharp.Compiler.Tast
open Microsoft.FSharp.Compiler.Tastops
open Microsoft.FSharp.Compiler.Env
open Microsoft.FSharp.Compiler.AbstractIL 
open Microsoft.FSharp.Compiler.AbstractIL.IL
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library
open Microsoft.FSharp.Compiler.PrettyNaming



/// A NameResolver primarily holds an InfoReader
type NameResolver =
    new : g:TcGlobals * amap:ImportMap * infoReader:InfoReader * instantiationGenerator:(range -> Typars -> TypeInst) -> NameResolver
    member InfoReader : InfoReader
    member amap : ImportMap
    member g : TcGlobals

//---------------------------------------------------------------------------
// 
//------------------------------------------------------------------------- 

/// Detect a use of a nominal type, including type abbreviations.
/// When reporting symbols, we care about abbreviations, e.g. 'int' and 'int32' count as two separate symbols.
val (|AbbrevOrAppTy|_|) : TType -> TyconRef option

[<NoEquality; NoComparison; RequireQualifiedAccess>]
type Item = 
  // These exist in the "eUnqualifiedItems" List.map in the type environment. 
  | Value of  ValRef
  | UnionCase of UnionCaseInfo
  | ActivePatternResult of ActivePatternInfo * TType * int  * range
  | ActivePatternCase of ActivePatternElemRef 
  | ExnCase of TyconRef 
  | RecdField of RecdFieldInfo
  | NewDef of Ident
  | ILField of ILFieldInfo
  | Event of EventInfo
  | Property of string * PropInfo list
  | MethodGroup of string * MethInfo list
  | CtorGroup of string * MethInfo list
  | FakeInterfaceCtor of TType
  | DelegateCtor of TType
  | Types of string * TType list
  /// CustomOperation(operationName, operationHelpText, operationImplementation).
  /// 
  /// Used to indicate the availability or resolution of a custom query operation such as 'sortBy' or 'where' in computation expression syntax
  | CustomOperation of string * (unit -> string option) * MethInfo option
  | CustomBuilder of string * ValRef
  | TypeVar of string * Typar
  | ModuleOrNamespaces of Tast.ModuleOrNamespaceRef list
  /// Represents the resolution of a source identifier to an implicit use of an infix operator (+solution if such available)
  | ImplicitOp of Ident * TraitConstraintSln option ref
  /// Represents the resolution of a source identifier to a named argument
  | ArgName of Ident * TType
  | SetterArg of Ident * Item 
  | UnqualifiedType of TyconRef list
  member DisplayName : string


[<Sealed>]
/// Information about an extension member held in the name resolution environment
type ExtensionMember 

[<NoEquality; NoComparison>]
/// The environment of information used to resolve names
type NameResolutionEnv =
    {eDisplayEnv: DisplayEnv
     eUnqualifiedItems: LayeredMap<string,Item>
     ePatItems: NameMap<Item>
     eModulesAndNamespaces: NameMultiMap<ModuleOrNamespaceRef>
     eFullyQualifiedModulesAndNamespaces: NameMultiMap<ModuleOrNamespaceRef>
     eFieldLabels: NameMultiMap<RecdFieldRef>
     eTyconsByAccessNames: LayeredMultiMap<string,TyconRef>
     eFullyQualifiedTyconsByAccessNames: LayeredMultiMap<string,TyconRef>
     eTyconsByDemangledNameAndArity: LayeredMap<NameArityPair,TyconRef>
     eFullyQualifiedTyconsByDemangledNameAndArity: LayeredMap<NameArityPair,TyconRef>
     eIndexedExtensionMembers: TyconRefMultiMap<ExtensionMember>
     eUnindexedExtensionMembers: ExtensionMember list
     eTypars: NameMap<Typar> }
    static member Empty : g:TcGlobals -> NameResolutionEnv
    member DisplayEnv : DisplayEnv
    member FindUnqualifiedItem : string -> Item

type FullyQualifiedFlag =
  | FullyQualified
  | OpenQualified

[<RequireQualifiedAccess>]
type BulkAdd = Yes | No

val internal AddFakeNamedValRefToNameEnv : string -> NameResolutionEnv -> ValRef -> NameResolutionEnv
val internal AddFakeNameToNameEnv : string -> NameResolutionEnv -> Item -> NameResolutionEnv

val internal AddValRefToNameEnv                    : NameResolutionEnv -> ValRef -> NameResolutionEnv
val internal AddActivePatternResultTagsToNameEnv   : ActivePatternInfo -> NameResolutionEnv -> TType -> range -> NameResolutionEnv
val internal AddTyconRefsToNameEnv                 : BulkAdd -> bool -> TcGlobals -> ImportMap -> range -> bool -> NameResolutionEnv -> TyconRef list -> NameResolutionEnv
val internal AddExceptionDeclsToNameEnv            : BulkAdd -> NameResolutionEnv -> TyconRef -> NameResolutionEnv
val internal AddModuleAbbrevToNameEnv              : Ident -> NameResolutionEnv -> ModuleOrNamespaceRef list -> NameResolutionEnv
val internal AddModuleOrNamespaceRefsToNameEnv                   : TcGlobals -> ImportMap -> range -> bool -> AccessorDomain -> NameResolutionEnv -> ModuleOrNamespaceRef list -> NameResolutionEnv
val internal AddModuleOrNamespaceRefToNameEnv                    : TcGlobals -> ImportMap -> range -> bool -> AccessorDomain -> NameResolutionEnv -> ModuleOrNamespaceRef -> NameResolutionEnv
val internal AddModulesAndNamespacesContentsToNameEnv : TcGlobals -> ImportMap -> AccessorDomain -> range -> NameResolutionEnv -> ModuleOrNamespaceRef list -> NameResolutionEnv

type CheckForDuplicateTyparFlag =
  | CheckForDuplicateTypars
  | NoCheckForDuplicateTypars

val internal AddDeclaredTyparsToNameEnv : CheckForDuplicateTyparFlag -> NameResolutionEnv -> Typar list -> NameResolutionEnv
val internal LookupTypeNameInEnvNoArity : FullyQualifiedFlag -> string -> NameResolutionEnv -> TyconRef list

type TypeNameResolutionFlag =
  | ResolveTypeNamesToCtors
  | ResolveTypeNamesToTypeRefs

[<Sealed>]
[<NoEquality; NoComparison>]
type TypeNameResolutionStaticArgsInfo = 
  /// Indicates definite knowledge of empty type arguments, i.e. the logical equivalent of name< >
  static member DefiniteEmpty : TypeNameResolutionStaticArgsInfo
  /// Deduce definite knowledge of type arguments
  static member FromTyArgs : numTyArgs:int -> TypeNameResolutionStaticArgsInfo

[<NoEquality; NoComparison>]
type TypeNameResolutionInfo = 
  | TypeNameResolutionInfo of TypeNameResolutionFlag * TypeNameResolutionStaticArgsInfo
  static member Default : TypeNameResolutionInfo
  static member ResolveToTypeRefs : TypeNameResolutionStaticArgsInfo -> TypeNameResolutionInfo

[<RequireQualifiedAccess>]
type internal ItemOccurence = 
    | Binding 
    | Use 
    | UseInType 
    | UseInAttribute 
    | Pattern 
    | Implemented 
  
val ItemsReferToSameDefinition : TcGlobals -> Item -> Item -> bool

[<Class>]
type internal CapturedNameResolution = 
    /// line and column
    member Pos : pos
    /// Named item
    member Item : Item
    member ItemOccurence : ItemOccurence
    /// Information about printing. For example, should redundant keywords be hidden?
    member DisplayEnv : DisplayEnv
    /// Naming environment--for example, currently open namespaces.
    member NameResolutionEnv : NameResolutionEnv
    member AccessorDomain : AccessorDomain
    /// The starting and ending position      
    member Range : range

[<Class>]
type internal TcResolutions = 
    /// Name resolution environments for every interesting region in the file. These regions may
    /// overlap, in which case the smallest region applicable should be used.
    member CapturedEnvs : ResizeArray<range * NameResolutionEnv * AccessorDomain>
    /// Information of exact types found for expressions, that can be to the left of a dot.
    /// typ - the inferred type for an expression
    member CapturedExpressionTypings : ResizeArray<pos * TType * DisplayEnv * NameResolutionEnv * AccessorDomain * range>
    /// Exact name resolutions
    member CapturedNameResolutions : ResizeArray<CapturedNameResolution>
    member CapturedMethodGroupResolutions : ResizeArray<CapturedNameResolution>
    member GetUsesOfSymbol : Item -> (ItemOccurence * DisplayEnv * range)[]
    member GetAllUsesOfSymbols : unit -> (Item * ItemOccurence * DisplayEnv * range)[]

type ITypecheckResultsSink =
    abstract NotifyEnvWithScope   : range * NameResolutionEnv * AccessorDomain -> unit
    abstract NotifyExprHasType    : pos * TType * DisplayEnv * NameResolutionEnv * AccessorDomain * range -> unit
    abstract NotifyNameResolution : pos * Item * Item * ItemOccurence * DisplayEnv * NameResolutionEnv * AccessorDomain * range -> unit

type internal TcResultsSinkImpl =
    new : tcGlobals : TcGlobals -> TcResultsSinkImpl
    member GetTcResolutions : unit -> TcResolutions
    interface ITypecheckResultsSink

type TcResultsSink = 
    { mutable CurrentSink : ITypecheckResultsSink option }
    static member NoSink : TcResultsSink
    static member WithSink : ITypecheckResultsSink -> TcResultsSink

val internal WithNewTypecheckResultsSink : ITypecheckResultsSink * TcResultsSink -> System.IDisposable
val internal TemporarilySuspendReportingTypecheckResultsToSink : TcResultsSink -> System.IDisposable
val internal CallEnvSink                : TcResultsSink -> range * NameResolutionEnv * AccessorDomain -> unit
val internal CallNameResolutionSink     : TcResultsSink -> range * NameResolutionEnv * Item * Item * ItemOccurence * DisplayEnv * AccessorDomain -> unit
val internal CallExprHasTypeSink        : TcResultsSink -> range * NameResolutionEnv * TType * DisplayEnv * AccessorDomain -> unit

val internal AllPropInfosOfTypeInScope : InfoReader -> NameResolutionEnv -> string option * AccessorDomain -> FindMemberFlag -> range -> TType -> PropInfo list
val internal AllMethInfosOfTypeInScope : InfoReader -> NameResolutionEnv -> string option * AccessorDomain -> FindMemberFlag -> range -> TType -> MethInfo list

exception internal IndeterminateType of range
exception internal UpperCaseIdentifierInPattern of range

val FreshenRecdFieldRef :NameResolver -> Range.range -> Tast.RecdFieldRef -> Item

type LookupKind =
  | RecdField
  | Pattern
  | Expr
  | Type
  | Ctor


type WarnOnUpperFlag =
  | WarnOnUpperCase
  | AllIdsOK

[<RequireQualifiedAccess>]
/// Indicates whether we permit a direct reference to a type generator. Only set when resolving the
/// right-hand-side of a [<Generate>] declaration.
type PermitDirectReferenceToGeneratedType = 
    | Yes 
    | No

val internal ResolveLongIndentAsModuleOrNamespace   : Import.ImportMap -> range -> FullyQualifiedFlag -> NameResolutionEnv -> AccessorDomain -> Ident list -> ResultOrException<(int * ModuleOrNamespaceRef * ModuleOrNamespaceType) list >
val internal ResolveObjectConstructor               : NameResolver -> DisplayEnv -> range -> AccessorDomain -> TType -> ResultOrException<Item>
val internal ResolveLongIdentInType                 : TcResultsSink -> NameResolver -> NameResolutionEnv -> LookupKind -> range -> AccessorDomain -> Ident list -> FindMemberFlag -> TypeNameResolutionInfo -> TType -> Item * Ident list
val internal ResolvePatternLongIdent                : TcResultsSink -> NameResolver -> WarnOnUpperFlag -> bool -> range -> AccessorDomain -> NameResolutionEnv -> TypeNameResolutionInfo -> Ident list -> Item
val internal ResolveTypeLongIdentInTyconRef         : TcResultsSink -> NameResolver -> NameResolutionEnv -> TypeNameResolutionInfo -> AccessorDomain -> range -> ModuleOrNamespaceRef -> Ident list -> TyconRef 
val internal ResolveTypeLongIdent                   : TcResultsSink -> NameResolver -> ItemOccurence -> FullyQualifiedFlag -> NameResolutionEnv -> AccessorDomain -> Ident list -> TypeNameResolutionStaticArgsInfo -> PermitDirectReferenceToGeneratedType -> ResultOrException<TyconRef>
val internal ResolveField                           : NameResolver -> NameResolutionEnv -> AccessorDomain -> TType -> Ident list * Ident -> RecdFieldRef list
val internal ResolveExprLongIdent                   : TcResultsSink -> NameResolver -> range -> AccessorDomain -> NameResolutionEnv -> TypeNameResolutionInfo -> Ident list -> Item * Ident list
val internal ResolvePartialLongIdentToClassOrRecdFields : NameResolver -> NameResolutionEnv -> range -> AccessorDomain -> string list -> bool -> Item list
val internal ResolveRecordOrClassFieldsOfType       : NameResolver -> range -> AccessorDomain -> TType -> bool -> Item list

type IfOverloadResolutionFails = IfOverloadResolutionFails of (unit -> unit)
// Specifies if overload resolution needs to notify Language Service of overload resolution
[<RequireQualifiedAccess>]
type AfterOverloadResolution =
    // Notfication is not needed
    |   DoNothing
    // Notfy the sink
    |   SendToSink of (Item -> unit) * IfOverloadResolutionFails // overload resolution failure fallback
    // Find override among given overrides and notify the sink
    // 'Item' contains the candidate overrides.
    |   ReplaceWithOverrideAndSendToSink of Item * (Item -> unit) * IfOverloadResolutionFails // overload resolution failure fallback

val internal ResolveLongIdentAsExprAndComputeRange  : TcResultsSink -> NameResolver -> range -> AccessorDomain -> NameResolutionEnv -> TypeNameResolutionInfo -> Ident list -> Item * range * Ident list * AfterOverloadResolution
val internal ResolveExprDotLongIdentAndComputeRange : TcResultsSink -> NameResolver -> range -> AccessorDomain -> NameResolutionEnv -> TType -> Ident list -> FindMemberFlag -> bool -> Item * range * Ident list * AfterOverloadResolution

val FakeInstantiationGenerator : range -> Typar list -> TType list
val ResolvePartialLongIdent : NameResolver -> NameResolutionEnv -> (MethInfo -> TType -> bool) -> range -> AccessorDomain -> string list -> bool -> Item list
val ResolveCompletionsInType       : NameResolver -> NameResolutionEnv -> (MethInfo -> TType -> bool) -> Range.range -> Infos.AccessorDomain -> bool -> TType -> Item list
