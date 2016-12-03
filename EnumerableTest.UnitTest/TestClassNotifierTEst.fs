namespace EnumerableTest.UnitTest

open System
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner
open EnumerableTest.Runner.Console

module TestClassNotifierTest =
  let seed types =
    let schema =
      TestSuiteSchema.ofTypes types
    let testResults =
      TestSuite.ofTypes types
    let testAssembly =
      { new TestAssembly() with
          override this.Start() =
            testResults.Connect()
          override this.TestResults =
            testResults :> IObservable<_>
          override this.Dispose() = ()
      }
    (new TestClassNotifier(schema, testAssembly), testResults)

  let ``test it notiies completed classes`` =
    test {
      let (it, connectable) = seed [| typeof<TestClass.Passing> |]
      use notifications = it |> Observable.collectNotifications
      connectable.Connect()
      connectable |> Observable.wait
      do! notifications.Count |> assertEquals 1
      let testClass = notifications |> Seq.head
      do! testClass.InstantiationError |> assertEquals None
      do! testClass.Result |> assertSatisfies (Array.length >> (=) 1)
      do! testClass.NotCompletedMethods |> assertEquals Array.empty
    }

  let ``test it notifies classes with instantiation errors`` =
    test {
      let (it, connectable) = seed [| typeof<TestClass.Uninstantiatable> |]
      use notifications = it |> Observable.collectNotifications
      connectable.Connect()
      connectable |> Observable.wait
      do! notifications.Count |> assertEquals 1
      let testClass = notifications |> Seq.head
      do! testClass.InstantiationError |> assertSatisfies Option.isSome
      do! testClass.Result |> assertSatisfies Array.isEmpty
      do! testClass.NotCompletedMethods |> assertSatisfies (Array.length >> (=) 2)
    }

  let ``test it notifies not-completed classes when completed`` =
    test {
      let (it, connectable) = seed [| typeof<TestClass.Never> |]
      use notifications = it |> Observable.collectNotifications
      connectable.Connect()
      let isCompleted = connectable |> Observable.waitTimeout (TimeSpan.FromMilliseconds(50.0))
      do! isCompleted |> assertEquals false
      do! notifications.Count |> assertEquals 0
      it.Complete()
      do! notifications.Count |> assertEquals 1
      let testClass = notifications |> Seq.head
      do! testClass.InstantiationError |> assertSatisfies Option.isNone
      do! testClass.Result |> assertSatisfies (Array.length >> (=) 2)
      do! testClass.NotCompletedMethods |> assertSatisfies (Array.length >> (=) 1)
    }
