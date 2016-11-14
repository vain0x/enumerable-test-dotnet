namespace EnumerableTest.Runner.Console

open System
open Argu
open EnumerableTest.Runner

[<RequireQualifiedAccess>]
type AppArgument =
  | [<MainCommand>]
    Files of list<string>
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Files _ ->
        "Paths to test assembly"

type TestClass =
  {
    TypeFullName                : string
    InstantiationError          : option<Exception>
    Result                      : array<TestMethod>
  }

type TestSuite =
  array<TestClass>
