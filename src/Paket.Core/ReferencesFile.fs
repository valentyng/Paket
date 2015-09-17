﻿namespace Paket

open System
open System.IO
open Logging
open Paket.Domain
open Paket.Requirements

type RemoteFileReference = 
    { Name : string
      Link : string
      Settings : RemoteFileInstallSettings }

type PackageInstallSettings = 
    { Name : PackageName
      Settings : InstallSettings }

    static member Default(name) =
        { Name = PackageName name
          Settings = InstallSettings.Default }

type InstallGroup = 
    { Name : GroupName
      NugetPackages : PackageInstallSettings list
      RemoteFiles : RemoteFileReference list }

type ReferencesFile = 
    { FileName: string
      Groups: Map<GroupName,InstallGroup> } 
    
    static member DefaultLink = Constants.PaketFilesFolderName

    static member New(fileName) = 
        let groups = [Constants.MainDependencyGroup, { Name = Constants.MainDependencyGroup; NugetPackages = []; RemoteFiles = [] }] |> Map.ofList
        { FileName = fileName
          Groups = groups }

    static member FromLines(lines : string[]) = 
        let groupedLines =
            lines
            |> Array.fold (fun state line -> 
                match state with
                | [] -> failwithf "error while parsing %A" lines
                | ((name,lines) as currentGroup)::otherGroups ->
                    if line.StartsWith "group " then
                        let name = line.Replace("group","").Trim()
                        (GroupName name,[])::currentGroup::otherGroups
                    else 
                        (name,line::lines)::otherGroups) [Constants.MainDependencyGroup,[]]
            |> List.map (fun (name,lines) -> name,lines |> List.rev |> Array.ofList)

        let isSingleFile (line: string) = line.StartsWith "File:"
        let notEmpty (line: string) = not <| String.IsNullOrWhiteSpace line
        let parsePackageInstallSettings (line: string) : PackageInstallSettings = 
            let parts = line.Split(' ')
            { Name = PackageName parts.[0]
              Settings = InstallSettings.Parse(line.Replace(parts.[0],"")) } 

        let groups = 
            groupedLines 
            |> List.map (fun (groupName,lines) ->
                    let remoteLines,nugetLines =
                        lines 
                        |> Array.filter notEmpty 
                        |> Array.map (fun s -> s.Trim())
                        |> Array.toList
                        |> List.partition isSingleFile 

        
                    let nugetPackages =
                        nugetLines
                        |> List.map parsePackageInstallSettings

                    let remoteFiles = 
                        remoteLines
                        |> List.map (fun s -> s.Replace("File:","").Split([|' '|], StringSplitOptions.RemoveEmptyEntries))
                        |> List.map (fun segments ->
                                        let hasPath =
                                            let get x = if segments.Length > x then segments.[x] else ""
                                            segments.Length >= 2 && not ((get 1).Contains(":")) && not ((get 2).StartsWith(":")) 

                                        let rest = 
                                            let skip = if hasPath then 2 else 1
                                            if segments.Length < skip then "" else String.Join(" ",segments |> Seq.skip skip)

                                        { Name = segments.[0]
                                          Link = if hasPath then segments.[1] else ReferencesFile.DefaultLink 
                                          Settings = RemoteFileInstallSettings.Parse rest })
                    { Name = groupName; NugetPackages = nugetPackages; RemoteFiles = remoteFiles })
            |> List.fold (fun m g -> 
                match Map.tryFind g.Name m with
                | None -> Map.add g.Name g m
                | Some group -> 
                    let newGroup = 
                        { Name = g.Name
                          NugetPackages = g.NugetPackages @ group.NugetPackages
                          RemoteFiles = g.RemoteFiles @ group.RemoteFiles }
                    Map.add g.Name newGroup m) Map.empty

        { FileName = ""
          Groups = groups }

    static member FromFile(fileName : string) =
        let lines = File.ReadAllLines(fileName)
        { ReferencesFile.FromLines lines with FileName = fileName }

    member this.AddNuGetReference(groupName, packageName : PackageName, copyLocal: bool, importTargets: bool, frameworkRestrictions, includeVersionInPath, omitContent : bool, createBindingRedirects: bool, referenceCondition) =
        let (PackageName referenceName) = packageName
        let package: PackageInstallSettings =
            { Name = packageName
              Settings = 
                  { CopyLocal = if not copyLocal then Some copyLocal else None
                    ImportTargets = if not importTargets then Some importTargets else None
                    FrameworkRestrictions = frameworkRestrictions
                    IncludeVersionInPath = if includeVersionInPath then Some includeVersionInPath else None
                    ReferenceCondition = if String.IsNullOrWhiteSpace referenceCondition |> not then Some referenceCondition else None
                    CreateBindingRedirects = if createBindingRedirects then Some createBindingRedirects else None
                    OmitContent = if omitContent then Some omitContent else None } }


        match this.Groups |> Map.tryFind groupName with
        | None -> 
                tracefn "Adding %s to %s into new group %O" referenceName this.FileName groupName  

                let newGroup = 
                    { Name = groupName
                      NugetPackages = [ package ]
                      RemoteFiles = [] }
                let newGroups = this.Groups |> Map.add newGroup.Name newGroup

                { this with Groups = newGroups }

        | Some group -> 
            if group.NugetPackages |> Seq.exists (fun p -> p.Name = packageName) then
                this
            else
                tracefn "Adding %s to %s into group %O" referenceName this.FileName groupName

                let newGroup = { group with NugetPackages = group.NugetPackages @ [ package ] }
                let newGroups = this.Groups |> Map.add newGroup.Name newGroup

                { this with Groups = newGroups }

    member this.AddNuGetReference(groupName, packageName : PackageName) = this.AddNuGetReference(groupName, packageName, true, true, [], false, false, false, null)

    member this.RemoveNuGetReference(groupName, packageName : PackageName) =
        let (PackageName referenceName) = packageName
        let group = this.Groups.[groupName]
        if group.NugetPackages |> Seq.exists (fun p ->  p.Name = packageName) |> not then
            this
        else
            tracefn "Removing %s from %s" referenceName this.FileName

            let newGroup = { group with  NugetPackages = group.NugetPackages |> List.filter (fun p -> p.Name <> packageName) }
            let newGroups = this.Groups |> Map.add newGroup.Name newGroup

            { this with Groups = newGroups }

    member this.Save() =
        File.WriteAllText(this.FileName, this.ToString())
        tracefn "References file saved to %s" this.FileName

    override this.ToString() =
        let printGroup g = 
            (g.NugetPackages |> List.map (fun p -> String.Join(" ",[p.Name.ToString(); p.Settings.ToString()] |> List.filter (fun s -> s <> "")))) @
              (g.RemoteFiles |> List.map (fun s -> "File:" + s.Name + if s.Link <> ReferencesFile.DefaultLink then " " + s.Link else ""))

        String.Join
            (Environment.NewLine,
             [|let mainGroup = this.Groups.[Constants.MainDependencyGroup]
               yield! printGroup mainGroup
               for g in this.Groups do 
                if g.Key <> Constants.MainDependencyGroup then
                    if g.Value.NugetPackages <> [] || g.Value.RemoteFiles <> [] then
                        yield "group " + g.Key.ToString()
                        yield! printGroup g.Value|])