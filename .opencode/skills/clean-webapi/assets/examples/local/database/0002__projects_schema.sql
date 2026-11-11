-- Project schema. Forward-only: never edit this file after it has been deployed; a change to the table goes
-- in a new, higher-numbered script.
--
-- Applied on every AppHost start by CleanWebApiTemplate.DbInitializer before the API starts.
-- Types match src/CleanWebApiTemplate.Infrastructure/Project/ProjectRepository.cs:
--   Id bytea (16-byte Ulid), text, timestamptz and integer (EProjectStatus).

CREATE TABLE IF NOT EXISTS "Project" (
    "Id"          bytea       NOT NULL,
    "Name"        text        NOT NULL,
    "Description" text        NOT NULL DEFAULT '',
    "CreatedAt"   timestamptz NOT NULL,
    "UpdatedAt"   timestamptz NOT NULL,
    "Status"      integer     NOT NULL,
    "CreatedBy"   text        NOT NULL,
    "UpdatedBy"   text        NOT NULL,
    CONSTRAINT "PK_Project" PRIMARY KEY ("Id")
);

-- NameExistsAsync filters by name and owner. The unique index closes the race between the application
-- check and the insert; the repository maps it to a 409 Conflict.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Project_CreatedBy_Name" ON "Project" ("CreatedBy", "Name");

CREATE INDEX IF NOT EXISTS "IX_Project_Name" ON "Project" ("Name");
CREATE INDEX IF NOT EXISTS "IX_Project_Status" ON "Project" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Project_CreatedBy" ON "Project" ("CreatedBy");
CREATE INDEX IF NOT EXISTS "IX_Project_CreatedAt" ON "Project" ("CreatedAt");
