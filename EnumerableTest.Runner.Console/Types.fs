namespace EnumerableTest.Runner.Console

open System
open Argu
open EnumerableTest.Runner

[<RequireQualifiedAccess>]
type AppArgument =
  | [<MainCommand>]
    Files of list<string>
  | Verbose
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Files _ ->
        "Paths to test assembly"
      | Verbose ->
        "Print debug outputs"

type TestClass =
  {
    TypeFullName                : string
    InstantiationError          : option<Exception>
    Result                      : array<TestMethod>
  }

type TestSuite =
  array<TestClass>
