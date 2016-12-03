namespace EnumerableTest.Runner.Console

open System
open System.Collections.Concurrent
open System.Reflection
open EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClass =
  let isPassed (testClass: TestClass) =
    testClass.InstantiationError.IsNone
    && testClass.NotCompletedMethods |> Array.isEmpty
    && testClass.Result |> Seq.forall TestMethod.isPassed
