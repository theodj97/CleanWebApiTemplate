// Unique title per run: the API enforces a unique title and the created row survives until `aspire stop`.
bru.setVar('newTodoTitle', `Bruno smoke test ${Date.now()}`);
