﻿module Paket.InstallModel.Xml.FantomasSpecs

open Paket
open NUnit.Framework
open FsUnit
open Paket.TestHelpers
open Paket.Domain
open Paket.Requirements

let expected = """
<ItemGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Reference Include="FantomasLib">
    <HintPath>..\..\..\Fantomas\lib\FantomasLib.dll</HintPath>
    <Private>True</Private>
    <Paket>True</Paket>
  </Reference>
</ItemGroup>"""

[<Test>]
let ``should generate Xml for Fantomas 1.5``() = 
    let model =
        InstallModel.CreateFromLibs(PackageName "Fantomas", SemVer.Parse "1.5.0", [],
            [ @"..\Fantomas\lib\FantomasLib.dll" 
              @"..\Fantomas\lib\FSharp.Core.dll" 
              @"..\Fantomas\lib\Fantomas.exe" ],
              [],
              [],
              Nuspec.Explicit ["FantomasLib.dll"])
    
    let propertyNodes,chooseNode,additionalNode, _ = ProjectFile.Load("./ProjectFile/TestData/Empty.fsprojtest").Value.GenerateXml(model,true,true,None)
    chooseNode.OuterXml
    |> normalizeXml
    |> shouldEqual (normalizeXml expected)
    
    propertyNodes |> Seq.length |> shouldEqual 0


let emptyDoc = """<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
</Project>"""

let fullDoc = """<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <ItemGroup>
    <Reference Include="FantomasLib">
      <HintPath>..\..\..\Fantomas\lib\FantomasLib.dll</HintPath>
      <Private>True</Private>
      <Paket>True</Paket>
    </Reference>
  </ItemGroup>
</Project>"""

[<Test>]
let ``should generate full Xml for Fantomas 1.5``() = 
    let model =
        InstallModel.CreateFromLibs(PackageName "Fantomas", SemVer.Parse "1.5.0", [],
            [ @"..\Fantomas\lib\FantomasLib.dll" 
              @"..\Fantomas\lib\FSharp.Core.dll" 
              @"..\Fantomas\lib\Fantomas.exe" ],
              [],
              [],
              Nuspec.Explicit ["FantomasLib.dll"])
    
    let project = ProjectFile.Load("./ProjectFile/TestData/Empty.fsprojtest").Value
    let completeModel = [(Constants.MainDependencyGroup, (PackageName "Fantomas")),(model,model)] |> Map.ofSeq
    let used = [(Constants.MainDependencyGroup, (PackageName "fantoMas")), (InstallSettings.Default,InstallSettings.Default)] |> Map.ofSeq
    project.UpdateReferences(completeModel,used,false)
    
    project.Document.OuterXml
    |> normalizeXml
    |> shouldEqual (normalizeXml fullDoc)


[<Test>]
let ``should not generate full Xml for Fantomas 1.5 if not referenced``() = 
    let model =
        InstallModel.CreateFromLibs(PackageName "Fantomas", SemVer.Parse "1.5.0", [],
            [ @"..\Fantomas\lib\FantomasLib.dll" 
              @"..\Fantomas\lib\FSharp.Core.dll" 
              @"..\Fantomas\lib\Fantomas.exe" ],
              [],
              [],
              Nuspec.Explicit ["FantomasLib.dll"])
    
    let project = ProjectFile.Load("./ProjectFile/TestData/Empty.fsprojtest").Value
    let completeModel = [(Constants.MainDependencyGroup, (PackageName "Fantomas")),(model,model)] |> Map.ofSeq
    let used = [(Constants.MainDependencyGroup, (PackageName "blub")), (InstallSettings.Default,InstallSettings.Default) ] |> Map.ofSeq
    project.UpdateReferences(completeModel,used,false)
    
    project.Document.OuterXml
    |> normalizeXml
    |> shouldEqual (normalizeXml emptyDoc)

let fullDocWithRefernceCondition = """<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <Choose>
    <When Condition="'$(LEGACY)' == 'True'">
      <ItemGroup>
        <Reference Include="FantomasLib">
          <HintPath>..\..\..\Fantomas\lib\FantomasLib.dll</HintPath>
          <Private>True</Private>
          <Paket>True</Paket>
        </Reference>
      </ItemGroup>
    </When>
  </Choose>
</Project>"""

[<Test>]
let ``should generate full Xml with reference condition for Fantomas 1.5``() = 
    let model =
        InstallModel.CreateFromLibs(PackageName "Fantomas", SemVer.Parse "1.5.0", [],
            [ @"..\Fantomas\lib\FantomasLib.dll" 
              @"..\Fantomas\lib\FSharp.Core.dll" 
              @"..\Fantomas\lib\Fantomas.exe" ],
              [],
              [],
              Nuspec.Explicit ["FantomasLib.dll"])
    
    let project = ProjectFile.Load("./ProjectFile/TestData/Empty.fsprojtest").Value
    let completeModel = [(Constants.MainDependencyGroup, (PackageName "Fantomas")),(model,model)] |> Map.ofSeq
    let settings =
        { InstallSettings.Default 
            with ReferenceCondition = Some "LEGACY" }
    let used = [(Constants.MainDependencyGroup, (PackageName "fantoMas")), (InstallSettings.Default,settings)] |> Map.ofSeq
    project.UpdateReferences(completeModel,used,false)
    
    project.Document.OuterXml
    |> normalizeXml
    |> shouldEqual (normalizeXml fullDocWithRefernceCondition)

let fullDocWithRefernceConditionAndFrameworkRestriction = """<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <Choose>
    <When Condition="'$(LEGACY)' == 'True' And (($(TargetFrameworkIdentifier) == 'MonoAndroid') Or ($(TargetFrameworkIdentifier) == 'Xamarin.iOS'))">
      <ItemGroup>
        <Reference Include="FantomasLib">
          <HintPath>..\..\..\Fantomas\lib\portable-net45+win8\FantomasLib.dll</HintPath>
          <Private>True</Private>
          <Paket>True</Paket>
        </Reference>
      </ItemGroup>
    </When>
  </Choose>
</Project>"""

[<Test>]
let ``should generate full Xml with reference condition and framework restrictions without msbuild warning``() =
    // msbuild triggers a warning MSB4130 when we leave out the quotes around $(LEGACY) and add the condition at the end
    // It seems like the warning is triggered when there is an "Or" without parentheses somewhere
    let model =
        InstallModel.CreateFromLibs(PackageName "Fantomas", SemVer.Parse "1.5.0",
            [ FrameworkRestriction.Exactly (FrameworkIdentifier.XamariniOS)
              FrameworkRestriction.Exactly (FrameworkIdentifier.MonoAndroid)],
            [ @"..\Fantomas\lib\portable-net45+win8\FantomasLib.dll" ],
              [],
              [],
              Nuspec.All)

    let project = ProjectFile.Load("./ProjectFile/TestData/Empty.fsprojtest").Value
    let completeModel = [(Constants.MainDependencyGroup, (PackageName "Fantomas")),(model,model)] |> Map.ofSeq
    let settings =
        { InstallSettings.Default
            with ReferenceCondition = Some "LEGACY" }
    let used = [(Constants.MainDependencyGroup, (PackageName "fantoMas")), (InstallSettings.Default,settings)] |> Map.ofSeq
    project.UpdateReferences(completeModel,used,false)

    project.Document.OuterXml
    |> normalizeXml
    |> shouldEqual (normalizeXml fullDocWithRefernceConditionAndFrameworkRestriction)
