open System
open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators


module Tool =

    let run cmd args =
        CreateProcess.fromRawCommandLine cmd args
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    let git = run "git"

let withCWD path fn =
    let prevDir = Directory.GetCurrentDirectory ()
    Directory.SetCurrentDirectory path

    fn()

    Directory.SetCurrentDirectory prevDir

let buildDocs () =

    Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__ </> ".." </> "work")

    let projects =
        [ {| Dir = "fsharp-core-docs"
             Repo = "git@github.com:fsharp/fsharp-core-docs.git"
             Build = fun () ->
                Tool.run "dotnet" "restore FSharp.Core"
                let fsdocs = "."</>"FSharp.Formatting"</>"src"</>"fsdocs-tool"</>"bin"</>"Release"</>"net5.0"</>"fsdocs.dll"
                Tool.run "dotnet" $"%s{fsdocs} build --sourcefolder fsharp" |}

          {| Dir = "fsharp-core-docs" </> "FSharp.Formatting"
             Repo = "git@github.com:fsprojects/FSharp.Formatting.git"
             Build = fun () ->
                Tool.run
                    (if Environment.isWindows then ".\\build.cmd" else "./build.sh")
                    "-t Build" |}

          {| Dir = "fsharp-core-docs" </> "fsharp"
             Repo = "git@github.com:dotnet/fsharp.git"
             Build = fun () ->
                if Environment.isWindows
                then ".\\Build.cmd", "-noVisualStudio"
                else "./build.sh", ""
                ||> Tool.run |}
        ]

    printfn ""
    printfn ""
    printfn "====> fetching repos"
    printfn ""
    printfn ""

    projects
    |> List.iter (fun proj ->
        if not <| Directory.Exists proj.Dir then
            Tool.git $"clone --depth 1 %s{proj.Repo} %s{proj.Dir}"
        else
            fun _ -> Tool.git "pull"
            |> withCWD proj.Dir)

    projects
    |> List.rev
    |> List.iter (fun proj ->
        printfn ""
        printfn ""
        printfn $"====> building %s{proj.Dir}"
        printfn ""
        printfn ""

        proj.Build |> withCWD proj.Dir)

buildDocs()