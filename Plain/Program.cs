using System.Collections.Concurrent;
using System.Text;
using _1.PlainCookie;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

var savedSessions = new ConcurrentDictionary<int, Session>();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDataProtection();
builder.Services.AddSingleton<Database>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();


var app = builder.Build();

app.MapGet("/signup", async (HttpContext httpContext) => {
    httpContext.Response.ContentType = "text/html";

    var response = @"
    <html>
    <body>
            <center>
                    <fieldset>
                            <legend>Registration</legend>
                            <form method='post' action='/signup'>
                                    <input type='text' name='name' placeholder='Identity credential: User name' required />
</br>
                                    <input type='password' name='password' placeholder='Identity credential: Password' required />
</br>
                                    <label for='title'>Choose a job title:</label>
                                    <select name='title' id='title'>
                                                <option value='cto'>CTO</option>
                                                <option value='devops'>DevOps</option>
                                                <option value='programmer'>Programmer</option>
                                                <option value='tester'>Tester</option>
                                    </select>
<br>

                                    <label for='skills'>Rate your skill level:</label>
                                        <select name='skills' id='skills'>
                                            <option value='Beginner'>1 - Beginner</option>
                                            <option value='Novice'>2 - Novice</option>
                                            <option value='Intermediate'>3 - Intermediate</option>
                                            <option value='Advanced'>4 - Advanced</option>
                                            <option value='Expert'>5 - Expert</option>
                                    </select>

                                    <button type='submit'>Register</button>
                            </form>
                    </fieldset>
            </center>
    </body>
    </html>";

    await httpContext.Response.WriteAsync(response);
});

app.MapPost("/signup", async (HttpContext httpContext, IPasswordHasher<User> hasher, Database db) => {
    var form = httpContext.Request.Form;
    var nameCredential = form["name"].ToString();
    var passwordCredential = form["password"].ToString();
    var personTitleClaim = form["title"].ToString();
    var skillsClaim = form["skills"].ToString();


    var user = new User() { Username = nameCredential };
    user.Password = hasher.HashPassword(user, passwordCredential);
    var titleClaim = new UserClaim(type: "title", value: personTitleClaim);
    var skillClaim = new UserClaim(type: "skills", value: skillsClaim);
    user.Claims.Add(titleClaim);
    user.Claims.Add(skillClaim);

    await db.PutAsync(user);

    Random random = new Random();
    int randomNumber = random.Next(1, 7);
    var session = new Session(randomNumber, user.Claims);
    savedSessions.TryAdd(session.Id, session);

    httpContext.Response.Headers.SetCookie = ConversionScheme.ClaimsToCookieString(session, user);
    httpContext.Response.Redirect("/user");
});


app.MapGet("/signin", async (HttpContext httpContext) => {
    httpContext.Response.ContentType = "text/html";
    var response = @"
    <html>
    <body>
            <center>
                    <fieldset>
                            <legend>Sign in</legend>
                            <form method='post' action='/signin'>
                                    <input type='text' name='name' placeholder='Identity credential: User name' required /></br>
                                    <input type='password' name='password' placeholder='Identity credential: Password' required /></br>
                                    <button type='submit'>Sign in</button>
                            </form>
                    </fieldset>
            </center>
    </body>
    </html>";
    await httpContext.Response.WriteAsync(response);
});

app.MapPost("/signin", async (HttpContext httpContext, IPasswordHasher<User> hasher, Database db) => {
    var form = httpContext.Request.Form;
    var providedNameCredential = form["name"].ToString();
    var providedPasswordCredential = form["password"].ToString();


    var user = await db.GetUserAsync(providedNameCredential);
    if (user == null || hasher.VerifyHashedPassword(user, user.Password, providedPasswordCredential) !=
        PasswordVerificationResult.Success) {
        await httpContext.Response.WriteAsync("Invalid credentials");
        return;
    }

    var random = new Random();
    var randomNumber = random.Next(1, 7);
    var session = new Session(randomNumber, user.Claims);
    savedSessions.TryAdd(session.Id, session);

    httpContext.Response.Headers.SetCookie = ConversionScheme.ClaimsToCookieString(session, user);
    httpContext.Response.Redirect("/task-list");
});

app.MapGet("/signout", async (HttpContext httpContext) => {
    await httpContext.SignOutAsync("cookie");
    httpContext.Response.Headers.SetCookie = "auth=";
    httpContext.Response.Redirect("/");
});

// 2. Теперь браузер по нашей просьбе устанавливает куки в каждый запрос, которые мы может читать.

app.MapGet("/user", async (HttpContext httpContext) => {
    // Check if the authentication cookie is present
    var authCookie = httpContext.Request.Headers.Cookie.FirstOrDefault(x => x.StartsWith("auth="));
    if (authCookie != null) {
        // Try to convert the cookie back to claims
        var claims = ConversionScheme.CookieStringToClaims(authCookie);

        if (claims.Any()) {
            // Convert the first claim type to an integer key
            var key = Convert.ToInt32(claims[0].Value);

            // Check if the session exists in the saved sessions
            if (savedSessions.TryGetValue(key, out var session)) {
                if (session.IsExpired) {
                    savedSessions.TryRemove(key, out _);
                    httpContext.Response.Redirect("/signin");
                    return "";
                }

                var claimDetails = session.Claims.Select(c => $"{c.Type}:{c.Value};");
                return $"User's {claims[0].Type}: {claims[0].Value}; {string.Join(", ", claimDetails)}";
            }
        }
    }

    httpContext.Response.Redirect("/signin");
    return "";
});

