namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

type TestCase =
  MethodInfo * Lazy<Test>

type TestObject =
  Type * Lazy<Result<seq<TestCase> * IDisposable, exn>>

type TestSuite =
  seq<TestObject>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestObject =
  let internal testMethods (typ: Type) =
    typ.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Seq.filter
      (fun m ->
        not m.IsSpecialName
        && not m.IsGenericMethodDefinition
        && (m.GetParameters() |> Array.isEmpty)
        && m.ReturnType = typeof<seq<Test>>
      )

  let internal isTestClass (typ: Type) =
    typ.GetConstructor([||]) |> isNull |> not
    && typ |> testMethods |> Seq.isEmpty |> not

  let internal testCases (it: obj) =
    it.GetType()
    |> testMethods
    |> Seq.map
      (fun m ->
        let thunk () =
          let tests = m.Invoke(it, [||]) :?> seq<Test>
          Test.OfTests(m.Name, tests)
        (m, lazy thunk ())
      )

  let tryCreate (typ: Type): option<TestObject> =
    if typ |> isTestClass then
      let thunk () =
        let defaultConstructor =
          typ.GetConstructor([||])
        Result.catch (fun () -> defaultConstructor.Invoke([||]))
        |> Result.map
          (fun instance ->
            (instance |> testCases, instance |> Disposable.ofObj)
          )
      (typ, lazy thunk ()) |> Some
    else
      None

  let runAsync (testObject: TestObject) =
    async {
      let typ = testObject |> fst
      let instance = testObject |> snd
      let! result =
        async {
          match instance.Value with
          | Success (cases, disposable) ->
            use disposable = disposable
            let! results =
              cases
              |> Seq.map (fun (_, test) -> Async.run (fun () -> test.Value))
              |> Async.Parallel
            return results
          | Failure error ->
            return [| Test.Error("constructor", error) |]
        }
      return (typ, result)
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuite =
  let ofAssembly (assembly: Assembly): TestSuite =
    assembly.GetTypes() |> Seq.choose TestObject.tryCreate

  let runAsync (testSuite: TestSuite) =
    testSuite
    |> Seq.map TestObject.runAsync
    |> Async.Parallel
