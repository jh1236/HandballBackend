using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HandballBackend.Tests.Controllers;

[TestClass]
public class GlobalSetup {
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext) {
        Directory.SetCurrentDirectory("../../../");

        Config.USING_POSTGRES = false;
        Config.SECRETS_FOLDER = @".\Config\secrets\";
        Config.RESOURCES_FOLDER = @".\HandballBackend\build\resources\";
    }
}