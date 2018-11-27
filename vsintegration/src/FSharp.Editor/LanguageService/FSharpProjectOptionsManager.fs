﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Collections.Immutable
open System.ComponentModel.Composition
open System.IO
open System.Linq
open Microsoft.CodeAnalysis
open Microsoft.FSharp.Compiler.CompileOps
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.VisualStudio
open Microsoft.VisualStudio.FSharp.Editor
open Microsoft.VisualStudio.LanguageServices
open Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
open Microsoft.VisualStudio.Shell
open System.Threading
open Microsoft.VisualStudio.Shell.Interop
open Microsoft.VisualStudio.LanguageServices.Implementation.TaskList

[<AutoOpen>]
module private FSharpProjectOptionsHelpers =

    let mapCpsProjectToSite(workspace:VisualStudioWorkspaceImpl, project:Project, serviceProvider:System.IServiceProvider, cpsCommandLineOptions: IDictionary<ProjectId, string[] * string[]>) =
        let hier = workspace.GetHierarchy(project.Id)
        let sourcePaths, referencePaths, options =
            match cpsCommandLineOptions.TryGetValue(project.Id) with
            | true, (sourcePaths, options) -> sourcePaths, [||], options
            | false, _ -> [||], [||], [||]
        {
            new IProvideProjectSite with
                member x.GetProjectSite() =
                    let mutable errorReporter = 
                        let reporter = ProjectExternalErrorReporter(project.Id, "FS", serviceProvider)
                        Some(reporter:> IVsLanguageServiceBuildErrorReporter2)

                    {
                        new IProjectSite with
                            member __.Description = project.Name
                            member __.CompilationSourceFiles = sourcePaths
                            member __.CompilationOptions =
                                Array.concat [options; referencePaths |> Array.map(fun r -> "-r:" + r)]
                            member __.CompilationReferences = referencePaths
                            member site.CompilationBinOutputPath = site.CompilationOptions |> Array.tryPick (fun s -> if s.StartsWith("-o:") then Some s.[3..] else None)
                            member __.ProjectFileName = project.FilePath
                            member __.AdviseProjectSiteChanges(_,_) = ()
                            member __.AdviseProjectSiteCleaned(_,_) = ()
                            member __.AdviseProjectSiteClosed(_,_) = ()
                            member __.IsIncompleteTypeCheckEnvironment = false
                            member __.TargetFrameworkMoniker = ""
                            member __.ProjectGuid =  project.Id.Id.ToString()
                            member __.LoadTime = System.DateTime.Now
                            member __.ProjectProvider = Some (x)
                            member __.BuildErrorReporter with get () = errorReporter and set (v) = errorReporter <- v
                    }
            interface IVsHierarchy with
                member __.SetSite(psp) = hier.SetSite(psp)
                member __.GetSite(psp) = hier.GetSite(ref psp)
                member __.QueryClose(pfCanClose)= hier.QueryClose(ref pfCanClose)
                member __.Close() = hier.Close()
                member __.GetGuidProperty(itemid, propid, pguid) = hier.GetGuidProperty(itemid, propid, ref pguid)
                member __.SetGuidProperty(itemid, propid, rguid) = hier.SetGuidProperty(itemid, propid, ref rguid)
                member __.GetProperty(itemid, propid, pvar) = hier.GetProperty(itemid, propid, ref pvar) 
                member __.SetProperty(itemid, propid, var)  = hier.SetProperty(itemid, propid, var)
                member __.GetNestedHierarchy(itemid, iidHierarchyNested, ppHierarchyNested, pitemidNested) = 
                    hier.GetNestedHierarchy(itemid, ref iidHierarchyNested, ref ppHierarchyNested, ref pitemidNested)
                member __.GetCanonicalName(itemid, pbstrName) = hier.GetCanonicalName(itemid, ref pbstrName)
                member __.ParseCanonicalName(pszName, pitemid) = hier.ParseCanonicalName(pszName, ref pitemid)
                member __.Unused0() = hier.Unused0()
                member __.AdviseHierarchyEvents(pEventSink, pdwCookie) = hier.AdviseHierarchyEvents(pEventSink, ref pdwCookie)
                member __.UnadviseHierarchyEvents(dwCookie) = hier.UnadviseHierarchyEvents(dwCookie)
                member __.Unused1() = hier.Unused1()
                member __.Unused2() = hier.Unused2()
                member __.Unused3() = hier.Unused3()
                member __.Unused4() = hier.Unused4()
        }

[<RequireQualifiedAccess>]
type private FSharpProjectOptionsMessage =
    | TryGetOptionsByDocument of Document * AsyncReplyChannel<(FSharpParsingOptions * FSharpProjectOptions) option>
    | TryGetOptionsByProject of Project * AsyncReplyChannel<(FSharpParsingOptions * FSharpProjectOptions) option>
    | ClearOptions of ProjectId
    | ClearSingleFileOptionsCache of DocumentId

