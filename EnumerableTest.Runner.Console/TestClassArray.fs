namespace EnumerableTest.Runner.Console

open System
open System.Reflection
open EnumerableTest.Sdk
open EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassArray =
  let ofAssemblyAsync timeout (assembly: Assembly) =
    assembly.GetTypes()
    |> Seq.filter (fun typ -> typ |> TestClassType.isTestClass)
    |> Seq.map (fun typ -> async { return typ |> TestClass.create timeout })
