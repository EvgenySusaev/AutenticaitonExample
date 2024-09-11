using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
// OAuth extends our application


builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie")
    .AddOAuth("github", options => {
        // связываем наш куки "cookie" с "github"
        // указывает, что после успешной аутентификации через GitHub полученные утверждения (информация о пользователе) должны сохраняться в схеме куки.
        // Таким образом, OAuth используется только для начального входа, а для управления сессией используются куки.
        options.SignInScheme = "cookie";


        // OAuth это спецификация, которой мы должны следовать

        // Это Credentials для github
        options.ClientId = configuration.GetSection("ClientId").Value;
        options.ClientSecret = configuration.GetSection("ClientSecret").Value;

        // 1) куда перенаправляется запрос от нашего приложения
        // перенаправляет пользователя на страницу авторизации GitHub (https://github.com/login/oauth/authorize)
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        
        // на этом шаге мы проходим аутентификацию на github

        // 2) куда вернуть access_token
        // Пользователь перенаправляется на GitHub, где проходит аутентификацию и предоставляет разрешения приложению.
        // Затем GitHub возвращает временный Code на адрес обратного вызова вашего приложения (/oauth/github-callback).
        options.CallbackPath = "/oauth/github-callback";
        
        // 3) по какому адресу обменять Code на access_token
        // Приложение затем обменивает этот код на токен доступа, отправляя запрос на конечную точку токенов GitHub (https://github.com/login/oauth/access_token)
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        
        options.SaveTokens = true;
        
        
        // 4) по access_token можно получить информация о пользователе.
        // После получения токена доступа приложение использует его для получения информации о пользователе через API GitHub (https://api.github.com/user).
        // Полученная информация обрабатывается, и для пользователя создаются утверждения (claims), которые сохраняются в куки
        options.UserInformationEndpoint = "https://api.github.com/user";
        
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
        options.ClaimActions.MapJsonKey("sub", "id");
        
        options.Events.OnCreatingTicket = async (context) => {
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
            using var result = await context.Backchannel.SendAsync(request);
            var user = await result.Content.ReadFromJsonAsync<JsonElement>();
            context.RunClaimActions(user);
        };
    });

var app = builder.Build();


app.UseAuthentication();


app.MapGet("/", (HttpContext ctx) => {
    ctx.GetTokenAsync("access_token");
    return ctx.User.Claims.Select(x => new { x.Type, x.Value }).ToList();
});

app.MapGet("/login", () => {
    // START здесь - инициация 
    // 1) Пользователь обращается к /login → начинается OAuth процесс → перенаправление на GitHub.
    return Results.Challenge(
        properties: new AuthenticationProperties() {
            RedirectUri = "https://localhost:7061",
        },
        authenticationSchemes: new List<string> { "github" });
});

app.Run();
