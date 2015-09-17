module paket.dependenciesFile.ParserSpecs

open Paket
open Paket.PackageSources
open NUnit.Framework
open FsUnit
open TestHelpers
open System
open Paket.Domain
open Paket.Requirements

[<Test>]
let ``should read empty config``() = 
    let cfg = DependenciesFile.FromCode("")
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false

    cfg.Groups.[Constants.MainDependencyGroup].Packages.Length |> shouldEqual 0
    cfg.Groups.[Constants.MainDependencyGroup].RemoteFiles.Length |> shouldEqual 0

let configWithSourceOnly = """
source http://nuget.org/api/v2
"""

[<Test>]
let ``should read config which only contains a source``() = 
    let cfg = DependenciesFile.FromCode(configWithSourceOnly)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false

    cfg.Groups.[Constants.MainDependencyGroup].Sources.Length |> shouldEqual 1
    cfg.Groups.[Constants.MainDependencyGroup].Sources.Head  |> shouldEqual (Nuget({ Url = "http://nuget.org/api/v2"; Authentication = None }))

let config1 = """
source "http://nuget.org/api/v2"

nuget "Castle.Windsor-log4net" "~> 3.2"
nuget "Rx-Main" "~> 2.0"
nuget "FAKE" "= 1.1"
nuget "SignalR" "= 3.3.2"
"""

[<Test>]
let ``should read simple config``() = 
    let cfg = DependenciesFile.FromCode(config1)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"].Range |> shouldEqual (VersionRange.Between("2.0", "3.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Castle.Windsor-log4net"].Range |> shouldEqual (VersionRange.Between("3.2", "4.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Exactly "1.1")
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "SignalR"].Range |> shouldEqual (VersionRange.Exactly "3.3.2")

let config2 = """
source "http://nuget.org/api/v2"

// this rocks
nuget "FAKE" "~> 3.0"
nuget "Rx-Main" "~> 2.2"
nuget "MinPackage" "1.1.3"
"""

[<Test>]
let ``should read simple config with additional comment``() = 
    let cfg = DependenciesFile.FromCode(config2)
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"].Range |> shouldEqual (VersionRange.Between("2.2", "3.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Between("3.0", "4.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "MinPackage"].Range |> shouldEqual (VersionRange.Exactly "1.1.3")

let config3 = """
source "https://nuget.org/api/v2" // here we are

nuget "FAKE" "~> 3.0" // born to rule
nuget "Rx-Main" "~> 2.2"
nuget "MinPackage" "1.1.3"
"""

[<Test>]
let ``should read simple config with comments``() = 
    let cfg = DependenciesFile.FromCode(config3)
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"].Range |> shouldEqual (VersionRange.Between("2.2", "3.0"))
    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Rx-Main")).Sources |> List.head |> shouldEqual PackageSources.DefaultNugetSource
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Between("3.0", "4.0"))
    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "FAKE")).Sources |> List.head  |> shouldEqual PackageSources.DefaultNugetSource

let config4 = """
source "https://nuget.org/api/v2" // first source
source "http://nuget.org/api/v3"  // second

nuget "FAKE" "~> 3.0" 
nuget "Rx-Main" "~> 2.2"
nuget "MinPackage" "1.1.3"
"""

[<Test>]
let ``should read config with multiple sources``() = 
    let cfg = DependenciesFile.FromCode(config4)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Rx-Main")).Sources |> shouldEqual [PackageSources.DefaultNugetSource; PackageSource.NugetSource "http://nuget.org/api/v3"]
    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "MinPackage")).Sources |> shouldEqual [PackageSources.DefaultNugetSource; PackageSource.NugetSource "http://nuget.org/api/v3"]
    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "FAKE")).Sources |> shouldEqual [PackageSources.DefaultNugetSource; PackageSource.NugetSource "http://nuget.org/api/v3"]

