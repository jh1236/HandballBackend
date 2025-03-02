// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using HandballBackend.Models;

namespace HandballBackend.Database.SendableTypes;

public class PersonData {
    public string name { get; private set; }
    public string searchableName { get; private set; }
    public string imageUrl { get; private set; }
    public string bigImageUrl { get; private set; }

    public PersonData(Person person) {
        name = person.Name;
        searchableName = person.SearchableName;
        imageUrl = person.ImageUrl;
        bigImageUrl = person.ImageUrl; //TODO: fix this later
    }
}