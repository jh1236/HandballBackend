using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Http.Headers;
using HandballBackend.Authentication;
using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Authorization;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/")]
public class ImageController : ControllerBase {
    // GET api/values
    [HttpGet("image")]
    public IActionResult Get([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "");
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }

    [HttpGet("people/image")]
    public IActionResult GetPeople([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "") + "users/";
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }

    [HttpGet("tournaments/image")]
    public IActionResult GetTournaments([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "") + "tournaments/";
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }

    [HttpGet("teams/image")]
    public IActionResult GetTeams([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "") + "teams/";
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }


    //Set the method to be a Http POST method (meaning that it has a body)
    [HttpPost("people/upload")]
    //Set the method to only be usable as an Admin
    [Authorize(Policy = Policies.IsAdmin)]
    public IActionResult Post(List<IFormFile> file) {
        // Handball Contexts are used to access the db
        var db = new HandballContext();
        if (file.Count != 1) {
            //when we receive a file it's a list for some reason; we only want 1 file
            return BadRequest("Only one image is allowed");
        }

        var formFile = file.First();
        //do some voodoo shit on the image to make it circle; also saves it.
        var image = ImageHelper.SavePlayerImageWithCircle(formFile.OpenReadStream(), formFile.FileName);
        // get the person by searchable name
        var person = db.People.Single(p => p.SearchableName == formFile.FileName);
        // set their image paths
        person.ImageUrl = image;
        person.BigImageUrl = $"{image}&big=true";


        // Save the changes (duh.)
        db.SaveChanges();
        return Ok();
    }
}