namespace Chessero.Api.Services
{
    public class InvalidGameRequestException : Exception
    {
        public InvalidGameRequestException(string message)
            : base(message)
        {
        }
    }
}
