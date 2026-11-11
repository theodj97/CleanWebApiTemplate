-- Projects: a second feature used as the skill's worked example.
--
-- Applied on every AppHost start by {App}.DbInitializer, after 0001__initial_schema.sql.
-- The local database is ephemeral: the container and its data are removed when the AppHost stops.
--
-- Types match src/{App}.Infrastructure/Repositories/Project/ProjectRepository.cs:
--   Id bytea (16-byte Ulid), texts, timestamptz and integer.

CREATE TABLE IF NOT EXISTS "Project" (
    "Id"          bytea       NOT NULL,
    "Name"        text        NOT NULL,
    "Description" text        NULL,
    "CreatedAt"   timestamptz NOT NULL,
    "UpdatedAt"   timestamptz NOT NULL,
    "Status"      integer     NOT NULL,
    "CreatedBy"   text        NOT NULL,
    "UpdatedBy"   text        NOT NULL,
    CONSTRAINT "PK_Project" PRIMARY KEY ("Id")
);

-- NameExistsAsync filters by name and owner. The unique index closes the race between
-- validation and insert; the API also checks it for a friendly 409.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Project_CreatedBy_Name" ON "Project" ("CreatedBy", "Name");

-- Name fragments are filtered with ILIKE; this index helps exact-name lookups.
CREATE INDEX IF NOT EXISTS "IX_Project_Name" ON "Project" ("Name");

-- SearchAsync filters by status, creator and creation date range.
CREATE INDEX IF NOT EXISTS "IX_Project_Status" ON "Project" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Project_CreatedBy" ON "Project" ("CreatedBy");
CREATE INDEX IF NOT EXISTS "IX_Project_CreatedAt" ON "Project" ("CreatedAt");
