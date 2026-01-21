using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Responses
{
   
    public sealed class ApiError
    {
        public string Code { get; init; }
        public string Description { get; init; }

        public ApiError(string code, string description)
        {
            Code = code;
            Description = description;
        }
    }

}
