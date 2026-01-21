using MediatR;
using Shared.Contracts.Responses;

namespace AuthService.API.Features.ConfirmUserEmail
{
    public record ConfirmUserEmailCommand(string UserId,string Token) : IRequest<string>;
  

//public sealed class ConfirmUserEmailCommand : IRequest<string>
//    {
//        public string UserId { get; set; } = null!;
//        public string Token { get; set; } = null!;
//    }

}