[<Sealed>]
type private FSharpProjectOptionsReactor (workspace: VisualStudioWorkspaceImpl, settings: EditorOptions, serviceProvider, checkerProvider: FSharpCheckerProvider) =
    let cancellationTokenSource = new CancellationTokenSource()

    // Hack to store command line options from HandleCommandLineChanges
    let cpsCommandLineOptions = new ConcurrentDictionary<ProjectId, string[] * string[]>()

    let cache = Dictionary<ProjectId, VersionStamp * FSharpParsingOptions * FSharpProjectOptions>()
    let singleFileCache = Dictionary<DocumentId, VersionStamp * FSharpParsingOptions * FSharpProjectOptions>()

    let rec tryComputeOptionsByFile (document: Document) =
        async {
            let! text = document.GetTextAsync() |> Async.AwaitTask
            let! fileStamp = document.GetTextVersionAsync() |> Async.AwaitTask
            let! scriptProjectOptions, _ = checkerProvider.Checker.GetProjectOptionsFromScript(document.FilePath, text.ToString(), DateTime.Now)
            match singleFileCache.TryGetValue(document.Id) with
            | false, _ ->
                let projectOptions =
                    if isScriptFile document.FilePath then
                        scriptProjectOptions
                    else
                        {
                            ProjectFileName = document.FilePath
                            ProjectId = None
                            SourceFiles = [|document.FilePath|]
                            OtherOptions = [||]
                            ReferencedProjects = [||]
                            IsIncompleteTypeCheckEnvironment = false
                            UseScriptResolutionRules = SourceFile.MustBeSingleFileProject (Path.GetFileName(document.FilePath))
                            LoadTime = DateTime.Now
                            UnresolvedReferences = None
                            OriginalLoadReferences = []
                            ExtraProjectInfo= None
                            Stamp = Some(int64 (fileStamp.GetHashCode()))
                        }

                checkerProvider.Checker.InvalidateConfiguration(projectOptions, startBackgroundCompileIfAlreadySeen = true, userOpName = "computeOptions")

                let parsingOptions, _ = checkerProvider.Checker.GetParsingOptionsFromProjectOptions(projectOptions)

                singleFileCache.[document.Id] <- (fileStamp, parsingOptions, projectOptions)

                return Some(parsingOptions, projectOptions)

            | true, (fileStamp2, parsingOptions, projectOptions) ->
                if fileStamp <> fileStamp2 then
                    singleFileCache.Remove(document.Id) |> ignore
                    return! tryComputeOptionsByFile document
                else
                    return Some(parsingOptions, projectOptions)
        }
    
    let rec tryComputeOptions (project: Project) =
        let projectId = project.Id
        let projectStamp = project.Version
        match cache.TryGetValue(projectId) with
        | false, _ ->

            // Because this code can be kicked off before the hack, HandleCommandLineChanges, occurs,
            //     the command line options will not be available and we should bail if one of the project references does not give us anything.
            let mutable canBail = false

            let referencedProjects =
                if settings.LanguageServicePerformance.EnableInMemoryCrossProjectReferences then
                    project.ProjectReferences
                    |> Seq.choose (fun projectReference -> 
                        let referenceProject = project.Solution.GetProject(projectReference.ProjectId)
                        if referenceProject.Language = FSharpConstants.FSharpLanguageName then
                            Some(referenceProject)
                        else
                            None
                    )
                    |> Seq.choose (fun referenceProject ->
                        let result =
                            tryComputeOptions referenceProject
                            |> Option.map (fun (_, projectOptions) ->
                                (referenceProject.OutputFilePath, projectOptions)
                            )

                        if result.IsNone then
                            canBail <- true

                        result
                    )
                    |> Seq.toArray
                else
                    [||]

            if canBail then
                None
            else

            let hier = workspace.GetHierarchy(projectId)
            let projectSite = 
                match hier with
                // Legacy
                | (:? IProvideProjectSite as provideSite) -> provideSite.GetProjectSite()
                // Cps
                | _ -> 
                    let provideSite = mapCpsProjectToSite(workspace, project, serviceProvider, cpsCommandLineOptions)
                    provideSite.GetProjectSite()

            let otherOptions =
                project.ProjectReferences
                |> Seq.map (fun x -> "-r:" + project.Solution.GetProject(x.ProjectId).OutputFilePath)
                |> Array.ofSeq
                |> Array.append (
                        project.MetadataReferences.OfType<VisualStudioMetadataReference.Snapshot>()
                        |> Seq.map (fun x -> "-r:" + x.FilePath)
                        |> Array.ofSeq
                        |> Array.append (
                                // Clear any references from CompilationOptions. 
                                // We get the references from Project.ProjectReferences/Project.MetadataReferences.
                                projectSite.CompilationOptions 
                                |> Array.filter (fun x -> not (x.Contains("-r:")))
                            )
                    )

            let projectOptions =
                {
                    ProjectFileName = projectSite.ProjectFileName
                    ProjectId = Some(projectId.ToFSharpProjectIdString())
                    SourceFiles = projectSite.CompilationSourceFiles
                    OtherOptions = otherOptions
                    ReferencedProjects = referencedProjects
                    IsIncompleteTypeCheckEnvironment = projectSite.IsIncompleteTypeCheckEnvironment
                    UseScriptResolutionRules = SourceFile.MustBeSingleFileProject (Path.GetFileName(project.FilePath))
                    LoadTime = projectSite.LoadTime
                    UnresolvedReferences = None
                    OriginalLoadReferences = []
                    ExtraProjectInfo= None
                    Stamp = Some(int64 (projectStamp.GetHashCode()))
                }

            // This can happen if we didn't receive the callback from HandleCommandLineChanges yet.
            if Array.isEmpty projectOptions.SourceFiles then
                None
            else
                checkerProvider.Checker.InvalidateConfiguration(projectOptions, startBackgroundCompileIfAlreadySeen = true, userOpName = "computeOptions")

                let parsingOptions, _ = checkerProvider.Checker.GetParsingOptionsFromProjectOptions(projectOptions)

                cache.[projectId] <- (projectStamp, parsingOptions, projectOptions)

                Some(parsingOptions, projectOptions)
  
        | true, (projectStamp2, parsingOptions, projectOptions) ->
            if projectStamp <> projectStamp2 then
                cache.Remove(projectId) |> ignore
                tryComputeOptions project
            else
                Some(parsingOptions, projectOptions)

    let loop (agent: MailboxProcessor<FSharpProjectOptionsMessage>) =
        async {
            while true do
                match! agent.Receive() with
                | FSharpProjectOptionsMessage.TryGetOptionsByDocument(document, reply) ->
                    try
                        // For now, disallow miscellaneous workspace since we are using the hacky F# miscellaneous files project.
                        if document.Project.Solution.Workspace.Kind = WorkspaceKind.MiscellaneousFiles then
                            reply.Reply(None)
                        elif document.Project.Name = FSharpConstants.FSharpMiscellaneousFilesName then
                            let! options = tryComputeOptionsByFile document
                            reply.Reply(options)
                        else
                            reply.Reply(tryComputeOptions document.Project)
                    with
                    | _ ->
                        reply.Reply(None)
                | FSharpProjectOptionsMessage.TryGetOptionsByProject(project, reply) ->
                    try
                        if project.Solution.Workspace.Kind = WorkspaceKind.MiscellaneousFiles || project.Name = FSharpConstants.FSharpMiscellaneousFilesName then
                            reply.Reply(None)
                        else
                            reply.Reply(tryComputeOptions project)
                    with
                    | _ ->
                        reply.Reply(None)
                | FSharpProjectOptionsMessage.ClearOptions(projectId) ->
                    cache.Remove(projectId) |> ignore
                | FSharpProjectOptionsMessage.ClearSingleFileOptionsCache(documentId) ->
                    singleFileCache.Remove(documentId) |> ignore
        }

    let agent = MailboxProcessor.Start((fun agent -> loop agent), cancellationToken = cancellationTokenSource.Token)

    member __.TryGetOptionsByProjectAsync(project) =
        agent.PostAndAsyncReply(fun reply -> FSharpProjectOptionsMessage.TryGetOptionsByProject(project, reply))

    member __.TryGetOptionsByDocumentAsync(document) =
        agent.PostAndAsyncReply(fun reply -> FSharpProjectOptionsMessage.TryGetOptionsByDocument(document, reply))

    member __.ClearOptionsByProjectId(projectId) =
        agent.Post(FSharpProjectOptionsMessage.ClearOptions(projectId))

    member __.ClearSingleFileOptionsCache(documentId) =
        agent.Post(FSharpProjectOptionsMessage.ClearSingleFileOptionsCache(documentId))

    member __.SetCpsCommandLineOptions(projectId, sourcePaths, options) =
        cpsCommandLineOptions.[projectId] <- (sourcePaths, options)

    member __.TryGetCachedOptionsByProjectId(projectId) =
        match cache.TryGetValue(projectId) with
        | true, result -> Some(result)
        | _ -> None

    interface IDisposable with
        member __.Dispose() = 
            cancellationTokenSource.Cancel()
            cancellationTokenSource.Dispose() 
            (agent :> IDisposable).Dispose()

