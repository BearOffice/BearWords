using BearMarkupLanguage;
using BearWordsAPI;
using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.RequestHandler;
using BearWordsAPI.Services;
using BearWordsAPI.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddSingleton(_ => new ConfigService(new BearML("server_configs.txt")));
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
builder.Services.AddSingleton<IUUIDGenerator, UUIDGenerator>();

builder.Services.AddDbContext<BearWordsContext>((serviceProvider, options) =>
    {
        var config = serviceProvider.GetRequiredService<ConfigService>();
        options.UseSqlite($"Data Source={config.Database}");
    });

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<ConfigService>((options, configService) =>
    {
        var key = Encoding.UTF8.GetBytes(configService.IssuerKey);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "bear_auth",
            ValidAudience = "bear_words",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("UserPolicy", policy => policy.RequireRole("User"));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BearWordsContext>();
    db.Database.EnsureCreated();

    var config = scope.ServiceProvider.GetRequiredService<ConfigService>();
    var dateTimeService = scope.ServiceProvider.GetRequiredService<IDateTimeService>();

    foreach (var user in config.UserNames)
    {
        if (await db.Users.SingleOrDefaultAsync(item => item.UserName == user) is null)
        {
            var userEntity = new User()
            {
                UserName = user,
                CreatedAt = dateTimeService.GetCurrentTicksLong(),
            };
            await db.Users.AddAsync(userEntity);
        }
    }
    await db.SaveChangesAsync();
}


app.MapPost("/login", RootHandler.PostLogin);
app.MapPost("/register", RootHandler.PostRegister).RequireAuthorization("UserPolicy");
app.MapPost("/re-register", RootHandler.PostReregister).RequireAuthorization("UserPolicy");

var syncGroup = app.MapGroup("/syncs/{clientId}")
                   .RequireAuthorization("UserPolicy")
                   .AddEndpointFilter<ClientValidationFilter>();

syncGroup.MapPost("/pull", SyncsHandler.PostSyncPull);
syncGroup.MapPost("/push", SyncsHandler.PostSyncPush);
syncGroup.MapGet("/status", SyncsHandler.GetSyncStatus);
syncGroup.MapGet("/server-time", SyncsHandler.GetSyncServerTime);


var conflictGroup = app.MapGroup("/conflicts")
                       .RequireAuthorization("UserPolicy")
                       .AddEndpointFilter<ClientValidationFilter>();

conflictGroup.MapGet("", ConflictsHandler.GetConflicts);
conflictGroup.MapPost("{clientId}/pull", ConflictsHandler.PostConflictsPull);
conflictGroup.MapPost("{clientId}/push", ConflictsHandler.PostConflictsPush);


var itemsGroup = app.MapGroup("/items").RequireAuthorization("UserPolicy");

itemsGroup.MapGet("", ItemsHandler.GetItems);
itemsGroup.MapGet("/phrases", ItemsHandler.GetItemsPhrases);
itemsGroup.MapGet("/phrase-tags", ItemsHandler.GetItemsPhraseTags);
itemsGroup.MapGet("/bookmarks", ItemsHandler.GetItemsBookmarks);
itemsGroup.MapGet("/bookmark-tags", ItemsHandler.GetItemsBookmarkTags);
itemsGroup.MapGet("/tag-categories", ItemsHandler.GetItemsTagCategories);
itemsGroup.MapGet("/tags", ItemsHandler.GetItemsTags);

itemsGroup.MapGet("/phrases/{id}", ItemsHandler.GetItemsPhraseById);
itemsGroup.MapGet("/phrase-tags/{id}", ItemsHandler.GetItemsPhraseTagById);
itemsGroup.MapGet("/bookmarks/{id}", ItemsHandler.GetItemsBookmarkById);
itemsGroup.MapGet("/bookmark-tags/{id}", ItemsHandler.GetItemsBookmarkTagById);
itemsGroup.MapGet("/tag-categories/{id}", ItemsHandler.GetItemsTagCategoryById);
itemsGroup.MapGet("/tags/{id}", ItemsHandler.GetItemsTagById);

itemsGroup.MapGet("/global", ItemsHandler.GetItemsGlobal);
itemsGroup.MapGet("/languages", ItemsHandler.GetItemsLanguages);
itemsGroup.MapGet("/dictionaries", ItemsHandler.GetItemsDictionaries);
itemsGroup.MapGet("/translations", ItemsHandler.GetItemsTranslations);

itemsGroup.MapGet("/dictionaries/{id}", ItemsHandler.GetItemsDictionaryById);
itemsGroup.MapGet("/translations/{id}", ItemsHandler.GetItemsTranslationById);


app.Run();
