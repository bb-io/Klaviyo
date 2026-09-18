using Apps.Klaviyo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;
using System.Net;

namespace Apps.Klaviyo.Connections;

public class ConnectionValidator(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new KlaviyoClient(authProviders);
            var request = new RestRequest("translations", Method.Get)
                .AddQueryParameter("page[size]", "1");
            var response = await client.ExecuteAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new()
                {
                    IsValid = false,
                    Message = "Unauthorized. Please check your private API key and its scopes."
                };
            }

            return new()
            {
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            InvocationContext.Logger?.LogError(
                $"[KlaviyoConnectionValidator] Exception occurred while validating connection: {ex.Message}", []);

            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}
