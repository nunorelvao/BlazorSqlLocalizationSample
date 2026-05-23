-- Idempotent SQLite-compatible initialization for LocalizationResources

CREATE TABLE IF NOT EXISTS LocalizationResources (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ResourceKey TEXT NOT NULL,
    Culture TEXT NOT NULL,
    ResourceValue TEXT NOT NULL,
    UpdatedAt DATETIME NOT NULL DEFAULT (datetime('now')),
    UNIQUE(ResourceKey, Culture)
);

-- Upsert rows: insert new or update existing values
INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Welcome', 'en', 'Welcome', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Welcome' AND Culture = 'en');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Welcome', 'pt', 'Bem-vindo', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Welcome' AND Culture = 'pt');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Save', 'en', 'Save', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Save' AND Culture = 'en');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Save', 'pt', 'Guardar', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Save' AND Culture = 'pt');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Logout', 'en', 'Logout', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Logout' AND Culture = 'en');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Logout', 'pt', 'Terminar Sessão', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Logout' AND Culture = 'pt');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Login', 'en', 'Login', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Login' AND Culture = 'en');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Login', 'pt', 'Entrar', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Login' AND Culture = 'pt');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Dashboard', 'en', 'Dashboard', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Dashboard' AND Culture = 'en');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Dashboard', 'pt', 'Painel', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Dashboard' AND Culture = 'pt');

--- Title_Test
INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Title_Test', 'pt', 'Teste', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Title_Test' AND Culture = 'pt');

INSERT INTO LocalizationResources (ResourceKey, Culture, ResourceValue, UpdatedAt)
SELECT 'Title_Test', 'en', 'Test', datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM LocalizationResources WHERE ResourceKey = 'Title_Test' AND Culture = 'en');
