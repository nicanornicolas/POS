namespace Application.Exceptions;

public class TransactionNotFoundException(string message) : Exception(message);

public class InvalidTransactionStateException(string message) : Exception(message);

public class ProviderOperationFailedException(string message) : Exception(message);

public class AmountMismatchException(string message) : Exception(message);
