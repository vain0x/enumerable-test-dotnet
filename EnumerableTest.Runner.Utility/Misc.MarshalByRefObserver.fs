namespace EnumerableTest.Runner

open System

type MarshalByRefObserver<'x>() =
  inherit MarshalByRefObject()

  let onNext = Event<'x>()
  let onError = Event<exn>()
  let onCompleted = Event<_>()

  [<CLIEvent>]
  member this.OnNext = onNext.Publish

  [<CLIEvent>]
  member this.OnError = onError.Publish

  [<CLIEvent>]
  member this.OnCompleted = onCompleted.Publish

  interface IObserver<'x> with
    override this.OnNext(value) =
      onNext.Trigger(value)

    override this.OnError(error) =
      onError.Trigger(error)

    override this.OnCompleted() =
      onCompleted.Trigger(())

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MarshalByRefObserver =
  let ofObserver (observer: IObserver<_>) =
    let o = MarshalByRefObserver()
    o.OnNext.Add(observer.OnNext)
    o.OnError.Add(observer.OnError)
    o.OnCompleted.Add(observer.OnCompleted)
    o
