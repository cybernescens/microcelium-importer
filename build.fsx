#r "paket:
nuget AWSSDK.S3
nuget JetBrains.dotCover.CommandLineTools
nuget Fake.BuildServer.TeamCity
nuget Fake.Core.Xml
nuget Fake.Core.Target
nuget Fake.Core.Trace
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.NuGet
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
nuget Fake.Runtime
//"
#load ".microcelium/lib/Microcelium.fsx"
#load "./.fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.BuildServer
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Microcelium.Fake

BuildServer.install [ TeamCity.Installer ]
CoreTracing.ensureConsoleListener ()

(* read EnvVar
  let myEnvVar = Util.environVarOrDefault ["myEnvVarKey1"; "myEnvVarKey2"] "default value"
*)

(* setting the version
  let version = Version.fromVersionIni ()   //looks for a .\Version.ini file
  let version = Version.fromFile "filepath" //looks for a file @ "filepath"
*)

let version = Version.from "1.0" //parses from param
let versionparts = Version.parts version
let versionstr = Version.toString version

let srcDir = Path.getFullName "./src"
let binDir = Path.getFullName "./bin"

let project = "Microcelium.Importer"
let tests = seq { yield (srcDir, Default) }

Target.create "Clean" <| Targets.clean srcDir binDir
Target.create "Version" <| Targets.version version
Target.create "Build" <| Targets.build srcDir versionparts None
Target.create "Test" <| Targets.test tests project binDir
Target.create "Publish" <| Targets.publish binDir

Target.create "Package" (fun _ ->
  Build.packageNuget srcDir "Microcelium.Importer" versionparts binDir
  Build.packageNuget srcDir "Microcelium.Importer.Cmd" versionparts binDir
)

Target.create "ToLocalNuget"  <| Targets.publishLocal binDir versionstr

(* `NuGetCachePath` EnvVar should be set to your Nuget Packages Install dir already, but
    `TargetVersion` should be set prior to running build.bat :
    set TargetVersion=1.14 *)
Target.create "ToLocalPackageRepo" <| Targets.packageLocal srcDir

"Clean"
  ==> "Version"
  ==> "Build"
  ==> "Test"
  ==> "Package"
  =?> ("Publish", Environment.runPublish)

Target.runOrDefault <| if Environment.runPublish then "Publish" else "Test"