/// Exposes FCS FSharpProjectOptions information management as MEF component.
//
// This service allows analyzers to get an appropriate FSharpProjectOptions value for a project or single file.
// It also allows a 'cheaper' route to get the project options relevant to parsing (e.g. the #define values).
// The main entrypoints are TryGetOptionsForDocumentOrProject and TryGetOptionsForEditingDocumentOrProject.
[<Export(typeof<FSharpProjectOptionsManager>); Composition.Shared>]
type internal FSharpProjectOptionsManager
    [<ImportingConstructor>]
    (
        checkerProvider: FSharpCheckerProvider,
        [<Import(typeof<VisualStudioWorkspace>)>] workspace: VisualStudioWorkspaceImpl,
        [<Import(typeof<SVsServiceProvider>)>] serviceProvider: System.IServiceProvider,
        settings: EditorOptions
    ) =

    let projectDisplayNameOf projectFileName =
        if String.IsNullOrWhiteSpace projectFileName then projectFileName
        else Path.GetFileNameWithoutExtension projectFileName

    let reactor = new FSharpProjectOptionsReactor(workspace, settings, serviceProvider, checkerProvider)

    do
        // We need to listen to this event for lifecycle purposes.
        workspace.WorkspaceChanged.Add(fun args ->
            match args.Kind with
            | WorkspaceChangeKind.ProjectRemoved -> reactor.ClearOptionsByProjectId(args.ProjectId)
            | _ -> ()
        )

    /// Clear a project from the project table
    member this.ClearInfoForProject(projectId:ProjectId) = 
        reactor.ClearOptionsByProjectId(projectId)

    member this.ClearSingleFileOptionsCache(documentId) =
        reactor.ClearSingleFileOptionsCache(documentId)

    /// Get compilation defines relevant for syntax processing.  
    /// Quicker then TryGetOptionsForDocumentOrProject as it doesn't need to recompute the exact project 
    /// options for a script.
    member this.GetCompilationDefinesForEditingDocument(document:Document) = 
        let parsingOptions =
            match reactor.TryGetCachedOptionsByProjectId(document.Project.Id) with
            | Some (_, parsingOptions, _) -> parsingOptions
            | _ -> { FSharpParsingOptions.Default with IsInteractive = IsScript document.Name }
        CompilerEnvironment.GetCompilationDefinesForEditing parsingOptions     

    member this.TryGetOptionsByProject(project) =
        reactor.TryGetOptionsByProjectAsync(project)

    /// Get the exact options for a document or project
    member this.TryGetOptionsForDocumentOrProject(document: Document) =
        async { 
            match! reactor.TryGetOptionsByDocumentAsync(document) with
            | Some(parsingOptions, projectOptions) ->
                return Some(parsingOptions, None, projectOptions)
            | _ ->
                return None
        }

    /// Get the options for a document or project relevant for syntax processing.
    /// Quicker then TryGetOptionsForDocumentOrProject as it doesn't need to recompute the exact project options for a script.
    member this.TryGetOptionsForEditingDocumentOrProject(document:Document) = 
        async {
            let! result = this.TryGetOptionsForDocumentOrProject(document) 
            return result |> Option.map(fun (parsingOptions, _, projectOptions) -> parsingOptions, projectOptions)
        }

    [<Export>]
    /// This handles commandline change notifications from the Dotnet Project-system
    /// Prior to VS 15.7 path contained path to project file, post 15.7 contains target binpath
    /// binpath is more accurate because a project file can have multiple in memory projects based on configuration
    member __.HandleCommandLineChanges(path:string, sources:ImmutableArray<CommandLineSourceFile>, _references:ImmutableArray<CommandLineReference>, options:ImmutableArray<string>) =
        use _logBlock = Logger.LogBlock(LogEditorFunctionId.LanguageService_HandleCommandLineArgs)

        let projectId =
            match workspace.ProjectTracker.TryGetProjectByBinPath(path) with
            | true, project -> project.Id
            | false, _ -> workspace.ProjectTracker.GetOrCreateProjectIdForPath(path, projectDisplayNameOf path)
        let project =  workspace.ProjectTracker.GetProject(projectId)
        let path = project.ProjectFilePath
        let fullPath p =
            if Path.IsPathRooted(p) || path = null then p
            else Path.Combine(Path.GetDirectoryName(path), p)
        let sourcePaths = sources |> Seq.map(fun s -> fullPath s.Path) |> Seq.toArray

        reactor.SetCpsCommandLineOptions(projectId, sourcePaths, options.ToArray())

    member __.Checker = checkerProvider.Checker