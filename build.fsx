#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Target //"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO

let artifactsDir = "./artifacts"

let buildConfig = (fun (opts: DotNet.PackOptions) -> {opts with Configuration = DotNet.BuildConfiguration.Release
                                                                OutputPath = Some artifactsDir
                                                     })

Target.create "Build CodeElements.NetworkCall.NetSerializer" (fun _ ->
    "./NetSerializer/CodeElements.NetworkCall.NetSerializer" |> DotNet.pack buildConfig
)

Target.create "Build CodeElements.NetworkCall" (fun _ ->
    "./CodeElements.NetworkCall" |> DotNet.pack buildConfig
)

Target.create "Cleanup" (fun _ ->
    Shell.cleanDir artifactsDir
)

Target.create "All" ignore

open Fake.Core.TargetOperators

"Cleanup"
    ==> "Build CodeElements.NetworkCall.NetSerializer"
    ==> "Build CodeElements.NetworkCall"
    ==> "All"

Target.runOrDefault "All"