# Lists request params and JSON body fields that are still empty in a generated Bruno (OpenCollection YAML) collection.
# Run by the agent from the skill folder; not copied into the repository. Needs Python 3 + PyYAML.
#   python3 <skill>/assets/bruno/find-empty-values.py <Solution>Collection     (exit 1 when something is empty)
# Only the "http" section of each request is checked: response "examples" are ignored.
import json, pathlib, sys, yaml
def empties(node, path):
    if isinstance(node, dict):
        for k, v in node.items(): yield from empties(v, f"{path}.{k}")
    elif isinstance(node, list):
        for i, v in enumerate(node): yield from empties(v, f"{path}[{i}]")
    elif node == "" or node is None:
        yield path
found = 0
for f in sorted(pathlib.Path(sys.argv[1]).rglob("*.yml")):
    doc = yaml.safe_load(f.read_text(encoding="utf-8")) or {}
    http = doc.get("http")
    if not isinstance(http, dict):
        continue
    for p in http.get("params") or []:
        if p.get("value") in ("", None) and not p.get("disabled"):
            print(f"{f.name}: param '{p['name']}' is empty"); found += 1
    body = http.get("body") or {}
    if body.get("type") == "json" and body.get("data"):
        for path in empties(json.loads(body["data"]), "body"):
            print(f"{f.name}: {path} is empty"); found += 1
print(f"{found} empty value(s)"); sys.exit(1 if found else 0)
