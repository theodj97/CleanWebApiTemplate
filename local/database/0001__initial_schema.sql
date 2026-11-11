-- Initial schema for the local PostgreSQL database.
--
-- Applied on every AppHost start by CleanWebApiTemplate.DbInitializer (local/aspire), before the API starts.
-- The local database is ephemeral: the container and its data are removed when the AppHost stops.
--
-- Scripts are forward-only: never edit one that has been deployed. A change to an existing table goes in a
-- new, higher-numbered script.
--
-- Types match src/CleanWebApiTemplate.Infrastructure/Todo/TodoRepository.cs:
--   Id bytea (16-byte Ulid), text, timestamptz and integer (ETodoStatus).

CREATE TABLE IF NOT EXISTS "Todo" (
    "Id"          bytea       NOT NULL,
    "Title"       text        NOT NULL,
    "Description" text        NOT NULL DEFAULT '',
    "CreatedAt"   timestamptz NOT NULL,
    "UpdatedAt"   timestamptz NOT NULL,
    "Status"      integer     NOT NULL,
    "CreatedBy"   text        NOT NULL,
    "UpdatedBy"   text        NOT NULL,
    CONSTRAINT "PK_Todo" PRIMARY KEY ("Id")
);

-- TitleExistsAsync filters by title and owner. The unique index closes the race between the
-- application check and the insert; the repository maps it to a 409 Conflict.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Todo_CreatedBy_Title" ON "Todo" ("CreatedBy", "Title");

-- Title fragments are filtered with ILIKE; this index helps exact-title lookups.
CREATE INDEX IF NOT EXISTS "IX_Todo_Title" ON "Todo" ("Title");

-- SearchAsync filters by status, owner and creation date range.
CREATE INDEX IF NOT EXISTS "IX_Todo_Status" ON "Todo" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Todo_CreatedBy" ON "Todo" ("CreatedBy");
CREATE INDEX IF NOT EXISTS "IX_Todo_CreatedAt" ON "Todo" ("CreatedAt");
