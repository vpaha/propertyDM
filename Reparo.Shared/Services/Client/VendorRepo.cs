using System.Net.Http.Json;

public interface IVendorRepo
{
    Task<VendorModel?> GetVendorAsync(CancellationToken cancellationToken = default);
    Task<VendorModel?> GetVendorByPlaceAsync(string placeId, CancellationToken cancellationToken = default);
}

public sealed class VendorRepo : IVendorRepo
{
    private readonly HttpClient _http;

    public VendorRepo(HttpClient http)
    {
        _http = http;
    }

    public async Task<VendorModel?> GetVendorByPlaceAsync(string placeId, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<VendorModel>($"damage/vendor-get?placeid={placeId}", ct);
    }

    public async Task<VendorModel?> GetVendorAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<VendorModel>("vendor/vendor-profile", ct);
    }
}