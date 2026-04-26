using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

public class BaseService
{
    protected readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BaseService()
    {
    }

    public void InvalidateCache()
    {
        // Placeholder for future caching strategy
    }

    protected async Task HandleErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflicts = await response.Content.ReadFromJsonAsync<ProcessResult>(JsonOptions, ct);
            throw new ConflictException(conflicts);
        }
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Request failed: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}");
    }
}