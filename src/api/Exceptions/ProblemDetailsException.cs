using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace api.Exceptions
{
    public class ProblemDetailsException : Exception
    {
        public ProblemDetails ProblemDetails { get; init; }

        public ProblemDetailsException(ProblemDetails problemDetails)
        {
            ProblemDetails = problemDetails;
        }

        public ProblemDetailsException(HttpStatusCode statusCode, string title, string detail)
        {
            ProblemDetails = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = (int)statusCode
            };
        }
    }
}
