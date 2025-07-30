using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HandballBackend.Tests.Controllers;

[TestClass]
public class GlobalSetup {
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext) {
        Directory.SetCurrentDirectory("../../../../HandballBackend/build/");

        Config.USING_POSTGRES = false;
        Config.SECRETS_FOLDER = @".\secrets";
        Config.RESOURCES_FOLDER = @".\resources\";
    }
}