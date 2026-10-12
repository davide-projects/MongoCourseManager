# NTT DATA - Esercitazione .NET 10 + MongoDB

## API REST a livelli DAL / BLL / PL con Repository, Service e AutoMapper

**Docente:** Marco Storelli  
**Tecnologie:** .NET 10, ASP.NET Core Web API, MongoDB, MongoDB.Driver, AutoMapper, Visual Studio  
**Tipologia:** laboratorio guidato + esercizi semi-guidati + completamento autonomo

---

# 1. Obiettivo dell'esercitazione

In questa esercitazione realizzeremo una piccola API per la gestione di un **catalogo corsi aziendale**.

Lo scopo principale non è costruire un'applicazione complessa, ma capire come integrare MongoDB in un progetto C# strutturato su più livelli, mantenendo separate le responsabilità.

Utilizzeremo questa architettura:

```text
Client / Swagger / file .http
            |
            v
+---------------------------+
| PL - Presentation Layer   |
| ASP.NET Core Web API      |
| - Controller              |
| - Dependency Injection    |
| - AutoMapper Profile      |
+-------------+-------------+
              |
              v
+---------------------------+
| BLL - Business Logic      |
| - Model                   |
| - Service                 |
| - Interfacce dei Service  |
| - Regole applicative      |
+-------------+-------------+
              |
              v
+---------------------------+
| DAL - Data Access Layer   |
| - Entity                  |
| - Repository              |
| - Interfacce Repository   |
| - MongoDbContext          |
+-------------+-------------+
              |
              v
+---------------------------+
| MongoDB                   |
| database: TrainingDb      |
| collection: courses       |
+---------------------------+
```

Alla fine del laboratorio avremo API per:

- creare un corso;
- leggere un corso per `id`;
- leggere un corso per `code`;
- cercare i corsi con filtri;
- ordinare i risultati;
- paginare i risultati;
- aggiornare un corso;
- eliminare un corso;
- pubblicare o nascondere un corso;
- aggiungere un tag senza duplicati;
- rimuovere un tag;
- ottenere il numero totale dei risultati di una ricerca.

Quindi useremo sia le classiche operazioni CRUD sia alcune operazioni MongoDB leggermente più avanzate.

---

# 2. Concetti che vogliamo allenare

Durante l'esercitazione useremo questi concetti.

| Concetto | Dove lo vedremo |
|---|---|
| Connessione a MongoDB | `MongoDbContext` |
| `IMongoCollection<T>` | DAL |
| `Find()` | ricerca e lettura |
| `InsertOneAsync()` | creazione |
| `UpdateOneAsync()` | modifica |
| `$set` | aggiornamento campi |
| `$addToSet` | aggiunta tag senza duplicati |
| `$pull` | rimozione tag |
| `DeleteOneAsync()` | cancellazione |
| `Builders<T>.Filter` | filtri dinamici |
| `SortBy` / `SortByDescending` | ordinamento |
| `Skip()` / `Limit()` | paginazione |
| `CountDocumentsAsync()` | totale risultati |
| Repository Pattern | DAL |
| Service Layer | BLL |
| AutoMapper | trasformazione Entity <-> Model |
| Dependency Injection | `Program.cs` |
| Controller REST | PL |

---

# 3. Scenario applicativo

Ogni corso avrà una struttura simile alla seguente:

```json
{
  "_id": "68e123456789abcdef123456",
  "code": "CSHARP-BASE",
  "title": "C# Base",
  "category": "Development",
  "durationHours": 24,
  "isPublished": true,
  "tags": [
    "csharp",
    "dotnet"
  ],
  "createdAt": "2026-10-12T08:00:00Z",
  "updatedAt": "2026-10-12T08:00:00Z"
}
```

Notare che `tags` è un array MongoDB. Lo useremo per provare operazioni specifiche sugli array.

---

# 4. Prerequisiti

Prima di iniziare verificare di avere:

1. **Visual Studio** con workload ASP.NET e sviluppo web;
2. **.NET 10 SDK**;
3. **MongoDB** disponibile in locale oppure un cluster MongoDB raggiungibile;
4. facoltativamente **MongoDB Compass** per visualizzare database, collection e documenti.

Per MongoDB locale useremo nell'esercitazione:

```text
mongodb://localhost:27017
```

MongoDB usa normalmente la porta:

```text
27017
```

> Se state usando MongoDB Atlas, sostituite la connection string locale con quella del vostro cluster.

---

# 5. Creazione della solution con Visual Studio

## 5.1 Creare la solution

Aprire Visual Studio e scegliere:

```text
Create a new project
```

Creare inizialmente un progetto:

```text
ASP.NET Core Web API
```

Nome progetto:

```text
MongoCourseManager.PL
```

Nome solution:

```text
MongoCourseManager
```

Framework:

```text
.NET 10.0
```

Lasciare abilitato HTTPS.

Se Visual Studio propone OpenAPI, lasciarlo abilitato.

---

## 5.2 Aggiungere il progetto DAL

Click destro sulla solution:

```text
Add
 -> New Project
 -> Class Library
```

Nome:

```text
MongoCourseManager.DAL
```

Framework:

```text
.NET 10.0
```

---

## 5.3 Aggiungere il progetto BLL

Ripetere l'operazione creando:

```text
MongoCourseManager.BLL
```

Tipo progetto:

```text
Class Library
```

Framework:

```text
.NET 10.0
```

Alla fine avremo:

```text
MongoCourseManager
|
+-- MongoCourseManager.DAL
+-- MongoCourseManager.BLL
+-- MongoCourseManager.PL
```

---

# 6. Aggiungere i riferimenti tra i progetti

La BLL deve poter utilizzare repository ed entity del DAL.

Aggiungere quindi:

```text
MongoCourseManager.BLL
    -> reference a MongoCourseManager.DAL
```

La PL deve poter registrare e utilizzare componenti BLL e DAL.

Aggiungere:

```text
MongoCourseManager.PL
    -> reference a MongoCourseManager.BLL
    -> reference a MongoCourseManager.DAL
```

Schema finale:

```text
PL ------> BLL ------> DAL
 |                     ^
 +---------------------+
```

La PL ha un riferimento al DAL principalmente per configurare Dependency Injection e AutoMapper.

---

# 7. Installazione dei pacchetti NuGet

## 7.1 DAL

Nel progetto:

```text
MongoCourseManager.DAL
```

installare:

```text
MongoDB.Driver
```

