using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;

namespace Tests.Klaviyo.Base;
public class TestBase
{
    protected IConfigurationRoot Configuration { get; }

    public IEnumerable<AuthenticationCredentialsProvider> Creds { get; set; }

    public InvocationContext InvocationContext { get; set; }

    public FileManager FileManager { get; set; }

    public TestBase()
    {
        Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        Creds = Configuration.GetSection("ConnectionDefinition").GetChildren()
            .Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();


        var relativePath = Configuration.GetSection("TestFolder").Value;
        var projectDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
        var folderLocation = Path.Combine(projectDirectory, relativePath);

        InvocationContext = new InvocationContext
        {
            AuthenticationCredentialsProviders = Creds,
        };

        FileManager = new FileManager();
    }
}
