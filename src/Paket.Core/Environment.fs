﻿namespace Paket

open System.IO

open Chessie.ErrorHandling
open Paket.Domain

type PaketEnv = {
    RootDirectory : DirectoryInfo
    DependenciesFile : DependenciesFile
    LockFile : option<LockFile>
    Projects : list<ProjectFile * ReferencesFile>
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PaketEnv = 

    let create root dependenciesFile lockFile projects = 
        { RootDirectory = root
          DependenciesFile = dependenciesFile
          LockFile = lockFile
          Projects = projects }

    let fromRootDirectory (directory : DirectoryInfo) = trial {
        if not directory.Exists then 
            return! fail (DirectoryDoesntExist directory)
        else
            let! dependenciesFile = 
                let fi = FileInfo(Path.Combine(directory.FullName, Constants.DependenciesFileName))
                if not fi.Exists then
                    fail (DependenciesFileNotFoundInDir directory)
                else
                    try
                        ok (DependenciesFile.ReadFromFile(fi.FullName))
                    with _ ->
                        fail (DependenciesFileParseError fi)

            let! lockFile =
                let fi = FileInfo(Path.Combine(directory.FullName, Constants.LockFileName))
                if not fi.Exists then
                    None |> ok
                else
                    try
                        LockFile.LoadFrom(fi.FullName) |> Some |> ok
                    with _ ->
                        fail (LockFileParseError fi)

            let! projects = InstallProcess.findAllReferencesFiles(directory.FullName)

            return create directory dependenciesFile lockFile projects
    }

    let locatePaketRootDirectory (directory : DirectoryInfo) = 
        if not directory.Exists then 
            None
        else
            directory
            |> Seq.unfold (function
                | null -> None
                | dir -> Some(Path.Combine(dir.FullName, Constants.DependenciesFileName), dir.Parent))
            |> Seq.tryFind File.Exists
            |> Option.map (fun f -> DirectoryInfo(Path.GetDirectoryName(f)))

    let ensureNotExists (directory : DirectoryInfo) =
        match fromRootDirectory directory with
        | Result.Ok(_) -> fail (PaketEnvAlreadyExistsInDirectory directory)
        | Result.Bad(msgs) -> 
            let filtered = 
                msgs
                |> List.filter (function
                    | DependenciesFileNotFoundInDir _ -> false
                    | _ -> true )
            if filtered |> List.isEmpty then ok directory
            else Result.Bad filtered

    let ensureNotInStrictMode environment =
        if not environment.DependenciesFile.Groups.[Constants.MainDependencyGroup].Options.Strict then ok environment
        else fail StrictModeDetected

    let ensureLockFileExists environment =
        environment.LockFile
        |> failIfNone (LockFileNotFound environment.RootDirectory)

    let init (directory : DirectoryInfo) =
        match locatePaketRootDirectory directory with
        | Some rootDirectory -> 
            Logging.tracefn "Paket is already initialized in %s" rootDirectory.FullName
            ok ()
        | None -> 
            let sources = [PackageSources.DefaultNugetSource]
            let serialized = 
                (sources
                |> List.map (string >> DependenciesFileSerializer.sourceString))
                @ [""]
                |> Array.ofList

            let mainGroup = { Name = Constants.MainDependencyGroup; Options = InstallOptions.Default; Sources = sources; Packages = []; RemoteFiles = [] }
            let groups = [Constants.MainDependencyGroup, mainGroup] |> Map.ofSeq
                
            let dependenciesFile = 
                DependenciesFile(
                    Path.Combine(directory.FullName, Constants.DependenciesFileName),
                    groups,
                    serialized)

            dependenciesFile.ToString() |> saveFile dependenciesFile.FileName