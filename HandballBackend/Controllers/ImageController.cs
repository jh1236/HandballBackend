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


    [HttpPost("people/upload")]
    [Authorize(Policy = Policies.IsAdmin)]
    public IActionResult Post(List<IFormFile> file) {
        var size = file.Sum(f => f.Length);
        var db = new HandballContext();
        Console.WriteLine($"FileName: {file.First().FileName}");
        // full path to file in temp location
        foreach (var formFile in file.Where(formFile => formFile.Length > 0)) {
            var image = ImageHelper.SaveImageWithCircle(formFile.OpenReadStream(), formFile.FileName);
            var single = db.People.Single(p => p.SearchableName == formFile.FileName);
            single.ImageUrl = image;
            single.BigImageUrl = $"{image}&big=true";
        }

        // process uploaded file
        // Don't rely on or trust the FileName property without validation.
        db.SaveChanges();
        return Ok(new {count = file.Count, size});
    }
}