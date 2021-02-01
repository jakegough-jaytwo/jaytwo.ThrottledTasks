# jaytwo.ThrottledTasks

<p align="center">
  <a href="https://jenkins.jaytwo.com/job/jaytwo.ThrottledTasks/job/main/" alt="Build Status (main)">
    <img src="https://jenkins.jaytwo.com/buildStatus/icon?job=jaytwo.ThrottledTasks%2Fmain&subject=build%20(main)" /></a>
  <a href="https://jenkins.jaytwo.com/job/jaytwo.ThrottledTasks/job/develop/" alt="Build Status (develop)">
    <img src="https://jenkins.jaytwo.com/buildStatus/icon?job=jaytwo.ThrottledTasks%2Fdevelop&subject=build%20(develop)" /></a>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/jaytwo.ThrottledTasks/" alt="NuGet Package jaytwo.ThrottledTasks">
    <img src="https://img.shields.io/nuget/v/jaytwo.ThrottledTasks.svg?logo=nuget&label=jaytwo.ThrottledTasks" /></a>
  <a href="https://www.nuget.org/packages/jaytwo.ThrottledTasks/" alt="NuGet Package jaytwo.ThrottledTasks (beta)">
    <img src="https://img.shields.io/nuget/vpre/jaytwo.ThrottledTasks.svg?logo=nuget&label=jaytwo.ThrottledTasks" /></a>
</p>

I've had to do a number of migration jobs where the input file is a multi-million row CSV file.  Usually the file is just a row of ID's, and there is a
download and/or transform and/or upload task associated with that ID.  The uploads and downloads are always I/O bound and leverage `async`/`await`, so
parallel tasks scale quite nicely to the limits of the systems involved in the migration.

I was tired of being limited to `await Task.WhenAll`; I didn't want to kick off millions of `Task`s (literally).  I thought it would be better to
build something that accepts an unbounded `IEnumerable` (or `IAsyncEnumerable`) and limits the iteration through the enumerable according to the number
of max parallel tasks invoked.

## Installation

Add the NuGet package

```
PM> Install-Package jaytwo.ThrottledTasks
```

## Usage

```csharp
using jaytwo.ThrottledTasks;

public async Task Main()
{
  await ThrottledTaskRunner.RunInParallelAsync(MigrateFilesAsync(), maxConcurrentTasks: 10);
  Console.WriteLine("Donezo.");
  Console.ReadLine();
}

private async IAsyncEnumerable<Func<Task>> MigrateFilesAsync()
{
  await foreach(var id in ReadIdsFromCsvAsync())
  {
    yield return () => MigrateFile(id);
  }
}

private IAsyncEnumerable<string> ReadIdsFromCsvAsync()
{
  // read from csv
}

public async Task MigrateFile(string id)
{
  var stopwatch = Stopwatch.StartNew();
  var downloadedFile = await DownloadFileFromOldSystem(id);
  await UploadFileToNewSystem(downloadedFile);
  stopwatch.Stop();

  Console.WriteLine("Finished "{0}" in {1:n2}ms", id, stopwatch.ElapsedMilliseconds);
}
```

---

Made with &hearts; by Jake
