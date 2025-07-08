using System.IO;
using System.Security.Claims;
using HandballBackend.Controllers;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HandballBackend.Tests.Controllers;

[TestClass]
[TestSubject(typeof(PlayersController))]
public class PlayersControllerTest {
    [TestInitialize]
    public void Setup() {
        Directory.SetCurrentDirectory("../../../");

        Config.USING_POSTGRES = false;
        Config.SECRETS_FOLDER = @".\Config\secrets";
        Config.RESOURCES_FOLDER = @"..\HandballBackend\resources\";
        var db = new HandballContext();
        db.Database.EnsureCreated();
        db.Tournaments.Add(new Tournament {
            Name = "The Test Tournament",
            SearchableName = "test",
            Editable = false,
            FixturesType = "RoundRobin",
            FinalsType = "BasicFinals",
            Ranked = true,
            TwoCourts = true,
            Finished = false,
            InFinals = false,
            HasScorer = true,
            TextAlerts = false,
            IsPooled = false,
            Notes = "These are the tournament notes!",
            ImageUrl = "/tournament/image",
            BadmintonServes = true
        });
        db.People.Add(new Person {
            Name = "Fooseph Barionette",
            SearchableName = "foo_bar",
            Password = null,
            ImageUrl = "/a/local/image",
            BigImageUrl = "/a/local/image?big=true",
            PermissionLevel = 2,
        });
        var personOne = new Person {
            Name = "Digby Test",
            SearchableName = "digby_test",
            Password = null,
            ImageUrl = "/a/different/local/image",
            BigImageUrl = "/a/different/local/image?big=true",
            PermissionLevel = 2,
        };
        var personTwo = new Person {
            Name = "Charlie Walters",
            SearchableName = "charlie_walters",
            Password = null,
            ImageUrl =
                "https://scontent.fper10-1.fna.fbcdn.net/v/t1.6435-9/159559563_1348265148862883_366387183636859896_n.jpg?_nc_cat=104&ccb=1-7&_nc_sid=0b6b33&_nc_ohc=K8qmireMGj4Q7kNvwHRhYNP&_nc_oc=AdlEG6AQGwztfey-xZkTmb_xNcJq2ZMArouac4_3y5Nl4JdKYiaeM194ctRSv8GHJdo&_nc_zt=23&_nc_ht=scontent.fper10-1.fna&_nc_gid=a377dmAsw09kmiD3OkjZ6A&oh=00_AfQxp5wXLw7uaYvErZ_MRI8LAYIpeLkOdNquWYH2XxJEPA&oe=68941AD4",
            PermissionLevel = 0,
        };
        db.People.Add(personOne);
        db.People.Add(personTwo);
        db.SaveChanges();
        db.Teams.Add(new Team {
            Name = "Lovers",
            SearchableName = "lovers",
            ImageUrl = "/a/local/image",
            CaptainId = personOne.Id,
            NonCaptainId = personTwo.Id,
            SubstituteId = null,
            TeamColor = null,
            BigImageUrl = null,
        });
    }

    [TestCleanup]
    public void TearDown() {
        var db = new HandballContext();
        db.Database.EnsureDeleted();
    }

    [TestMethod]
    public void TestGetPlayer() {
        var authService = new Mock<IAuthorizationService>();
        authService.Setup(auth => auth.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()).Result)
            .Returns(AuthorizationResult.Success);
        var controller = new PlayersController(authService.Object);
        var result = controller.GetSingle("foo_bar").Result.Value;
        Assert.IsNotNull(result);
        Assert.AreEqual("Fooseph Barionette", result.Player.Name);
    }
}