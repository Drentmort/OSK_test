namespace OskTech.IntegrationTests;

public class SolutionSmokeTests
{
    [Fact]
    public void Infrastructure_project_exists()
    {
        Assert.Contains("OskTech.Infrastructure", typeof(OskTech.Infrastructure.Persistence.IAssemblyMarker).Assembly.FullName!);
    }
}
