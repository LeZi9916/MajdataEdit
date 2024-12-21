using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.Types;
public readonly struct CheckUpdateResult
{
    public required bool IsSuccess { get; init; }
    public required bool IsBusy { get; init; }
    public SemVersion? LatestVersion { get; init; }
    public string? DownloadUrl { get; init; }
    public HttpRequestError? HttpRequestError { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public Exception? Exception { get; init; }
}
