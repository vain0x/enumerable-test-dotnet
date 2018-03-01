// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"
#r @"packages/build/FAKE.Persimmon/lib/net451/FAKE.Persimmon.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
open System
open System.IO

// --------------------------------------------------------------------------------------
// Provide project-specific details below
// --------------------------------------------------------------------------------------

// File system information
let solutionFile  = "EnumerableTest.sln"

// Pattern specifying assemblies to be tested using Persimmon
let testAssemblies = "**/bin/Debug/*.UnitTest.dll"

// --------------------------------------------------------------------------------------
// Build projects
// --------------------------------------------------------------------------------------

let buildDebug () =
    !! solutionFile
    |> MSBuildDebug
        (* outputPath = *) null // Use project settings.
        "Build"
    |> ignore

let buildRelease () =
    !! solutionFile
#if MONO
    |> MSBuildReleaseExt "" [ ("DefineConstants","MONO") ] "Rebuild"
#else
    |> MSBuildRelease "" "Rebuild"
#endif
    |> ignore

Target "Build" (fun _ ->
    buildDebug ()
)

Target "BuildRelease" (fun _ ->
    buildRelease ()
)

// --------------------------------------------------------------------------------------
// Run the unit tests
// --------------------------------------------------------------------------------------

let runTests () =
    !! testAssemblies
    |> Persimmon id

Target "Test" (fun _ ->
    runTests ()
)

// --------------------------------------------------------------------------------------
// Watch build test loop
// --------------------------------------------------------------------------------------

Target "Watch" (fun _ ->
    use watcher = !! "**/*.fs" |> WatchChanges (fun changes ->
        buildDebug ()
        runTests ()
    )

    traceImportant "Waiting for help edits. Press any key to stop."

    System.Console.ReadKey() |> ignore

    watcher.Dispose()
)

// --------------------------------------------------------------------------------------
// dependencies
// --------------------------------------------------------------------------------------

"Build"
    ==> "Test"

RunTargetOrDefault "Test"
