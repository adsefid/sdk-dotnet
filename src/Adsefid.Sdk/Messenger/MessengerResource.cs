using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Http;
using Adsefid.Sdk.Json;
using Adsefid.Sdk.Messenger.Models;
using Adsefid.Sdk.Models.Common;

namespace Adsefid.Sdk.Messenger;

/// <summary>
/// Messenger send, file upload, status, and cancel operations. Obtain via
/// <see cref="AdsefidClient.Messenger"/>. Every method validates its documented client-side
/// constraints before making a network call, throwing <see cref="AdsefidValidationException"/> if
/// one fails; a call that reaches the network can throw <see cref="AdsefidApiException"/> (or its
/// <see cref="AdsefidRateLimitException"/> subtype) for a server-side rejection, or
/// <see cref="AdsefidTransportException"/> for a network or response-parsing failure.
/// </summary>
public sealed class MessengerResource
{
    /// <summary>The largest file content <see cref="UploadFileAsync"/> accepts: 15 MiB (15,728,640 bytes), inclusive.</summary>
    public const long MaxFileBytes = Limits.MessengerFileMaxBytes;

    private readonly RequestExecutor _executor;

    internal MessengerResource(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>Sends a single Messenger message to one receptor. <c>POST /v1/messenger/single</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendSingleMessengerRequest.Message"/> or <see cref="SendSingleMessengerRequest.Receptor"/>
    /// is empty; <see cref="SendSingleMessengerRequest.Message"/> exceeds 4000 characters; or
    /// <see cref="SendSingleMessengerRequest.LocalId"/> is not a valid local id.
    /// </exception>
    public async Task<SendSingleMessengerResponse> SendSingleAsync(SendSingleMessengerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmpty(request.Message, nameof(request.Message));
        Validation.RequireMaxLength(request.Message, Limits.MessengerMessageMaxLength, nameof(request.Message));
        Validation.RequireNonEmpty(request.Receptor, nameof(request.Receptor));
        Validation.RequireNonEmpty(request.Profile, nameof(request.Profile));
        Validation.ValidateLocalId(request.LocalId, nameof(request.LocalId));

        return await _executor.SendJsonAsync(
            HttpMethod.Post,
            "/v1/messenger/single",
            request,
            AdsefidJsonContext.Default.SendSingleMessengerRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendSingleMessengerResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends the same Messenger message to multiple receptors in one call. <c>POST /v1/messenger/bulk</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendBulkMessengerRequest.Receptors"/>, <see cref="SendBulkMessengerRequest.Message"/>, or
    /// <see cref="SendBulkMessengerRequest.Profile"/> is empty; or the message exceeds 4000 characters.
    /// </exception>
    /// <remarks>Item-level errors are returned in the partial-success response.</remarks>
    public async Task<SendBulkMessengerResponse> SendBulkAsync(SendBulkMessengerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmptyCollection(request.Receptors, nameof(request.Receptors));
        Validation.RequireNonEmpty(request.Message, nameof(request.Message));
        Validation.RequireMaxLength(request.Message, Limits.MessengerMessageMaxLength, nameof(request.Message));
        Validation.RequireNonEmpty(request.Profile, nameof(request.Profile));

        return await _executor.SendJsonAsync(
            HttpMethod.Post,
            "/v1/messenger/bulk",
            request,
            AdsefidJsonContext.Default.SendBulkMessengerRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendBulkMessengerResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a distinct Messenger message to each receptor (person-to-person) in one call. <c>POST /v1/messenger/p2p</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendP2PMessengerRequest.Receptors"/> or <see cref="SendP2PMessengerRequest.Profile"/> is empty.
    /// </exception>
    /// <remarks>Item-level errors are returned in the partial-success response.</remarks>
    public async Task<SendP2PMessengerResponse> SendP2PAsync(SendP2PMessengerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmptyCollection(request.Receptors, nameof(request.Receptors));
        Validation.RequireNonEmpty(request.Profile, nameof(request.Profile));

        return await _executor.SendJsonAsync(
            HttpMethod.Post,
            "/v1/messenger/p2p",
            request,
            AdsefidJsonContext.Default.SendP2PMessengerRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendP2PMessengerResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads a file attachment for later use as <c>FileId</c> on a Messenger send. <c>POST /v1/messenger/file</c>.
    /// </summary>
    /// <param name="fileStream">The file content. The stream is read but not disposed by this method — the caller owns its lifetime.</param>
    /// <param name="fileName">The file name to report to the API.</param>
    /// <param name="contentType">The MIME type of the file (e.g. <c>"application/pdf"</c>).</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="AdsefidValidationException">
    /// <paramref name="fileName"/> or <paramref name="contentType"/> is empty, <paramref name="contentType"/>
    /// is not a valid media type, or a seekable <paramref name="fileStream"/> holds more than
    /// <see cref="MaxFileBytes"/> bytes from its current position. The size of a non-seekable stream
    /// cannot be known up front; the service rejects an oversized upload with
    /// <see cref="Enums.WebServiceResponseCode.FileTooLarge"/>.
    /// </exception>
    public async Task<UploadMessengerFileResponse> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        Validation.RequireNonEmpty(fileName, nameof(fileName));
        Validation.RequireNonEmpty(contentType, nameof(contentType));

        using var content = MultipartHelper.CreateFileContent(fileStream, fileName, contentType);
        return await _executor.PostMultipartAsync(
            "/v1/messenger/file",
            content,
            AdsefidJsonContext.Default.ResponseEnvelopeUploadMessengerFileResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels eligible Messenger messages by message id or local id. <c>POST /v1/messenger/cancel</c>.</summary>
    /// <exception cref="AdsefidValidationException">Both id lists are empty, or their combined distinct count exceeds 2000.</exception>
    public async Task<CancelMessengerResponse> CancelAsync(CancelMessengerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireAtLeastOne(
            request.MessageIds is { Count: > 0 },
            request.LocalIds is { Count: > 0 },
            "At least one of 'MessageIds' or 'LocalIds' must be non-empty.");
        Validation.RequireCombinedCountAtMost(
            request.MessageIds?.Distinct().Count() ?? 0,
            request.LocalIds?.Distinct(StringComparer.Ordinal).Count() ?? 0,
            Limits.CombinedCancelIdsMax,
            $"The combined distinct count of 'MessageIds' and 'LocalIds' must not exceed {Limits.CombinedCancelIdsMax}.");

        return await _executor.SendJsonAsync(
            HttpMethod.Post,
            "/v1/messenger/cancel",
            request,
            AdsefidJsonContext.Default.CancelMessengerRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeCancelMessengerResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a pre-approved template Messenger message, filling in its parameters. <c>POST /v1/messenger/template</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendTemplateMessengerRequest.TemplateId"/> or <see cref="SendTemplateMessengerRequest.Receptor"/>
    /// is empty, or <see cref="SendTemplateMessengerRequest.LocalId"/> is not a valid local id.
    /// </exception>
    public async Task<SendTemplateMessengerResponse> SendTemplateAsync(SendTemplateMessengerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmpty(request.TemplateId, nameof(request.TemplateId));
        Validation.RequireNonEmpty(request.Receptor, nameof(request.Receptor));
        Validation.RequireNonEmpty(request.Profile, nameof(request.Profile));
        Validation.ValidateLocalId(request.LocalId, nameof(request.LocalId));

        return await _executor.SendJsonAsync(
            HttpMethod.Post,
            "/v1/messenger/template",
            request,
            AdsefidJsonContext.Default.SendTemplateMessengerRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendTemplateMessengerResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Looks up delivery status for previously sent Messenger messages, by <paramref name="messageIds"/> and/or <paramref name="localIds"/>. <c>QUERY /v1/messenger/status</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// Both <paramref name="messageIds"/> and <paramref name="localIds"/> are empty, or their
    /// combined count exceeds 2000.
    /// </exception>
    public async Task<GetMessengerStatusResponse> GetStatusAsync(
        IReadOnlyCollection<Guid>? messageIds = null,
        IReadOnlyCollection<string>? localIds = null,
        CancellationToken cancellationToken = default)
    {
        Validation.RequireAtLeastOne(
            messageIds is { Count: > 0 },
            localIds is { Count: > 0 },
            "At least one of 'messageIds' or 'localIds' is required.");
        Validation.RequireCombinedCountAtMost(
            messageIds?.Distinct().Count() ?? 0,
            localIds?.Distinct(StringComparer.Ordinal).Count() ?? 0,
            Limits.CombinedStatusIdsMax,
            "The combined count of 'messageIds' and 'localIds' must not exceed 2000.");

        return await _executor.SendJsonAsync(
            new HttpMethod("QUERY"),
            "/v1/messenger/status",
            new StatusRequest
            {
                MessageIds = messageIds is { Count: > 0 } ? messageIds.Distinct().ToArray() : null,
                LocalIds = localIds is { Count: > 0 } ? localIds.Distinct(StringComparer.Ordinal).ToArray() : null,
            },
            AdsefidJsonContext.Default.StatusRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeGetMessengerStatusResponse,
            cancellationToken).ConfigureAwait(false);
    }
}
