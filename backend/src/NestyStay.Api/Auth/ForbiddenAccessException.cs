namespace NestyStay.Api.Auth;

public sealed class ForbiddenAccessException(string message) : Exception(message);
