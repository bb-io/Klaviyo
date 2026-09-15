using Apps.Klaviyo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Connections;

public class ConnectionValidator(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new KlaviyoClient(authenticationCredentialsProviders);
            var request = new RestRequest("translations", Method.Get)
                .AddQueryParameter("page[size]", "1");

            cancellationToken.ThrowIfCancellationRequested();
            await client.ExecuteWithErrorHandling(request);
            cancellationToken.ThrowIfCancellationRequested();

            return new()
            {
                IsValid = true,
                Message = "Connection is valid."
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            InvocationContext.Logger?.LogError($"Connection validation failed: {ex.Message}", []);

            return new()
            {
                IsValid = false,
                Message = $"Connection validation failed. {ex.Message}"
            };
        }
    }
}
