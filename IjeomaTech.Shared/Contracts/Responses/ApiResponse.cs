using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Responses
{
    
    public class ApiResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        public int StatusCode { get; init; }
        public string? CorrelationId { get; init; }
        public IEnumerable<ApiError>? Errors { get; init; }

        protected ApiResponse(bool success,string message,int statusCode,
            IEnumerable<ApiError>? errors = null,string? correlationId = null)
        {
            Success = success;
            Message = message;
            StatusCode = statusCode;
            Errors = errors;
            CorrelationId = correlationId;
        }

        public static ApiResponse Created(string message = "Resource created", string? correlationId = null)
    => new(true, message, StatusCodes.Status201Created, correlationId: correlationId);

        public static ApiResponse Ok(string message = "Request successful", string? correlationId = null)
      => new(true, message, StatusCodes.Status200OK, correlationId: correlationId);


       public static ApiResponse Fail(string message,int statusCode,IEnumerable<ApiError>? errors = null,string? correlationId = null)
         => new(false, message, statusCode, errors, correlationId);
    }



    public class ApiResponse<T> : ApiResponse where T: class
    {
        public T? Data { get; init; }

        private ApiResponse(bool success,string message,int statusCode, T? data = default,
            IEnumerable<ApiError>? errors = null, string? correlationId = null)
            : base(success, message, statusCode, errors, correlationId)
        {
            Data = data;
        }

        public static ApiResponse<T> Ok(T data,string message = "Request successful", string? correlationId = null)
    => new(true, message, StatusCodes.Status200OK, data, correlationId: correlationId);

        public static ApiResponse<T> Created(T data,string message = "Resource created",string? correlationId = null)
    => new(true, message, StatusCodes.Status201Created, data, correlationId: correlationId);

        public static ApiResponse<T> Fail(string message,int statusCode,IEnumerable<ApiError>? errors = null, string? correlationId=null)
            => new(false, message, statusCode, default, errors,correlationId);
    }
}
