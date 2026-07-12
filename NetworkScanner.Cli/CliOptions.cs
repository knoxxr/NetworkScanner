using NetworkScanner;

namespace NetworkScanner.Cli;

internal enum OutputFormat { Table, Json, Csv }

// 명령행 옵션 파서. 최소한의 GNU 스타일 --옵션만 다룬다.
internal sealed class CliOptions
{
    public List<ScanRangeInfo> Ranges { get; } = new();
    public string Ports { get; private set; } = "";      // "22/80/443" 형식(내부), 미지정 시 ""
    public OutputFormat Output { get; private set; } = OutputFormat.Table;
    public string? OutFile { get; private set; }
    public string? SystemName { get; private set; }
    public string Language { get; private set; } = Localization.English;
    public bool Quiet { get; private set; }
    public bool ShowHelp { get; private set; }
    public string? Error { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string? Next() => i + 1 < args.Length ? args[++i] : null;

            switch (a)
            {
                case "-h":
                case "--help":
                    o.ShowHelp = true;
                    return o;

                case "--range":
                {
                    string? v = Next();
                    if (v == null) return o.Fail("--range requires a value (CIDR or start-end)");
                    if (!TryParseRange(v, out var r)) return o.Fail($"invalid --range: {v}");
                    o.Ranges.Add(r);
                    break;
                }

                case "--ports":
                {
                    string? v = Next();
                    if (v == null) return o.Fail("--ports requires a value (e.g. 22,80,443)");
                    // 쉼표/슬래시 모두 허용하고 내부 형식(슬래시 구분)으로 정규화한다.
                    var nums = v.Split(new[] { ',', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(t => int.TryParse(t, out _));
                    o.Ports = string.Join("/", nums);
                    break;
                }

                case "--output":
                case "-o":
                {
                    string? v = Next()?.ToLowerInvariant();
                    o.Output = v switch
                    {
                        "table" => OutputFormat.Table,
                        "json" => OutputFormat.Json,
                        "csv" => OutputFormat.Csv,
                        _ => OutputFormat.Table,
                    };
                    if (v is not ("table" or "json" or "csv")) return o.Fail($"invalid --output: {v} (table|json|csv)");
                    break;
                }

                case "--out":
                {
                    o.OutFile = Next();
                    if (o.OutFile == null) return o.Fail("--out requires a file path");
                    break;
                }

                case "--system-name":
                    o.SystemName = Next();
                    break;

                case "--lang":
                {
                    string? v = Next()?.ToLowerInvariant();
                    if (v is not ("en" or "ko")) return o.Fail("--lang must be 'en' or 'ko'");
                    o.Language = v!;
                    break;
                }

                case "--quiet":
                case "-q":
                    o.Quiet = true;
                    break;

                default:
                    return o.Fail($"unknown option: {a}");
            }
        }
        return o;
    }

    private CliOptions Fail(string message)
    {
        Error = message;
        return this;
    }

    // "192.168.1.0/24"(CIDR) 또는 "192.168.1.10-192.168.1.50"(범위) 형식을 파싱한다.
    private static bool TryParseRange(string value, out ScanRangeInfo range)
    {
        range = new ScanRangeInfo { Index = 0, StartIP = "", EndIP = "", Description = "cli" };

        if (value.Contains('/'))
        {
            if (!IPRangeUtil.TryParseCidr(value, out string s, out string e)) return false;
            range.StartIP = s;
            range.EndIP = e;
            return true;
        }

        if (value.Contains('-'))
        {
            string[] parts = value.Split('-', 2);
            if (!System.Net.IPAddress.TryParse(parts[0].Trim(), out _) ||
                !System.Net.IPAddress.TryParse(parts[1].Trim(), out _)) return false;
            range.StartIP = parts[0].Trim();
            range.EndIP = parts[1].Trim();
            return true;
        }

        // 단일 IP
        if (!System.Net.IPAddress.TryParse(value.Trim(), out _)) return false;
        range.StartIP = value.Trim();
        range.EndIP = value.Trim();
        return true;
    }

    public static void PrintHelp()
    {
        Console.WriteLine(
@"netscan - NetworkScanner command-line interface

USAGE:
  netscan [options]

OPTIONS:
  --range <CIDR|start-end|ip>   Range to scan (repeatable). Examples:
                                  --range 192.168.1.0/24
                                  --range 10.0.0.1-10.0.0.50
                                If omitted, the local subnet is auto-detected.
  --ports <list>                Ports to scan, e.g. --ports 22,80,443
                                (omit to skip port scanning)
  -o, --output <table|json|csv> Output format (default: table)
  --out <file>                  Write output to a file instead of stdout
  --system-name <name>          System name recorded with results (default: hostname)
  --lang <en|ko>                Language for messages/labels (default: en)
  -q, --quiet                   Suppress progress messages on stderr
  -h, --help                    Show this help

NOTES:
  Progress is written to stderr; final results to stdout (safe to pipe).
  Reserved/backdoor port matching and vendor/OS/service detection are
  always applied to discovered hosts.

EXAMPLES:
  netscan --range 192.168.1.0/24 --ports 22,80,443
  netscan --range 10.0.0.0/24 -o json --out scan.json
  netscan            # scan the local subnet, table output");
    }
}
