namespace EnumerableTest.Runner.UnitTest

open System
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module ``test TestClassNotifier`` =
  let seed types =
    let schema =
      TestSuiteSchema.ofTypes types
    let testResults =
      TestSuite.ofTypes types
    let testAssembly =
      { new TestAssembly() with
          override this.Start() =
            testResults.Connect() |> ignore
          override this.TestCompleted =
            testResults :> IObservable<_>
          override this.Dispose() = ()
      }
    (new TestClassNotifier(schema, testAssembly), testResults)

  let ``test it notiies completed classes`` =
    test {
      let (it, connectable) = seed [| typeof<TestClasses.Passing> |]
      use notifications = it |> Observable.collectNotifications
      connectable.Connect() |> ignore
      connectable |> Observable.wait
      do! notifications.Count |> assertEquals 1
      let result = notifications |> Seq.head
      do! result.InstantiationError |> assertEquals None
      do! result.TestMethodResults |> assertSatisfies (Array.length >> (=) 1)
      do! result.NotCompletedMethods |> assertEquals Array.empty
    }

  let ``test it notifies classes with instantiation errors`` =
    test {
      let (it, connectable) = seed [| typeof<TestClasses.Uninstantiatable> |]
      use notifications = it |> Observable.collectNotifications
      connectable.Connect() |> ignore
      connectable |> Observable.wait
      do! notifications.Count |> assertEquals 1
      let result = notifications |> Seq.head
      do! result.InstantiationError |> assertSatisfies Option.isSome
      do! result.TestMethodResults |> assertSatisfies Array.isEmpty
      do! result.NotCompletedMethods |> assertSatisfies (Array.length >> (=) 2)
    }

  let ``test it notifies not-completed classes when completed`` =
    test {
      let (it, connectable) = seed [| typeof<TestClasses.Never> |]
      use notifications = it |> Observable.collectNotifications
      connectable.Connect() |> ignore
      let isCompleted = connectable |> Observable.waitTimeout (TimeSpan.FromMilliseconds(50.0))
      do! isCompleted |> assertEquals false
      do! notifications.Count |> assertEquals 0
      it.Complete()
      do! notifications.Count |> assertEquals 1
      let result = notifications |> Seq.head
      do! result.InstantiationError |> assertSatisfies Option.isNone
      do! result.TestMethodResults |> assertSatisfies (Array.length >> (=) 2)
      do! result.NotCompletedMethods |> assertSatisfies (Array.length >> (=) 1)
    }
