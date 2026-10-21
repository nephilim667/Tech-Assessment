using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebApi.Domain.Common
{
    internal sealed class Result<T>
    {
        private readonly T? _value;
        private readonly Error? _error;

        private Result(bool isSuccess, T? value, Error? error)
        {
            IsSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        public bool IsSuccess { get; }

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                "Un résultat en échec ne possède pas de valeur.");

        public Error Error => !IsSuccess
            ? _error!
            : throw new InvalidOperationException(
                "Un résultat réussi ne possède pas d’erreur.");

        public static Result<T> Success(T value)  => new(true, value, null);

        public static Result<T> Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new(false, default, error);
        }
    }
}
