using System.IO;
using HandballBackend.Controllers;
using HandballBackend.Database.Models;
using HandballBackend.ErrorTypes;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HandballBackend.Tests.Controllers;

[TestClass]
[TestSubject(typeof(OfficialsController))]
public class OfficialsControllerTest {
    [ClassInitialize]
    public static void Initialize(TestContext testContext) {
        Directory.SetCurrentDirectory("../../../");

        Config.USING_POSTGRES = false;
        Config.SECRETS_FOLDER = @".\Config\secrets";
        Config.RESOURCES_FOLDER = @"..\HandballBackend\resources\";
    }

    [TestInitialize]
    public void Setup() {
        var db = new HandballContext();
        db.Database.EnsureCreated();
        var personOne = new Person {
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
        db.People.Add(personOne);
        var personTwo = new Person {
            Name = "Bar",
            SearchableName = "bar",
            Password = null,
            ImageUrl = "/a/fake/url",
            BigImageUrl = null,
            SessionToken = null,
            TokenTimeout = null,
            PermissionLevel = 0,
            PhoneNumber = null,
            Availability = null
        };
        db.People.Add(personTwo);
        db.SaveChanges();
        db.Officials.Add(new Official {
            PersonId = personOne.Id,
            Proficiency = 2
        });
        db.Officials.Add(new Official {
            PersonId = personTwo.Id,
            Proficiency = 5
        });
        db.SaveChanges();
    }


    [TestCleanup]
    public void TearDown() {
        var db = new HandballContext();
        db.Database.EnsureDeleted();
    }

    [TestMethod]
    public void TestGetOfficial() {
        var controller = new OfficialsController();
        OfficialsController.GetOfficialResponse response = controller.GetOneOfficial("foo").Value;
        Assert.IsNotNull(response);
        Assert.AreEqual("Foo", response.Official.Name);
        Assert.AreEqual("foo", response.Official.SearchableName);
        Assert.AreEqual($"{Config.MY_ADDRESS}/a/fake/url", response.Official.ImageUrl);
    }

    [TestMethod]
    public void TestTestGetOfficialBadTournamentName() {
        var controller = new OfficialsController();
        var response = controller.GetOneOfficial("foo", "a_name_not_existing").Result;
        Assert.IsNotNull(response);
        var actual = response as NotFoundObjectResult;
        Assert.IsNotNull(actual);
        Assert.AreEqual(404, actual.StatusCode);
        Assert.IsInstanceOfType<InvalidTournament>(actual.Value);
    }

    [TestMethod]
    public void TestTestGetOfficialBadOfficialName() {
        var controller = new OfficialsController();
        var response = controller.GetOneOfficial("a_name_not_existing").Result;
        var actual = response as NotFoundObjectResult;
        Assert.IsNotNull(actual);
        Assert.AreEqual(404, actual.StatusCode);
    }
}