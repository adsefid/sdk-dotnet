using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Json;
using Adsefid.Sdk.Models.Common;

namespace Adsefid.Sdk.Http;

internal sealed class RequestExecutor(HttpClient httpClient, Uri baseUri, string apiKey, string userAgent)
{
    private const WebServiceResponseCode UnknownResponseCode = (WebServiceResponseCode)(-1);

    public Task<TData> GetAsync<TData>(
        string requestUri,
        JsonTypeInfo<ResponseEnvelope<TData>> responseTypeInfo,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, requestUri, contentFactory: null, responseTypeInfo, cancellationToken);

    public Task<TData> PostJsonAsync<TRequest, TData>(
        string requestUri,
        TRequest body,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<ResponseEnvelope<TData>> responseTypeInfo,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            requestUri,
            () => new StringContent(JsonSerializer.Serialize(body, requestTypeInfo), Encoding.UTF8, "application/json"),
            responseTypeInfo,
            cancellationToken);

    public Task<TData> PostMultipartAsync<TData>(
        string requestUri,
        MultipartFormDataContent content,
        JsonTypeInfo<ResponseEnvelope<TData>> responseTypeInfo,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, requestUri, () => content, responseTypeInfo, cancellationToken);

    private async Task<TData> SendAsync<TData>(
        HttpMethod method,
        string requestUri,
        Func<HttpContent>? contentFactory,
        JsonTypeInfo<ResponseEnvelope<TData>> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(baseUri, requestUri));
        request.Headers.Add("X-API-KEY", apiKey);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        request.Headers.Accept.ParseAdd("application/json");
        if (contentFactory is not null)
        {
            request.Content = contentFactory();
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new AdsefidTransportException("A transport-level error occurred while calling the adsefid.com API.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AdsefidTransportException("The request to the adsefid.com API timed out.", ex);
        }

        using (response)
        {
            string responseBody;
            try
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                throw new AdsefidTransportException("Failed to read the adsefid.com API response body.", ex);
            }

            var errorPayload = TryReadError(responseBody);
            if (errorPayload is not null)
            {
                throw CreateApiException((int)response.StatusCode, errorPayload);
            }

            if (response.IsSuccessStatusCode)
            {
                return ParseSuccess(responseBody, responseTypeInfo);
            }

            if ((int)response.StatusCode == 429)
            {
                throw new AdsefidRateLimitException(WebServiceResponseCode.RequestLimitReached, "RATE_LIMITED", (int)response.StatusCode, null);
            }

            throw new AdsefidApiException(UnknownResponseCode, "UNKNOWN_ERROR", (int)response.StatusCode, null);
        }
    }

    private static TData ParseSuccess<TData>(string responseBody, JsonTypeInfo<ResponseEnvelope<TData>> responseTypeInfo)
    {
        ResponseEnvelope<TData>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(responseBody, responseTypeInfo);
        }
        catch (JsonException ex)
        {
            throw new AdsefidTransportException("Failed to deserialize a successful adsefid.com API response.", ex);
        }

        if (envelope is null || envelope.Status != "success" || envelope.Data is null)
        {
            throw new AdsefidTransportException("The adsefid.com API returned an unexpected response shape.");
        }

        return envelope.Data;
    }

    private static ApiErrorPayload? TryReadError(string responseBody)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize(responseBody, AdsefidJsonContext.Default.ErrorEnvelope);
            return envelope?.Status == "error" ? envelope.Error : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AdsefidApiException CreateApiException(int httpStatusCode, ApiErrorPayload errorPayload)
    {
        var code = (WebServiceResponseCode)errorPayload.Code;
        if (code is WebServiceResponseCode.MessageLimitReached or WebServiceResponseCode.RequestLimitReached
            || httpStatusCode == 429)
        {
            return new AdsefidRateLimitException(code, errorPayload.Name, httpStatusCode, errorPayload.Details);
        }

        return new AdsefidApiException(code, errorPayload.Name, httpStatusCode, errorPayload.Details);
    }
}
