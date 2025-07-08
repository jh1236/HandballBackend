using System.IO;
using System.Linq;
using HandballBackend.Controllers;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HandballBackend.Tests.Controllers;

[TestClass]
[TestSubject(typeof(OfficialsController))]
public class OfficialsControllerTest {
    [TestInitialize]
    public void Setup() {
        Directory.SetCurrentDirectory("../../../");

        Config.USING_POSTGRES = false;
        Config.SECRETS_FOLDER = @".\Config\secrets";
        Config.RESOURCES_FOLDER = @"..\HandballBackend\resources\";
        var db = new HandballContext();
        db.Database.EnsureCreated();
    }

    [TestCleanup]
    public void TearDown() {
        var db = new HandballContext();
        db.Database.EnsureDeleted();
    }

    [TestMethod]
    public void TestGetOfficial() {
        var db = new HandballContext();
        var person = new Person {
            Name = "Foo",
            SearchableName = "foo",
            Password = null,
            ImageUrl = "/a/fake/url",
            BigImageUrl = null,
            SessionToken = null,
            TokenTimeout = null,
            PermissionLevel = 0,
            PhoneNumber = null,
            Availability = null
        };
        db.People.Add(person);
        db.SaveChanges();
        db.Officials.Add(new Official {
            PersonId = person.Id,
            Proficiency = 0,
            CreatedAt = 0,
            Person = null,
            Games = null
        });
        db.SaveChanges();
        var controller = new OfficialsController();
        OfficialsController.GetOfficialResponse actualController = controller.GetSingleOfficial("foo").Value;
        Assert.IsNotNull(actualController);
        Assert.AreEqual("Foo", actualController.Official.Name);
        Assert.AreEqual("foo", actualController.Official.SearchableName);
        Assert.AreEqual($"{Config.MY_ADDRESS}/a/fake/url", actualController.Official.ImageUrl);
    }
}