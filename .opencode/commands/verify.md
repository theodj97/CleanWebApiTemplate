---
description: Run the repository gates and report real results
agent: build
---

Run the repository gates and report the real output of each one. Do not fix anything unless I ask.

```bash
dotnet build CleanWebApiTemplate.slnx
dotnet publish src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj -c Release -r linux-x64 --self-contained
dotnet test --solution CleanWebApiTemplate.slnx
npm run verify
npm run coverage
```

For each command: exit code, warnings, pass/fail counts and the coverage table. If Docker is unavailable,
say exactly which test projects could not run; never claim they passed. Do not hide warnings behind `tail`
or `grep`.
