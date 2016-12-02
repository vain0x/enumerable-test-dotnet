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
        (typ: Type).FullName
      Result =
        result
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuite =
  let executeType (typ: Type) =
    match TestMethod.createManyAsync typ with
    | (_, Some e) ->
      let result = TestResult.create typ (Failure e)
      [| async { return result } |]
    | (methods, None) ->
      methods |> Array.map (snd >> Async.map (Success >> TestResult.create typ))

  let ofTypesAsObservable types =
    let (types, asyncSeqSeq) =
      types
      |> Seq.filter (fun typ -> typ |> TestClassType.isTestClass)
      |> Seq.map (fun typ -> (typ, typ |> executeType))
      |> Seq.toArray
      |> Array.unzip
    let (schema: TestSuiteSchema) =
      types
      |> Array.map TestClassSchema.ofType
    let observable =
      asyncSeqSeq
      |> Seq.collect id
      |> Observable.startParallel
    (schema, observable)

  let ofAssemblyAsObservable (assembly: Assembly) =
    assembly.GetTypes()
    |> ofTypesAsObservable
