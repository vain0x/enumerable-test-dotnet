namespace EnumerableTest.Runner.Wpf

open Basis.Core
open DotNetKit.Observing
open EnumerableTest.Sdk
open EnumerableTest.Runner

type TestMethodNode(name: string) =
  let lastResult = Uptodate.Create(None)

  let lastResultUntyped =
    lastResult.Select
      (function
        | Some (Success test) -> test :> obj
        | Some (Failure testError) -> testError :> obj
        | None -> NotExecutedResult.Instance :> obj
      )

  let testStatus =
    lastResult.Select
      (function
        | Some testMethodResult ->
          TestStatus.ofTestMethodResult testMethodResult
        | None ->
          TestStatus.NotCompleted
      )
      
  let isPassed =
    lastResult.Select
      (function
        | Some (Success test) ->
          (test: GroupTest).IsPassed
        | Some (Failure _) ->
          false
        | None ->
          true
      )

  member this.Name = name

  member this.LastResult = lastResultUntyped

  member this.TestStatus = testStatus

  member this.IsPassed = isPassed

  member this.Update() =
    lastResult.Value <- None

  member this.UpdateResult(result: obj) =
    lastResult.Value <- result |> Result.ofObj<GroupTest, TestError>
