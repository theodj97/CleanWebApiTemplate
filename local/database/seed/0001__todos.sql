-- Synthetic seed data for local development only (never production data).
-- Applied by CleanWebApiTemplate.DbInitializer after the schema, only when the AppHost passes Seed__Path.

INSERT INTO "Todo" ("Id", "Title", "Description", "CreatedAt", "UpdatedAt", "Status", "CreatedBy", "UpdatedBy")
VALUES
    (decode('018f3b1c2d0000010000000000000001', 'hex'),
     'Set up the local environment',
     'Started with Aspire, PostgreSQL and locally signed JWTs.',
     now() - interval '2 days', now() - interval '2 days', 1,
     'seed@example.com', 'seed@example.com'),
    (decode('018f3b1c2d0000010000000000000002', 'hex'),
     'Review the first pull request',
     'Pending review by the team.',
     now() - interval '1 day', now() - interval '1 day', 2,
     'seed@example.com', 'seed@example.com'),
    (decode('018f3b1c2d0000010000000000000003', 'hex'),
     'Publish the release notes',
     'Already published.',
     now() - interval '6 hours', now() - interval '6 hours', 3,
     'seed@example.com', 'seed@example.com')
ON CONFLICT ("Id") DO NOTHING;
