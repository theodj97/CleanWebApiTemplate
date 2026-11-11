-- Local-only synthetic seed for the Project feature. Applied only in Local mode.
-- Emails match local/tools/devtoken.json and local/openapi/users.json.
-- One row per status, so the filter and the Swagger examples have something to show.

INSERT INTO "Project" ("Id", "Name", "Description", "CreatedAt", "UpdatedAt", "Status", "CreatedBy", "UpdatedBy")
VALUES
    (decode('018f3b1c2d0000010000000000000001', 'hex'),
     'Release 1.0',
     'Ship the first public release.',
     '2026-10-20T09:00:00Z',
     '2026-10-24T16:30:00Z',
     2, -- Active
     'local.user@example.com',
     'local.user@example.com'),

    (decode('018f3b1c2d0000010000000000000002', 'hex'),
     'Write the runbook',
     'Document the local environment and the deployment steps.',
     '2026-10-21T10:15:00Z',
     '2026-10-21T10:15:00Z',
     1, -- Draft
     'local.user@example.com',
     'local.user@example.com'),

    (decode('018f3b1c2d0000010000000000000003', 'hex'),
     'Migrate the database',
     'Apply the new schema to every environment.',
     '2026-10-18T08:00:00Z',
     '2026-10-25T12:00:00Z',
     3, -- Completed
     'local.operator@example.com',
     'local.operator@example.com'),

    (decode('018f3b1c2d0000010000000000000004', 'hex'),
     'Quarterly audit',
     'Archive the findings and the remediation plan.',
     '2026-10-01T07:45:00Z',
     '2026-10-26T09:30:00Z',
     4, -- Archived
     'local.admin@example.com',
     'local.admin@example.com');
