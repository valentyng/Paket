module Paket.Resolver.CasingSpecs

open Paket
open NUnit.Framework
open FsUnit
open TestHelpers
open Paket.Domain

let graph = [
    "Nancy.Bootstrappers.Windsor","0.23",["Castle.Windsor",VersionRequirement(VersionRange.AtLeast "3.2.1",PreReleaseStatus.No)]
    "Castle.Windsor","3.2.1",[]
    "Castle.Windsor","3.3.0",[]
]

let config1 = """
source "http://nuget.org/api/v2"

nuget "Nancy.bootstrappers.windsor" "!~> 0.23"
"""

[<Test>]
let ``should resolve wrong casing in config file``() = 
    let cfg = DependenciesFile.FromCode(config1)
    let resolved = ResolveWithGraph(cfg,noSha1,VersionsFromGraph graph, PackageDetailsFromGraph graph).[Constants.MainDependencyGroup].ResolvedPackages.GetModelOrFail()
    getVersion resolved.[PackageName "Castle.Windsor"] |> shouldEqual "3.2.1"
    getVersion resolved.[PackageName "Nancy.Bootstrappers.Windsor"] |> shouldEqual "0.23"

let graph2 = [
    "Nancy.Bootstrappers.Windsor","0.23",["castle.windsor",VersionRequirement(VersionRange.AtLeast "3.2.1",PreReleaseStatus.No)]
    "Castle.Windsor","3.2.1",[]
    "Castle.Windsor","3.3.0",[]
]

[<Test>]
let ``should resolve wrong casing in package dependency``() = 
    let cfg = DependenciesFile.FromCode(config1)
    let resolved = ResolveWithGraph(cfg,noSha1,VersionsFromGraph graph2, PackageDetailsFromGraph graph2).[Constants.MainDependencyGroup].ResolvedPackages.GetModelOrFail()
    getVersion resolved.[PackageName "Castle.Windsor"] |> shouldEqual "3.2.1"
    getVersion resolved.[PackageName "Nancy.Bootstrappers.Windsor"] |> shouldEqual "0.23"

let graph3 = [
    "Nancy.Bootstrappers.Windsor","0.21",["Castle.Windsor",VersionRequirement(VersionRange.AtLeast "3.2.1",PreReleaseStatus.No)]
    "Nancy.Bootstrappers.Windsor","0.23",["Castle.Windsor",VersionRequirement(VersionRange.AtLeast "3.2.1",PreReleaseStatus.No)]
    "Castle.Windsor","3.2.1",[]
    "castle.windsor","3.3.0",[]
]

[<Test>]
let ``should resolve wrong casing in retrieved package``() = 
    let cfg = DependenciesFile.FromCode(config1)
    let resolved = ResolveWithGraph(cfg,noSha1,VersionsFromGraph graph3, PackageDetailsFromGraph graph3).[Constants.MainDependencyGroup].ResolvedPackages.GetModelOrFail()
    getVersion resolved.[PackageName "Castle.Windsor"] |> shouldEqual "3.2.1"
    getVersion resolved.[PackageName "Nancy.Bootstrappers.Windsor"] |> shouldEqual "0.23"

let config2 = """
source "http://nuget.org/api/v2"

nuget "Nancy.Bootstrappers.Windsor" "!~> 0.21"
nuget "Nancy.bootstrappers.windsor" "!~> 0.23"
"""

[<Test>]
let ``should resolve conflicting casing in package``() = 
    let cfg = DependenciesFile.FromCode(config1)
    let resolved = ResolveWithGraph(cfg,noSha1,VersionsFromGraph graph3, PackageDetailsFromGraph graph3).[Constants.MainDependencyGroup].ResolvedPackages.GetModelOrFail()
    getVersion resolved.[PackageName "Castle.Windsor"] |> shouldEqual "3.2.1"
    getVersion resolved.[PackageName "Nancy.Bootstrappers.Windsor"] |> shouldEqual "0.23"
