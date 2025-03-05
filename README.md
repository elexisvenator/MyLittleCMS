# My Little CMS

## Purpose

My Little CMS is vaguely a website api and vaguely a CMS. It was not built with any production use case in mind.  Instead the purpose is to imagine what a clean, production ready application might look like if it went all-in with [Critterstack](https://jasperfx.net/).  Additionally, this app is used to identify potential questions, feature gaps and bugs in critterstack usage so that they can be fed back (or contributed back) to the critterstack team.

## Get started

### Prerequisites

- Docker
- Dotnet 9

### How to try it out

- Open in visual studio
- Run the application by running the MyLittleCMS.AppHost project.

## Application requirements

(these requirements are nonsensical, and intended to test a bunch of different features)

- The application must be multi-tenanted
  - Any uniqueness rules for the below requirements should be considered unique per tenant.
- Users 
  - Have a username, name and internal id
  - Can be registered
  - Can be marked as archived. Archived users cannot perform actions
  - Can get by id
  - Can get a paged list of users. "IncludeArchived" is an optional filter
- Pages
  - Pages have a name
  - Pages have a uri component, which is part of a url path
  - Pages have a parent page they live under
  - Per tenant, there is one page that does not have a parent or uri component. This is the "root" page.
  - Pages can be published
    - Pages can only be published if they have non-draft content
  - You can get a page by its id, by its parent page id + uri component, or by a uri component "path".
  - Pages have content
    - Content is a string.
    - Tracking changes to content is important, including who performed the changes.
    - Content starts as draft and can be "published" to be the current content of a page.
    - It should be possible to have both an active draft and a published/current content at the same time.
    - There should be no more than 1 draft and 1 current/published content versions
    - It is possible to archive content
      - Archiving the current content of a published page should only be possible by publishing another draft to take it's place
    - When creating a new draft, it should start with a copy of the current content.
    - All changes to content should have an author (active user) associated with it.
- ~~Pages can have a list of tags to be searched on~~ temporarily dropped due to issues with using arrays in marten-defined indexes.

## Technology choices

- Go all-in with [Wolverine](https://wolverinefx.net/) and [Marten](https://martendb.io/).
- Use Wolverine's integration with [FluentValidation](https://docs.fluentvalidation.net/en/latest/index.html) 
  - Avoid writing my own validators if it can be done with fluentValidation instead
- Use Postgres
  - Use a mix of data types and indexes
  - Use a mix of query approaches, including linq-query and raw sql.
- Tenancy mode is conjoined
- Play with soft deletion, partitioning and other features
- Use a mix of events and documents to model the data
- Events should use quickappend
- Avoid using inline projections
- Use .Net Aspire to orchestrate everything and keep dev easy.
- Use .Net 9, open api and other 9.x features.
- Use System.Text.Json
- All api endpoints are wolverine endpoints
  - Actual endpoint code should contain no validation logic (do this in fluentValidation, or `Validate`/`ValidateAsync` methods)
  - Actual endpoint code should not by async, accept any `IDocumentSession`, or directly cause side effects. Instead any side effects should be returned and handled by marten.
  - And additional data required by an endpoint should be retrieved using `[Document]`, `[Attribute]` or by a dedicated `LoadAsync` method.

Auth is not used at this time for convenience.

## Future work

Testing!!!

## Critterstack findings

### Bugs

- Setting the npgsql type to array + text (aka `text[]`) doesn't work, codegen creates invalid code
- adding a strong typed string (vogen) as a non-id property threw exception on startup (could not find npgsql type)
- `opts.Policies.AllDocumentsSoftDeleted()` does not work if you also have aggregate projections

### Feature gaps

- Query strings and get endpoints have limited support compared to request bodies. It would be great if we can specify a model that would bind to querystring values, possibly with the attribute `[FromQuery]` 
  - ideally should work with fluentvalidation when that is turned on
  - For swagger/openapi, the querystring values metadata should be derived from this model's property metadata
- Support strongly typed ids when using `[Document]` and `[Aggregate]` - marten works (i think) but wolverine's attributes do not.
- FluentValidation - this is injected before any `[Document]` or `[Aggregate]` is loaded, meaning that you cannot inject these objects into validation.
  - alternative, should I be calling LoadAsync and rely on identity map to not make multiple db calls?
- Need a `MartenOps.AppendEvents()` for when you need to append to a stream you don't have an `[Aggregate]` for.


### Misc/nice to haves

- `CombGuidUiGeneration` - there are two of these, can the one in Marten be removed?
- Support using [Collation](https://www.postgresql.org/docs/current/collation.html) for string comparisons + indexing, as these are much more powerful for case and accent insensitive comparisons
- ~~`session.Events.FetchLatest<T>()` has a return signature of `T`, but should be `T?`~~
- ~~`IEventStream<T>.Aggregate` is of type `T` but should be `T?`~~

- TODO: (marten) create an example of `fetchLatest` that works on a predicate instead of a stream key. (for natural key support)
  - BONUS: instead of a predicate on the same projection, do a predicate on a different projection (reasoning here is to have a dedicated inline projection for mapping natural ids)
- TODO: (wolverine) create an example codgen attribute version of `[Document]` and `[Aggregate]` that functions the same but first translates a natural key to a stream key, OR uses the natural key for lookup