Da Visual Studio:

```text
Dependencies
 -> Manage NuGet Packages
 -> Browse
 -> MongoDB.Driver
```

Installare una versione stabile compatibile con .NET 10.

Il pacchetto `MongoDB.Driver` è il driver ufficiale MongoDB per .NET/C#.

---

## 7.2 BLL

Nel progetto:

```text
MongoCourseManager.BLL
```

installare:

```text
AutoMapper
```

La BLL userà `IMapper` all'interno dei Service.

---

## 7.3 PL

Nel progetto:

```text
MongoCourseManager.PL
```

installare:

```text
AutoMapper
Swashbuckle.AspNetCore
```

`Swashbuckle.AspNetCore` ci permette di utilizzare Swagger per provare facilmente le API.

> **Nota AutoMapper:** nelle versioni moderne di AutoMapper la registrazione Dependency Injection fa parte del package `AutoMapper`; non è necessario installare il vecchio package `AutoMapper.Extensions.Microsoft.DependencyInjection`. Dalla versione 15 AutoMapper prevede inoltre un sistema di licenza: durante il corso utilizzate la versione e la licenza previste dalla vostra organizzazione/docente.

---

# 8. Struttura finale dei progetti

Creeremo la seguente struttura.

```text
MongoCourseManager
|
+-- MongoCourseManager.DAL
|   |
|   +-- Configuration
|   |   +-- MongoDbSettings.cs
|   |
|   +-- Context
|   |   +-- MongoDbContext.cs
|   |
|   +-- Entities
|   |   +-- CourseEntity.cs
|   |
|   +-- Repositories
|       |
|       +-- Interfaces
|       |   +-- ICourseRepository.cs
|       |
|       +-- CourseRepository.cs
|
+-- MongoCourseManager.BLL
|   |
|   +-- Models
|   |   +-- CourseModel.cs
|   |   +-- CreateCourseModel.cs
|   |   +-- UpdateCourseModel.cs
|   |   +-- PagedResultModel.cs
|   |
|   +-- Services
|       |
|       +-- Interfaces
|       |   +-- ICourseService.cs
|       |
|       +-- CourseService.cs
|
+-- MongoCourseManager.PL
    |
    +-- Controllers
    |   +-- CoursesController.cs
    |
    +-- Mapping
    |   +-- MappingProfile.cs
    |
    +-- Program.cs
    +-- appsettings.json
```

---

# PARTE 1 - DAL

# 9. Creare la configurazione MongoDB

Nel DAL creare la cartella:

```text
Configuration
```

Creare:

```text
MongoDbSettings.cs
```

Codice:

```csharp
namespace MongoCourseManager.DAL.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
```

Questa classe conterrà i dati letti da `appsettings.json`.

Non stiamo ancora collegandoci al database: stiamo solo definendo la struttura della configurazione.

---

# 10. Creare la Entity MongoDB

Nel DAL creare:

```text
Entities/CourseEntity.cs
```

Codice:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoCourseManager.DAL.Entities;

public class CourseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("durationHours")]
    public int DurationHours { get; set; }

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
```

## Cosa fanno gli attributi BSON?

### `[BsonId]`

Indica che `Id` corrisponde al campo MongoDB:

```text
_id
```

### `[BsonRepresentation(BsonType.ObjectId)]`

Nel codice C# lavoriamo comodamente con una `string`, mentre MongoDB salva il valore come `ObjectId`.

### `[BsonElement("title")]`

Permette di specificare il nome del campo nel documento MongoDB.

Per esempio:

```csharp
public int DurationHours { get; set; }
```

diventa:

```json
"durationHours": 24
```

---

# 11. Creare MongoDbContext

Creare:

```text
Context/MongoDbContext.cs
```

Codice:

```csharp
using Microsoft.Extensions.Options;
using MongoCourseManager.DAL.Configuration;
using MongoCourseManager.DAL.Entities;
using MongoDB.Driver;

namespace MongoCourseManager.DAL.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<CourseEntity> Courses =>
        _database.GetCollection<CourseEntity>("courses");
}
```

## Cosa succede?

Questa riga:

```csharp
var mongoClient = new MongoClient(settings.Value.ConnectionString);
```

crea il client utilizzato per collegarsi a MongoDB.

Questa:

```csharp
_database = mongoClient.GetDatabase(settings.Value.DatabaseName);
```

seleziona il database.

Infine:

```csharp
_database.GetCollection<CourseEntity>("courses")
```

restituisce un oggetto:

```csharp
IMongoCollection<CourseEntity>
```

che rappresenta la collection MongoDB `courses`.

> Nel nostro progetto `MongoDbContext` verrà registrato come Singleton. In questo modo viene creato un solo `MongoClient` per l'applicazione, che è l'approccio raccomandato per il client MongoDB.

---

# 12. Creare l'interfaccia del Repository

Creare:

```text
Repositories/Interfaces/ICourseRepository.cs
```

Codice:

```csharp
using MongoCourseManager.DAL.Entities;

