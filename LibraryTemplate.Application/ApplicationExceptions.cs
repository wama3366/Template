namespace LibraryTemplate.Application;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ForbiddenException(string message) : Exception(message);

public sealed class UnauthenticatedException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);
