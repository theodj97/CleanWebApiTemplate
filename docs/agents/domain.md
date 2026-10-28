# Domain Docs

Cómo deben consumir las engineering skills la documentación de dominio de este repositorio al
explorar el código.

## Antes de explorar, lee esto

- **`GLOSSARY.md`** en la raíz del repositorio, o
- **`GLOSSARY-MAP.md`** en la raíz si existe: apunta a un `GLOSSARY.md` por contexto. Lee cada uno
  de los relevantes para el tema.
- **`docs/adr/`**: lee los ADRs que toquen el área en la que vas a trabajar. En repos
  multi-contexto, revisa también `src/<context>/docs/adr/` por si hay decisiones acotadas al
  contexto.

Si alguno de estos ficheros no existe, **continúa en silencio**. No señales su ausencia ni sugieras
crearlos por adelantado. La skill `/domain-modeling` (a la que se llega vía `/grill-with-docs` y
`/improve-codebase-architecture`) los crea de forma perezosa cuando de verdad se resuelven términos
o decisiones.

## Estructura de ficheros

Repo single-context (la mayoría de repos):

```
/
├── GLOSSARY.md
├── docs/adr/
│   ├── 0001-event-sourced-orders.md
│   └── 0002-postgres-for-write-model.md
└── src/
```

Repo multi-contexto (presencia de `GLOSSARY-MAP.md` en la raíz):

```
/
├── GLOSSARY-MAP.md
├── docs/adr/                          ← decisiones de todo el sistema
└── src/
    ├── ordering/
    │   ├── GLOSSARY.md
    │   └── docs/adr/                  ← decisiones específicas del contexto
    └── billing/
        ├── GLOSSARY.md
        └── docs/adr/
```

## Usa el vocabulario del glosario

Cuando tu output nombre un concepto de dominio (en el título de una incidencia, una propuesta de
refactor, una hipótesis, el nombre de un test), usa el término tal y como se define en
`GLOSSARY.md`. No derives a sinónimos que el glosario evite explícitamente.

Si el concepto que necesitas aún no está en el glosario, eso es una señal: o estás inventando
lenguaje que el proyecto no usa (reconsidéralo) o hay un hueco real (anótalo para
`/domain-modeling`).

## Señala conflictos con ADRs

Si tu output contradice un ADR existente, sácalo explícitamente en vez de sobrescribirlo en
silencio:

> _Contradice el ADR-0007 (event-sourced orders), pero merece la pena reabrirlo porque…_
