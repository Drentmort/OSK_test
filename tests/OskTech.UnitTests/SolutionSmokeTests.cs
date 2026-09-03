namespace OskTech.UnitTests;

public class SolutionSmokeTests
{
    [Fact]
    public void Application_project_exists()
    {
        Assert.Contains("OskTech.Application", typeof(OskTech.Application.Interfaces.IAssemblyMarker).Assembly.FullName!);
    }
}
