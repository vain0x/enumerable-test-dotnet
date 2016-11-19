namespace EnumerableTest.Runner.Console

open System
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

  let timeout =
    let defaultTimeout = 10 * 1000
#if DEBUG
    let defaultTimeout = 3 * 1000
#endif
    appConfig.GetResult(<@ AppArgument.Timeout @>, defaultValue = defaultTimeout)
    |> float |> TimeSpan.FromMilliseconds
