# Yautbox integration benchmarks

These tests run the complete hosted-service processing path, including storage reads and deletes.
PostgreSQL scenarios create an isolated schema, seed a realistic backlog with `generate_series`,
then start 1, 3, or 5 independent hosts (pods) against that schema. InMemory scenarios use the same
handler and runner settings, but stay in one process because in-memory state cannot be shared by pods.

Run a smaller local smoke benchmark:

```bash
YAUTBOX_POSTGRES_MESSAGES=10000 \
YAUTBOX_INMEMORY_MESSAGES=10000 \
YAUTBOX_BENCHMARK_REPORT=benchmark-results/summary.md \
dotnet test benchmarks/Yautbox.Benchmarks/Yautbox.Benchmarks.csproj \
  --configuration Release --filter "Category=Benchmark"
```

The defaults are 2,000,000 PostgreSQL messages and 250,000 InMemory messages. The report is printed
as Markdown and, when running in GitHub Actions, is also added to the job summary and uploaded as an
artifact. Override `YAUTBOX_POSTGRES` when PostgreSQL is not available with the default local test
connection string.
