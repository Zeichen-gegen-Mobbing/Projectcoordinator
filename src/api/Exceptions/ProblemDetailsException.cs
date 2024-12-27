using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace api.Exceptions
{
    internal class ProblemDetailsException : Exception
    {
        internal ProblemDetails ProblemDetails { get; init; }

        internal ProblemDetailsException(ProblemDetails problemDetails)
        {
            ProblemDetails = problemDetails;
        }

        internal ProblemDetailsException( HttpStatusCode statusCode, string title, string detail)
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
