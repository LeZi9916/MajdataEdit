using MajdataEdit.Interfaces;
using MajdataEdit.Types;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace MajdataEdit.Utils;

internal static class WebControl
{
    public static string Post(string url, in string data = "")
    {
        try
        {
            using var client = new HttpClient();

            var webRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(data, Encoding.UTF8)
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            return reader.ReadToEnd();
        }
        catch
        {
            return "ERROR";
        }
    }
    public static ViewResponse RequestPost<T>(string url, in T req) where T : IEditRequest
    {
        try
        {
            using var client = new HttpClient();
            var json = Serializer.Json.Serialize(req);
            var webRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8)
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            var rsp = new ViewResponse()
            {
                Code = ResponseCode.OK,
                Response = reader.ReadToEnd(),
                Exception = null
            };
            return rsp;
        }
        catch (Exception e)
        {
            return new ViewResponse()
            {
                Code = ResponseCode.Error,
                Response = null,
                Exception = e
            };
        }
    }
    public static async Task<ViewResponse> RequestPostAsync<T>(string url, T req) where T : IEditRequest
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 1);
            string json = await Serializer.Json.SerializeAsync(req);

            var webRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8)
            };

            var response = await client.SendAsync(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            var rsp = new ViewResponse()
            {
                Code = ResponseCode.OK,
                Response = await reader.ReadToEndAsync(),
                Exception = null
            };
            return rsp;
        }
        catch (Exception e)
        {
            return new ViewResponse()
            {
                Code = ResponseCode.Error,
                Response = null,
                Exception = e
            };
        }
    }

    public static async Task<string> RequestGETAsync(string url)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", $"{executingAssembly.GetName().Name!} / {executingAssembly.GetName().Version!.ToString(3)}");
        var response = await httpClient.SendAsync(request);
        using var reader = new StreamReader(response.Content.ReadAsStream());

        return reader.ReadToEnd();
    }
}