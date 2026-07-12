using System.Net;
using System.Text;
using NetworkScanner;
using NetworkScanner.Cli;

// 헤드리스/서버 환경에서 GUI 없이 대역을 스캔하는 CLI. NetworkScanner.Core의 ScanEngine을 그대로 구동한다.
// 진행 상황은 stderr로, 최종 결과는 stdout으로 내보내 파이프 연동(특히 JSON)이 깨지지 않게 한다.

Console.OutputEncoding = Encoding.UTF8;

var options = CliOptions.Parse(args);
if (options.ShowHelp)
{
    CliOptions.PrintHelp();
    return 0;
}
if (options.Error != null)
{
    Console.Error.WriteLine("error: " + options.Error);
    Console.Error.WriteLine("Run 'netscan --help' for usage.");
    return 2;
}

Localization.Current = options.Language;

// Core의 콘솔용 로그 대상은 파일 폴백을 쓰므로 별도 배선이 필요 없다. 오류만 stderr로도 흘려보낸다.
OUIInfo.OnError = m => Console.Error.WriteLine("warn: " + m);
PingTester.OnError = m => Console.Error.WriteLine("warn: " + m);
PortReferenceLoader.OnError = m => Console.Error.WriteLine("warn: " + m);
ArpResolver.OnError = m => Console.Error.WriteLine("warn: " + m);

// 스캔 대역 결정: --range로 지정한 것이 없으면 이 PC의 로컬 서브넷을 자동 사용한다.
var ranges = new ScanRangeList();
if (options.Ranges.Count == 0)
{
    ScanRangeInfo? local = LocalNetworkInfo.GetLocalSubnetRange();
    if (local == null)
    {
        Console.Error.WriteLine("error: no --range given and local subnet could not be detected.");
        return 2;
    }
    ranges.AddItem(local);
    Console.Error.WriteLine($"info: no --range given; using detected local subnet {local.StartIP} ~ {local.EndIP}");
}
else
{
    foreach (var r in options.Ranges) ranges.AddItem(r);
}

var oui = new OUIInfo();
oui.LoadInfo();
var (reserved, prohibited) = PortReferenceLoader.Load();

var config = new CliConfig
{
    Ranges = ranges,
    PortList = options.Ports,           // "22/80/443" 또는 ""
    UsePorts = options.Ports.Length > 0,
    SystemName = options.SystemName ?? SafeHostName(),
    Reserved = reserved,
    Prohibited = prohibited,
};

var items = new IPInfoList();
var engine = new ScanEngine(items, oui, config);

bool quiet = options.Quiet || Console.IsErrorRedirected;
engine.Message += m => { if (!quiet) Console.Error.WriteLine(m); };
engine.ScanChangesDetected += changes =>
{
    foreach (var c in changes)
        Console.Error.WriteLine("change: " + ScanDiff.Describe(c));
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; engine.ScanningStop(); };

engine.InitFromConfig();
if (!engine.StartRefreshAllRange(config.GetSystemName()))
{
    Console.Error.WriteLine("error: scan could not be started.");
    return 1;
}
if (engine.Scanning != null) await engine.Scanning;

// 결과 스냅샷을 원하는 형식으로 출력한다.
string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
List<IPInfo> results = new(items);
string rendered = options.Output switch
{
    OutputFormat.Json => ReportGenerator.BuildJson(results, config.GetSystemName(), ts),
    OutputFormat.Csv => BuildCsv(results),
    _ => TableRenderer.Render(results),
};

if (options.OutFile != null)
{
    File.WriteAllText(options.OutFile, rendered, new UTF8Encoding(false));
    Console.Error.WriteLine($"info: wrote {results.Count(i => i.Alive)} alive host(s) to {options.OutFile}");
}
else
{
    Console.Out.WriteLine(rendered);
}

return 0;

static string SafeHostName()
{
    try { return Dns.GetHostName(); } catch { return "netscan"; }
}

static string BuildCsv(IReadOnlyList<IPInfo> items)
{
    var sb = new StringBuilder();
    sb.AppendLine("IPAddress,Status,Name,Type,OS,Ports,Service,MAC,Vendor,RTT");
    foreach (var i in items)
        sb.AppendLine(string.Join(",", new[]
        {
            i.Ip, i.StatusText, i.SystemName, i.DeviceType, i.OsGuess,
            i.Ports, i.Service, i.Macaddr, i.Vendor, i.RountTime
        }.Select(CsvEscape)));
    return sb.ToString();
}

static string CsvEscape(string? s)
{
    s ??= "";
    return s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
