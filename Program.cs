using Collections.Pooled;
using System.Diagnostics;
using System.Runtime.CompilerServices;

Console.Write("Please Input Search Path:");
var path = Console.ReadLine();

Console.Write("Please Input Search File:");
var file = Console.ReadLine();

Console.Write("Please Input Search Text:");
var text = Console.ReadLine();

IEnumerable<SearchResult<IAsyncEnumerable<TextLine>>> SearchText(string searchPath, string target, string pattern)
{
    async IAsyncEnumerable<string?> EnumerateLineAsync(string file)
    {
        await using var fs = File.OpenRead(file);
        using var sr = new StreamReader(fs);
        while (!sr.EndOfStream)
        {
            yield return await sr.ReadLineAsync();
        }
    }

    return Directory.EnumerateFiles(searchPath, target, SearchOption.AllDirectories)
        .Select(x => new SearchResult<IAsyncEnumerable<TextLine>>
        {
            File = x,
            TextLine = EnumerateLineAsync(x).
        SelectAsync(static (s, l) => new TextLine { text = s?.Trim(), line = l }).
        WhereAsync(v => v.text?.Contains(pattern) ?? false)
        });
}

var resultList = await Task.WhenAll(SearchText(path, file, text).Select(static async x => new SearchResult<PooledList<TextLine>> { File = x.File, TextLine = await x.TextLine.ToListAsync() }));

for (int i = 0; i < resultList?.Length; i++)
{
    Console.WriteLine($"Index:{i} FileName: {resultList[i].File} --> Text: {resultList?[i].TextLine?.FirstOrDefault()?.text} --> Line: {resultList?[i].TextLine?.FirstOrDefault()?.line}");
}

Console.Write("Please Select File Index: ");
var isNum = int.TryParse(Console.ReadLine(),out int index);
if (!isNum)
{
    Console.WriteLine("Please Input Num");
}

var info = resultList?[index];

var folderPath = info?.File.AsMemory().Slice(0,info.File.LastIndexOf(@"\")).ToString();

Process.Start("Explorer.exe", folderPath);

Console.ReadLine();


static class AsyncLinqExtension
{
    public static async Task<PooledList<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        var list = new PooledList<T>();
        await foreach (var i in enumerable)
        {
            list.Add(i);
        }
        return list;

    }
    public static async IAsyncEnumerable<U> SelectAsync<T, U>(this IAsyncEnumerable<T> enumerable, Func<T, long, U> selector)
    {
        await foreach (var i in enumerable)
        {
            long line = 0;
            yield return selector(i, ++line);
        }
    }
    public static async IAsyncEnumerable<T> WhereAsync<T>(this IAsyncEnumerable<T> enumerable, Predicate<T> predicate)
    {
        await foreach (var i in enumerable)
        {
            if (predicate(i))
            {
                yield return i;
            }
        }
    }
}

public class SearchResult<T>
{
    public string File { get; set; }

    public T TextLine { get; set; }
}

public class TextLine
{
    public string? text { get; set; }

    public long line { get; set; }
}