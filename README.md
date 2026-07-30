# InsightFlow.Nl2Sql

> **Plug-and-play Natural Language to SQL (NL2SQL) SDK for .NET** with built-in Column Masking, Row-Level Security abstractions, and AST Guardrails.

`InsightFlow.Nl2Sql` allows developers to add safe, production-grade text-to-SQL functionality to any ASP.NET Core API or .NET application in **one line of code**.

---

## Features

-  **Plug & Play Registration:** Single-line dependency injection setup for ASP.NET Core (`AddNl2SqlEngine`).
-  **Multi-Database Extractor Support:** Native schema extractors for both **SQLite** (`PRAGMA`) and **MySQL** (`INFORMATION_SCHEMA`).
-  **Column-Level Masking:** Automatically hide sensitive columns (e.g., `PasswordHash`, `CreditCardNumber`) from the LLM prompt based on the requesting user's security context.
-  **AST Security Guardrails:** 
  - Strictly enforces **`SELECT` / read-only** queries.
  - Blocks multi-statement SQL injection attacks (`;` delimiter blocking).
  - Automatically caps result limits (`LIMIT N`).
-  **Direct ADO.NET Execution:** Asynchronous query execution with enforced timeout guardrails returning JSON-friendly dictionary objects.
-  **Zero Unnecessary Bloat:** Clean, SOLID architecture designed for standalone NuGet packaging.

---

##  Installation

Install the package via .NET CLI:

```bash
dotnet add package InsightFlow.Nl2Sql