app.MapGet("/", async httpContext => {
    var authCookie = httpContext.Request.Headers.Cookie.FirstOrDefault(x => x.StartsWith("auth="));


    if (authCookie != null) {
        var payload = authCookie.Split("=").Last();
        var parts = payload.Split(':');
        var keyClaim = parts[0];
        await httpContext.Response.WriteAsync($"Hello, user!");
    }
    else {
        await httpContext.Response.WriteAsync("Hello, guest!");
    }
});


app.MapGet("/task-list", async (httpContext) => {
    httpContext.Response.ContentType = "text/html";
    var response = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>List of Routes</title>
</head>
<body>
    <h1>Available Routes</h1>
    <ul>
        <li><a href='/push-to-master-branch'>push to master branch</a></li>
        <li><a href='/write-complex-system'>write complex system</a></li>
        <li><a href='/config-pipeline'> config pipeline</a></li>
        <li><a href='/test-system'>test system</a></li>
    </ul>
</body>
</html>";
    await httpContext.Response.WriteAsync(response);
});
app.MapGet("/push-to-master-branch", (HttpContext httpContext) => {
    // Get the authentication cookie (or other means of identifying the user)
    var authCookie = httpContext.Request.Headers.Cookie.FirstOrDefault(x => x.StartsWith("auth="));

    if (authCookie != null) {
        // Convert the cookie back to claims
        var claims = ConversionScheme.CookieStringToClaims(authCookie);
        
        if (claims.Any()) {
            // Convert the first claim type to an integer key
            var key = Convert.ToInt32(claims[0].Value);

            // Check if the session exists in the saved sessions
            if (savedSessions.TryGetValue(key, out var session)) {
                if (!session.IsExpired) {
                    // Check if user has the required claims: title = programmer and skills = Expert
                    var titleClaim = session.Claims.FirstOrDefault(c => c.Type == "title" && c.Value == "programmer");
                    var skillsClaim = session.Claims.FirstOrDefault(c => c.Type == "skills" && c.Value == "Expert");

                    if (titleClaim != null && skillsClaim != null) {
                        // User has the correct claims, proceed with the action
                        return "Access granted. You can push to the master branch.";
                    }
                    
                    savedSessions.TryRemove(key, out _);

                    var details = session.Claims.Select(c => $"{c.Type}:{c.Value};");
                    return $"Unauthorized: {details}";
                }

                var claimDetails = session.Claims.Select(c => $"{c.Type}:{c.Value};");
                return $"User's {claims[0].Type}: {claims[0].Value}; {string.Join(", ", claimDetails)}";
            }
        }
    }

    // If user does not have the required claims, deny access
    return "Unauthorized";
});

app.MapGet("/write-complex-system", () => "");
app.MapGet("/config-pipeline", () => "");
app.MapGet("/test-system", () => "");

app.MapGet("/promote", () => { });


app.Run();

public class Session(int id, List<UserClaim> claims) {
    public int Id { get; set; } = id;
    public List<UserClaim> Claims { get; set; } = claims;
    public DateTime Expiration { get; set; } = DateTime.Now.AddMinutes(10);

    public bool IsExpired => DateTime.Now > Expiration;
}

public class ConversionScheme {
    public static string ClaimsToCookieString(Session session, User user) {
        var sb = new StringBuilder();

        sb.Append("auth=");
        sb.Append($"sessionId:{session.Id};");
        foreach (var claim in user.Claims) {
            sb.Append($"{claim.Type}:{claim.Value};");
        }

        sb.Append($"exp:{session.Expiration:yyyy-MM-dd HH-mm-ss};");

        return sb.ToString();
    }

    public static List<UserClaim> CookieStringToClaims(string str) {
        var payload = str.Split("=").Last();
        var parts = payload.Split(';');

        var claims = new List<UserClaim>();
        foreach (var part in parts) {
            var claim = part.Split(':');
            var keyClaim = claim[0];
            var valueClaim = claim[1];

            claims.Add(new UserClaim(keyClaim, valueClaim));
        }

        return claims;
    }

    // public static string ClaimsToCookieString(Session session, User user) {
    //     var sb = new StringBuilder();
    //
    //     sb.Append("auth=");
    //     sb.Append($"sessionId={session.Id}");
    //     foreach (var claim in user.Claims) {
    //         sb.Append($"{claim.Type}:{claim.Value};");
    //     }
    //
    //     sb.Append($"exp:{session.Expiration:yyyy-MM-dd HH:mm:ss};");
    //
    //     return sb.ToString();
    // }
    //
    // public static List<UserClaim> CookieStringToClaims(string str) {
    //     var payload = str.Split("=").Last();
    //     var parts = payload.Split(';');
    //
    //     var claims = new List<UserClaim>();
    //     foreach (var part in parts) {
    //         var claim = part.Split(':');
    //         var keyClaim = claim[0];
    //         var valueClaim = claim[1];
    //
    //         claims.Add(new UserClaim(keyClaim, valueClaim));
    //     }
    //
    //     return claims;
    // }
}

public class User {
    public string Username { get; set; }
    public string Password { get; set; }
    public List<UserClaim> Claims { get; set; } = new();
}

public class UserClaim(string type, string value) {
    public string Type { get; set; } = type;
    public string Value { get; set; } = value;
}