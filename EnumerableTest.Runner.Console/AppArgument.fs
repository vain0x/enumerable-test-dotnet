namespace EnumerableTest.Runner.Console

open System.IO
open Argu

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AppArgument =
  let argumentParser = ArgumentParser.Create<AppArgument>()
  let appConfig = argumentParser.ParseCommandLine()

  let files =
    appConfig.GetResult(<@ AppArgument.Files @>, defaultValue = [])
    |> Seq.map FileInfo
    |> Seq.toArray

  let isVerbose =
    appConfig.Contains <@ AppArgument.Verbose @>
