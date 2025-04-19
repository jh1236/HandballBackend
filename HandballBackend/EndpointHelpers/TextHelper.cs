using HandballBackend.Database.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace HandballBackend.EndpointHelpers;

public static class TextHelper {
    private static string UserName() {
        return File.ReadAllText(@".\Secrets\TwilioAccount.txt");
    }
    private static string Key() {
        return File.ReadAllText(@".\Secrets\TwilioKey.txt");
    }

    private static bool _hasBeenSetup = false;

    public static void Setup() {
        if (_hasBeenSetup) return;
        _hasBeenSetup = true;
        TwilioClient.Init("", Key());
    }


    public static async Task<bool> Text(Person target, string msg) {
        if (true) {
            Console.WriteLine($"Texting to {target.Name} ({target.PhoneNumber ?? "No Number"}): {msg}");
            await Task.Delay(2000);
            return true;
        }
        Setup();
        var targetPhoneNumber = target.PhoneNumber;
        if (targetPhoneNumber == null) return false;
        var m = await MessageResource.CreateAsync(
            new PhoneNumber(targetPhoneNumber),
            from: new PhoneNumber("+14093592698"),
            body: msg
        );
        return true;
    }
}