using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Http;
using Adsefid.Sdk.Json;
using Adsefid.Sdk.User.Models;

namespace Adsefid.Sdk.User;

/// <summary>
/// Account info, line, profile, and template lookup operations. Obtain via <see cref="AdsefidClient.User"/>.
/// A call that reaches the network can throw <see cref="AdsefidApiException"/> for a server-side
/// rejection, or <see cref="AdsefidTransportException"/> for a network or response-parsing failure.
/// </summary>
public sealed class UserResource
{
    private readonly RequestExecutor _executor;

    internal UserResource(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>Gets the authenticated account's profile info and remaining credit. <c>GET /v1/user/info</c>.</summary>
    public async Task<UserInfo> GetInfoAsync(CancellationToken cancellationToken = default) =>
        await _executor.GetAsync(
            "/v1/user/info",
            AdsefidJsonContext.Default.ResponseEnvelopeUserInfo,
            cancellationToken).ConfigureAwait(false);

    /// <summary>Lists the SMS lines available to the authenticated account. <c>GET /v1/user/lines</c>.</summary>
    public async Task<IReadOnlyList<UserLine>> GetLinesAsync(CancellationToken cancellationToken = default) =>
        await _executor.GetAsync(
            "/v1/user/lines",
            AdsefidJsonContext.Default.ResponseEnvelopeListUserLine,
            cancellationToken).ConfigureAwait(false);

    /// <summary>Lists the Messenger profiles available to the authenticated account. <c>GET /v1/user/profiles</c>.</summary>
    public async Task<IReadOnlyList<UserProfile>> GetProfilesAsync(CancellationToken cancellationToken = default) =>
        await _executor.GetAsync(
            "/v1/user/profiles",
            AdsefidJsonContext.Default.ResponseEnvelopeListUserProfile,
            cancellationToken).ConfigureAwait(false);

    /// <summary>Lists the authenticated account's SMS/Messenger templates, optionally filtered and paged. <c>GET /v1/user/templates</c>.</summary>
    /// <param name="state">Only return templates in this <see cref="TemplateState"/>. Omit to return templates in any state.</param>
    /// <param name="skip">Number of items to skip, for paging.</param>
    /// <param name="take">Maximum number of items to return (1-100). Omit for the API's default page size.</param>
    /// <exception cref="AdsefidValidationException"><paramref name="skip"/> is negative, or <paramref name="take"/> is outside 1-100.</exception>
    public async Task<GetUserTemplatesResponse> GetTemplatesAsync(
        TemplateState? state = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        if (skip is not null)
        {
            Validation.RequireInRange(skip.Value, 0, int.MaxValue, nameof(skip));
        }

        if (take is not null)
        {
            Validation.RequireInRange(take.Value, Limits.TemplatesTakeMin, Limits.TemplatesTakeMax, nameof(take));
        }

        var parts = new List<string>();
        if (state is not null)
        {
            parts.Add($"state={Uri.EscapeDataString(ToQueryValue(state.Value))}");
        }

        if (skip is not null)
        {
            parts.Add($"skip={skip.Value}");
        }

        if (take is not null)
        {
            parts.Add($"take={take.Value}");
        }

        var query = parts.Count == 0 ? string.Empty : $"?{string.Join('&', parts)}";
        return await _executor.GetAsync(
            $"/v1/user/templates{query}",
            AdsefidJsonContext.Default.ResponseEnvelopeGetUserTemplatesResponse,
            cancellationToken).ConfigureAwait(false);
    }

    private static string ToQueryValue(TemplateState state) => state switch
    {
        TemplateState.PendingApproval => "pendingapproval",
        TemplateState.Approved => "approved",
        TemplateState.Rejected => "rejected",
        _ => throw new AdsefidValidationException($"Unsupported template state '{state}'."),
    };
}
