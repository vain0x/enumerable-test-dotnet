namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AppArgument =
  let files =
    Environment.commandLineArguments ()
    |> Seq.map FileInfo
    |> Seq.toArray
