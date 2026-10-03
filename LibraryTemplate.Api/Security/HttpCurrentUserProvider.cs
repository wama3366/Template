using System.Security.Claims;
using LibraryTemplate.Application;

namespace LibraryTemplate.Api.Security;

public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public string ExternalUserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var subject = user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new UnauthenticatedException("Authenticated request is missing a subject claim.");
            }

            return subject;
        }
    }
}
