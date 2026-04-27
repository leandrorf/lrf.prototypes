namespace lrfdev.auth.api.Authorization;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Escopos usados no demo de API protegida (alinhados ao seed do cliente M2M).
    /// </summary>
    public static readonly string[] DemoScopes =
    [
        "auth.read",
        "auth.manage",
        "orders.read",
        "orders.write"
    ];

    public static IServiceCollection AddScopeBasedAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var scope in DemoScopes)
            {
                var policyName = ScopeAuthorizationEvaluator.PolicyName(scope);
                options.AddPolicy(policyName, policy =>
                {
                    policy.RequireAssertion(ctx =>
                        ScopeAuthorizationEvaluator.HasScope(ctx.User, scope));
                });
            }
        });

        return services;
    }
}