[<Test>]
let ``should read source file from config``() =
    let config = """github "fsharp/FAKE:master" "src/app/FAKE/Cli.fs"
                    github "fsharp/FAKE:bla123zxc" "src/app/FAKE/FileWithCommit.fs" """
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "fsharp"
            Project = "FAKE"
            Name = "src/app/FAKE/Cli.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Commit = Some "master" }
          { Owner = "fsharp"
            Project = "FAKE"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Name = "src/app/FAKE/FileWithCommit.fs"
            Commit = Some "bla123zxc" } ]

let strictConfig = """
references strict
source "http://nuget.org/api/v2" // first source

nuget "FAKE" "~> 3.0"
"""

[<Test>]
let ``should read strict config``() = 
    let cfg = DependenciesFile.FromCode(strictConfig)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual true
    cfg.Groups.[Constants.MainDependencyGroup].Options.Redirects |> shouldEqual false

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "FAKE")).Sources |> shouldEqual [PackageSource.NugetSource "http://nuget.org/api/v2"]

let redirectsConfig = """
redirects on
source "http://nuget.org/api/v2" // first source

nuget "FAKE" "~> 3.0"
"""

[<Test>]
let ``should read config with redirects``() = 
    let cfg = DependenciesFile.FromCode(redirectsConfig)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false
    cfg.Groups.[Constants.MainDependencyGroup].Options.Redirects |> shouldEqual true

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "FAKE")).Sources |> shouldEqual [PackageSource.NugetSource "http://nuget.org/api/v2"]

let noRedirectsConfig = """
redirects off
source "http://nuget.org/api/v2" // first source

nuget "FAKE" "~> 3.0"
"""

[<Test>]
let ``should read config with no redirects``() = 
    let cfg = DependenciesFile.FromCode(noRedirectsConfig)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false
    cfg.Groups.[Constants.MainDependencyGroup].Options.Redirects |> shouldEqual false

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "FAKE")).Sources |> shouldEqual [PackageSource.NugetSource "http://nuget.org/api/v2"]

let noneContentConfig = """
content none
source "http://nuget.org/api/v2" // first source

nuget "Microsoft.SqlServer.Types"
"""

[<Test>]
let ``should read content none config``() = 
    let cfg = DependenciesFile.FromCode(noneContentConfig)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.OmitContent |> shouldEqual (Some true)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.CopyLocal |> shouldEqual None
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.ImportTargets |> shouldEqual None

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Microsoft.SqlServer.Types")).Sources |> shouldEqual [PackageSource.NugetSource "http://nuget.org/api/v2"]

let specificFrameworkConfig = """
framework net40 net35
source "http://nuget.org/api/v2" // first source

nuget "Microsoft.SqlServer.Types"
"""

[<Test>]
let ``should read config with specific framework``() = 
    let cfg = DependenciesFile.FromCode(specificFrameworkConfig)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.OmitContent |> shouldEqual None
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.CopyLocal |> shouldEqual None
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.ImportTargets |> shouldEqual None

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Microsoft.SqlServer.Types")).Sources |> shouldEqual [PackageSource.NugetSource "http://nuget.org/api/v2"]

let noTargetsImportConfig = """
import_targets false
copy_local false
source "http://nuget.org/api/v2" // first source

nuget "Microsoft.SqlServer.Types"
"""

[<Test>]
let ``should read no targets import config``() = 
    let cfg = DependenciesFile.FromCode(noTargetsImportConfig)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.ImportTargets |> shouldEqual (Some false)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.CopyLocal |> shouldEqual (Some false)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.OmitContent |> shouldEqual None

    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Microsoft.SqlServer.Types")).Sources |> shouldEqual [PackageSource.NugetSource "http://nuget.org/api/v2"]

let configWithoutQuotes = """
source http://nuget.org/api/v2

nuget Castle.Windsor-log4net ~> 3.2
nuget Rx-Main ~> 2.0
nuget FAKE = 1.1
nuget SignalR = 3.3.2
"""

