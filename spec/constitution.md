<!-- Sync Impact Report
Version change: none → 1.0.0
Modified principles: none
Added sections: header (version, ratification date, last amended date)
Removed sections: none
Templates requiring updates: none
Follow-up TODOs: Ratification Date
-->

# Project Constitution

Version: 1.0.0

Ratification Date: TODO

Last Amended Date: 2025-10-18

## 1) Mission & Scope

Build a **full-stack Blazor (.NET 8)** application that **simulates, visualizes, and compares blockchain consensus mechanisms** (PoW, PoS, DPoS, PoA, PBFT, PoET, PoB) with optional **supply-chain** and **federated-learning** payload modes. The system must be **pluggable**, **testable**, and **demo-ready**.

Non-goals:

* No production cryptocurrency or real financial transactions.
* No real SGX—PoET uses a software attestation mock.

## 2) Roles

* **Owner (PO)**: approves scope, milestones, releases.
* **Tech Lead**: architecture decisions; merges to `main`.
* **Contributors**: feature branches + PRs.
* **QA**: maintains test plans, CI status.
* **Security Champion**: threat modeling, dependency scanning.

## 3) Branching & Releases

* Default: `main` (protected).
* Feature: `feat/<short>`; Fix: `fix/<short>`; Docs: `docs/<short>`.
* Release tags: `v<major>.<minor>.<patch>`.
* PR rules: 1 approver + green CI + no high-severity vulnerabilities.

## 4) Standards

* Language: C# 12, .NET 8, Blazor Server (default) + class library for core engine.
* Style: `.editorconfig` + `dotnet format`.
* Commits (Conventional): `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
* Tests: xUnit + 80% line coverage in `Consensus.Core`.
* API: JSON over HTTP; versioned routes `/api/v1/...`.

## 5) Security & Privacy

* Identity: ASP.NET Core Identity + Roles (Admin, Operator, Viewer).
* Never store private keys or user secrets in repo.
* OWASP ASVS L1 baseline; rate-limit POST actions.
* All metrics and sample data are synthetic.

## 6) Decision Log (ADR)

* ADRs live under `/docs/adrs/NNN-title.md`.
* Breaking changes require an ADR + PO approval.

## 7) Definition of Done (per feature)

* Code + unit tests + Playwright UI test (if user-facing).
* Swagger updated, EF migrations generated, seed updated.
* Docs updated (`/docs/` + help text in UI).
* CI green; feature flag added if risky.
