using System.Net.Http.Headers;

namespace Adsefid.Sdk.Http;

internal static class MultipartHelper
{
    public static MultipartFormDataContent CreateFileContent(Stream fileStream, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);
        return content;
    }
}