[<Test>]
let ``should read config without quotes``() = 
    let cfg = DependenciesFile.FromCode(configWithoutQuotes)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).Count |> shouldEqual 4

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"].Range |> shouldEqual (VersionRange.Between("2.0", "3.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Castle.Windsor-log4net"].Range |> shouldEqual (VersionRange.Between("3.2", "4.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Exactly "1.1")
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "SignalR"].Range |> shouldEqual (VersionRange.Exactly "3.3.2")

let configLocalQuotedSource = """source "D:\code\temp with space"

nuget Castle.Windsor-log4net ~> 3.2
nuget Rx-Main ~> 2.0
nuget FAKE = 1.1
nuget SignalR = 3.3.2
"""

[<Test>]
let ``should read config local quoted source``() = 
    let cfg = DependenciesFile.FromCode(configLocalQuotedSource)
    cfg.Groups.[Constants.MainDependencyGroup].Sources.Head |> shouldEqual (LocalNuget("D:\code\\temp with space"))
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).Count |> shouldEqual 4

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"].Range |> shouldEqual (VersionRange.Between("2.0", "3.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Castle.Windsor-log4net"].Range |> shouldEqual (VersionRange.Between("3.2", "4.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Exactly "1.1")
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "SignalR"].Range |> shouldEqual (VersionRange.Exactly "3.3.2")

let configWithoutQuotesButLotsOfWhiteSpace = """
source      http://nuget.org/api/v2

nuget   Castle.Windsor-log4net   ~>     3.2
nuget Rx-Main ~> 2.0
nuget FAKE =    1.1
nuget SignalR    = 3.3.2
"""

[<Test>]
let ``should read config without quotes but lots of whitespace``() = 
    let cfg = DependenciesFile.FromCode(configWithoutQuotes)
    cfg.Groups.[Constants.MainDependencyGroup].Options.Strict |> shouldEqual false
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).Count |> shouldEqual 4

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"].Range |> shouldEqual (VersionRange.Between("2.0", "3.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Castle.Windsor-log4net"].Range |> shouldEqual (VersionRange.Between("3.2", "4.0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Exactly "1.1")
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "SignalR"].Range |> shouldEqual (VersionRange.Exactly "3.3.2")


[<Test>]
let ``should read github source file from config without quotes``() =
    let config = """github fsharp/FAKE:master   src/app/FAKE/Cli.fs
                    github    fsharp/FAKE:bla123zxc src/app/FAKE/FileWithCommit.fs """
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "fsharp"
            Project = "FAKE"
            Name = "src/app/FAKE/Cli.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Commit = Some "master" }
          { Owner = "fsharp"
            Project = "FAKE"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Name = "src/app/FAKE/FileWithCommit.fs"
            Commit = Some "bla123zxc" } ]

[<Test>]
let ``should read github source file from config with quotes``() =
    let config = """github fsharp/FAKE:master  "src/app/FAKE/Cli.fs"
                    github fsharp/FAKE:bla123zxc "src/app/FAKE/FileWith Space.fs" """
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "fsharp"
            Project = "FAKE"
            Name = "src/app/FAKE/Cli.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Commit = Some "master" }
          { Owner = "fsharp"
            Project = "FAKE"
            Name = "src/app/FAKE/FileWith Space.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Commit = Some "bla123zxc" } ]

[<Test>]
let ``should read github source files withou sha1``() =
    let config = """github fsharp/FAKE  src/app/FAKE/Cli.fs
                    github    fsharp/FAKE:bla123zxc src/app/FAKE/FileWithCommit.fs """
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "fsharp"
            Project = "FAKE"
            Name = "src/app/FAKE/Cli.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Commit = None }
          { Owner = "fsharp"
            Project = "FAKE"
            Name = "src/app/FAKE/FileWithCommit.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink
            Commit = Some "bla123zxc" } ]

