namespace EnumerableTest.Runner

open System
open System.Reflection
open System.Threading.Tasks
open Basis.Core
open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestResult =
  let create typ result: TestResult =
    {
      TypeFullName =
        typ |> Type.fullName
      Result =
        result
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuite =
  let executeType (typ: Type) =
    match TestMethodResult.createManyAsync typ with
    | (_, Some e) ->
      let result = TestResult.create typ (Failure e)
      [| async { return result } |]
    | (methods, None) ->
      methods |> Array.map (snd >> Async.map (Success >> TestResult.create typ))

  let ofTypes types =
    types
    |> Seq.filter TestClassType.isTestClass
    |> Seq.collect executeType
    |> Observable.startParallel

  let ofAssembly (assembly: Assembly) =
    assembly.GetTypes()
    |> ofTypes
