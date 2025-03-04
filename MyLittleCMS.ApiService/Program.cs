using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Storage;
using MyLittleCMS.ApiService;
using MyLittleCMS.ApiService.DataModels;
using MyLittleCMS.ApiService.DataModels.Events;
using Oakton;
using Scalar.AspNetCore;
using Weasel.Postgresql;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.AddNpgsqlDataSource(connectionName: "mylittlecmsdb");

builder.Host.ApplyOaktonExtensions();
builder.Services.AddMarten(opts =>
    {
        opts.DatabaseSchemaName = "document_store";
        opts.Events.DatabaseSchemaName = "event_store";

        // ! does not work with projections
        //// deleting a document will set a delete flag, they can be searched or hard deleted
        //opts.Policies.AllDocumentsSoftDeleted();
        
        //// all documents are associated with a tenant and have a tenantId column,
        //// additionally, documents are partitioned into 10 partition table using a hash of the tenantId
        //opts.Policies.AllDocumentsAreMultiTenantedWithPartitioning(partition =>
        //    partition.ByHash(Enumerable.Range(1, 10).Select(n => "_part" + n.ToString("D2")).ToArray()));
        opts.Policies.AllDocumentsAreMultiTenanted();
        // events are associated with a tenant and have a tenantId column
        opts.Events.TenancyStyle = TenancyStyle.Conjoined;
        // event streams use string ids
        opts.Events.StreamIdentity = StreamIdentity.AsString;
        // events are partitioned by whether they are archived or not 
        opts.Events.UseArchivedStreamPartitioning = true;
        // performance optimisation for wolverine aggregates, FetchForWriting and FetchLatest
        // tradeoff is that singleStreamProjections should be made immutable
        opts.Events.UseIdentityMapForAggregates = true;
        // require streams to have a stream type
        opts.Events.UseMandatoryStreamTypeDeclaration = true;
        // performance improvements to projections, including rebuilding per-stream
        opts.Events.UseOptimizedProjectionRebuilds = true;
        // enable quick append, writes are a LOT faster, projections are more stable
        // tradeoff is that inline projections must be inline and cannot use event metadata
        opts.Events.AppendMode = EventAppendMode.Quick;

        // performance optimisation for wolverine aggregates, FetchForWriting and FetchLatest
        // tradeoff is that singleStreamProjections should be made immutable
        opts.Projections.UseIdentityMapForAggregates = true;

        opts.Storage.ExtendedSchemaObjects.Add(new Extension("btree_gin"));

        opts.RegisterDocumentType<User>();
        opts.RegisterDocumentType<Page>();
        opts.Schema.For<Page>();
            //.Duplicate(
            //    x => x.Tags,
            //    "text[]",
            //    NpgsqlDbType.Array | NpgsqlDbType.Text,
            //    index =>
            //    {
            //        index.Method = IndexMethod.gin;
            //        index.TenancyScope = TenancyScope.PerTenant;
            //    },
            //    notNull: true);

        opts.Events.AddEventType<PageContentCreated>();
        opts.Events.AddEventType<PageContentUpdated>();
        opts.Events.AddEventType<PageContentPublished>();
        opts.Events.AddEventType<PageContentArchived>();

        opts.Projections.Add<PageContent.Projector>(ProjectionLifecycle.Async);
    })
    .UseNpgsqlDataSource()
    .UseLightweightSessions()
    .IntegrateWithWolverine(opts =>
    {
        opts.UseWolverineManagedEventSubscriptionDistribution = true;
        opts.MessageStorageSchemaName = "wolverine_messages";
    })
    .ApplyAllDatabaseChangesOnStartup();

builder.Host.UseWolverine(opts =>
{
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.Policies.AutoApplyTransactions();
    opts.UseSystemTextJsonForSerialization();
    opts.UseFluentValidation();

    // Turn off all logging of the message execution starting and finishing
    // The default is Debug
    opts.Policies.MessageExecutionLogLevel(LogLevel.None);

    // Turn down Wolverine's built in logging of all successful
    // message processing
    opts.Policies.MessageSuccessLogLevel(LogLevel.Debug);
});

builder.Services.AddWolverineHttp();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(opts =>
{
    opts.AddOperationTransformer<WolverineOperationTransformer>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().CacheOutput();
    app.MapScalarApiReference(opts =>
    {
        opts.Servers = [];
        opts.Theme = ScalarTheme.Saturn;
    });
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.LocalRedirect("~/scalar"));

app.MapWolverineEndpoints(opts =>
{
    opts.TenantId.IsRouteArgumentNamed("tenantId");
    // can be overridden with [NotTenanted]
    opts.TenantId.AssertExists();
    opts.UseFluentValidationProblemDetailMiddleware();
    
    //opts.RequireAuthorizeOnAll()
});

await app.RunOaktonCommands(args);
