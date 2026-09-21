using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Klaviyo.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups =>
    [
        new()
        {
            Name = "Private API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties =
            [
                new(CredsNames.ApiKey)
                {
                    DisplayName = "Private API key",
                    Sensitive = true
                }
            ]
        }
    ];

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values) => values
        .Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value))
        .ToList();
}
