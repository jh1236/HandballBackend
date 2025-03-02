// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 

namespace HandballBackend.Models.SendableTypes;

public record PersonData {
    /*
     *  name: "str",
     *  searchableName: "str",
     *  imageUrl: "str",
     *  bigImageUrl: "str"
     */

    public string name;
    public string searchableName;
    public string imageUrl;
    public string bigImageUrl;

    public PersonData(Person person) {
        name = person.Name;
        searchableName = person.SearchableName;
        imageUrl = person.ImageUrl;
        bigImageUrl = person.ImageUrl; //TODO: fix this later
    }
}