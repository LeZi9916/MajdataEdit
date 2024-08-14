using MajdataEdit.Interfaces;
using MajdataEdit.Types;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MajdataEdit;

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
    public static ViewResponse RequestPost<T>(string url,in T req) where T : IEditRequest
    {
        try
        {
            using var client = new HttpClient();
            var json = JsonSerializer.Serialize(req);
            var webRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8)
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            var rsp = new ViewResponse()
            {
                Code = ResponseCode.OK,
                Response =  reader.ReadToEnd(),
                Exception = null
            };
            return rsp;
        }
        catch(Exception e)
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
            string json = string.Empty;

            await using (var memStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memStream,req);
                memStream.Position = 0;
                using (var memReader = new StreamReader(memStream))
                    json = await memReader.ReadToEndAsync();
            }
                
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