namespace EnumerableTest.Runner.Wpf

open System
open System.IO

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AppArgument =
  let files =
    Environment.GetCommandLineArgs()
    |> Seq.tail
    |> Seq.map FileInfo
    |> Seq.toArray
