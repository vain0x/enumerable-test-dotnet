namespace EnumerableTest.Runner

open System
open System.IO
open System.Reactive.Disposables
open System.Reflection
open System.Text

type LogFile() =
  let logDirectory =
    let executingPath =
      FileInfo(Assembly.GetExecutingAssembly().Location)
    DirectoryInfo(Path.Combine(executingPath.Directory.FullName, "log"))

  do
    if logDirectory.Exists |> not then
      logDirectory.Create()

  let getFile (dateTime: DateTimeOffset) =
    FileInfo(Path.Combine(logDirectory.FullName, dateTime.ToString("yyyy-MM-dd") + ".log"))

  let utf8 =
    UTF8Encoding()

  let writeStringAsync (string: string) (stream: Stream) =
    async {
      let data = utf8.GetBytes(string)
      return! stream.WriteAsync(data, 0, data.Length) |> Async.AwaitTask
    }

  let writeLineAsync (string: string) (stream: Stream) =
    async {
      do! stream |> writeStringAsync string
      return! stream |> writeStringAsync Environment.NewLine
    }

  let addWarningAsync message =
    async {
      let now = DateTimeOffset.Now
      let file = getFile now
      use stream = file.OpenWrite()
      let lines =
        seq {
          yield sprintf "%02d:%02d:%02d" now.Hour now.Minute now.Second
          yield sprintf "  Warning: %s" message
        }
      for line in lines do
        do! stream |> writeLineAsync line
    }

  let subscriptions =
    new CompositeDisposable()

  member this.ObserveNotifications(notifier: Notifier) =
    notifier |> Observable.subscribe
      (function
        | Info _ -> ()
        | Warning (message, _) ->
          addWarningAsync message |> Async.Start
      )
    |> subscriptions.Add

  member this.Dispose() =
    subscriptions.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
