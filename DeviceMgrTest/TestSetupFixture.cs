namespace RJCP.IO.Device
{
    // This file is only for .NET Core

    using NUnit.Framework;

    [SetUpFixture]
    public class TestSetupFixture
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            GlobalLogger.Initialize();
        }
    }
}
