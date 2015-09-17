module Paket.LockFile.GenerateAuthModeSpecs

open Paket
open NUnit.Framework
open FsUnit
open TestHelpers
open Paket.Domain

let config1 = """
source "http://nuget.org/api/v2"  username: "user" password: "pass"

nuget "Castle.Windsor-log4net" "~> 3.2"
"""

let graph = [
    "Castle.Windsor-log4net","3.2",[]
]

let expected = """NUGET
  remote: http://nuget.org/api/v2
  specs:
    Castle.Windsor-log4net (3.2)"""

[<Test>]
let ``should generate no auth in lock file``() = 
    let cfg = DependenciesFile.FromCode(config1)
    ResolveWithGraph(cfg,noSha1,VersionsFromGraph graph, PackageDetailsFromGraph graph).[Constants.MainDependencyGroup].ResolvedPackages.GetModelOrFail()
    |> LockFileSerializer.serializePackages cfg.Groups.[Constants.MainDependencyGroup].Options
    |> shouldEqual (normalizeLineEndings expected)