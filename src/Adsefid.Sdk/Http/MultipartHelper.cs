using System.Net.Http.Headers;
using Adsefid.Sdk.Exceptions;

namespace Adsefid.Sdk.Http;

internal static class MultipartHelper
{
    public static MultipartFormDataContent CreateFileContent(Stream fileStream, string fileName, string contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            throw new AdsefidValidationException("'contentType' must be a valid media type such as 'application/pdf'.");
        }

        if (fileStream.CanSeek && fileStream.Length - fileStream.Position > Limits.MessengerFileMaxBytes)
        {
            throw new AdsefidValidationException(
                $"The file content must be at most {Limits.MessengerFileMaxBytes} bytes (15 MiB).");
        }

        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new NonDisposingStream(fileStream));
        streamContent.Headers.ContentType = mediaType;
        content.Add(streamContent, "file", fileName);
        return content;
    }
}
