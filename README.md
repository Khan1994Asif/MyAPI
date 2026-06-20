# WebAPI For Sql to Contact Object data sync : 
Created API project for inserting record using EF core in SQL and checking the Salesforce Object and by using REST API.
Below API fetches contact object fields:
URL : $"https://orgfarm-93b146fa03-dev-ed.develop.my.salesforce.com/services/data/v60.0/sobjects/Contact/describe"
We need access token to access the above URL by calling "https://login.salesforce.com/services/oauth2/authorize".
Postman provides the set of URLs to work with salesforce and get the accessToken

After getting all the fields from API response with the help of DeserializeObject, converting object into stongly typed class.
If any mismatch at field size of field is missing it will not allow the request to sync the SQL to SalesForce object.

# API Project Structure : 
Making use of Interface to seprate the repository and services and to achieve the abstraction.
Keeping my DB calls in Repository forlder. And Business logic to service folder.
Inside the model folder creating the creating neccessary Request & Response classes.
Making use of DTO to make sure data does not exposed to outside world.
Registering Repository & Services in Program.cs file.
With the help of DI container injecting services and accessing with constructor.
Adding cors policy to allow the frontend to communicate with backend.
Keeping my

# Future Enhancement : 
Impliment the clean architeture style to seprate the folder into class library.
we can impliment the Authentication mechanism as well to allow only authenticated user.
Generation of access token from auth service instead of calling the post man API.
Instead of inserting one by one record we can use bulk upload mechanism by passing data in excel.
Ratelimiting can be introduced.

# Salesforce Contact Synchronization Engine (.NET 8 Web API)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-purple.svg)](https://learn.microsoft.com/en-us/ef/core/)
[![Salesforce API](https://img.shields.io/badge/Salesforce%20REST%20API-v60.0-cloud.svg)](https://developer.salesforce.com/docs/)

A production-grade ASP.NET Core Web API built to facilitate reliable, resilient data synchronization between a local relational SQL database and the Salesforce `Contact` standard SObject. 

The core feature of this engine is a **Just-In-Time (JIT) Schema Drift Validation** pipeline that safeguards integration routines from failing silently due to dynamic changes (schema mutations) initiated by Salesforce Administrators.

---

## 🚀 Core Features & Implementation Details

### 1. Just-In-Time (JIT) Schema Sync Safeguard
To prevent integration pipelines from crashing with hard-to-debug `400 Bad Request` or unmapped structural payload errors, the system implements a runtime guardrail:
* **Dynamic Inspection:** Right before executing a sync payload, the API requests live object metadata from Salesforce’s native REST API SObject Describe layout endpoint (`/services/data/v60.0/sobjects/Contact/describe`).
* **Reflection-Driven Validation:** The backend uses C# Reflection to map the JSON schema payload arrays against local Entity Framework Core model definitions (`[StringLength]`, datatypes, and field mappings).
* **Drift Mitigation:** If a field size mismatch (e.g., field truncated in Salesforce) or a missing column dependency is discovered, the sync operation is cleanly blocked, the local record status is flagged with a mismatch exception, and a descriptive notification is bubbled up to the client application.

### 2. Architecture & Software Design Patterns
The backend strictly adheres to clean-coding conventions and decouple responsibilities across structured boundaries:
* **Repository Pattern:** Encapsulates raw database queries and mutations via Entity Framework Core inside a dedicated `Repositories/` layer, protecting the database context (`ApplicationDbContext`).
* **Service Layer Pattern:** Houses business workflows and validation mechanisms inside a `Services/` tier. Controllers never execute business calculations or DB transactions directly.
* **Abstract Factory Pattern:** Decouples API client footprints by separating the composition of raw `HttpRequestMessage` envelopes (headers, tokens, media types) from the integration services executing outbound calls.
* **Data Transfer Objects (DTOs):** Implements dedicated input request and output response models to shield interior domain structural schemas from leaking across network perimeter zones.
* **Dependency Injection (DI):** Explicitly utilizes constructor dependency injection via the built-in .NET IoC container to manage service lifecycles seamlessly.

---

## 🛠️ Technology Stack

* **Runtime Environment:** .NET 8 (ASP.NET Core Web API)
* **ORM:** Entity Framework Core 8 (Code-First)
* **Database Engine:** Microsoft SQL Server
* **External Integration:** Salesforce REST API (v60.0 SObject Describe & SObject Rows)
* **Authentication:** OAuth 2.0 (Bearer Token Protocol)
* **Testing & Prototyping:** Postman API client

---

## 📁 Project Structure

```text
📁 src/
├── 📁 Controllers/         # REST API Gateways and endpoints
├── 📁 Data/                # EF Core ApplicationDbContext and migration files
├── 📁 Models/              # Internal domain models & Entity definitions
│   └── 📁 DTOs/            # Request and Response contracts (Data Transfer Objects)
├── 📁 Repositories/        # Concrete and Interface definitions for Database IO
│   ├── IContactRepository.cs
│   └── ContactRepository.cs
├── 📁 Services/            # Business validation logic and Integration components
│   ├── IContactService.cs
│   ├── ContactService.cs
│   └── ISalesforceRequestFactory.cs
├── ⚙️ appsettings.json     # Configuration file (Salesforce endpoints & DB Strings)
└── 📜 Program.cs           # Application bootstrap, CORS policies, & DI Registrations
