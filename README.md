# HL7 to CCD

Standalone monorepo for converting `HL7v2` messages into pragmatic `CCD` XML.

The conversion pipeline is:

`HL7v2 -> vendored Microsoft FHIR-Converter templates -> FHIR Bundle JSON -> CCD XML`

## What is in this repo

- `src/Hl7ToCcda.Core`: reusable .NET library with the public conversion API.
- `src/Hl7ToCcda.Api`: ASP.NET Core API used by the website and other clients.
- `apps/web`: React + Vite upload UI.
- `tests/Hl7ToCcda.Core.Tests`: unit and golden-file tests for the conversion library.
- `tests/Hl7ToCcda.Api.Tests`: API integration tests.
- `vendor/fhir-converter`: vendored `FHIR-Converter` source/templates pinned to commit `7b0d02dab73e21afd75768ecde65575d03c5c333`, plus preserved `LICENSE` and `NOTICE`.

## Current scope

- Input: HL7v2 messages supported by the vendored upstream template set.
- Output: pragmatic CCD XML.
- CCD sections generated when data exists:
  - Problems
  - Allergies
  - Medications
  - Results
  - Procedures
  - Immunizations
  - Encounters
- The output is intentionally pragmatic. It is well-formed CCD-style XML, but it is not positioned as certification-grade C-CDA conformance.

## Prerequisites

- `.NET SDK 8.0.418`
- `Node 20+`
- `npm 10+`

If `dotnet` is not already on your PATH and you installed it with the official script:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

## Local development

Install dependencies:

```bash
npm install
dotnet restore Hl7ToCcda.sln
```

Run the API and web app together:

```bash
npm run dev
```

That starts:

- API: `http://localhost:5080`
- Web app: `http://localhost:5173`

If you want to run them separately:

```bash
dotnet run --project src/Hl7ToCcda.Api/Hl7ToCcda.Api.csproj --urls http://localhost:5080
```

```bash
cp apps/web/.env.example apps/web/.env
npm run dev --workspace web
```

## API

### Health check

```http
GET /api/health
```

### Convert to CCD

```http
POST /api/convert/ccd
Content-Type: multipart/form-data
```

Accepted form fields:

- `file`: uploaded HL7 file
- `message`: raw HL7 text
- `rootTemplateOverride`: optional upstream template override such as `ORU_R01`
- `documentTitle`: optional CCD title

Success response body:

```json
{
  "fileName": "ORU_R01-20260306123000.ccda.xml",
  "detectedRootTemplate": "ORU_R01",
  "warnings": [
    {
      "code": "MissingAuthor",
      "message": "No Practitioner resource was available for the CCD author."
    }
  ],
  "ccdaXml": "<ClinicalDocument ... />"
}
```

## Reusable library

Public API:

```csharp
public interface IHl7ToCcdaConverter
{
    Task<Hl7ToCcdaConversionResult> ConvertAsync(
        Hl7ToCcdaConversionRequest request,
        CancellationToken cancellationToken = default);
}
```

Register it with DI:

```csharp
using Hl7ToCcda.Core;
using Hl7ToCcda.Core.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging();
services.AddHl7ToCcdaConversion();

var provider = services.BuildServiceProvider();
var converter = provider.GetRequiredService<IHl7ToCcdaConverter>();

var result = await converter.ConvertAsync(
    new Hl7ToCcdaConversionRequest(
        Hl7Message: File.ReadAllText("sample.hl7"),
        DocumentTitle: "Patient CCD"));

Console.WriteLine(result.DetectedRootTemplate);
Console.WriteLine(result.CcdaXml);
```

## Package the library

Build the NuGet package:

```bash
dotnet pack src/Hl7ToCcda.Core/Hl7ToCcda.Core.csproj -o artifacts/packages
```

That produces a private package for `Hl7ToCcda.Core`.

## Use the library from another project

Add the local package source:

```bash
dotnet nuget add source /absolute/path/to/hl7_to_ccda/artifacts/packages --name hl7-to-ccda-local
```

Add the package:

```bash
dotnet add package Hl7ToCcda.Core --source /absolute/path/to/hl7_to_ccda/artifacts/packages
```

Then use the same `AddHl7ToCcdaConversion()` registration shown above.

## Verification

Run the .NET tests:

```bash
dotnet test Hl7ToCcda.sln
```

Run the frontend tests:

```bash
npm run test:web
```

Build the frontend:

```bash
npm run build:web
```

## Third-party attribution

This repo vendors portions of [microsoft/FHIR-Converter](https://github.com/microsoft/FHIR-Converter) under the MIT license.

- Upstream commit: `7b0d02dab73e21afd75768ecde65575d03c5c333`
- Preserved files:
  - `vendor/fhir-converter/LICENSE`
  - `vendor/fhir-converter/NOTICE`

