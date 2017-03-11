namespace EnumerableTest.Runner

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection

module ObservableTest =
  open System
  open System.Reactive.Linq
  open System.Reactive.Subjects
  open System.Threading

  let ``test waitTimeout`` =
    [
      test {
        let observable = new Subject<_>()
        do!
          observable |> Observable.waitTimeout (TimeSpan.FromMilliseconds(1.0))
          |> assertEquals false
      }
      test {
        let observable = [| 0; 1; 2 |].ToObservable()
        do!
         observable |> Observable.waitTimeout (TimeSpan.FromMilliseconds(1.0))
         |> assertEquals true
      }
    ]
    
  let ``test ofParallel`` =
    test {
      let executionCount = ref 0
      let notificationCount = ref 0
      let n = 5
      let computations =
        seq {
          for i in 0..(n - 1) ->
            async {
              Interlocked.Increment(executionCount) |> ignore
              return i
            }
        }
      let connectable =
        computations |> Observable.ofParallel
      connectable
      |> Observable.subscribe (fun i -> notificationCount := !notificationCount + i)
      |> ignore
      connectable.Connect()
      connectable |> Observable.wait
      do! !executionCount |> assertEquals n
      do! !notificationCount |> assertEquals (seq { 0..(n - 1) } |> Seq.sum)
    }

  let ``test startParallel`` =
    test {
      let executionCount = ref 0
      let notificationCount = ref 0
      let n = 5
      let computations =
        seq {
          for i in 0..(n - 1) ->
            async {
              Interlocked.Increment(executionCount) |> ignore
              return i
            }
        }
      let connectable =
        computations |> Observable.startParallel
      connectable
      |> Observable.subscribe (fun i -> notificationCount := !notificationCount + i)
      |> ignore
      connectable.Connect()
      connectable |> Observable.wait
      do! !executionCount |> assertEquals n
      do! !notificationCount |> assertEquals (seq { 0..(n - 1) } |> Seq.sum)
    }
