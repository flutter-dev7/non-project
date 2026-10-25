using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Non.IntegrationTests.Infrastructure;

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}

public class ApiEnvelope<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

public class ApiResponse<T>
{
    public HttpStatusCode StatusCode { get; init; }
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public string RawBody { get; init; } = string.Empty;

    public T Require()
    {
        if (!IsSuccess)
        {
            throw new Xunit.Sdk.XunitException(
                $"HTTP {(int)StatusCode}: {Error ?? RawBody}{Environment.NewLine}" +
                $"Ошибки из логов API:{Environment.NewLine}{TestDiagnostics.Dump()}");
        }

        return Data!;
    }
}

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http, Guid userId = default, string phone = "")
    {
        _http = http;
        UserId = userId;
        Phone = phone;
    }

    public Guid UserId { get; }

    public string Phone { get; }

    public void SetToken(string token) =>
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public Task<ApiResponse<T>> GetAsync<T>(string url) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, url));

    public Task<ApiResponse<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(Build(HttpMethod.Post, url, body));

    public Task<ApiResponse<T>> PutAsync<T>(string url, object? body = null) =>
        SendAsync<T>(Build(HttpMethod.Put, url, body));
    
    public Task<ApiResponse<T>> DeleteAsync<T>(string url) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Delete, url));

    private static HttpRequestMessage Build(HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), null, TestJson.Options);

        return request;
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request)
    {
        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        ApiEnvelope<T>? envelope = null;

        try
        {
            envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(text, TestJson.Options);
        }
        catch (JsonException)
        {
        }

        return new ApiResponse<T>
        {
            StatusCode = response.StatusCode,
            IsSuccess = envelope?.IsSuccess ?? false,
            Data = envelope is null ? default : envelope.Data,
            Error = envelope?.Error,
            RawBody = text
        };
    }
}