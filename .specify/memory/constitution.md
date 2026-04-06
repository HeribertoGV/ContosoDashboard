<!--
Sync Impact Report:
- Version change: none → 1.0.0
- List of modified principles: All principles defined
- Added sections: Core Principles, Technology Stack, Development Workflow, Governance
- Removed sections: None
- Templates requiring updates: None
- Follow-up TODOs: None
-->
# ContosoDashboard Constitution

## Core Principles

### I. Security-First
All features must demonstrate security best practices, including authentication, authorization, data protection, and defense in depth. Mock authentication suitable for training only.

### II. Training-Oriented
Code and architecture designed for educational purposes, with clear examples, documentation, and simplified implementations to facilitate learning Spec-Driven Development.

### III. Mock Authentication (NON-NEGOTIABLE)
Use mock authentication system for offline training, avoiding external dependencies. No real passwords or external identity providers.

### IV. Data Isolation
Ensure user data isolation and IDOR protection in all services. Each user sees only their authorized data.

### V. Blazor Server Architecture
Implement using Blazor Server for real-time updates and server-side rendering, suitable for dashboard applications.

## Technology Stack

- .NET 8 with Blazor Server
- Entity Framework Core with SQLite for local database
- Cookie-based authentication with claims and roles
- Razor Pages for login/logout
- Custom authentication state provider

## Development Workflow

- Spec-Driven Development (SDD) using GitHub Spec Kit
- Phases: Constitution, Specification, Planning, Tasking, Implementation
- All changes must comply with this constitution
- Code reviews verify constitution compliance

## Governance

Constitution supersedes all other practices. Amendments require documentation and review. All PRs must verify compliance with principles. Use this constitution for guidance on development decisions.

**Version**: 1.0.0 | **Ratified**: 2026-04-05 | **Last Amended**: 2026-04-05
