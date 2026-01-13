# Pre-processor/bootstraping (READ THIS FIRST)
Always look for an .agents folder at the project root when starting a new session. It should always have at least two (or more) folders:
.agents/docs
.agents/input

The docs folder will contain important information about the project outline, project analysis, and coding standards. In general you should always reference the coding standards.

The input folder will be used for specific prompts and referenced by "the input" folder. For example, I may put screenshots in the input folder and have you reference them. 

When starting a new session if the .agents folder structure does not exist, create it with empty folders.

NOTE: sometimes a project will be a parent of many smaller projects who also have their own .agents folders. Please be mindful of this as you will want to check those folders as well.

# Global Development Standards

## Environment
- Windows 11 / PowerShell preferred over bash
- Visual Studio 2022/2026, Cursor IDE, VSCode
- Windows Terminal with Oh-My-Posh

## Tech Stack Defaults
When not otherwise specified:
- Backend: .NET 8+ / C#
- Frontend: React (Javascript)
- Mobile: React Native
- Database: SQL Server
- Cloud: Azure, some AWS
- CI/CD: Azure DevOps
- Git: Bitbucket or GitHub

## Git Conventions
- Conventional commits: feat:, fix:, chore:, docs:, refactor:
- Squash merge to master/main
- Branch naming: feature/, bugfix/, hotfix/

## Testing
- xUnit for .NET
- Jest + React Testing Library for frontend
- Arrange-Act-Assert pattern
- Test file naming: *.Tests.cs, *.test.tsx

## What NOT to do
- Don't generate XML doc comments unless asked
- Only add comments for complex operations
- Don't refactor unrelated code
- PowerShell examples, not bash (unless Linux context)