[<Test>]
let ``should read http source file from config without quotes with file specs``() =
    let config = """http http://www.fssnip.net/raw/1M test1.fs
                    http http://www.fssnip.net/raw/1M/1 src/test2.fs """
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "www.fssnip.net"
            Project = ""
            Name = "test1.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://www.fssnip.net"
            Commit = Some "/raw/1M" }
          { Owner = "www.fssnip.net"
            Project = ""
            Name = "src/test2.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://www.fssnip.net"
            Commit = Some "/raw/1M/1" } ]


[<Test>]
let ``should read http source file from config without quotes with file specs and project and query string after filename``() =
    let config = """http http://server-stash:7658/projects/proj1/repos/repo1/browse/Source/SolutionFolder/Rabbit.fs?at=a5457f3d811830059cd39d583f264eab340c273d&raw Rabbit.fs project"""
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "server-stash_7658"
            Project = "project"
            Name = "Rabbit.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://server-stash:7658"
            Commit = Some "/projects/proj1/repos/repo1/browse/Source/SolutionFolder/Rabbit.fs?at=a5457f3d811830059cd39d583f264eab340c273d&raw" }
        ]

[<Test>]
let ``should read http source file from config without quotes with file specs and project``() =
    let config = """http http://www.fssnip.net/raw/1M test1.fs project
                    http http://www.fssnip.net/raw/1M/1 src/test2.fs project"""
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "www.fssnip.net"
            Project = "project"
            Name = "test1.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://www.fssnip.net"
            Commit = Some "/raw/1M" }
          { Owner = "www.fssnip.net"
            Project = "project"
            Name = "src/test2.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://www.fssnip.net"
            Commit = Some "/raw/1M/1" } ]


[<Test>]
let ``should read gist source file from config without quotes with file specs``() =
    let config = """gist Thorium/1972308 gistfile1.fs
                    gist Thorium/6088882 """ //Gist supports multiple files also
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "Thorium"
            Project = "1972308"
            Name = "gistfile1.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.GistLink
            Commit = None }
          { Owner = "Thorium"
            Project = "6088882"
            Name = "FULLPROJECT"
            Origin = ModuleResolver.SingleSourceFileOrigin.GistLink
            Commit = None } ]

[<Test>]
let ``should read gist source file``() =
    let config = """source https://www.nuget.org/api/v2

nuget JetBrainsAnnotations.Fody

gist misterx/5d9c6983004c1c9ec91f""" 
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "misterx"
            Project = "5d9c6983004c1c9ec91f"
            Name = "FULLPROJECT"
            Origin = ModuleResolver.SingleSourceFileOrigin.GistLink
            Commit = None } ]

[<Test>]
let ``should read http source file from config without quotes, parsing rules``() =
    // The empty "/" should be ommited. After that, parsing amount of "/"-marks:
    let config = """
        http http://example/
        http http://example/item
        http http://example/item/
        http http://example/item/3
        http http://example/item/3/1"""
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "example"
            Project = ""
            Name = "example.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://example"
            Commit = Some "/" }
          { Owner = "example"
            Project = ""
            Name = "item.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://example"
            Commit = Some "/item" }
          { Owner = "example"
            Project = ""
            Name = "item.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://example"
            Commit = Some "/item" }
          { Owner = "example"
            Project = ""
            Name = "3.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://example"
            Commit = Some "/item/3" }
          { Owner = "example"
            Project = ""
            Name = "1.fs"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://example"
            Commit = Some "/item/3/1" } ]

[<Test>]
let ``should read http binary references from config``() =
    let config = """
        http http://www.frijters.net/ikvmbin-8.0.5449.0.zip
        http http://www.frijters.net/ikvmbin-8.0.5449.0.zip ikvmbin.zip"""
    let dependencies = DependenciesFile.FromCode(config)
    dependencies.Groups.[Constants.MainDependencyGroup].RemoteFiles
    |> shouldEqual
        [ { Owner = "www.frijters.net"
            Project = ""
            Name = "ikvmbin-8.0.5449.0.zip"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://www.frijters.net"
            Commit = Some "/ikvmbin-8.0.5449.0.zip" }
          { Owner = "www.frijters.net"
            Project = ""
            Name = "ikvmbin.zip"
            Origin = ModuleResolver.SingleSourceFileOrigin.HttpLink "http://www.frijters.net"
            Commit = Some "/ikvmbin-8.0.5449.0.zip" } ]


