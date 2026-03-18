using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace identityserver.api.Swagger;

/// <summary>Configura o Swagger para enviar POST /connect/token e /connect/revoke como application/x-www-form-urlencoded com exemplos.</summary>
public class TokenEndpointOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("connect/", StringComparison.OrdinalIgnoreCase))
            return;

        if (context.ApiDescription.HttpMethod?.ToUpperInvariant() != "POST")
            return;

        // POST connect/token
        if (path.Equals("connect/token", StringComparison.OrdinalIgnoreCase))
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["application/x-www-form-urlencoded"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Required = new HashSet<string> { "grant_type", "client_id", "client_secret" },
                            Properties =
                            {
                                ["grant_type"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("authorization_code"), Description = "authorization_code ou refresh_token" },
                                ["code"] = new OpenApiSchema { Type = "string", Description = "Código recebido no redirect (quando grant_type=authorization_code)" },
                                ["redirect_uri"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("http://localhost:5235/connect/callback-demo"), Description = "Obrigatório para authorization_code; deve ser idêntico ao usado no /authorize" },
                                ["code_verifier"] = new OpenApiSchema { Type = "string", Description = "Obrigatório se usou code_challenge no /authorize (PKCE)" },
                                ["refresh_token"] = new OpenApiSchema { Type = "string", Description = "Para grant_type=refresh_token" },
                                ["client_id"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("meu-app") },
                                ["client_secret"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("secret") }
                            }
                        }
                    }
                }
            };
        }

        // POST connect/revoke
        if (path.Equals("connect/revoke", StringComparison.OrdinalIgnoreCase))
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["application/x-www-form-urlencoded"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["token"] = new OpenApiSchema { Type = "string", Description = "Refresh token a revogar" },
                                ["token_type_hint"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("refresh_token") },
                                ["client_id"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("meu-app") },
                                ["client_secret"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("secret") }
                            }
                        }
                    }
                }
            };
        }
    }
}
