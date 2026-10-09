# -*- coding: utf-8 -*-
"""调试：显示某文件里，哪些标识符触发了哪个 using"""
import os, re, sys
sys.path.insert(0, r"D:/Unity/UnderWordYoo")
import importlib.util
spec = importlib.util.spec_from_file_location("mig", r"D:/Unity/UnderWordYoo/_migrate_ns.py")
# 手动复用逻辑
ROOT = r"D:/Unity/UnderWordYoo/Assets/Scripts/HotUpdate"
DECL_RE = re.compile(r'^\s*(?:\[[^\]]*\]\s*)?(?:public|internal|abstract|sealed|static|partial|\s)*\b(?:class|interface|enum|struct)\s+(\w+)', re.M)
IDENT_RE = re.compile(r'\b[A-Za-z_]\w*\b')

def compute_ns(path):
    rel = os.path.relpath(path, ROOT).replace("\\", "/")
    return "HotUpdate." + ".".join(rel.split("/")[:-1])

files = []
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.endswith(".cs"):
            files.append(os.path.join(dp, f))

type_ns = {}
for p in files:
    ns = compute_ns(p)
    txt = open(p, encoding="utf-8-sig").read()
    for m in DECL_RE.finditer(txt):
        type_ns.setdefault(m.group(1), set()).add(ns)

for target in sys.argv[1:]:
    p = os.path.join(ROOT, target)
    ns = compute_ns(p)
    txt = open(p, encoding="utf-8-sig").read()
    idents = set(IDENT_RE.findall(txt))
    print("===", target, "->", ns)
    for t in sorted(idents):
        for tns in type_ns.get(t, ()):
            if tns != ns:
                print(f"   {t}  ->  {tns}")