let configWithoutVersions = """
source "http://nuget.org/api/v2"

nuget Castle.Windsor-log4net
nuget Rx-Main
nuget "FAKE"
"""

[<Test>]
let ``should read config without versions``() = 
    let cfg = DependenciesFile.FromCode(configWithoutVersions)

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Rx-Main"] .Range|> shouldEqual (VersionRange.AtLeast "0")
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Castle.Windsor-log4net"].Range |> shouldEqual (VersionRange.AtLeast "0")
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FAKE"].Range |> shouldEqual (VersionRange.AtLeast "0")


let configWithPassword = """
source http://nuget.org/api/v2 username: "tat� tata" password: "you got hacked!"
nuget Rx-Main
"""

[<Test>]
let ``should read config with encapsulated password source``() = 
    let cfg = DependenciesFile.FromCode( configWithPassword)
    
    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Rx-Main")).Sources 
    |> shouldEqual [ 
        PackageSource.Nuget { 
            Url = "http://nuget.org/api/v2"
            Authentication = Some (PlainTextAuthentication("tat� tata", "you got hacked!")) } ]

let configWithPasswordInSingleQuotes = """
source http://nuget.org/api/v2 username: 'tat� tata' password: 'you got hacked!'
nuget Rx-Main
"""

[<Test>]
let ``should read config with single-quoted password source``() = 
    try
        DependenciesFile.FromCode( configWithPasswordInSingleQuotes) |> ignore
        failwith "Expected error"
    with
    | exn when exn.Message <> "Expected error" -> ()

let configWithPasswordInEnvVariable = """
source http://nuget.org/api/v2 username: "%FEED_USERNAME%" password: "%FEED_PASSWORD%"
nuget Rx-Main
"""

[<Test>]
let ``should read config with password in env variable``() = 
    Environment.SetEnvironmentVariable("FEED_USERNAME", "user XYZ", EnvironmentVariableTarget.Process)
    Environment.SetEnvironmentVariable("FEED_PASSWORD", "pw Love", EnvironmentVariableTarget.Process)
    let cfg = DependenciesFile.FromCode( configWithPasswordInEnvVariable)
    
    (cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun p -> p.Name = PackageName "Rx-Main")).Sources 
    |> shouldEqual [ 
        PackageSource.Nuget { 
            Url = "http://nuget.org/api/v2"
            Authentication = Some (EnvVarAuthentication
                                    ({Variable = "%FEED_USERNAME%"; Value = "user XYZ"},
                                     {Variable = "%FEED_PASSWORD%"; Value = "pw Love"}))} ]

let configWithExplicitVersions = """
source "http://nuget.org/api/v2"

nuget FSharp.Compiler.Service == 0.0.62 
nuget FsReveal == 0.0.5-beta
"""

[<Test>]
let ``should read config explicit versions``() = 
    let cfg = DependenciesFile.FromCode(configWithExplicitVersions)

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FSharp.Compiler.Service"].Range |> shouldEqual (VersionRange.OverrideAll (SemVer.Parse "0.0.62"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FsReveal"].Range |> shouldEqual (VersionRange.OverrideAll (SemVer.Parse "0.0.5-beta"))

let configWithLocalSource = """
source ./nugets

nuget Nancy.Owin 0.22.2
"""

[<Test>]
let ``should read config with local source``() = 
    let cfg = DependenciesFile.FromCode(configWithLocalSource)

    let p = cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun x-> x.Name = PackageName "Nancy.Owin")
    p.VersionRequirement.Range |> shouldEqual (VersionRange.Specific (SemVer.Parse "0.22.2"))
    p.Settings.FrameworkRestrictions |> shouldEqual []


