namespace Adsefid.Sdk.Enums;

/// <summary>
/// Error codes the adsefid.com API returns in <c>error.code</c> on a failed request (surfaced via
/// <see cref="Adsefid.Sdk.Exceptions.AdsefidApiException.Code"/>). An unrecognized numeric code from the API is
/// cast to this enum without throwing, so a value outside the named members here is possible as the
/// API adds new codes over time.
/// </summary>
public enum WebServiceResponseCode
{
    InternalError = 2000,
    InvalidPlan = 2001,
    LineNotFound = 2002,
    TooManyReceptors = 2003,
    InvalidLine = 2004,
    InvalidApiKey = 2005,
    IpNotAllowed = 2006,
    DuplicateLocalId = 2007,
    UserInformationNotFound = 2008,
    EmptyReceptors = 2009,
    InvalidReceptors = 2010,
    EmptyBody = 2011,
    EmptyLine = 2012,
    EmptyMessage = 2013,
    InvalidReceptor = 2014,
    EmptyReceptor = 2015,
    MessageTooLarge = 2016,
    InvalidLineSelector = 2017,
    Unauthorized = 2018,
    InvalidSendRange = 2019,
    AllReceptorsBlacklisted = 2020,
    MessageContainsForbiddenWords = 2021,
    NotEnoughCredit = 2022,
    DuplicateTag = 2023,
    InvalidParameter = 2024,
    ReceptorBlacklisted = 2025,
    InvalidLinkInMessage = 2026,
    TemplateNotApproved = 2027,
    InvalidTemplateParameter = 2028,
    InvalidLocalIds = 2029,
    EmptyLocalIds = 2030,
    EmptyMessageIds = 2031,
    InvalidSmsType = 2032,
    LineNotActive = 2033,
    LineExpired = 2034,
    MessageLimitReached = 2035,
    RequestLimitReached = 2036,
    InvalidSendTime = 2037,
    InvalidExpiry = 2038,
    InvalidTemplateId = 2039,
    ProfileNotFound = 2040,
    ProfileExpired = 2041,
    FileNotFound = 2042,
    InvalidFile = 2043,
    AccessDenied = 2044,
    Rejected = 2045,
}
