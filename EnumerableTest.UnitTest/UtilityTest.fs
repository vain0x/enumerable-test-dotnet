namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf

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

module StringTest =
  let ``test convertToLF`` =
    let body (source, expected) =
      test {
        do! source |> String.convertToLF |> assertEquals expected
      }
    parameterize {
      case ("a\r\nb", "a\nb")
      case ("a\n\r\n\rb", "a\n\n\nb")
      run body
    }

module DisposableTest =
  open System.Reactive.Disposables

  module test_dispose =
    let ofDisposable =
      test {
        let count = ref 0
        let disposable = Disposable.Create(fun () -> count |> incr)
        disposable |> Disposable.dispose
        do! !count |> assertEquals 1
      }

    let ofNonDisposable =
      test {
        let x = obj()
        x |> Disposable.dispose
        return ()
      }

module ReadOnlyListTest =
  open System.Collections.Generic

  let ``test symmetricDifferenceBy`` =
    let body (left, right, expected) =
      test {
        let difference =
          ReadOnlyList.symmetricDifferenceBy fst fst
            (left :> IReadOnlyList<_>)
            (right :> IReadOnlyList<_>)
        do!
          ( difference.Left |> Seq.toArray
          , difference.Intersect |> Seq.toArray
          , difference.Right |> Seq.toArray
          ) |> assertEquals expected
      }
    parameterize {
      case
        ( [| (0, "a"); (1, "b") |]
        , [||]
        , ( [| (0, "a"); (1, "b") |]
          , [||]
          , [||]
          ))
      case
        ( [||]
        , [| (0, "A"); (1, "B") |]
        , ( [||]
          , [||]
          , [| (0, "A"); (1, "B") |]
          ))
      case
        ( [| (1, "b"); (2, "c") |]
        , [| (0, "A"); (3, "D") |]
        , ( [| (1, "b"); (2, "c") |]
          , [||]
          , [| (0, "A"); (3, "D") |]
          ))
      case
        ( [| (0, "a"); (1, "b"); (2, "c") |]
        , [| (0, "A"); (2, "C") |]
        , ( [| (1, "b") |]
          , [| (0, (0, "a"), (0, "A")); (2, (2, "c"), (2, "C")) |]
          , [||]
          ))
      case
        ( [| (0, "a"); (2, "c") |]
        , [| (2, "C"); (1, "B") |]
        , ( [| (0, "a") |]
          , [| (2, (2, "c"), (2, "C")) |]
          , [| (1, "B") |]
          ))
      run body
    }

module ReadOnlyUptodateCollectionTest =
  open System.Collections.ObjectModel
  open Reactive.Bindings

  let ``test ofUptodate`` =
    test {
      let uptodate = ReactiveProperty.create 0
      use collection = ReadOnlyUptodateCollection.ofUptodate uptodate
      use notifications = collection |> Observable.collectNotifications
      do! notifications |> Seq.toList |> assertEquals [UptodateCollectionNotification.ofAdded 0]
      uptodate.Value <- 1
      do!
        notifications |> Seq.toList |> assertEquals
          [
            UptodateCollectionNotification.ofAdded 0
            UptodateCollectionNotification.ofRemoved 0
            UptodateCollectionNotification.ofAdded 1
          ]
    }

  module ``test ofObservableCollection`` =
    let seed values =
      let source = ObservableCollection(values :> seq<_>)
      let collection = ReadOnlyUptodateCollection.ofObservableCollection source
      let notifications = collection |> Observable.collectNotifications
      (source, collection, notifications)

    let ``it notifies additions`` =
      test {
        let (source, collection, notifications) = seed []
        do! notifications.Count |> assertEquals 0
        source.Add(0)
        source.Add(1)
        do! notifications |> Seq.toList |> assertEquals
              ([0; 1] |> List.map UptodateCollectionNotification.ofAdded)
      }

    let ``it notifies that initial values are added`` =
      test {
        let (source, collection, notifications) = seed [0; 1; 2]
        do! notifications |> Seq.toList |> assertEquals
              ([0; 1; 2] |> List.map UptodateCollectionNotification.ofAdded)
      }

    let ``it notifies removals`` =
      test {
        let (source, collection, notifications) = seed [0; 1; 2]
        do! notifications.Count |> assertEquals 3
        do! source.Remove(1) |> assertEquals true
        do! notifications.Count |> assertEquals 4
        do! notifications |> Seq.last |> assertEquals (UptodateCollectionNotification.ofRemoved 1)
      }

    let ``it doesn't notify removal if missing`` =
      test {
        let (source, collection, notifications) = seed [0; 1; 2]
        do! source.Remove(-1) |> assertEquals false
        do! notifications.Count |> assertEquals 3
      }

  module ``test map`` =
    let seed () =
      let count = ref 0
      let source = UptodateCollection.create ()
      let mapped =
        source |> ReadOnlyUptodateCollection.map
          (fun i ->
            count |> incr
            i + 1
          )
      let notifications = mapped |> Observable.collectNotifications
      (source, mapped, notifications, count)

    let ``it notifies mapped values`` =
      test {
        let (source, mapped, notifications, count) = seed ()
        source.Add(0)
        source.Add(1)
        source.Add(2)
        source.Remove(1) |> ignore<bool>
        do! notifications |> Seq.toList |> assertEquals
              [
                UptodateCollectionNotification.ofAdded 1
                UptodateCollectionNotification.ofAdded 2
                UptodateCollectionNotification.ofAdded 3
                UptodateCollectionNotification.ofRemoved 2
              ]
        do! !count |> assertEquals 4
      }

  let ``test flatten`` =
    test {
      let source = UptodateCollection.create ()
      let flattened = source |> ReadOnlyUptodateCollection.flatten
      let notifications = flattened |> Observable.collectNotifications
      let subsource1 = UptodateCollection.create ()
      let subsource2 = UptodateCollection.create ()
      source.Add(subsource1)
      source.Add(subsource2)
      subsource1.Add(0)
      subsource2.Add(1)
      subsource2.Add(2)
      subsource2.Remove(1) |> ignore<bool>
      do! notifications |> Seq.toList |> assertEquals
            [
              UptodateCollectionNotification.ofAdded 0
              UptodateCollectionNotification.ofAdded 1
              UptodateCollectionNotification.ofAdded 2
              UptodateCollectionNotification.ofRemoved 1
            ]
    }

  let ``test sumBy`` =
    test {
      let additive =
        { new GroupSig<int>() with
            override this.Unit = 0
            override this.Multiply(l, r) = l + r
            override this.Divide(l, r) = l - r
        }
      let source = UptodateCollection.create ()
      let sum = source |> ReadOnlyUptodateCollection.sumBy additive
      let notifications = sum |> Observable.collectNotifications
      source.Add(1)
      source.Add(2)
      source.Add(3)
      source.Remove(2) |> ignore<bool>
      do! notifications |> Seq.toList |> assertEquals
            [0; 1; 3; 6; 4]
    }
