namespace EnumerableTest.Runner.Wpf

open System
open System.Windows.Input
open System.Reactive.Subjects
open Reactive.Bindings
open EnumerableTest.Runner

type ObservableCommand<'x>(canExecute: IReadOnlyReactiveProperty<bool>) as this =
  let canExecuteChanged = Event<_, _>()

  let context = SynchronizationContext.capture ()

  let subject = new Subject<_>()

  let subscription =
    canExecute.Subscribe
      (fun _ ->
        context |> SynchronizationContext.send
          (fun () ->
            canExecuteChanged.Trigger(this, EventArgs.Empty)
          ))

  member this.CanExecute =
    canExecute

  member this.Execute(x) =
    subject.OnNext(x)

  member this.Subscribe(observer) =
    subject.Subscribe(observer)

  member this.Dispose() =
    subject.Dispose()
    subscription.Dispose()

  interface ICommand with
    [<CLIEvent>]
    override this.CanExecuteChanged = canExecuteChanged.Publish

    override this.CanExecute(_) = this.CanExecute.Value

    override this.Execute(x) = this.Execute(x :?> 'x)

  interface IObservable<'x> with
    override this.Subscribe(observer) = this.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ObservableCommand =
  let never =
    new ObservableCommand<unit>(ReactiveProperty.create false)

  let ofFunc f canExecute =
    new ObservableCommand<_>(canExecute)
    |> tap (fun c -> c |> Observable.add f)
