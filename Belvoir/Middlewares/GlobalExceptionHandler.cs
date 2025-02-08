﻿using Belvoir.DAL.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace Belvoir.Middlewares
{
    public class GlobalExceptionHandler : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Response<object> response;
                if (ex is MySqlException exc)
                {
                    response = new Response<object>
                    {
                        StatusCode = 500,
                        Message = "An database error occurred.",
                        Error = ex.Message,
                        Data = null
                    };
                }
                else
                {
                    response = new Response<object>
                    {
                        StatusCode = 500,
                        Message = "An unexpected error occurred.",
                        Error = ex.Message,
                        Data = null
                    };
                }
                

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var responseJson = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(responseJson);

            }
            
        }
    }
}
