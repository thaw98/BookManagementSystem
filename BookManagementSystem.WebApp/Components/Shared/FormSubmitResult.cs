namespace WebApp.Components.Shared;

public readonly record struct FormSubmitResult(bool IsSuccess, string? Message = null);
