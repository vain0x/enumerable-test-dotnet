namespace EnumerableTest.Runner

open System
open System.Reflection
open Basis.Core
open EnumerableTest

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestType =
  let testMethodInfos (typ: Type) =
    typ.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Seq.filter
      (fun m ->
        not m.IsSpecialName
        && not m.IsGenericMethodDefinition
        && m.ReturnType = typeof<seq<Test>>
        && (m.GetParameters() |> Array.isEmpty)
      )

  let isTestClass (typ: Type) =
    typ.GetConstructor([||]) |> isNull |> not
    && typ |> testMethodInfos |> Seq.isEmpty |> not

  let instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])
