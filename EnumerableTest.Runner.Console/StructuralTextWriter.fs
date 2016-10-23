namespace EnumerableTest.Runner

open System
open System.IO
open System.Text
open EnumerableTest.Runner

type StructuralTextWriter(writer: TextWriter) =
  let indent = ref 0

  let indentLength () =
    !indent * 2

  let createIndent () =
    String.replicate (indentLength ()) " "

  member this.Indent =
    indent

  member this.IndentLength =
    indentLength ()

  member this.AddIndent() =
    indent |> incr
    { new IDisposable with
        override this.Dispose() =
          indent |> decr
    }

  member this.WriteLineAsync(text) =
    async {
      let indent = createIndent ()
      for line in text |> String.splitByLinebreak do
        do! writer.WriteAsync(indent) |> Async.AwaitTask
        do! writer.WriteLineAsync(line) |> Async.AwaitTask
    }
