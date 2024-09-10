using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// OAuth extends our application


builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie")
    .AddOAuth("github", options => {
        // Это схема запускается только для callback path

        // связываем наш куки "cookie" с "github" 
        options.SignInScheme = "cookie";


        // OAuth это спецификация, которой мы должны следовать

        // Это Credentials нашего приложения
        options.ClientId = configuration.GetSection("ClientId").Value;
        options.ClientSecret = configuration.GetSection("ClientSecret").Value;

        // 1) куда перенаправляется запрос от нашего приложения
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        // на этом шаге мы проходим аутентификацию на github
        // github возвращает нам code по адресу, указанному Authorization callback URL
        // Code это как sessionId, по которому мы проходим аутентификацию. мы загружаем сессию по Code. Это выходит SessionId

        // 2) по какому адресу обменять Code на access_token
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";

        // 3) куда вернуть access_token
        options.CallbackPath = "/oauth/github-callback";
        options.SaveTokens = true;
        
        
        // 4) по access_token можно получить информация о пользователе.
        options.UserInformationEndpoint = "https://api.github.com/user";
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
    // 1) аутентификация в github
    // 2) возвращает Code 
    // 3) перенаправляет нас куда надо)
    return Results.Challenge(
        properties: new AuthenticationProperties() {
            RedirectUri = "https://localhost:7061",
        },
        authenticationSchemes: new List<string> { "github" });
});

app.Run();
