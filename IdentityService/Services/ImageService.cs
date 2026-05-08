namespace IdentityService.Services;

public class ImageService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://shiko-image-api.azurewebsites.net";

    public ImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        content.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync($"{BaseUrl}/api/images", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to upload image. Status: {response.StatusCode}, Body: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
        return result!.Url;
    }
}

public class ImageUploadResponse
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}