[<Test>]
let ``should read config with package name containing nuget``() = 
    let config = """
    nuget nuget.Core 0.1
    """
    let cfg = DependenciesFile.FromCode(config)

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "nuget.Core"].Range |> shouldEqual (VersionRange.Specific (SemVer.Parse "0.1"))

[<Test>]
let ``should read config with single framework restriction``() = 
    let config = """
    nuget Foobar 1.2.3 framework: >= net40
    """
    let cfg = DependenciesFile.FromCode(config)

    let p = cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun x-> x.Name = PackageName "Foobar")
    p.VersionRequirement.Range |> shouldEqual (VersionRange.Specific (SemVer.Parse "1.2.3"))
    p.Settings.FrameworkRestrictions |> shouldEqual [FrameworkRestriction.AtLeast(DotNetFramework(FrameworkVersion.V4_Client))]
    p.Settings.ImportTargets |> shouldEqual None


[<Test>]
let ``should read config with framework restriction``() = 
    let config = """
    nuget Foobar 1.2.3 alpha beta framework: net35, >= net40
    """
    let cfg = DependenciesFile.FromCode(config)

    let p = cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun x-> x.Name = PackageName "Foobar")
    p.VersionRequirement.Range |> shouldEqual (VersionRange.Specific (SemVer.Parse "1.2.3"))
    p.Settings.FrameworkRestrictions |> shouldEqual [FrameworkRestriction.Exactly(DotNetFramework(FrameworkVersion.V3_5)); FrameworkRestriction.AtLeast(DotNetFramework(FrameworkVersion.V4_Client))]
    p.Settings.ImportTargets |> shouldEqual None
    p.Settings.CopyLocal |> shouldEqual None

[<Test>]
let ``should read config with no targets import``() = 
    let config = """
    nuget Foobar 1.2.3 alpha beta import_targets: false, copy_local: false
    """
    let cfg = DependenciesFile.FromCode(config)

    let p = cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun x-> x.Name = PackageName "Foobar")
    p.VersionRequirement.Range |> shouldEqual (VersionRange.Specific (SemVer.Parse "1.2.3"))
    p.Settings.FrameworkRestrictions |> shouldEqual []
    p.Settings.ImportTargets |> shouldEqual (Some false)
    p.Settings.CopyLocal |> shouldEqual (Some false)
    p.Settings.OmitContent |> shouldEqual None

[<Test>]
let ``should read config with content none``() = 
    let config = """
    nuget Foobar 1.2.3 alpha beta content: none, copy_local: false
    """
    let cfg = DependenciesFile.FromCode(config)

    let p = cfg.Groups.[Constants.MainDependencyGroup].Packages |> List.find (fun x-> x.Name = PackageName "Foobar")
    p.VersionRequirement.Range |> shouldEqual (VersionRange.Specific (SemVer.Parse "1.2.3"))
    p.Settings.FrameworkRestrictions |> shouldEqual []
    p.Settings.ImportTargets |> shouldEqual None
    p.Settings.CopyLocal |> shouldEqual (Some false)
    p.Settings.OmitContent |> shouldEqual (Some true)

[<Test>]
let ``should read config with  !~> 3.3``() = 
    let config = """source https://nuget.org/api/v2

nuget AutoMapper ~> 3.2
nuget Castle.Windsor !~> 3.3
nuget DataAnnotationsExtensions 1.1.0.0
nuget EntityFramework 5.0.0
nuget FakeItEasy ~> 1.23
nuget FluentAssertions ~> 3.1
nuget Machine.Specifications ~> 0.9
nuget Machine.Specifications.Runner.Console ~> 0.9
nuget NDbfReader 1.1.1.0
nuget Newtonsoft.Json ~> 6.0
nuget Plossum.CommandLine != 0.3.0.14
nuget PostSharp 3.1.52
nuget SharpZipLib 0.86.0
nuget Topshelf ~> 3.1
nuget Caliburn.Micro !~> 2.0.2
    """
    let cfg = DependenciesFile.FromCode(config)
    ()


