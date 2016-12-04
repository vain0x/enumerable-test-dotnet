namespace EnumerableTest.Runner

open System.Collections.Generic
open System.Collections.ObjectModel
open System.Reactive.Disposables
open System.Reactive.Subjects

[<Sealed>]
type NullNotifier() =
  inherit Notifier()

  let warnings =
    ObservableCollection<_>()

  override this.Warnings =
    warnings

  override this.NotifyInfo(_) =
    ()

  override this.NotifyWarning(_, _) =
    ()

  override this.Subscribe(_) =
    Disposable.Empty

  override this.Dispose() =
    ()

[<Sealed>]
type ConcreteNotifier() =
  inherit Notifier()

  let subject =
    new Subject<_>()

  let warnings =
    ObservableCollection<_>()

  override this.Warnings =
    warnings

  override this.NotifyInfo(message) =
    subject.OnNext(Info message)

  override this.NotifyWarning(message, data) =
    let warning =
      {
        Message =
          message
        Data =
          data |> Seq.map KeyValuePair
      }
    warnings.Add(warning)
    subject.OnNext(Warning warning)

  override this.Subscribe(observer) =
    subject.Subscribe(observer)

  override this.Dispose() =
    subject.Dispose()
