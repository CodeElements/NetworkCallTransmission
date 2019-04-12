#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Target //"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO

let artifactsDir = "./artifacts"

let runDotnet options command args =
    let result = DotNet.exec options command args
    if result.ExitCode <> 0 then
        let errors = System.String.Join(System.Environment.NewLine,result.Errors)
        Trace.traceError <| System.String.Join(System.Environment.NewLine,result.Messages)
        failwithf "dotnet process exited with %d: %s" result.ExitCode errors

let packWithSymbols path = runDotnet (fun opts -> opts) "pack" <| sprintf """"%s" -c Release -o "%s" --include-symbols -p:SymbolPackageFormat=snupkg""" path artifactsDir

Target.create "Build CodeElements.NetworkCall.NetSerializer" (fun _ ->
    "./NetSerializer/CodeElements.NetworkCall.NetSerializer" |> packWithSymbols
)

Target.create "Build CodeElements.NetworkCall" (fun _ ->
    "./CodeElements.NetworkCall" |> packWithSymbols
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