let configWithInvalidPrereleaseString = """
    nuget Plossum.CommandLine !0.3.0.14   
"""

[<Test>]
let ``should report error on invalid prerelease string``() = 
    try
        DependenciesFile.FromCode(configWithInvalidPrereleaseString) |> ignore
        failwith "error"
    with
    | exn -> Assert.IsTrue(exn.Message.Contains("Invalid prerelease version !0.3.0.14")) |> ignore

let html = """
<!DOCTYPE html><html><head></head></html>"
"""

[<Test>]
let ``should not read hhtml``() = 
    try
        DependenciesFile.FromCode(html) |> ignore
        failwith "error"
    with
    | exn -> Assert.IsTrue(exn.Message.Contains"Unrecognized token")

let configWithAdditionalGroup = """
source "http://nuget.org/api/v2"

nuget FSharp.Compiler.Service
nuget FsReveal

group Build

nuget FAKE
nuget NUnit
"""

[<Test>]
let ``should read config with additional group``() = 
    let cfg = DependenciesFile.FromCode(configWithAdditionalGroup)

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FSharp.Compiler.Service"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FsReveal"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))

    cfg.GetDependenciesInGroup(GroupName "Build").[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(GroupName "Build").[PackageName "NUnit"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))

let configWithNestedGroup = """
source "http://nuget.org/api/v2"

nuget FSharp.Compiler.Service
nuget FsReveal

group Build

    nuget FAKE
    nuget NUnit
"""

[<Test>]
let ``should read config with nested group``() = 
    let cfg = DependenciesFile.FromCode(configWithNestedGroup)

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FSharp.Compiler.Service"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FsReveal"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))

    cfg.GetDependenciesInGroup(GroupName "Build").[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(GroupName "Build").[PackageName "NUnit"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))

let configWithExplicitMainGroup = """
nuget Paket.Core

group Main
source "http://nuget.org/api/v2"

nuget FSharp.Compiler.Service
nuget FsReveal

group Build

    nuget FAKE
    nuget NUnit
"""

[<Test>]
let ``should read config with explizit main group``() = 
    let cfg = DependenciesFile.FromCode(configWithExplicitMainGroup)

    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FSharp.Compiler.Service"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "FsReveal"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(Constants.MainDependencyGroup).[PackageName "Paket.Core"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))

    cfg.GetDependenciesInGroup(GroupName "Build").[PackageName "FAKE"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))
    cfg.GetDependenciesInGroup(GroupName "Build").[PackageName "NUnit"].Range |> shouldEqual (VersionRange.Minimum (SemVer.Parse "0"))

let configWithReferenceCondition = """
source http://nuget.org/api/v2
condition: main-group

nuget Paket.Core
nuget FSharp.Compiler.Service
nuget FsReveal

group Build

    nuget FAKE redirects: on
    nuget NUnit condition: legacy
"""

[<Test>]
let ``should read config with reference condition``() = 
    let cfg = DependenciesFile.FromCode(configWithReferenceCondition)

    cfg.Groups.[Constants.MainDependencyGroup].Options.Settings.ReferenceCondition |> shouldEqual (Some "MAIN-GROUP")

    cfg.Groups.[GroupName "Build"].Packages.Head.Settings.ReferenceCondition |> shouldEqual None
    cfg.Groups.[GroupName "Build"].Packages.Head.Settings.CreateBindingRedirects |> shouldEqual (Some true)
    cfg.Groups.[GroupName "Build"].Packages.Tail.Head.Settings.ReferenceCondition |> shouldEqual (Some "LEGACY")
    cfg.Groups.[GroupName "Build"].Packages.Tail.Head.Settings.CreateBindingRedirects |> shouldEqual None