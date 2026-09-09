using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Http;
using Adsefid.Sdk.Json;
using Adsefid.Sdk.Sms.Models;

namespace Adsefid.Sdk.Sms;

/// <summary>
/// SMS send, status, cancel, and inbound-message operations. Obtain via <see cref="AdsefidClient.Sms"/>.
/// Every method validates its documented client-side constraints (required fields, max lengths,
/// count limits) before making a network call, throwing <see cref="AdsefidValidationException"/> if
/// one fails; a call that reaches the network can throw <see cref="AdsefidApiException"/> (or its
/// <see cref="AdsefidRateLimitException"/> subtype) for a server-side rejection, or
/// <see cref="AdsefidTransportException"/> for a network or response-parsing failure.
/// </summary>
public sealed class SmsResource
{
    private readonly RequestExecutor _executor;

    internal SmsResource(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>Sends a single SMS to one receptor. <c>POST /v1/sms/single</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendSingleSmsRequest.Receptor"/>, <see cref="SendSingleSmsRequest.LineNumber"/>, or
    /// <see cref="SendSingleSmsRequest.Message"/> is empty; <see cref="SendSingleSmsRequest.Message"/>
    /// exceeds 900 characters; or <see cref="SendSingleSmsRequest.LocalId"/> is not a valid local id.
    /// </exception>
    public async Task<SendSingleSmsResponse> SendSingleAsync(SendSingleSmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmpty(request.Receptor, nameof(request.Receptor));
        Validation.RequireNonEmpty(request.LineNumber, nameof(request.LineNumber));
        Validation.RequireNonEmpty(request.Message, nameof(request.Message));
        Validation.RequireMaxLength(request.Message, Limits.SmsMessageMaxLength, nameof(request.Message));
        Validation.ValidateLocalId(request.LocalId, nameof(request.LocalId));

        return await _executor.PostJsonAsync(
            "/v1/sms/single",
            request,
            AdsefidJsonContext.Default.SendSingleSmsRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendSingleSmsResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends the same SMS to multiple receptors in one call. <c>POST /v1/sms/bulk</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendBulkSmsRequest.Receptors"/> is empty; <see cref="SendBulkSmsRequest.Message"/>
    /// or <see cref="SendBulkSmsRequest.LineNumber"/> is empty; <see cref="SendBulkSmsRequest.Message"/>
    /// exceeds 900 characters; a receptor's <see cref="BulkSmsReceptor.Receptor"/> is empty; or a
    /// receptor's <see cref="BulkSmsReceptor.LocalId"/> is not a valid local id.
    /// </exception>
    public async Task<SendBulkSmsResponse> SendBulkAsync(SendBulkSmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmptyCollection(request.Receptors, nameof(request.Receptors));
        Validation.RequireNonEmpty(request.Message, nameof(request.Message));
        Validation.RequireMaxLength(request.Message, Limits.SmsMessageMaxLength, nameof(request.Message));
        Validation.RequireNonEmpty(request.LineNumber, nameof(request.LineNumber));

        foreach (var receptor in request.Receptors)
        {
            Validation.RequireNonEmpty(receptor.Receptor, nameof(receptor.Receptor));
            Validation.ValidateLocalId(receptor.LocalId, nameof(receptor.LocalId));
        }

        return await _executor.PostJsonAsync(
            "/v1/sms/bulk",
            request,
            AdsefidJsonContext.Default.SendBulkSmsRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendBulkSmsResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a distinct message to each receptor (person-to-person) in one call. <c>POST /v1/sms/p2p</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendP2PSmsRequest.Messages"/> or <see cref="SendP2PSmsRequest.LineNumber"/> is
    /// empty; a message's <see cref="P2PSmsMessage.Receptor"/> or <see cref="P2PSmsMessage.Message"/>
    /// is empty; a message's <see cref="P2PSmsMessage.Message"/> exceeds 900 characters; or a
    /// message's <see cref="P2PSmsMessage.LocalId"/> is not a valid local id.
    /// </exception>
    public async Task<SendP2PSmsResponse> SendP2PAsync(SendP2PSmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmptyCollection(request.Messages, nameof(request.Messages));
        Validation.RequireNonEmpty(request.LineNumber, nameof(request.LineNumber));

        foreach (var message in request.Messages)
        {
            Validation.RequireNonEmpty(message.Receptor, nameof(message.Receptor));
            Validation.RequireNonEmpty(message.Message, nameof(message.Message));
            Validation.RequireMaxLength(message.Message, Limits.SmsMessageMaxLength, nameof(message.Message));
            Validation.ValidateLocalId(message.LocalId, nameof(message.LocalId));
        }

        return await _executor.PostJsonAsync(
            "/v1/sms/p2p",
            request,
            AdsefidJsonContext.Default.SendP2PSmsRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendP2PSmsResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a pre-approved template SMS, filling in its parameters. <c>POST /v1/sms/template</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="SendTemplateSmsRequest.TemplateId"/>, <see cref="SendTemplateSmsRequest.Receptor"/>,
    /// or <see cref="SendTemplateSmsRequest.LineNumber"/> is empty, or
    /// <see cref="SendTemplateSmsRequest.LocalId"/> is not a valid local id.
    /// </exception>
    public async Task<SendTemplateSmsResponse> SendTemplateAsync(SendTemplateSmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireNonEmpty(request.TemplateId, nameof(request.TemplateId));
        Validation.RequireNonEmpty(request.Receptor, nameof(request.Receptor));
        Validation.RequireNonEmpty(request.LineNumber, nameof(request.LineNumber));
        Validation.ValidateLocalId(request.LocalId, nameof(request.LocalId));

        return await _executor.PostJsonAsync(
            "/v1/sms/template",
            request,
            AdsefidJsonContext.Default.SendTemplateSmsRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeSendTemplateSmsResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Looks up delivery status for previously sent messages, by <paramref name="messageIds"/> and/or <paramref name="localIds"/>. <c>GET /v1/sms/status</c>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// Both <paramref name="messageIds"/> and <paramref name="localIds"/> are empty, or their
    /// combined count exceeds 2000.
    /// </exception>
    public async Task<GetSmsStatusResponse> GetStatusAsync(
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

        var query = CsvHelper.BuildIdsQuery(messageIds, localIds);
        return await _executor.GetAsync(
            $"/v1/sms/status{query}",
            AdsefidJsonContext.Default.ResponseEnvelopeGetSmsStatusResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels previously scheduled/queued SMS messages, by <see cref="CancelSmsRequest.MessageIds"/> and/or <see cref="CancelSmsRequest.LocalIds"/>. <c>POST /v1/sms/cancel</c>.</summary>
    /// <exception cref="AdsefidValidationException">Both <see cref="CancelSmsRequest.MessageIds"/> and <see cref="CancelSmsRequest.LocalIds"/> are empty.</exception>
    public async Task<CancelSmsResponse> CancelAsync(CancelSmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validation.RequireAtLeastOne(
            request.MessageIds is { Count: > 0 },
            request.LocalIds is { Count: > 0 },
            "At least one of 'MessageIds' or 'LocalIds' must be non-empty.");

        return await _executor.PostJsonAsync(
            "/v1/sms/cancel",
            request,
            AdsefidJsonContext.Default.CancelSmsRequest,
            AdsefidJsonContext.Default.ResponseEnvelopeCancelSmsResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Fetches inbound SMS messages received on <paramref name="lineNumber"/>. <c>GET /v1/sms/receive</c>.</summary>
    /// <param name="lineNumber">The line to fetch inbound messages for.</param>
    /// <param name="count">Maximum number of messages to return (1-499). Omit for the API's default page size.</param>
    /// <param name="since">Only return messages received at or after this time.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="AdsefidValidationException"><paramref name="lineNumber"/> is empty, or <paramref name="count"/> is outside 1-499.</exception>
    public async Task<GetReceivedSmsResponse> GetReceivedAsync(
        string lineNumber,
        int? count = null,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default)
    {
        Validation.RequireNonEmpty(lineNumber, nameof(lineNumber));
        if (count is not null)
        {
            Validation.RequireInRange(count.Value, Limits.ReceiveCountMin, Limits.ReceiveCountMax, nameof(count));
        }

        var parts = new List<string> { $"line_number={Uri.EscapeDataString(lineNumber)}" };
        if (count is not null)
        {
            parts.Add($"count={count.Value}");
        }

        if (since is not null)
        {
            parts.Add($"since={Uri.EscapeDataString(since.Value.ToString("O"))}");
        }

        var query = $"?{string.Join('&', parts)}";
        return await _executor.GetAsync(
            $"/v1/sms/receive{query}",
            AdsefidJsonContext.Default.ResponseEnvelopeGetReceivedSmsResponse,
            cancellationToken).ConfigureAwait(false);
    }
}
