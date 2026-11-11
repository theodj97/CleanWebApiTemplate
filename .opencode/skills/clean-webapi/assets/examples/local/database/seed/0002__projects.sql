-- Synthetic seed for the Project feature. Local mode only (the AppHost passes Seed__Path); the test modes
-- start from an empty schema.
--
-- CreatedBy matches the email claim of the devtoken profiles and local/openapi/users.json, so every profile
-- owns rows and the examples return meaningful results.

INSERT INTO "Project" ("Id", "Name", "Description", "CreatedAt", "UpdatedAt", "Status", "CreatedBy", "UpdatedBy")
VALUES
    (decode('018f3b1c2d0000020000000000000001', 'hex'),
     'Local developer portal',
     'Draft project owned by the local user.',
     now() - interval '2 days', now() - interval '2 days', 1,
     'local.user@example.com', 'local.user@example.com'),
    (decode('018f3b1c2d0000020000000000000002', 'hex'),
     'Billing migration',
     'Active project owned by the local operator.',
     now() - interval '1 day', now() - interval '1 day', 2,
     'local.operator@example.com', 'local.operator@example.com'),
    (decode('018f3b1c2d0000020000000000000003', 'hex'),
     'Release automation',
     'Completed project owned by the local admin.',
     now() - interval '6 hours', now() - interval '6 hours', 3,
     'local.admin@example.com', 'local.admin@example.com'),
    (decode('018f3b1c2d0000020000000000000004', 'hex'),
     'Legacy reporting',
     'Archived project owned by the local operator.',
     now() - interval '3 hours', now() - interval '3 hours', 4,
     'local.operator@example.com', 'local.operator@example.com')
ON CONFLICT ("Id") DO NOTHING;