namespace MongoCourseManager.DAL.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<CourseEntity?> GetByIdAsync(string id);

    Task<CourseEntity?> GetByCodeAsync(string code);

    Task<(IReadOnlyCollection<CourseEntity> Items, long Total)> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int page,
        int pageSize,
        bool sortDescending);

    Task<CourseEntity> CreateAsync(CourseEntity entity);

    Task<bool> UpdateAsync(CourseEntity entity);

    Task<bool> DeleteAsync(string id);

    Task<bool> SetPublishedAsync(string id, bool isPublished);

    Task<bool> AddTagAsync(string id, string tag);

    Task<bool> RemoveTagAsync(string id, string tag);
}
```

Il Service non dovrà conoscere direttamente `IMongoCollection<T>`.

La BLL parlerà solamente con questa interfaccia.

---

# 13. Creare il Repository

Creare:

```text
Repositories/CourseRepository.cs
```

Codice completo:

```csharp
using MongoCourseManager.DAL.Context;
using MongoCourseManager.DAL.Entities;
using MongoCourseManager.DAL.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace MongoCourseManager.DAL.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly IMongoCollection<CourseEntity> _collection;

    public CourseRepository(MongoDbContext context)
    {
        _collection = context.Courses;
    }

    public async Task<CourseEntity?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return null;

        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<CourseEntity?> GetByCodeAsync(string code)
    {
        return await _collection
            .Find(x => x.Code == code)
            .FirstOrDefaultAsync();
    }

    public async Task<(IReadOnlyCollection<CourseEntity> Items, long Total)> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int page,
        int pageSize,
        bool sortDescending)
    {
        var filterBuilder = Builders<CourseEntity>.Filter;
        var filters = new List<FilterDefinition<CourseEntity>>();

        if (!string.IsNullOrWhiteSpace(text))
        {
            var regex = new BsonRegularExpression(
                Regex.Escape(text.Trim()),
                "i");

            filters.Add(
                filterBuilder.Or(
                    filterBuilder.Regex(x => x.Title, regex),
                    filterBuilder.Regex(x => x.Code, regex)
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(filterBuilder.Eq(x => x.Category, category));
        }

        if (isPublished.HasValue)
        {
            filters.Add(filterBuilder.Eq(x => x.IsPublished, isPublished.Value));
        }

        var finalFilter = filters.Count == 0
            ? FilterDefinition<CourseEntity>.Empty
            : filterBuilder.And(filters);

        var total = await _collection.CountDocumentsAsync(finalFilter);

        var query = _collection.Find(finalFilter);

        query = sortDescending
            ? query.SortByDescending(x => x.Title)
            : query.SortBy(x => x.Title);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<CourseEntity> CreateAsync(CourseEntity entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task<bool> UpdateAsync(CourseEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id) ||
            !ObjectId.TryParse(entity.Id, out _))
        {
            return false;
        }

        var filter = Builders<CourseEntity>.Filter.Eq(x => x.Id, entity.Id);

        var update = Builders<CourseEntity>.Update
            .Set(x => x.Code, entity.Code)
            .Set(x => x.Title, entity.Title)
            .Set(x => x.Category, entity.Category)
            .Set(x => x.DurationHours, entity.DurationHours)
            .Set(x => x.IsPublished, entity.IsPublished)
            .Set(x => x.Tags, entity.Tags)
            .Set(x => x.UpdatedAt, entity.UpdatedAt);

        var result = await _collection.UpdateOneAsync(filter, update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var result = await _collection.DeleteOneAsync(x => x.Id == id);

        return result.DeletedCount > 0;
    }

    public async Task<bool> SetPublishedAsync(string id, bool isPublished)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<CourseEntity>.Update
            .Set(x => x.IsPublished, isPublished)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> AddTagAsync(string id, string tag)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<CourseEntity>.Update
            .AddToSet(x => x.Tags, tag)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> RemoveTagAsync(string id, string tag)
    {
        if (!ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<CourseEntity>.Update
            .Pull(x => x.Tags, tag)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.MatchedCount > 0;
    }
}
```

---

# 14. Analizziamo le operazioni MongoDB del Repository

## 14.1 Find

```csharp
_collection.Find(x => x.Id == id)
```

serve a cercare documenti.

Equivale concettualmente a una query MongoDB simile a:

```javascript
db.courses.find({ _id: ObjectId("...") })
```

---

## 14.2 InsertOneAsync

```csharp
await _collection.InsertOneAsync(entity);
```

inserisce un singolo documento.

Concettualmente:

```javascript
db.courses.insertOne({ ... })
```

---

## 14.3 UpdateOneAsync + `$set`

Nel driver C# scriviamo:

```csharp
var update = Builders<CourseEntity>.Update
    .Set(x => x.Title, entity.Title)
    .Set(x => x.Category, entity.Category);
```

che corrisponde concettualmente a:

```javascript
{
  $set: {
    title: "Nuovo titolo",
    category: "Development"
  }
}
```

Il documento non viene necessariamente sostituito completamente: stiamo indicando esplicitamente quali campi aggiornare.

---

## 14.4 `$addToSet`

Per aggiungere un tag utilizziamo:

```csharp
.AddToSet(x => x.Tags, tag)
```

`$addToSet` è particolarmente utile perché aggiunge il valore solo se non è già presente.

Se il documento contiene:

```json
"tags": ["csharp", "dotnet"]
```

e proviamo ad aggiungere nuovamente:

```text
dotnet
```

non otterremo un duplicato.

---

## 14.5 `$pull`

Per rimuovere un elemento dall'array:

```csharp
.Pull(x => x.Tags, tag)
```

Concettualmente:

```javascript
{
  $pull: {
    tags: "dotnet"
  }
}
```

---

## 14.6 DeleteOneAsync

```csharp
await _collection.DeleteOneAsync(x => x.Id == id);
```

elimina il documento che soddisfa il filtro.

---

## 14.7 Filtri dinamici

In `SearchAsync()` costruiamo una lista di filtri:

```csharp
var filters = new List<FilterDefinition<CourseEntity>>();
```

Aggiungiamo un filtro solamente se il parametro corrispondente è stato passato.

Per esempio:

```csharp
if (isPublished.HasValue)
{
    filters.Add(
        filterBuilder.Eq(x => x.IsPublished, isPublished.Value));
}
```

Infine combiniamo i filtri con:

```csharp
filterBuilder.And(filters)
```

Questo permette di costruire una ricerca flessibile senza creare un metodo diverso per ogni possibile combinazione.

---

# PARTE 2 - BLL

# 15. Creare i Model

Le Entity rappresentano il modo in cui i dati sono memorizzati nel DAL.

Non vogliamo però utilizzare direttamente le Entity nel Controller.

Per questo introduciamo i Model nella BLL.

---

# 16. CourseModel

Creare:

```text
Models/CourseModel.cs
```

```csharp
namespace MongoCourseManager.BLL.Models;

public class CourseModel
{
    public string? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int DurationHours { get; set; }

    public bool IsPublished { get; set; }

    public List<string> Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
```

---

# 17. CreateCourseModel

Creare:

```text
Models/CreateCourseModel.cs
```

```csharp
namespace MongoCourseManager.BLL.Models;

public class CreateCourseModel
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int DurationHours { get; set; }

    public bool IsPublished { get; set; }

    public List<string> Tags { get; set; } = [];
}
```

Notare che qui non abbiamo:

```text
Id
CreatedAt
UpdatedAt
```

perché saranno gestiti dall'applicazione.

---

# 18. UpdateCourseModel

Creare:

```text
Models/UpdateCourseModel.cs
```

```csharp
namespace MongoCourseManager.BLL.Models;

public class UpdateCourseModel
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int DurationHours { get; set; }

    public bool IsPublished { get; set; }

    public List<string> Tags { get; set; } = [];
}
```

---

# 19. PagedResultModel

La ricerca sarà paginata.

Creare:

```text
Models/PagedResultModel.cs
```

```csharp
namespace MongoCourseManager.BLL.Models;

public class PagedResultModel<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = [];

    public long TotalItems { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public long TotalPages =>
        PageSize <= 0
            ? 0
            : (long)Math.Ceiling(TotalItems / (double)PageSize);
}
```

Una risposta potrà quindi avere questa forma:

```json
{
  "items": [
    {
      "id": "...",
      "code": "CSHARP-BASE",
      "title": "C# Base"
    }
  ],
  "totalItems": 25,
  "page": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

---

# 20. Creare l'interfaccia del Service

Creare:

```text
Services/Interfaces/ICourseService.cs
```

```csharp
using MongoCourseManager.BLL.Models;

namespace MongoCourseManager.BLL.Services.Interfaces;

public interface ICourseService
{
    Task<CourseModel?> GetByIdAsync(string id);

    Task<PagedResultModel<CourseModel>> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int page,
        int pageSize,
        bool sortDescending);

    Task<CourseModel> CreateAsync(CreateCourseModel model);

    Task<CourseModel?> UpdateAsync(string id, UpdateCourseModel model);

    Task<bool> DeleteAsync(string id);

    Task<bool> SetPublishedAsync(string id, bool isPublished);

    Task<bool> AddTagAsync(string id, string tag);

    Task<bool> RemoveTagAsync(string id, string tag);
}
```

---

# 21. Creare il Service

Creare:

```text
Services/CourseService.cs
```

Codice:

```csharp
using AutoMapper;
using MongoCourseManager.BLL.Models;
using MongoCourseManager.BLL.Services.Interfaces;
using MongoCourseManager.DAL.Entities;
using MongoCourseManager.DAL.Repositories.Interfaces;

namespace MongoCourseManager.BLL.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _repository;
    private readonly IMapper _mapper;

    public CourseService(
        ICourseRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CourseModel?> GetByIdAsync(string id)
    {
        var entity = await _repository.GetByIdAsync(id);

        return entity is null
            ? null
            : _mapper.Map<CourseModel>(entity);
    }

    public async Task<PagedResultModel<CourseModel>> SearchAsync(
        string? text,
        string? category,
        bool? isPublished,
        int page,
        int pageSize,
        bool sortDescending)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        pageSize = pageSize > 50 ? 50 : pageSize;

        var result = await _repository.SearchAsync(
            text,
            category,
            isPublished,
            page,
            pageSize,
            sortDescending);

        return new PagedResultModel<CourseModel>
        {
            Items = _mapper.Map<IReadOnlyCollection<CourseModel>>(result.Items),
            TotalItems = result.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CourseModel> CreateAsync(CreateCourseModel model)
    {
        ValidateCourse(
            model.Code,
            model.Title,
            model.Category,
            model.DurationHours);

        var normalizedCode = model.Code.Trim().ToUpperInvariant();

        var existingCourse = await _repository.GetByCodeAsync(
            normalizedCode);

        if (existingCourse is not null)
        {
            throw new InvalidOperationException(
                $"Esiste già un corso con codice '{model.Code}'.");
        }

        var entity = _mapper.Map<CourseEntity>(model);

        entity.Code = normalizedCode;
        entity.Title = entity.Title.Trim();
        entity.Category = entity.Category.Trim();
        entity.Tags = NormalizeTags(entity.Tags);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.CreateAsync(entity);

        return _mapper.Map<CourseModel>(created);
    }

    public async Task<CourseModel?> UpdateAsync(
        string id,
        UpdateCourseModel model)
    {
        ValidateCourse(
            model.Code,
            model.Title,
            model.Category,
            model.DurationHours);

        var currentEntity = await _repository.GetByIdAsync(id);

        if (currentEntity is null)
            return null;

        var courseWithSameCode = await _repository.GetByCodeAsync(
            model.Code.Trim().ToUpperInvariant());

        if (courseWithSameCode is not null &&
            courseWithSameCode.Id != id)
        {
            throw new InvalidOperationException(
                $"Esiste già un altro corso con codice '{model.Code}'.");
        }

        _mapper.Map(model, currentEntity);

        currentEntity.Code = currentEntity.Code.Trim().ToUpperInvariant();
        currentEntity.Title = currentEntity.Title.Trim();
        currentEntity.Category = currentEntity.Category.Trim();
        currentEntity.Tags = NormalizeTags(currentEntity.Tags);
        currentEntity.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(currentEntity);

        return updated
            ? _mapper.Map<CourseModel>(currentEntity)
            : null;
    }

    public Task<bool> DeleteAsync(string id)
    {
        return _repository.DeleteAsync(id);
    }

    public Task<bool> SetPublishedAsync(
        string id,
        bool isPublished)
    {
        return _repository.SetPublishedAsync(id, isPublished);
    }

    public async Task<bool> AddTagAsync(string id, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Il tag è obbligatorio.");

        var normalizedTag = tag.Trim().ToLowerInvariant();

        return await _repository.AddTagAsync(id, normalizedTag);
    }

    public async Task<bool> RemoveTagAsync(string id, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Il tag è obbligatorio.");

        var normalizedTag = tag.Trim().ToLowerInvariant();

        return await _repository.RemoveTagAsync(id, normalizedTag);
    }

    private static void ValidateCourse(
        string code,
        string title,
        string category,
        int durationHours)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Il codice è obbligatorio.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Il titolo è obbligatorio.");

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("La categoria è obbligatoria.");

        if (durationHours <= 0)
            throw new ArgumentException(
                "La durata deve essere maggiore di zero.");
    }

    private static List<string> NormalizeTags(
        IEnumerable<string>? tags)
    {
        return tags?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList()
            ?? [];
    }
}
```

---

# 22. Perché la logica è nel Service?

Osserviamo questa parte:

```csharp
if (durationHours <= 0)
    throw new ArgumentException(
        "La durata deve essere maggiore di zero.");
```

Questa non è una responsabilità di MongoDB.

È una **regola applicativa**.

Per questo si trova nella BLL.

Lo stesso vale per:

```csharp
entity.Code = entity.Code.Trim().ToUpperInvariant();
```

oppure per la normalizzazione dei tag.

Il DAL deve sapere **come salvare e leggere i dati**.

La BLL deve sapere **quali regole applicare ai dati**.

---

# PARTE 3 - PL / API

# 23. Configurare AutoMapper

Nel progetto PL creare:

```text
Mapping/MappingProfile.cs
```

Codice:

```csharp
using AutoMapper;
using MongoCourseManager.BLL.Models;
using MongoCourseManager.DAL.Entities;

namespace MongoCourseManager.PL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CourseEntity, CourseModel>();

        CreateMap<CreateCourseModel, CourseEntity>()
            .ForMember(
                destination => destination.Id,
                options => options.Ignore())
            .ForMember(
                destination => destination.CreatedAt,
                options => options.Ignore())
            .ForMember(
                destination => destination.UpdatedAt,
                options => options.Ignore());

        CreateMap<UpdateCourseModel, CourseEntity>()
            .ForMember(
                destination => destination.Id,
                options => options.Ignore())
            .ForMember(
                destination => destination.CreatedAt,
                options => options.Ignore())
            .ForMember(
                destination => destination.UpdatedAt,
                options => options.Ignore());
    }
}
```

Abbiamo tre mapping.

### Entity -> Model

```csharp
CreateMap<CourseEntity, CourseModel>();
```

usato durante le letture.

### CreateModel -> Entity

```csharp
CreateMap<CreateCourseModel, CourseEntity>()
```

usato durante la creazione.

### UpdateModel -> Entity

```csharp
CreateMap<UpdateCourseModel, CourseEntity>()
```

usato durante la modifica.

---

# 24. Configurare appsettings.json

Nel progetto PL modificare `appsettings.json`.

Esempio:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "TrainingDb"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Non è necessario creare manualmente il database `TrainingDb`.

MongoDB lo materializzerà quando verrà scritto il primo documento.

Lo stesso vale per la collection `courses`.

---

# 25. Configurare Program.cs

Sostituire il contenuto di `Program.cs` con:

```csharp
using MongoCourseManager.BLL.Services;
using MongoCourseManager.BLL.Services.Interfaces;
using MongoCourseManager.DAL.Configuration;
using MongoCourseManager.DAL.Context;
using MongoCourseManager.DAL.Repositories;
using MongoCourseManager.DAL.Repositories.Interfaces;
using MongoCourseManager.PL.Mapping;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddAutoMapper(
    config =>
    {
        var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];

        if (!string.IsNullOrWhiteSpace(licenseKey))
        {
            config.LicenseKey = licenseKey;
        }
    },
    typeof(MappingProfile));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
```

## Osserviamo le registrazioni

### Configurazione MongoDB

```csharp
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));
```

lega la sezione:

```json
"MongoDb": {
  ...
}
```

alla classe:

```csharp
MongoDbSettings
```

### Context

```csharp
builder.Services.AddSingleton<MongoDbContext>();
```

Il Context viene condiviso per tutta la vita dell'applicazione.

### Repository

```csharp
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
```

### Service

```csharp
builder.Services.AddScoped<ICourseService, CourseService>();
```

### AutoMapper

```csharp
builder.Services.AddAutoMapper(..., typeof(MappingProfile));
```

AutoMapper cercherà il nostro `MappingProfile` e registrerà `IMapper` nella Dependency Injection.

> Se la versione AutoMapper adottata nel vostro ambiente richiede una chiave di licenza, valorizzate `AutoMapper:LicenseKey` tramite configurazione sicura o secondo le procedure aziendali. Non inserite una licenza reale nel repository Git.

---

# 26. Creare CoursesController

Creare:

```text
Controllers/CoursesController.cs
```

Codice:

```csharp
using Microsoft.AspNetCore.Mvc;
using MongoCourseManager.BLL.Models;
using MongoCourseManager.BLL.Services.Interfaces;

namespace MongoCourseManager.PL.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _service;

    public CoursesController(ICourseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultModel<CourseModel>>> Search(
        [FromQuery] string? text,
        [FromQuery] string? category,
        [FromQuery] bool? isPublished,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool sortDescending = false)
    {
        var result = await _service.SearchAsync(
            text,
            category,
            isPublished,
            page,
            pageSize,
            sortDescending);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseModel>> GetById(string id)
    {
        var course = await _service.GetByIdAsync(id);

        return course is null
            ? NotFound()
            : Ok(course);
    }

    [HttpPost]
    public async Task<ActionResult<CourseModel>> Create(
        [FromBody] CreateCourseModel model)
    {
        try
        {
            var created = await _service.CreateAsync(model);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CourseModel>> Update(
        string id,
        [FromBody] UpdateCourseModel model)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, model);

            return updated is null
                ? NotFound()
                : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);

        return deleted
            ? NoContent()
            : NotFound();
    }

    [HttpPatch("{id}/publication")]
    public async Task<IActionResult> SetPublished(
        string id,
        [FromQuery] bool published)
    {
        var updated = await _service.SetPublishedAsync(
            id,
            published);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpPost("{id}/tags")]
    public async Task<IActionResult> AddTag(
        string id,
        [FromBody] string tag)
    {
        try
        {
            var updated = await _service.AddTagAsync(id, tag);

            return updated
                ? NoContent()
                : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(
        string id,
        string tag)
    {
        try
        {
            var updated = await _service.RemoveTagAsync(id, tag);

            return updated
                ? NoContent()
                : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
```

---

# 27. Flusso completo di una richiesta

Supponiamo che il client invii:

```http
POST /api/courses
```

con:

```json
{
  "code": "mongo-csharp",
  "title": "MongoDB con C#",
  "category": "Database",
  "durationHours": 16,
  "isPublished": true,
  "tags": [
    "MongoDB",
    "CSharp",
    "mongodb"
  ]
}
```

Il flusso sarà:

```text
1. CoursesController
       |
       v
2. ICourseService / CourseService
       |
       |-- controlla i dati
       |-- normalizza Code
       |-- elimina tag duplicati
       |-- usa AutoMapper
       v
3. ICourseRepository / CourseRepository
       |
       |-- InsertOneAsync
       v
4. MongoDB
```

Il documento memorizzato sarà simile a:

```json
{
  "_id": ObjectId("..."),
  "code": "MONGO-CSHARP",
  "title": "MongoDB con C#",
  "category": "Database",
  "durationHours": 16,
  "isPublished": true,
  "tags": [
    "mongodb",
    "csharp"
  ],
  "createdAt": ISODate("..."),
  "updatedAt": ISODate("...")
}
```

---

# PARTE 4 - AVVIO E TEST

# 28. Avviare MongoDB

Verificare che MongoDB sia attivo.

Se utilizzate MongoDB Compass provate a collegarvi a:

```text
mongodb://localhost:27017
```

All'inizio `TrainingDb` potrebbe non essere presente.

È normale.

Avviando la prima `POST`, MongoDB creerà database e collection quando verranno effettivamente scritti i dati.

---

# 29. Impostare PL come progetto di avvio

In Visual Studio:

```text
MongoCourseManager.PL
 -> click destro
 -> Set as Startup Project
```

Avviare il progetto.

Dovrebbe aprirsi Swagger.

---

# 30. Test 1 - Creare il primo corso

Chiamare:

```http
POST /api/courses
```

Body:

```json
{
  "code": "CSHARP-BASE",
  "title": "C# Base",
  "category": "Development",
  "durationHours": 24,
  "isPublished": true,
  "tags": [
    "csharp",
    "dotnet"
  ]
}
```

Risposta attesa:

```text
201 Created
```

con un oggetto contenente anche `id`, `createdAt` e `updatedAt`.

---

# 31. Test 2 - Inserire altri corsi

Inserire almeno questi documenti attraverso le API.

## Corso MongoDB

```json
{
  "code": "MONGO-BASE",
  "title": "MongoDB Base",
  "category": "Database",
  "durationHours": 16,
  "isPublished": true,
  "tags": [
    "mongodb",
    "nosql"
  ]
}
```

## Corso Redis

```json
{
  "code": "REDIS-BASE",
  "title": "Redis Base",
  "category": "Database",
  "durationHours": 8,
  "isPublished": false,
  "tags": [
    "redis",
    "cache"
  ]
}
```

## Corso ASP.NET

```json
{
  "code": "ASPNET-API",
  "title": "ASP.NET Core Web API",
  "category": "Development",
  "durationHours": 20,
  "isPublished": true,
  "tags": [
    "dotnet",
    "api",
    "backend"
  ]
}
```

---

# 32. Test 3 - Leggere tutti i corsi

Chiamare:

```http
GET /api/courses
```

La risposta non è una semplice lista perché abbiamo implementato la paginazione.

Esempio:

```json
{
  "items": [
    ...
  ],
  "totalItems": 4,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

# 33. Test 4 - Ricerca per testo

Provare:

```http
GET /api/courses?text=mongo
```

La ricerca viene eseguita su:

```text
Title
Code
```

ed è case-insensitive perché nel Repository abbiamo usato:

```csharp
new BsonRegularExpression(text, "i")
```

La `i` significa:

```text
case insensitive
```

---

# 34. Test 5 - Ricerca per categoria

Provare:

```http
GET /api/courses?category=Database
```

Dovremmo ottenere i corsi MongoDB e Redis.

---

# 35. Test 6 - Combinare i filtri

Provare:

```http
GET /api/courses?category=Database&isPublished=true
```

Questa volta i filtri vengono combinati.

Nel Repository abbiamo:

```csharp
filterBuilder.And(filters)
```

Quindi la query richiede contemporaneamente:

```text
Category = Database
AND
IsPublished = true
```

---

# 36. Test 7 - Paginazione

Provare:

```http
GET /api/courses?page=1&pageSize=2
```

Poi:

```http
GET /api/courses?page=2&pageSize=2
```

Nel DAL la paginazione è realizzata con:

```csharp
.Skip((page - 1) * pageSize)
.Limit(pageSize)
```

Per pagina 1:

```text
Skip = 0
Limit = 2
```

Per pagina 2:

```text
Skip = 2
Limit = 2
```

---

# 37. Test 8 - Ordinamento

Per ordinamento crescente:

```http
GET /api/courses?sortDescending=false
```

Per ordinamento decrescente:

```http
GET /api/courses?sortDescending=true
```

Usiamo:

```csharp
SortBy(x => x.Title)
```

oppure:

```csharp
SortByDescending(x => x.Title)
```

---

# 38. Test 9 - Modificare un corso

Recuperare l'id del corso `MONGO-BASE`.

Eseguire:

```http
PUT /api/courses/{id}
```

Body:

```json
{
  "code": "MONGO-BASE",
  "title": "MongoDB Fundamentals",
  "category": "Database",
  "durationHours": 20,
  "isPublished": true,
  "tags": [
    "mongodb",
    "nosql",
    "database"
  ]
}
```

Nel Repository questo produce un aggiornamento basato su `$set`.

---

# 39. Test 10 - Pubblicare o nascondere un corso

Per pubblicare:

```http
PATCH /api/courses/{id}/publication?published=true
```

Per nascondere:

```http
PATCH /api/courses/{id}/publication?published=false
```

In questo caso non stiamo inviando nuovamente tutto il corso.

Aggiorniamo solamente:

```text
isPublished
updatedAt
```

Questa è una buona dimostrazione di aggiornamento parziale.

---

# 40. Test 11 - Aggiungere un tag

Chiamare:

```http
POST /api/courses/{id}/tags
```

Body JSON:

```json
"backend"
```

Il Repository utilizza:

```csharp
.AddToSet(x => x.Tags, tag)
```

Provare a chiamare due volte la stessa API con:

```json
"backend"
```

Verificare che MongoDB non crei due elementi uguali.

---

# 41. Test 12 - Rimuovere un tag

Chiamare:

```http
DELETE /api/courses/{id}/tags/backend
```

Il Repository utilizzerà:

```csharp
.Pull(x => x.Tags, tag)
```

Verificare con Compass che il tag sia stato rimosso dall'array.

---

# 42. Test 13 - Eliminare un corso

Chiamare:

```http
DELETE /api/courses/{id}
```

Risposta attesa:

```text
204 No Content
```

Richiamando:

```http
GET /api/courses/{id}
```

si dovrebbe ottenere:

```text
404 Not Found
```

---

# 43. Riepilogo degli endpoint

| Metodo | Endpoint | Operazione |
|---|---|---|
| GET | `/api/courses` | ricerca, filtri, sort, paginazione |
| GET | `/api/courses/{id}` | dettaglio |
| POST | `/api/courses` | creazione |
| PUT | `/api/courses/{id}` | aggiornamento |
| DELETE | `/api/courses/{id}` | eliminazione |
| PATCH | `/api/courses/{id}/publication?published=true` | modifica pubblicazione |
| POST | `/api/courses/{id}/tags` | aggiunta tag |
| DELETE | `/api/courses/{id}/tags/{tag}` | rimozione tag |

---

# 44. Quali operazioni MongoDB abbiamo utilizzato?

| Funzione applicativa | Driver C# | Concetto MongoDB |
|---|---|---|
| Leggere per id | `Find()` | find |
| Ricercare | `Find(filter)` | filtri |
| Creare | `InsertOneAsync()` | insertOne |
| Modificare | `UpdateOneAsync()` | updateOne |
| Aggiornare proprietà | `Set()` | `$set` |
| Aggiungere tag univoco | `AddToSet()` | `$addToSet` |
| Rimuovere tag | `Pull()` | `$pull` |
| Eliminare | `DeleteOneAsync()` | deleteOne |
| Contare | `CountDocumentsAsync()` | countDocuments |
| Ordinare | `SortBy()` | sort |
| Saltare record | `Skip()` | skip |
| Limitare record | `Limit()` | limit |
| Ricerca testuale semplice | `Regex()` | `$regex` |

---

# 45. Cosa abbiamo ottenuto rispetto a un CRUD minimale?

Un CRUD minimale avrebbe avuto soltanto:

```text
Create
Read
Update
Delete
```

Noi abbiamo aggiunto alcune funzionalità realistiche senza introdurre una complessità eccessiva:

```text
CRUD
+
filtri dinamici
+
paginazione
+
ordinamento
+
ricerca case-insensitive
+
aggiornamenti parziali
+
array MongoDB
+
regole di business
+
normalizzazione dati
+
AutoMapper
+
separazione DAL / BLL / PL
```

---

# 46. Domande di verifica

Prima di passare agli esercizi successivi, rispondere alle seguenti domande.

1. Perché `CourseEntity` si trova nel DAL e `CourseModel` nella BLL? Perchè l'entità fa riferimento al documentto (Parliamo di db non relazionali), mentre il model prende l'entità dal layer dal per applicarne la business logic
2. Perché il Controller non utilizza direttamente `MongoDbContext`? Perchè il controller ha la responsabilità di chiamare solo l'end point (divisione di responsabilità)
3. Che differenza c'è tra Repository e Service? Il repository è il livello da dove vengono presi i dati e accesso al db, mentre il service applica la business logic con le varie elaborazioni e controlli
4. Dove viene utilizzato AutoMapper? Nel CourseService per mappare la risposta e aggiungere anche i dati di Audit
5. A cosa serve `IMongoCollection<CourseEntity>`? Usato nel dbContext per indicare all'applicativo che l'entità corso è la collection del db
6. Che differenza c'è tra `$push` e `$addToSet`? in AddToSet in fase di inserimento applica un controllo che blocca i duplicati
7. Perché in questo esempio abbiamo scelto `$addToSet`? Perchè nelle prove dobbiamo provare ad inserire un duplicato
8. Cosa fa `$pull`? rimuove un tag
9. Perché la paginazione usa `Skip()` e `Limit()`? skip salta i primi n risultati in modo che salta i precedenti risultati mentre limit serve per appunto limitare una paginazione troppo grande
10. Perché `CountDocumentsAsync()` viene eseguito prima di applicare `Skip()` e `Limit()`? dobiamo contare la grandezza del documento prima di poterla paginare
11. Perché normalizziamo `Code` nella BLL e non nel Controller?
12. Perché `MongoDbContext` viene registrato Singleton? perchè ci deve essere soltanto un'istanza della sessione

---

# PARTE 5 - ESERCIZI SEMI-GUIDATI

# 47. Esercizio A - Filtro per durata minima

## Obiettivo

Aggiungere alla ricerca un parametro:

```text
minDurationHours
```

Esempio:

```http
GET /api/courses?minDurationHours=16
```

La query dovrà restituire solamente i corsi con durata maggiore o uguale a 16 ore.

## Suggerimento DAL

Il filtro MongoDB può essere costruito con:

```csharp
filterBuilder.Gte(
    x => x.DurationHours,
    minDurationHours.Value)
```

## Cosa modificare

Modificare:

```text
ICourseRepository
CourseRepository
ICourseService
CourseService
CoursesController
```

Non è necessario modificare Entity o AutoMapper.

---

# 48. Esercizio B - Filtro per tag

## Obiettivo

Aggiungere alla ricerca:

```text
tag
```

Esempio:

```http
GET /api/courses?tag=dotnet
```

Il risultato dovrà contenere solamente corsi che possiedono il tag richiesto.

## Suggerimento

Con un array di stringhe è possibile costruire un filtro sul campo `Tags`.

Provare a utilizzare:

```csharp
filterBuilder.AnyEq(x => x.Tags, tag)
```

Normalizzare il tag in lowercase prima di cercarlo.

Domanda:

```text
In quale livello dovrebbe essere normalizzato il valore?
```

Risposta attesa:

```text
BLL
```

perché è una decisione applicativa.

---

# 49. Esercizio C - Endpoint Publish e Unpublish separati

Attualmente abbiamo:

```http
PATCH /api/courses/{id}/publication?published=true
```

Realizzare invece anche:

```http
PATCH /api/courses/{id}/publish
```

E:

```http
PATCH /api/courses/{id}/unpublish
```

Entrambi possono richiamare lo stesso metodo del Service:

```csharp
SetPublishedAsync(...)
```

Scopo dell'esercizio: capire che più endpoint possono riutilizzare la stessa logica BLL.

---

# 50. Esercizio D - Controllo della dimensione massima dei tag

Imporre questa regola:

```text
un corso può avere al massimo 8 tag
```

La regola NON deve essere implementata nel Controller.

## Possibile strategia

Nel metodo:

```csharp
AddTagAsync()
```

prima recuperare il corso:

```csharp
var course = await _repository.GetByIdAsync(id);
```

Poi verificare:

```csharp
if (course is null)
    return false;

if (course.Tags.Count >= 8)
    throw new InvalidOperationException(
        "Un corso può avere al massimo 8 tag.");
```

Solo successivamente chiamare il Repository per aggiungere il tag.

---

# PARTE 6 - ESERCIZIO AUTONOMO

# 51. Mini-requisito finale

Implementare una nuova funzionalità:

```text
Duplicazione di un corso
```

Endpoint desiderato:

```http
POST /api/courses/{id}/duplicate
```

Il nuovo corso dovrà:

1. partire dai dati del corso originale;
2. avere un nuovo `_id` generato da MongoDB;
3. avere codice:

```text
CODICE-ORIGINALE-COPY
```

4. partire sempre come:

```text
IsPublished = false
```

5. mantenere categoria, durata e tag;
6. avere un titolo simile a:

```text
C# Base - Copia
```

7. avere nuovi valori `CreatedAt` e `UpdatedAt`.

## Vincoli

Non inserire logica MongoDB nel Controller.

Il Controller deve fare solamente qualcosa di simile a:

```csharp
var result = await _service.DuplicateAsync(id);
```

La BLL deve costruire il nuovo corso.

Il DAL deve solamente inserirlo.

## Domanda progettuale

Quale metodo Repository già esistente possiamo riutilizzare?

Risposta:

```csharp
CreateAsync(...)
```

Non è necessario creare un metodo MongoDB speciale per la duplicazione.

---

# PARTE 7 - BONUS LEGGERMENTE PIU' AVANZATO

# 52. Bonus - Statistiche per categoria

Questa parte è facoltativa.

Vogliamo ottenere una risposta simile a:

```json
[
  {
    "category": "Database",
    "courseCount": 2
  },
  {
    "category": "Development",
    "courseCount": 2
  }
]
```

Si può realizzare con una semplice aggregation MongoDB basata su `Group`.

Il metodo nel Repository potrebbe chiamarsi:

```csharp
GetCourseCountByCategoryAsync()
```

Questa attività serve solo come primo contatto con l'Aggregation Framework e non è richiesta per completare il laboratorio base.

---

# 53. Possibili errori e troubleshooting

## Errore: impossibile collegarsi a MongoDB

Controllare:

```text
mongodb://localhost:27017
```

Verificare che il servizio MongoDB sia avviato.

---

## Errore: database non visibile in Compass

Se non avete ancora inserito documenti è possibile che database e collection non siano ancora materializzati.

Eseguire prima una:

```http
POST /api/courses
```

---

## Errore: dependency injection non trova ICourseRepository

Controllare di avere in `Program.cs`:

```csharp
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
```

---

## Errore: dependency injection non trova ICourseService

Controllare:

```csharp
builder.Services.AddScoped<ICourseService, CourseService>();
```

---

## Errore relativo ad AutoMapper

Controllare:

1. package `AutoMapper` installato;
2. `MappingProfile` presente nel PL;
3. registrazione `AddAutoMapper` presente in `Program.cs`;
4. eventuale configurazione licenza prevista dalla versione adottata.

---

## Errore su ObjectId

MongoDB usa normalmente `_id` di tipo `ObjectId`.

Un ObjectId valido è simile a:

```text
68ecf212a3516dcff321c774
```

Nel Repository abbiamo utilizzato:

```csharp
ObjectId.TryParse(id, out _)
```

per evitare di inviare al driver un id malformato.

---

# 54. Miglioramenti che volutamente NON abbiamo introdotto

Per mantenere l'esercitazione comprensibile non abbiamo introdotto:

- autenticazione e autorizzazione;
- JWT;
- MediatR;
- CQRS;
- Unit of Work;
- transazioni MongoDB;
- FluentValidation;
- global exception handler;
- logging strutturato;
- Docker Compose;
- test unitari;
- indici MongoDB avanzati;
- caching;
- API versioning.

Sono tutte evoluzioni possibili, ma non sono necessarie per capire l'integrazione tra MongoDB e un'applicazione C# a livelli.

---

# 55. Nota importante sul codice corso univoco

Nel Service abbiamo controllato che non esista un altro documento con lo stesso `Code`.

Questa verifica è utile a livello didattico, ma in un'applicazione reale non garantisce da sola l'unicità in presenza di richieste concorrenti.

In produzione sarebbe opportuno creare anche un **unique index MongoDB** sul campo:

```text
code
```

Questa può essere una successiva esercitazione sugli indici.

---

# 56. Checklist finale

Alla fine dell'esercitazione ogni gruppo dovrebbe verificare di avere completato quanto segue.

- [ ] Solution .NET 10 creata
- [ ] Progetto DAL creato
- [ ] Progetto BLL creato
- [ ] Progetto PL Web API creato
- [ ] Riferimenti tra progetti configurati
- [ ] `MongoDB.Driver` installato
- [ ] AutoMapper configurato
- [ ] `MongoDbSettings` creato
- [ ] `MongoDbContext` creato
- [ ] `CourseEntity` creata
- [ ] `ICourseRepository` creato
- [ ] `CourseRepository` creato
- [ ] Model BLL creati
- [ ] `ICourseService` creato
- [ ] `CourseService` creato
- [ ] `MappingProfile` creato
- [ ] Dependency Injection configurata
- [ ] Controller creato
- [ ] POST funzionante
- [ ] GET by id funzionante
- [ ] ricerca funzionante
- [ ] filtri funzionanti
- [ ] paginazione funzionante
- [ ] PUT funzionante
- [ ] DELETE funzionante
- [ ] `$set` verificato
- [ ] `$addToSet` verificato
- [ ] `$pull` verificato
- [ ] dati visualizzati con MongoDB Compass

---

# 57. Obiettivo didattico finale

L'obiettivo non è ricordare a memoria tutti i metodi del driver MongoDB.

L'obiettivo è saper riconoscere questo flusso:

```text
HTTP Request
    |
    v
Controller
    |
    v
Service
    |
    v
AutoMapper / Business Logic
    |
    v
Repository
    |
    v
MongoDB Driver
    |
    v
MongoDB
```

E soprattutto comprendere la separazione delle responsabilità:

```text
PL
espone l'applicazione tramite API

BLL
gestisce Model e regole applicative

DAL
gestisce Entity e accesso a MongoDB
```

Quando questa separazione è chiara, diventa più semplice aggiungere nuove funzionalità senza trasformare Controller o Repository in classi troppo grandi e difficili da mantenere.

---

# 58. Riferimenti ufficiali

MongoDB - .NET/C# Driver:

```text
https://www.mongodb.com/docs/drivers/csharp/current/
```

MongoDB - MongoClient:

```text
https://www.mongodb.com/docs/drivers/csharp/current/connect/mongoclient/
```

MongoDB - Databases and Collections:

```text
https://www.mongodb.com/docs/drivers/csharp/current/databases-collections/
```

AutoMapper - Dependency Injection:

```text
https://docs.automapper.io/en/stable/Dependency-injection.html
```

AutoMapper - Licensing / configuration:

```text
https://docs.automapper.io/en/latest/License-configuration.html
```

---

**NTT DATA - Esercitazione .NET 10 + MongoDB**  
**Docente: Marco Storelli**
