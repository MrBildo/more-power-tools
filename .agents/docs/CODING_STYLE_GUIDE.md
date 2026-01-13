# KindKatch API Coding Style Guide

**Version:** 2.1
**Last Updated:** January 2026
**Applicable To:** All C# code in E313.*, KindKatch.*, and Web projects

This guide documents the established coding conventions, architectural patterns, and style preferences used in the KindKatch API codebase. It is based on analysis of code written primarily between 2024-2025, with emphasis on the most recent conventions.

---

## Table of Contents

1. [Language Features & Modern C#](#language-features--modern-c)
2. [Architectural Patterns (CQRS & DDD)](#architectural-patterns-cqrs--ddd)
3. [Project Structure](#project-structure)
4. [Naming Conventions](#naming-conventions)
5. [Code Organization](#code-organization)
6. [Commands & Command Handlers](#commands--command-handlers)
7. [Queries & Query Handlers](#queries--query-handlers)
8. [Events & Event Handlers](#events--event-handlers)
9. [Domain Entities](#domain-entities)
10. [Value Objects](#value-objects)
11. [Repositories](#repositories)
12. [Controllers](#controllers)
13. [Azure Functions](#azure-functions)
14. [Error Handling](#error-handling)
15. [Collections & Initialization](#collections--initialization)
16. [Formatting & Whitespace](#formatting--whitespace)
17. [Comments & Documentation](#comments--documentation)

---

## Language Features & Modern C#

### Required Language Features

The codebase uses **C# 12** features. All new code must utilize modern C# syntax where applicable.

#### File-Scoped Namespaces

**Always use file-scoped namespace declarations.**

```csharp
// ✅ CORRECT
namespace E313.KindKatch.CommandHandlers.Journeys;

public class CreateOrUpdateCommandHandler { }

// ❌ WRONG
namespace E313.KindKatch.CommandHandlers.Journeys
{
    public class CreateOrUpdateCommandHandler { }
}
```

#### Primary Constructors

**Use primary constructors for dependency injection in handlers, controllers, functions, and services.**

```csharp
// ✅ CORRECT - Few parameters (1-5), single line
public class CreateOrUpdateCommandHandler(IJourneyRepository journeyRepository, ITagRepository tagRepository) : CommandHandler<CreateOrUpdateCommand, Guid>
{
    private readonly IJourneyRepository _journeyRepository = journeyRepository ?? throw new ArgumentNullException(nameof(journeyRepository));
    private readonly ITagRepository _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));

    // ... implementation
}

// ✅ CORRECT - Many parameters (6+), opening paren on new line
internal class SmsMediaProcessor
(
    AppDbContext appDbContext,
    JsonSerializerOptions jsonSerializerOptions,
    IBlobStorageService blobStorageService,
    IStreamableBlobStorageService streamableBlobStorageService,
    IHttpClientFactory httpClientFactory,
    IVideoTranscodingService videoTranscodingService,
    ILogger<SmsMediaProcessor> logger
)
{
    private readonly ILogger _logger = logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    // ... more fields
}
```

**Key Points:**
- **1-5 parameters**: Opening paren on same line as class name
- **6+ parameters**: Opening paren on new line, each parameter on its own line
- Initialize private readonly fields explicitly from primary constructor parameters
- Apply null checks using `?? throw new ArgumentNullException(nameof(...))`
- Closing paren on own line, aligned with opening line

#### Records

**Use `record` (not `record struct`) for Commands, Queries, DTOs, and immutable data structures.**

```csharp
// ✅ CORRECT - Simple record, single line
public record QuickShareMember(Guid Id, string Phone);

// ✅ CORRECT - Record with few parameters, single line
public record CreateOrUpdateCommand(Guid? Id, Guid CharityId, bool IsActive, bool IsArchived, string Name, string Description, string TriggerTag, JourneyStepItem[] Steps) : ICommand;

// ✅ CORRECT - Record with many parameters, opening paren on new line
public record JourneyStepItem
(
    Guid? Id,
    string Name,
    string Description,
    int Ordinal,
    bool IsConfigured,
    string CampaignType,
    Guid? MediaId,
    Guid? ThumbnailMediaId,
    Guid? LogoId,
    string SenderEmail,
    string EmailSubject,
    string Body,
    string LandingPageBody,
    bool ShowLandingPageCta,
    string LandingPageCtaText,
    string LandingPageCtaUrl,
    bool AcceptResponses,
    string PreferredShareMethod,
    bool HasRule,
    int DayDelay,
    int HourDelay,
    int MinuteDelay
);

// ✅ CORRECT - Query record
public record GetMediaViewsQuery(Guid CharityId, DateTime? StartDate, DateTime? EndDate) : IQuery;

// ✅ CORRECT - Result record
public record GetMediaViewsQueryResult(int Total);

// ❌ WRONG - Don't use record struct
public record struct GetQuery(Guid JourneyId) : IQuery;  // Use record instead
```

**When to use `record`:**
- All commands (implements ICommand)
- All queries (implements IQuery)
- All query results
- DTOs and data transfer types
- Immutable data structures

**Record Property Naming:**
- ALL record properties MUST use PascalCase, including private records
- Record parameters follow camelCase like method parameters
- When defining the record, properties are auto-generated with PascalCase

```csharp
// ✅ CORRECT - Properties are PascalCase
private record ExpoApiResponse(ExpoReceiptData[] Data);
private record ExpoReceiptData(string Status, string Id, string Message, ExpoErrorDetails Details);
private record ExpoErrorDetails(string Error);

// ❌ WRONG - Don't use camelCase for record properties
private record ExpoApiResponse(ExpoReceiptData[] data);  // Wrong!
private record ExpoReceiptData(string status, string id, string message, ExpoErrorDetails details);  // Wrong!
```

**Record Whitespace:**
- When defining multiple records (public or private), add a blank line between each record definition for readability

```csharp
// ✅ CORRECT - Blank lines between records
private record ExpoApiResponse(ExpoReceiptData[] Data);

private record ExpoReceiptData(string Status, string Id, string Message, ExpoErrorDetails Details);

private record ExpoErrorDetails(string Error);

// ❌ WRONG - No separation
private record ExpoApiResponse(ExpoReceiptData[] Data);
private record ExpoReceiptData(string Status, string Id, string Message, ExpoErrorDetails Details);
private record ExpoErrorDetails(string Error);
```

**Formatting:**
- **1-8 parameters**: Opening paren on same line
- **9+ parameters**: Opening paren on new line, each parameter on its own line
- Parameters aligned vertically when multi-line
- Closing paren and type constraints on same line

#### Collection Expressions

**Use collection expressions `[]` instead of `new List<>()`, `new Array[]`, etc.**

```csharp
// ✅ CORRECT
private readonly List<IEvent> _eventsToPublish = [];
internal virtual List<JourneyStep> Steps { get; set; } = [];
var keywords = ["STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL"];

// ❌ WRONG
private readonly List<IEvent> _eventsToPublish = new List<IEvent>();
internal virtual List<JourneyStep> Steps { get; set; } = new List<JourneyStep>();
```

**Collection Expressions with Spread:**

```csharp
// ✅ CORRECT
public virtual IReadOnlyCollection<Donor> Donors => [.. _donors];
private List<JourneyStep> ActiveSteps => [.. Steps.Where(s => s.StatusId != JourneyStepStatus.Archived.Id)];
public IEnumerable<JourneyStep> GetActiveSteps() => [.. ActiveSteps.OrderBy(s => s.Ordinal)];
```

#### Target-Typed New

**Use `new()` when the type can be inferred.**

```csharp
// ✅ CORRECT (type inferred from return type or variable declaration)
return new JourneyStepRuleParameter
{
    JourneyStepId = journeyStepId,
    Name = name,
    Value = value
};

return new()  // when inside method returning specific type
{
    JourneyStepId = journeyStepId,
    Name = name,
    Value = value
};

// With anonymous objects for query parameters
await QuerySingleFromScriptAsync<GetMediaViewsQueryResult>("Dashboard\\GetMediaViews.sql", new
{
    query.CharityId,
    query.StartDate,
    query.EndDate
});
```

#### Pattern Matching

**Use modern pattern matching for null checks and complex conditions.**

```csharp
// ✅ CORRECT
if (command.Id is null)
{
    journey = Journey.Create(command.CharityId, command.Name, command.Description);
}

if (charity is null)
{
    return CommandResult.Fail("Charity not found");
}

if (donor is not null)
{
    donor.Name = new NameValue(command.FirstName, command.LastName);
}

// Complex patterns
if (donors.Any() is false && charity.HasActiveIntegration is false)
{
    // create donor
}

return words.Length is not 0 and not > 1 && _stopKeywords.Contains(words.FirstOrDefault()?.ToUpperInvariant());

// Pattern matching in property
return step is null ? throw new ArgumentOutOfRangeException(nameof(stepId), "Step not found") : step;
```

#### Guard Clauses

**Use guard clause methods from .NET for parameter validation.**

```csharp
// ✅ CORRECT
ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
ArgumentNullException.ThrowIfNull(stepId, nameof(stepId));
ArgumentNullException.ThrowIfNull(ruleType, nameof(ruleType));

// ❌ WRONG (older style, avoid in new code)
if (string.IsNullOrWhiteSpace(name))
{
    throw new ArgumentException("Name cannot be null or whitespace", nameof(name));
}
```

#### Expression-Bodied Members

**Use expression-bodied members for simple one-liners.**

```csharp
// ✅ CORRECT
public void Archive() => StatusId = JourneyStatus.Archived.Id;
public void Complete() => StatusId = EnrollmentStatus.Completed.Id;
public override string ToString() => Email;
internal void Archive() => StatusId = JourneyStepStatus.Archived.Id;
public void UpdateOrdinal(int ordinal) => Ordinal = ordinal;

// Properties
public override string ToString() => Id.ToString();
protected override int GetHashCodeCore() => Email.GetHashCode();
```

#### Null-Coalescing with Throw

**Use null-coalescing with throw for dependency injection validation.**

**This is the standard pattern - validate ALL constructor-injected dependencies.**

```csharp
// ✅ CORRECT
private readonly ICharityRepository _charityRepository = charityRepository ?? throw new ArgumentNullException(nameof(charityRepository));
private readonly IDonorRepository _donorRepository = donorRepository ?? throw new ArgumentNullException(nameof(donorRepository));
private readonly IMessageRepository _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
private readonly IDateTimeService _dateTimeService = dateTimeService ?? throw new ArgumentNullException(nameof(dateTimeService));
```

#### File-Scoped Types

**Use file-scoped types for internal helpers and extensions within a single file.**

This is part of the vertical slice architecture pattern.

```csharp
// ✅ CORRECT
namespace E313.KindKatch.CommandHandlers.Messaging;

public class CreateCharityMessageCommandHandler(...) : CommandHandler<...>
{
    // main handler implementation
}

file record FileMetadataMessage(Guid CharityId, Guid Id, string Source, string Url)
{
    public static FileMetadataMessage FromFileMetadata(Guid charityId, FileMetadata metadata)
        => new(charityId, metadata.Id, metadata.Source.Name.ToLower(), metadata.SourcePath);
}

static file class TwilioOptOutParser
{
    private static readonly string[] _stopKeywords = ["STOP", "STOPALL", "UNSUBSCRIBE"];

    public static bool IsStopMessage(string message) { /* ... */ }
}
```

**When to use file-scoped types:**
- Helper types only used within the single file (vertical slice)
- Extension methods specific to the file
- Internal DTOs or records not used elsewhere
- Parser or utility classes with file-local scope

#### Local Functions

**Use local async functions for repeated operations within a method.**

```csharp
protected override async ValueTask OnHandleAsync(DonorTagAddedEvent @event)
{
    var charities = new Dictionary<Guid, Charity>();

    foreach (var journey in journeys)
    {
        var charity = await GetCharityAsync(journey.CharityId);
        // use charity
    }

    // Local function defined at end of method
    async Task<Charity> GetCharityAsync(Guid charityId)
    {
        if (!charities.TryGetValue(charityId, out var charity))
        {
            charity = await _charityRepository.GetCharityByIdAsync(charityId);
            charities[charityId] = charity;
        }
        return charity;
    }
}
```

#### Async Method Naming

**All async methods must have the `Async` suffix.**

```csharp
// ✅ CORRECT
public async Task<Journey> LoadByIdAsync(Guid id)
public async Task SaveAsync()
public async Task<CommandResult> OnHandleAsync(CreateCommand command)

// ❌ WRONG
public async Task<Journey> LoadById(Guid id)
public async Task Save()
```

This applies to:
- Public methods
- Protected methods
- Private helper methods
- Local functions (if async)

---

## Architectural Patterns (CQRS & DDD)

### CQRS (Command Query Responsibility Segregation)

The codebase strictly separates read and write operations:

- **Commands**: Modify state, return CommandResult or CommandResult&lt;T&gt;
- **Queries**: Read-only, return QueryResult&lt;T&gt;
- **Events**: Published by commands for async side effects

**Project Structure:**
- `E313.KindKatch.Commands` - Command definitions
- `E313.KindKatch.CommandHandlers` - Command execution logic
- `E313.KindKatch.Queries` - Query definitions
- `E313.KindKatch.QueryHandlers` - Query execution logic (uses Dapper + embedded SQL)
- `E313.KindKatch.Events` - Event definitions
- `E313.KindKatch.EventHandlers` - Event handling logic

### DDD (Domain-Driven Design)

**Key Concepts:**

1. **Entities**: Rich domain models with behavior (E313.Data.Entities)
2. **Value Objects**: Immutable types with validation (E313.Data.Entities.Values)
3. **Aggregate Roots**: Entry points for domain operations (Charity, Donor, Journey, etc.)
4. **Repositories**: Data access abstraction
5. **Domain Events**: Published when important state changes occur

---

## Project Structure

### Projects by Layer

**Domain Layer:**
- `E313.Data` - Entities, Value Objects, DbContext, Migrations
- `E313.Data.Repositories` - Repository implementations

**Application Layer:**
- `E313.KindKatch.Commands` - Command models
- `E313.KindKatch.Queries` - Query models
- `E313.KindKatch.Events` - Event models
- `E313.KindKatch.CommandHandlers` - Command handling
- `E313.KindKatch.QueryHandlers` - Query handling
- `E313.KindKatch.EventHandlers` - Event handling
- `E313.KindKatch.ApplicationServices` - Orchestration services

**Service Layer:**
- `E313.KindKatch.Services` - Business logic services (legacy)
- `E313.KindKatch.Services.Contracts` - Service interfaces (**DEPRECATED** - being phased out)
- `KindKatch.Core` - **PREFERRED** location for new services

**Service Organization (Vertical Slice Architecture):**
- **New services**: Place in `KindKatch.Core/Services/`
- **Interface location**: Colocate interface with implementation in same file
- **Pattern**: Interface → Response Records → Implementation class in single file
- **Legacy**: `E313.KindKatch.Services.Contracts` is being phased out - do not add new interfaces there

```csharp
// ✅ CORRECT - KindKatch.Core vertical slice pattern
// File: KindKatch.Core/Services/ExpoPushNotificationService.cs
namespace KindKatch.Services;

public interface IExpoPushNotificationService
{
    Task<ExpoPushResponse> SendPushNotificationsAsync(...);
}

public record ExpoPushResponse(bool Success, string[] SuccessTokens, ...);

public class ExpoPushNotificationService(...) : IExpoPushNotificationService
{
    // Implementation
}

// ❌ WRONG - Separate Contracts project
// E313.KindKatch.Services.Contracts/IExpoPushNotificationService.cs
// E313.KindKatch.Services/ExpoPushNotificationService.cs
```

**Infrastructure:**
- `E313.Common` - Shared utilities and extensions
- `E313.KindKatch.Configuration` - DI setup
- `E313.KindKatch.DTOs` - Data transfer objects
- `KindKatch.Azure.Functions.Core` - Shared function infrastructure

**Presentation:**
- `Web (E313.Api)` - ASP.NET Core API controllers

**Azure Functions:**
- `KindKatch.Azure.Functions.ShareSending` - Email/SMS sending
- `KindKatch.Azure.Functions.Scheduling` - Share scheduling
- `KindKatch.Azure.Functions.Journeys.Scheduling` - Journey scheduling
- `KindKatch.Azure.Functions.SmsMediaProcessing` - Media processing
- `KindKatch.Azure.Functions.Twilio` - Twilio webhooks

**Worker Services:**
- `KindKatch.WebJobs.Virtuous` - Virtuous integration background jobs
- `E313.KindKatch.WebJobs` - General background jobs

**CLI:**
- `KindKatch.Cli` - Command-line utilities

### File Organization

**Commands/Queries/Events in domain folders:**

```
E313.KindKatch.CommandHandlers/
├── Journeys/
│   ├── CreateOrUpdate.cs       (contains Command + CommandHandler)
│   ├── Enroll.cs
│   └── Unenroll.cs
├── Messaging/
│   ├── CreateCharityMessageCommandHandler.cs
│   ├── UpdateAutoResponseSettings.cs
│   └── MarkThreadAsRead.cs
└── Shares/
    └── CreateQuickShare.cs
```

**Naming:**
- Single file per command/query handler
- File name matches the primary handler class or command name
- Commands and handlers often colocated in same file

---

## Naming Conventions

### General Rules

- **Classes/Interfaces**: PascalCase
- **Methods**: PascalCase
- **Properties**: PascalCase (including record properties - ALWAYS capitalize, even in private records)
- **Fields**:
  - Private: `_camelCase` with underscore prefix
  - Protected/Public: PascalCase (rare, prefer properties)
- **Parameters**: camelCase (including record parameters)
- **Local variables**: camelCase
  - **Loop counters**: ALWAYS use `var`, never explicit type (e.g., `for (var i = 0; ...)` not `for (int i = 0; ...)`)
- **Constants**:
  - Private: `_camelCase` with underscore prefix (e.g., `private const string _expoPushApiUrl`)
  - Public/Protected: PascalCase

### Specific Naming

**Commands:**
```csharp
public record CreateOrUpdateCommand(...) : ICommand;
public record EnrollCommand(...) : ICommand;
```
- End with "Command"
- Verb-based: Create, Update, Delete, Process, Send, etc.

**Command Handlers:**
```csharp
public class CreateOrUpdateCommandHandler(...) : CommandHandler<CreateOrUpdateCommand, Guid>
```
- End with "CommandHandler"
- No `sealed` modifier (unless specifically needed)

**Queries:**
```csharp
public record GetQuery(Guid JourneyId) : IQuery;
public record GetAllQuery(Guid CharityId) : IQuery;
```
- Simple verb: Get, GetAll, List
- Use `record` (not `record struct`)

**Query Handlers:**
```csharp
public class GetQueryHandler(...) : QueryHandler<GetQuery, GetQueryResult>
public class GetMediaViewsQueryHandler(...) : QueryHandler<GetMediaViewsQuery, GetMediaViewsQueryResult>
```
- End with "QueryHandler"
- No `sealed` modifier

**Events:**
```csharp
public record DonorTagAddedEvent(Guid DonorId, string[] Tags, bool SkipDeliveryWindow) : IEvent;
public record SendDonorMessageEvent(Guid CharityId, Guid DonorId, Guid MessageId, bool IncludeOptOutMessage) : IEvent;
```
- Past tense: Created, Updated, Added, Sent, etc.
- End with "Event"

**Event Handlers:**
```csharp
public class DonorTagAddedEventHandler(...) : EventHandler<DonorTagAddedEvent>
```
- End with "EventHandler"

**Repositories:**
```csharp
public interface ICharityRepository
public class CharityRepository : Repository, ICharityRepository
```
- Interface with "I" prefix
- End with "Repository"

**Entities:**
```csharp
public class Journey : DatabaseEntity
public class Donor : DatabaseEntity
```
- Singular noun
- Inherit from DatabaseEntity

**Value Objects:**
```csharp
public class EmailValue : ValueObject<EmailValue>
public class PhoneValue : ValueObject<PhoneValue>
```
- End with "Value"
- Use `sealed` for value objects

---

## Code Organization

### File Structure

**Typical CommandHandler file:**

```csharp
using System;
using System.Threading.Tasks;
using E313.Data.Entities;
using E313.Data.Repositories;
using E313.KindKatch.Commands;

namespace E313.KindKatch.CommandHandlers.Journeys;

// Command record
public record CreateOrUpdateCommand(...) : ICommand;

// Supporting records if needed
public record JourneyStepItem(...);

// Handler class
public class CreateOrUpdateCommandHandler(...) : CommandHandler<CreateOrUpdateCommand, Guid>
{
    // Fields with null validation
    private readonly IJourneyRepository _journeyRepository = journeyRepository ?? throw new ArgumentNullException(nameof(journeyRepository));
    private readonly ITagRepository _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));

    // OnHandleAsync implementation
    protected override async Task<CommandResult<Guid>> OnHandleAsync(CreateOrUpdateCommand command)
    {
        // implementation
    }
}
```

### Using Directives

**Order (enforced via .editorconfig):**
1. System namespaces
2. Third-party namespaces
3. E313 namespaces (alphabetically)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using E313.Data.Entities;
using E313.Data.Repositories;
using E313.KindKatch.Commands;
using E313.KindKatch.EventHandlers;
using Microsoft.AspNetCore.Mvc;
```

---

## Commands & Command Handlers

### Command Definition

```csharp
// Simple command - record
public record UpdateAutoResponseSettingsCommand
(
    Guid CharityId,
    bool AutoResponseEnabled,
    string AutoResponseMessage
) : ICommand;

// Complex command - record
public record CreateQuickShareCommand(Guid CharityId, string Message, bool IncludeOptOutMessage, IEnumerable<QuickShareMember> Members) : ICommand;

// Supporting types
public record QuickShareMember(Guid Id, string Phone);
```

### Command Handler Implementation

```csharp
public class UpdateAutoResponseSettingsCommandHandler(ICharityRepository charityRepository) : CommandHandler<UpdateAutoResponseSettingsCommand>
{
    private readonly ICharityRepository _charityRepository = charityRepository ?? throw new ArgumentNullException(nameof(charityRepository));

    protected override async Task<CommandResult> OnHandleAsync(UpdateAutoResponseSettingsCommand command)
    {
        var charity = await _charityRepository.GetCharityByIdAsync(command.CharityId);

        if (charity is null)
        {
            throw new Exception("charity not found.");
        }

        charity[CharitySettingType.SMSAutoResponseEnabled] = command.AutoResponseEnabled.ToString();
        charity[CharitySettingType.SMSAutoResponseMessage] = command.AutoResponseMessage;

        await _charityRepository.SaveAsync();

        return CommandResult.Success();
    }
}
```

**Key Points:**
- No `sealed` modifier on handlers
- Inherit from `CommandHandler<TCommand>` or `CommandHandler<TCommand, TResult>`
- For handlers with events: pass `EventDispatcher` to base constructor
- Validate ALL dependencies with `?? throw new ArgumentNullException(...)`
- Return `CommandResult.Success()` or `CommandResult<T>.Success(value)`
- Return `CommandResult.Fail(message)` for business rule failures
- Throw exceptions for unexpected errors

### Command Handler with Events

```csharp
public class CreateQuickShareCommandHandler
(
    IDateTimeService dateTimeService,
    IDonorRepository donorRepository,
    IMessageRepository messageRepository,
    ICharityRepository charityRepository,
    EventDispatcher eventDispatcher
) : CommandHandler<CreateQuickShareCommand>(eventDispatcher)
{
    private readonly ICharityRepository _charityRepository = charityRepository;
    private readonly IDonorRepository _donorRepository = donorRepository;
    private readonly IMessageRepository _messageRepository = messageRepository;
    private readonly IDateTimeService _dateTimeService = dateTimeService;

    protected override async Task<CommandResult> OnHandleAsync(CreateQuickShareCommand command)
    {
        // ... business logic

        // Publish events
        PublishEvent(new SendDonorMessageEvent(charity.Id, member.Id, message.Id, command.IncludeOptOutMessage));

        await _messageRepository.SaveAsync();

        return CommandResult.Success();
    }
}
```

### Command Handler with Result

```csharp
public class CreateOrUpdateCommandHandler(IJourneyRepository journeyRepository, ITagRepository tagRepository) : CommandHandler<CreateOrUpdateCommand, Guid>
{
    private readonly IJourneyRepository _journeyRepository = journeyRepository ?? throw new ArgumentNullException(nameof(journeyRepository));
    private readonly ITagRepository _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));

    protected override async Task<CommandResult<Guid>> OnHandleAsync(CreateOrUpdateCommand command)
    {
        Journey journey;

        if (command.Id is null)
        {
            journey = Journey.Create(command.CharityId, command.Name, command.Description);
            await _journeyRepository.AddAsync(journey);
        }
        else
        {
            journey = await _journeyRepository.LoadByIdAsync(command.Id.Value);

            if (journey is null)
            {
                return CommandResult<Guid>.Fail("Journey not found");
            }

            journey.UpdateDetails(command.Name, command.Description);
        }

        // ... more logic

        await _journeyRepository.SaveAsync();

        return CommandResult<Guid>.Success(journey.Id);
    }
}
```

---

## Queries & Query Handlers

### Query Definition

```csharp
// Simple query
public record GetQuery(Guid JourneyId) : IQuery;

// Query with multiple parameters
public record GetMediaViewsQuery(Guid CharityId, DateTime? StartDate, DateTime? EndDate) : IQuery;

// Result type
public record GetQueryResult
(
    Guid Id,
    string Name,
    string Description,
    string TriggerTag,
    bool IsActive
);

public record GetMediaViewsQueryResult(int Total);
```

### Query Handler Implementation

Query handlers use **Dapper** with embedded SQL scripts.

```csharp
public class GetQueryHandler(IConfiguration configuration, IEmbeddedResourceReaderService embeddedResourceReaderService) : QueryHandler<GetQuery, GetQueryResult>(configuration, embeddedResourceReaderService)
{
    protected override async Task<QueryResult<GetQueryResult>> OnHandleAsync(GetQuery query) =>
        await QuerySingleFromScriptAsync<GetQueryResult>("Journeys\\Get.sql", new { query.JourneyId });
}
```

**Key Points:**
- Query handlers are very concise
- Use embedded SQL scripts in `Scripts\` folder
- Pass anonymous objects for parameters using `new { query.Property }`
- Use base class helper methods:
  - `QueryFromScriptAsync<T>` - Returns IEnumerable&lt;T&gt;
  - `QuerySingleFromScriptAsync<T>` - Returns single T
  - `QuerySingleOrDefaultFromScriptAsync<T>` - Returns T or default
  - `QueryScalarFromScriptAsync<T>` - Returns scalar T

### SQL Scripts

SQL scripts are embedded resources stored in `E313.KindKatch.QueryHandlers\Scripts\` folder.

**File structure:**
```
Scripts/
├── Dashboard/
│   ├── GetMediaViews.sql
│   └── GetMessageMetrics.sql
├── Journeys/
│   ├── Get.sql
│   ├── GetAll.sql
│   └── GetSteps.sql
└── Messaging/
    └── GetThreads.sql
```

**Script naming:** Matches the query name without "Query" suffix.

---

## Events & Event Handlers

### Event Definition

```csharp
public record DonorTagAddedEvent(Guid DonorId, string[] Tags, bool SkipDeliveryWindow) : IEvent;

public record SendDonorMessageEvent(Guid CharityId, Guid DonorId, Guid MessageId, bool IncludeOptOutMessage) : IEvent;
```

### Event Handler Implementation

```csharp
public class DonorTagAddedEventHandler
(
    IJourneyRepository journeyRepository,
    ICharityRepository charityRepository,
    ITagRepository tagRepository
) : EventHandler<DonorTagAddedEvent>
{
    private readonly IJourneyRepository _journeyRepository = journeyRepository ?? throw new ArgumentNullException(nameof(journeyRepository));
    private readonly ICharityRepository _charityRepository = charityRepository ?? throw new ArgumentNullException(nameof(charityRepository));
    private readonly ITagRepository _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));

    protected override async ValueTask OnHandleAsync(DonorTagAddedEvent @event)
    {
        // Event handling logic
        // Note: Events use ValueTask, not Task
    }
}
```

**Key Points:**
- Event handlers return `ValueTask`, not `Task`
- Events should be idempotent
- Events can publish other events by passing EventDispatcher to base
- Use `@event` as parameter name (escaped keyword)

---

## Domain Entities

### Entity Structure

All entities inherit from `DatabaseEntity`:

```csharp
public class Journey : DatabaseEntity
{
    // Protected parameterless constructor for EF
    protected Journey() { }

    // Factory method
    public static Journey Create(Guid charityId, string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var journey = new Journey
        {
            CharityId = charityId,
            Name = name,
            Description = description,
            StatusId = JourneyStatus.Inactive.Id
        };

        return journey;
    }

    // Properties with protected setters
    public Guid CharityId { get; protected set; }
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public Guid StatusId { get; protected set; }
    public Guid? TriggerTagId { get; set; }  // public setter for simple properties

    // Collections
    internal virtual List<JourneyStep> Steps { get; set; } = [];

    // Private collection backing fields
    private List<JourneyStep> ActiveSteps => [.. Steps.Where(s => s.StatusId != JourneyStepStatus.Archived.Id)];

    // Public readonly collection exposing private backing
    public IEnumerable<JourneyStep> GetActiveSteps() => [.. ActiveSteps.OrderBy(s => s.Ordinal)];

    // Business logic methods
    public void UpdateDetails(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name;
        Description = description;
    }

    public void MakeActive()
    {
        if (StatusId == JourneyStatus.Archived.Id)
        {
            throw new InvalidOperationException("Cannot make an archived journey active");
        }

        StatusId = JourneyStatus.Active.Id;
    }

    // Expression-bodied for simple operations
    public void Archive() => StatusId = JourneyStatus.Archived.Id;
}
```

### Key Entity Patterns

**1. Protected Parameterless Constructor**
```csharp
protected Journey()
{
}
```

**2. Static Factory Method**
```csharp
public static Journey Create(Guid charityId, string name, string description)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

    var journey = new Journey
    {
        CharityId = charityId,
        Name = name,
        Description = description,
        StatusId = JourneyStatus.Inactive.Id
    };

    return journey;
}
```

**3. Object Initialization**
```csharp
// Opening brace on same line
var journey = new Journey
{
    CharityId = charityId,
    Name = name,
    Description = description,
    StatusId = JourneyStatus.Inactive.Id
};
```

**4. Property Protection**
```csharp
// Use protected set for properties that should only be modified through methods
public string Name { get; protected set; }

// Use public set for simple properties
public Guid? TriggerTagId { get; set; }

// Use internal for EF navigation properties
internal virtual List<JourneyStep> Steps { get; set; } = [];
```

**5. Collection Encapsulation**
```csharp
// Private backing field
private readonly List<Donor> _donors = [];

// Public readonly property
public virtual IReadOnlyCollection<Donor> Donors => [.. _donors];
```

**6. Method Parameters on Multiple Lines**

When methods have 6+ parameters:

```csharp
public void ConfigureMediaCampaign
(
    string name,
    string description,
    Guid mediaId,
    Guid? thumbnailMediaId,
    Guid? logoId,
    string senderEmail,
    string emailSubject,
    string body,
    string landingPageBody,
    bool showLandingPageCta,
    string landingPageCtaText,
    string landingPageCtaUrl,
    bool acceptResponses,
    string preferredShareMethod
)
{
    // implementation
}
```

**Formatting:**
- Opening parenthesis on new line (separate from method name)
- Each parameter on its own line, indented one level
- Closing parenthesis on its own line, aligned with method name

### Status Entities

Status entities use the **static readonly** pattern:

```csharp
public class JourneyStatus(string name) : StatusEntity(name, name, null, true)
{
    public static readonly JourneyStatus Active = new(StringCatalog.Journeys.Statuses.Active)
    {
        Id = Guid.Parse(KeyCatalog.Journeys.Statuses.Active),
        CreatedDate = DateTime.Parse("3/25/25").Date,
        CreatedBy = "system"
    };

    public static readonly JourneyStatus Inactive = new(StringCatalog.Journeys.Statuses.Inactive)
    {
        Id = Guid.Parse(KeyCatalog.Journeys.Statuses.Inactive),
        CreatedDate = DateTime.Parse("3/25/25").Date,
        CreatedBy = "system"
    };
}
```

### Metadata Indexers

Entities with metadata use indexer properties:

```csharp
internal virtual List<CharityMetadata> Metadata { get; set; } = default;

[NotMapped]
public string this[string key]
{
    get => Metadata is null
            ? null
            : Metadata.Any(md => md.Key.ToLower() == key.ToLower())
                ? Metadata.Single(md => md.Key.ToLower() == key.ToLower()).Value
                : null;

    set
    {
        Metadata ??= [];

        if (Metadata.Any(md => md.Key.ToLower() == key.ToLower()))
        {
            Metadata.Single(md => md.Key.ToLower() == key.ToLower()).Value = value;
        }
        else
        {
            Metadata.Add(CharityMetadata.Create(this, key, value));
        }
    }
}
```

**Ternary Formatting:**
- Multi-level ternaries each on their own line
- Indented to show nesting

---

## Value Objects

### Value Object Pattern

```csharp
public sealed class PhoneValue : ValueObject<PhoneValue>
{
    public PhoneValue(string phone)
    {
        if (!phone.IsNullOrWhitespace())
        {
            phone = phone.ToDigits().TrimStart('1');

            if (!Regex.IsMatch(phone, @"^(1|)[2-9]\d{2}[2-9]\d{6}$"))
            {
                throw new ArgumentException($"{phone} is not a valid phone number.");
            }

            Phone = phone;
        }
        else
        {
            Phone = null;
        }
    }

    public string Phone { get; private set; }

    public override string ToString() => Phone;

    protected override bool EqualsCore(PhoneValue other)
        => Phone == null
            ? (other.Phone == null)
            : Phone.Equals(other.Phone, StringComparison.InvariantCultureIgnoreCase);

    protected override int GetHashCodeCore() => Phone.GetHashCode();

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    // Implicit conversions
    public static implicit operator string(PhoneValue phone) => phone?.ToString();
    public static implicit operator PhoneValue(string s) => new(s);

    // Equality operators
    public static bool operator ==(PhoneValue phoneValue, string phone) => phoneValue?.Phone == phone;
    public static bool operator !=(PhoneValue phoneValue, string phone) => phoneValue?.Phone != phone;
}
```

**Key Points:**
- Use `sealed class`
- Inherit from `ValueObject<T>`
- Single property with private setter
- Validation in constructor
- Override equality members
- Provide implicit conversions to/from string
- Provide equality operators for common comparisons

---

## Repositories

### Repository Pattern

```csharp
public abstract class Repository(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public DbContextId DbContextId => _dbContext.ContextId;
}
```

**Interface:**
```csharp
public interface IJourneyRepository
{
    Task<Journey> LoadByIdAsync(Guid id);
    Task AddAsync(Journey journey);
    Task UnenrollAllAsync(Guid journeyId);
    Task SaveAsync();
}
```

**Implementation:**
```csharp
public class JourneyRepository(AppDbContext dbContext) : Repository(dbContext), IJourneyRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<Journey> LoadByIdAsync(Guid id)
    {
        return await _dbContext.Journeys
            .Include(j => j.Steps)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task AddAsync(Journey journey)
    {
        await _dbContext.Journeys.AddAsync(journey);
    }

    public async Task SaveAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
```

---

## Controllers

### Controller Structure

```csharp
namespace E313.Api.Controllers;

[Route("api/charities/me/journeys")]
[Authorize(Roles = StringCatalog.Roles.CharityManagerRoles)]
public class JourneyController
(
    IConfiguration configuration,
    ICharityRepository charityRepository,
    IMapper mapper,
    CommandDispatcher commandDispatcher,
    QueryDispatcher queryDispatcher,
    IUserRepository userRepository,
    IHttpContextAccessor httpContextAccessor
) : CharityControllerBase(charityRepository, mapper, commandDispatcher, queryDispatcher, userRepository, httpContextAccessor)
{
    // Request DTOs as nested records
    public record CreateOrUpdateJourneyRequest
    (
        Guid? Id,
        bool IsActive,
        bool IsArchived,
        string Name,
        string Description,
        string TriggerTag,
        JourneyStepRequest[] Steps
    );

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GetAllQueryResult>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var charityId = await GetLoggedInCharityIdAsync();

        if (charityId == null)
        {
            return NotFound();
        }

        var result = await _queryDispatcher.Dispatch<IEnumerable<GetAllQueryResult>>(new GetAllQuery(charityId.Value));

        return result.IsSuccessful ? Ok(result.Value) : BadRequest();
    }

    [HttpPost()]
    public async Task<IActionResult> CreateOrUpdateJourney([FromBody] CreateOrUpdateJourneyRequest request)
    {
        var charityId = await GetLoggedInCharityIdAsync();

        if (charityId == null)
        {
            return NotFound();
        }

        var steps = request.Steps?.Select(step => new JourneyStepItem
        (
            step.Id,
            step.Name,
            step.Description,
            step.Ordinal,
            step.IsConfigured,
            step.CampaignType,
            step.MediaId,
            step.ThumbnailMediaId,
            step.LogoId,
            step.SenderEmail,
            step.EmailSubject,
            step.Body,
            step.LandingPageBody,
            step.ShowLandingPageCta,
            step.LandingPageCtaText,
            step.LandingPageCtaUrl,
            step.AcceptResponses,
            step.PreferredShareMethod,
            step.HasRule,
            step.DayDelay,
            step.HourDelay,
            step.MinuteDelay
        )).ToArray();

        var command = new CreateOrUpdateCommand
        (
            request.Id,
            charityId.Value,
            request.IsActive,
            request.IsArchived,
            request.Name,
            request.Description,
            request.TriggerTag,
            steps ?? []
        );

        var result = await _commandDispatcher.Dispatch<Guid>(command);

        return result.IsSuccessful
            ? CreatedAtAction(nameof(GetJourneyDetails), new { id = result.Value }, null)
            : BadRequest();
    }
}
```

**Key Points:**
- Use primary constructors for dependency injection (6+ params = multi-line)
- Define request DTOs as nested `record` types
- Use ProducesResponseType attribute for documentation
- Use ternary for simple result mapping: `result.IsSuccessful ? Ok(result.Value) : BadRequest()`
- Null-coalescing for default collections: `steps ?? []`

---

## Azure Functions

### Function Structure

Azure Functions follow similar patterns but use Azure Functions Worker model:

```csharp
namespace KindKatch.Azure.Functions.ShareSending;

internal class EmailSender(IOptions<UrlSettings> settings, AppDbContext dbContext, IMessageBusService messageBusService, IEmailService emailService, JsonSerializerOptions jsonSerializerOptions, ILogger<EmailSender> logger)
{
    private readonly UrlSettings _settings = settings.Value;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IMessageBusService _messageBusService = messageBusService;
    private readonly IEmailService _emailService = emailService;
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions;
    private readonly ILogger _logger = logger;

    [Function(nameof(EmailSender))]
    public async Task RunAsync([ServiceBusTrigger(MessageBusQueueNames.Email, Connection = "MessageBusService:ConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions actions, FunctionContext functionContext)
    {
        // Function implementation
    }
}
```

**Multi-Parameter Function Methods:**

```csharp
[Function(nameof(SmsMediaProcessor))]
public async Task RunAsync
(
    [ServiceBusTrigger(MessageBusQueueNames.SmsMedia, Connection = "MessageBusService:ConnectionString")] ServiceBusReceivedMessage message,
    ServiceBusMessageActions actions,
    CancellationToken cancellationToken
)
{
    // Implementation
}
```

**Key Points:**
- Use `internal` visibility for function classes
- Primary constructor on single line for 6 or fewer parameters
- Primary constructor multi-line for 7+ parameters
- Function method parameters: multi-line when attributes are involved
- Use file-scoped helper types and extensions

---

## Error Handling

### Exception Types

**Domain/Business Rule Violations:**
```csharp
throw new Exception("charity not found.");
throw new ArgumentException($"{email} is not a valid email address.");
throw new ArgumentOutOfRangeException(nameof(step.CampaignType), $"Unknown campaign type: {step.CampaignType}");
throw new InvalidOperationException("Cannot make an archived journey active");
```

**Validation:**
```csharp
ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
ArgumentNullException.ThrowIfNull(stepId, nameof(stepId));
```

### CommandResult Pattern

```csharp
// Success
return CommandResult.Success();
return CommandResult<Guid>.Success(journey.Id);

// Failure
return CommandResult.Fail("Journey not found");
return CommandResult<Guid>.Fail("Invalid data");

// Exception handling in base class
// Handlers don't catch exceptions unless specific handling needed
```

### Try-Catch Usage

**Rarely used in handlers** - base classes handle exceptions:

```csharp
// Only use try-catch for specific recovery scenarios
protected override async Task<CommandResult> OnHandleAsync(CreateDonorImportCommand command)
{
    try
    {
        // operations that might fail
        var charity = await _charityRepository.GetCharityByIdAsync(command.CharityId);
        await _blobStorageService.UploadFileAsync(charity.Id.ToString(), donorImport.FileId, command.File, "application/vnd.ms-excel");

        return CommandResult.Success();
    }
    catch
    {
        return CommandResult.Fail("There was an error uploading the import file.");
    }
}
```

---

## Collections & Initialization

### Collection Initialization

**Use collection expressions:**

```csharp
// ✅ CORRECT
private readonly List<IEvent> _eventsToPublish = [];
internal virtual List<JourneyStep> Steps { get; set; } = [];
var keywords = ["STOP", "STOPALL", "UNSUBSCRIBE"];

// ❌ WRONG
private readonly List<IEvent> _eventsToPublish = new List<IEvent>();
internal virtual List<JourneyStep> Steps { get; set; } = new List<JourneyStep>();
```

### Collection Types

**Private backing fields:**
```csharp
private readonly List<Donor> _donors = [];
```

**Properties with EF:**
```csharp
// For EF navigation - use = default
internal virtual List<CharityMetadata> Metadata { get; set; } = default;

// or = default! to suppress nullability warnings
internal virtual List<JourneyStepRuleParameter> RuleParameters { get; set; } = default!;
```

**Public readonly collections:**
```csharp
public virtual IReadOnlyCollection<Donor> Donors => [.. _donors];
public IEnumerable<JourneyStep> GetActiveSteps() => [.. ActiveSteps.OrderBy(s => s.Ordinal)];
```

### Spread Operator

**Use spread operator `..` for collection conversions:**

```csharp
public virtual IReadOnlyCollection<Donor> Donors => [.. _donors];
public virtual IReadOnlyCollection<CharityMedia> CharityMedia => [.. _charityMedia];

// With LINQ
private List<JourneyStep> ActiveSteps => [.. Steps.Where(s => s.StatusId != JourneyStepStatus.Archived.Id)];
```

### Modern Array Initialization

**ALWAYS use collection expressions `[]` for arrays (C# 12+):**

```csharp
// ✅ CORRECT - Empty arrays
var tokens = expoPushTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? [];
return new ExpoPushResponse(false, [], [], ["No valid tokens"]);

// ✅ CORRECT - Array with values
var keywords = ["STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL"];
var errors = [errorMessage];
var multiple = [error1, error2, error3];

// ❌ WRONG - Old style
var tokens = expoPushTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? Array.Empty<string>();
return new ExpoPushResponse(false, Array.Empty<string>(), Array.Empty<string>(), new[] { "No valid tokens" });
```

**Deprecation Notice:**
- `Array.Empty<T>()` → Use `[]`
- `new[] { item1, item2 }` → Use `[item1, item2]`
- `new string[] { }` → Use `[]`

### Null-Coalescing Assignment

```csharp
// Initialize if null
Metadata ??= [];
Steps ??= [];
RuleParameters ??= [];
```

---

## Formatting & Whitespace

### Indentation

- **4 spaces** per indent level (enforced via .editorconfig)
- No tabs

### Braces

**Always use braces** for control flow, even single statements:

```csharp
// ✅ CORRECT
if (command.Id is null)
{
    journey = Journey.Create(command.CharityId, command.Name, command.Description);
}

// ❌ WRONG
if (command.Id is null)
    journey = Journey.Create(command.CharityId, command.Name, command.Description);
```

**Opening brace on same line for object initialization:**

```csharp
var journey = new Journey
{
    CharityId = charityId,
    Name = name,
    Description = description
};

var status = new JourneyStatus(StringCatalog.Journeys.Statuses.Active)
{
    Id = Guid.Parse(KeyCatalog.Journeys.Statuses.Active),
    CreatedDate = DateTime.Parse("3/25/25").Date,
    CreatedBy = "system"
};
```

### Field Sectioning with Blank Lines

**Use blank lines to separate logical "sections" of fields within a class:**

```csharp
// ✅ CORRECT - Blank lines between sections
public class ExpoPushNotificationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ExpoPushNotificationService> logger) : IExpoPushNotificationService
{
    // Section 1: Constructor parameter assignments
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly ILogger<ExpoPushNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string? _expoAccessToken = configuration["Expo:AccessToken"];

    // Section 2: Constants
    private const string _expoPushApiUrl = "https://exp.host/--/api/v2/push/send";

    // Section 3: Methods follow...
    public async Task<ExpoPushResponse> SendPushNotificationsAsync(...)
    {
        // ...
    }
}

// ❌ WRONG - No sectioning
public class ExpoPushNotificationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ExpoPushNotificationService> logger) : IExpoPushNotificationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly ILogger<ExpoPushNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string? _expoAccessToken = configuration["Expo:AccessToken"];
    private const string _expoPushApiUrl = "https://exp.host/--/api/v2/push/send";  // No separation

    public async Task<ExpoPushResponse> SendPushNotificationsAsync(...)
```

**Common sections:**
- Constructor-injected dependencies (private readonly fields)
- Constants (private const)
- Static fields
- Instance fields initialized inline
- Properties
- Methods

**Reasoning:** Blank lines create visual sections that group related members, making code structure immediately clear.

### Class/Record Declarations with Primary Constructors

**1-5 parameters - single line:**
```csharp
public class CreateOrUpdateCommandHandler(IJourneyRepository journeyRepository, ITagRepository tagRepository) : CommandHandler<CreateOrUpdateCommand, Guid>

public record CreateOrUpdateCommand(Guid? Id, Guid CharityId, bool IsActive, bool IsArchived, string Name, string Description, string TriggerTag, JourneyStepItem[] Steps) : ICommand;
```

**6+ parameters - opening paren on new line:**
```csharp
public class CreateQuickShareCommandHandler
(
    IDateTimeService dateTimeService,
    IDonorRepository donorRepository,
    IMessageRepository messageRepository,
    ICharityRepository charityRepository,
    EventDispatcher eventDispatcher
) : CommandHandler<CreateQuickShareCommand>(eventDispatcher)

public record JourneyStepItem
(
    Guid? Id,
    string Name,
    string Description,
    int Ordinal,
    bool IsConfigured,
    string CampaignType,
    Guid? MediaId,
    Guid? ThumbnailMediaId,
    Guid? LogoId,
    string SenderEmail,
    string EmailSubject,
    string Body,
    string LandingPageBody,
    bool ShowLandingPageCta,
    string LandingPageCtaText,
    string LandingPageCtaUrl,
    bool AcceptResponses,
    string PreferredShareMethod,
    bool HasRule,
    int DayDelay,
    int HourDelay,
    int MinuteDelay
);
```

### Method Declarations

**1-5 parameters - single line:**
```csharp
public static Journey Create(Guid charityId, string name, string description)
public async Task<Journey> LoadByIdAsync(Guid id)
```

**6+ parameters - opening paren on new line:**
```csharp
public void ConfigureMediaCampaign
(
    string name,
    string description,
    Guid mediaId,
    Guid? thumbnailMediaId,
    Guid? logoId,
    string senderEmail,
    string emailSubject,
    string body,
    string landingPageBody,
    bool showLandingPageCta,
    string landingPageCtaText,
    string landingPageCtaUrl,
    bool acceptResponses,
    string preferredShareMethod
)
{
    // implementation
}
```

### Method Calls with Many Parameters

**When calling methods/constructors with many parameters:**

```csharp
await _dbContext.UpdateMessageAttachmentAsync
(
    fileMetadata.Id,
    "File Exceeds Size Limit",
    size.Value,
    Statuses.InvalidId,
    cancellationBudget.Token
);

var command = new CreateOrUpdateCommand
(
    request.Id,
    charityId.Value,
    request.IsActive,
    request.IsArchived,
    request.Name,
    request.Description,
    request.TriggerTag,
    steps ?? []
);
```

### LINQ Query Formatting

**The "Mutation Drop" Pattern:**

Each LINQ operation that **mutates the result type** drops to a new indent level. Operations that **don't change the type** stay at the same level.

```csharp
// ✅ CORRECT - Different operations drop indent
var result = query.Results
    .Where(r => r.IsActive)
    .Where(r => r.Value > 10)
        .Select(r => r.RawValue)      // Mutation: IQueryable<Result> → IQueryable<int>
            .ToList();                 // Mutation: IQueryable<int> → List<int>

// Multiple Where at same level (same type), Select drops (changes type)
var commandStepIds = command.Steps
    .Where(s => s.Id.HasValue)
    .Where(s => s.IsConfigured)
        .Select(s => s.Id.Value)
            .ToHashSet();

// OrderBy doesn't mutate type, stays at same level
var activeSteps = journey.Steps
    .Where(s => s.StatusId != JourneyStepStatus.Archived.Id)
    .OrderBy(s => s.Ordinal)
        .Select(s => s.Name)
            .ToList();
```

**Reasoning:**
- **Same indent** = Same result type (e.g., multiple `Where` clauses)
- **Drop indent** = Type mutation (e.g., `Select`, `SelectMany`, `ToList`, `ToArray`, `FirstOrDefault`)
- Creates visual hierarchy showing data transformation pipeline

**Simple queries - single line:**
```csharp
var donors = charity.Donors.Where(d => d.Phone == phone && d.IsActive).ToList();
var count = items.Count(x => x.IsActive);
```

### Ternary Operators

**Simple ternary - single line:**
```csharp
var ruleType = step.HasRule ? RuleType.TimeDelay : RuleType.None;
return result.IsSuccessful ? Ok(result.Value) : BadRequest();
```

**Nested ternary - each level on new line:**
```csharp
get => Metadata is null
        ? null
        : Metadata.Any(md => md.Key.ToLower() == key.ToLower())
            ? Metadata.Single(md => md.Key.ToLower() == key.ToLower()).Value
            : null;

protected override bool EqualsCore(EmailValue other)
    => Email == null
        ? (other.Email == null)
        : Email.Equals(other.Email, StringComparison.InvariantCultureIgnoreCase);
```

### String Literals

**Use verbatim strings for regexes:**
```csharp
if (!Regex.IsMatch(phone, @"^(1|)[2-9]\d{2}[2-9]\d{6}$"))
```

**Use string interpolation over concatenation:**
```csharp
// ✅ CORRECT
throw new ArgumentException($"{phone} is not a valid phone number.");

// ❌ WRONG
throw new ArgumentException(phone + " is not a valid phone number.");
```

---

## Comments & Documentation

### General Philosophy

**Favor self-documenting code over comments.**

- Use clear variable names
- Use meaningful method names
- Keep methods focused and small
- Let the code speak for itself

### When to Comment

**Use comments sparingly, only for:**

1. **Complex business logic** that isn't obvious from the code
   ```csharp
   // Virtuous integration requires specific opt-in/opt-out handling
   // See: https://support.virtuoussoftware.com/article/123
   ```

2. **TODOs and FIXMEs**
   ```csharp
   // TODO: Refactor to use new caching strategy
   // FIXME: Handle edge case when donor has no email or phone
   ```

3. **Non-obvious constraints or requirements**
   ```csharp
   // Max file size enforced by Twilio API
   const int maxFileSizeInBytes = 300 * 1024 * 1024; // 300MB
   ```

### XML Documentation Comments

**Do NOT use XML documentation comments.**

- No `/// <summary>` tags
- No `/// <param>` tags
- No `/// <returns>` tags

The codebase does not use XML documentation.

### Commented-Out Code

**Commented-out code is acceptable but should be temporary.**

- ⚠️ Commented-out code should always produce a mental warning
- Eventually it should be refactored or removed
- If keeping for reference, add a comment explaining why

```csharp
// Keeping for reference until new caching is proven stable
//public async Task<Charity> GetCharityByIdAsync(Guid id)
//{
//    return await _cache.GetOrCreateAsync(id, async () =>
//        await _dbContext.Charities.FindAsync(id));
//}

public async Task<Charity> GetCharityByIdAsync(Guid id)
{
    return await _dbContext.Charities.FindAsync(id);
}
```

---

## Summary

This style guide captures the modern C# patterns and conventions established in the KindKatch API codebase from 2024-2025. The key themes are:

1. **Modern C# Features**: Leverage C# 12 features like primary constructors, collection expressions, file-scoped namespaces, and pattern matching
2. **CQRS Architecture**: Strict separation of commands and queries
3. **DDD Principles**: Rich domain models with encapsulation, value objects, and aggregate roots
4. **Consistency**: Uniform patterns across handlers, entities, repositories, and functions
5. **Readability**: Code that is self-documenting through clear naming and structure
6. **Vertical Slice Architecture**: Emerging pattern with file-scoped types for vertical slices

When in doubt, refer to recent implementations in:
- `E313.KindKatch.CommandHandlers\Journeys\CreateOrUpdate.cs`
- `E313.KindKatch.QueryHandlers\Dashboard\GetMediaViews.cs`
- `E313.Data\Entities\Journey.cs`
- `Web\Controllers\JourneyController.cs`
- `KindKatch.Azure.Functions.SmsMediaProcessing\Functions\SmsMediaProcessor.cs`

These files represent the current best practices and should serve as reference implementations.

---

## Version History

### Version 2.1 - January 2026

**Added:**
- Modern array initialization using collection expressions (`[]`)
- Const naming convention for private constants (`_lowerCamelCase`)
- Explicit rule for `var` usage in loop counters
- Record property naming must always be PascalCase (including private records)
- Whitespace requirements between multiple record definitions
- Vertical slice architecture guidance for service organization
- Field sectioning rule: Use blank lines to separate logical sections (constructor fields, constants, static fields, etc.)

**Changed:**
- Deprecated `Array.Empty<T>()` in favor of `[]`
- Deprecated `new[] { }` array initialization in favor of `[]`
- Deprecated `UPPER_CASE` for private const in favor of `_lowerCamelCase`
- Services should have interfaces colocated in same file (vertical slice), not in separate *.Contracts projects
- KindKatch.Core is the preferred location for new services

**Clarified:**
- LINQ "mutation drop" pattern examples now include `Select` as first operation scenario
- Field organization within classes should use blank lines to create visual "sections"

**Rationale:**
These changes align with modern C# 12 collection expressions, improve code readability through intentional whitespace, and move toward vertical slice architecture.

### Version 2.0 - December 2025

**Changed:**
- Removed `record struct` - always use `record`
- Removed `sealed` as standard practice
- Clarified parameter formatting: 6+ parameters = multi-line
- Explained LINQ "mutation drop" pattern
- Standardized null validation for all dependencies
- Documented async naming requirement

**Added:**
- Azure Functions section and analysis
- Comments & documentation philosophy
- File-scoped types as vertical slice architecture
- KindKatch.* projects to analysis scope

**Clarified:**
- When to use file-scoped types
- Commented-out code policy
- Using directive automation

### Version 1.0 - December 2025

- Initial release
- Comprehensive analysis of E313.* projects
- CQRS and DDD pattern documentation
