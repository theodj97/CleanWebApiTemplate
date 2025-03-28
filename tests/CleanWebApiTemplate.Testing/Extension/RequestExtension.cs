using System.Text;
using System.Text.Json;

namespace CleanWebApiTemplate.Testing.Extension;

public static class RequestExtension
{
    public static async Task<HttpResponseMessage> PostAsync(this HttpClient client, string url, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var content = ToJsonContent(json);
        return await client.PostAsync(url, content);
    }

    public static async Task<HttpResponseMessage> PostAsync(this HttpClient client, string url) => await client.PostAsync(url);

    public static async Task<HttpResponseMessage> PutAsync(this HttpClient client, string url, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var content = ToJsonContent(json);
        return await client.PutAsync(url, content);
    }

    public static HttpClient AddHeader(this HttpClient client, string key, string value)
    {
        client.DefaultRequestHeaders.Remove(key);
        client.DefaultRequestHeaders.Add(key, value);
        return client;
    }

    private static StringContent ToJsonContent(string json,
                                               Encoding? encoding = null,
                                               string mediaType = "application/json")
    => new(json, encoding ?? Encoding.UTF8, mediaType);
}
