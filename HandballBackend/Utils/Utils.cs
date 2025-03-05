namespace HandballBackend.Utils;

public static class Utilities {
    public static Dictionary<string, dynamic?> WrapInDictionary(string key, dynamic? objectToWrap) {
        return new Dictionary<string, dynamic?> {{key, objectToWrap}};
    }
}