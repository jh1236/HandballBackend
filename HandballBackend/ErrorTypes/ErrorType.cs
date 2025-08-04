using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace HandballBackend.ErrorTypes;

public abstract class ErrorType : ValidationProblemDetails {
    protected ErrorType(string title, string message, int code = 400) {
        Detail = message;
        Instance = "add extension here";
        Status = code;
        Title = title;
        Type = "add type here";
    }
}