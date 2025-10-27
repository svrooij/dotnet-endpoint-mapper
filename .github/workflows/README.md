# GitHub Actions Workflow Setup

This repository includes a CI/CD pipeline that automatically:

## Triggers
- **Push to main**: Runs linting and tests
- **Pull Request to main**: Runs linting and tests  
- **Tag creation** (format `vX.Y.Z`): Runs linting, tests, and publishes to NuGet

## Workflow Stages

### 1. Lint Stage
- Runs `dotnet format --verify-no-changes` to ensure code formatting consistency
- Must pass before proceeding to other stages

### 2. Test Stage
- Builds the solution with `dotnet build`
- Runs all tests with `dotnet test`
- Collects code coverage data
- Uploads test results as artifacts

### 3. Publish Stage (Tags only)
- Extracts version from the git tag (removes 'v' prefix)
- Packs the NuGet package with the tag version
- Publishes to NuGet.org
- Uploads packages as artifacts

## Setup Requirements

### NuGet API Key
To enable publishing to NuGet, you need to configure Nuget trusted publishing:

See [this blog-post](https://svrooij.io/2025/10/16/publish-nuget-token/) for detailed instructions, or check out (the official docs)[https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing]

### Usage
1. **For regular development**: Push to main or create PRs - linting and tests will run
2. **For releases**: Create and push a tag like `v1.0.0`:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

The workflow will automatically extract `1.0.0` as the package version and publish to NuGet.